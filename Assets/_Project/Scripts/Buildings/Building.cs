using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using SporeTribal.Combat;
using SporeTribal.Economy;
using SporeTribal.Feedback;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.Buildings
{
    [RequireComponent(typeof(Health))]
    public abstract class Building : MonoBehaviour
    {
        private static readonly List<Building> all = new List<Building>();
        public static IReadOnlyList<Building> All => all;

        [Tooltip("The tribe that owns this building.")]
        [SerializeField] private Village owner;
        [Tooltip("Distance from the building centre where villagers stand to use or attack it. Keep it outside the footprint.")]
        [SerializeField] private float standRadius = 5f;

        [Header("Hit feedback")]
        [SerializeField] private VfxId impactEffect = VfxId.BuildingHit;
        [Tooltip("Scale punch applied to the whole building on each hit.")]
        [SerializeField] private Vector3 hitPunch = new Vector3(0.03f, -0.04f, 0.03f);
        [SerializeField, Min(0.05f)] private float hitPunchSeconds = 0.2f;

        [Tooltip("Metres the attack ring extends beyond the building footprint.")]
        [SerializeField, Min(0f)] private float attackRingMargin = 1.5f;

        private const float AttackRingScanSeconds = 0.25f;
        private const float AttackRingLift = 0.06f;
        private static readonly int FillColorId = Shader.PropertyToID("_FillColor");

        [Header("Collapse")]
        [SerializeField] private VfxId collapseEffect = VfxId.Death;
        [Tooltip("Seconds the building takes to sink out of sight.")]
        [SerializeField, Min(0.2f)] private float sinkSeconds = 2.5f;
        [Tooltip("Seconds between dust puffs while it sinks.")]
        [SerializeField, Min(0.05f)] private float puffInterval = 0.25f;
        [Tooltip("Metres sunk below the building height, so no sliver is left above ground.")]
        [SerializeField, Min(0f)] private float sinkExtraDepth = 1f;

        private Tween hitTween;
        private Collider mainCollider;
        private MeshRenderer attackRing;
        private MaterialPropertyBlock ringBlock;
        private float nextRingScan;

        public Village Owner => owner;
        public float StandRadius => standRadius;
        public StandingSpots Spots { get; private set; }
        public Health Health { get; private set; }
        public bool IsStanding => Health.IsAlive;

        public static Building NearestStanding(Village owner, Vector3 from, bool includeMainHut)
        {
            Building nearest = null;
            float nearestSqr = float.MaxValue;
            foreach (Building building in all)
            {
                if (building.Owner != owner || !building.IsStanding) continue;
                if (!includeMainHut && building is MainHut) continue;
                float sqr = (building.transform.position - from).sqrMagnitude;
                if (sqr >= nearestSqr) continue;
                nearestSqr = sqr;
                nearest = building;
            }
            return nearest;
        }

        public Bounds Bounds => mainCollider != null ? mainCollider.bounds : new Bounds(transform.position, Vector3.one);

        protected virtual void Awake()
        {
            mainCollider = GetComponentInChildren<Collider>();
            Spots = new StandingSpots(transform);
            Health = GetComponent<Health>();
            Health.Damaged += OnDamaged;
            Health.Died += Collapse;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRingScan) return;
            nextRingScan = Time.unscaledTime + AttackRingScanSeconds;
            SetAttackRing(IsStanding && IsBeingRazed());
        }

        private bool IsBeingRazed()
        {
            foreach (VillagerUnit unit in VillagerUnit.All)
                if (unit.IsAlive && unit.RazeTarget == this) return true;
            return false;
        }

        private void SetAttackRing(bool visible)
        {
            if (attackRing == null)
            {
                if (!visible || VillagerUnit.MarkerMaterial == null) return;
                attackRing = GroundRing.Create(name + " Attack Ring", VillagerUnit.MarkerMaterial, VillagerUnit.AttackRimColour);
                ringBlock = new MaterialPropertyBlock();
                ringBlock.SetColor(FillColorId, VillagerUnit.AttackFillColour);
                ringBlock.SetColor("_RimColor", VillagerUnit.AttackRimColour);
                attackRing.SetPropertyBlock(ringBlock);
            }
            if (visible && !attackRing.enabled) PlaceAttackRing();
            attackRing.enabled = visible;
        }

        private void PlaceAttackRing()
        {
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            foreach (Collider body in GetComponentsInChildren<Collider>()) bounds.Encapsulate(body.bounds);
            float diameter = (Mathf.Max(bounds.extents.x, bounds.extents.z) + attackRingMargin) * 2f;
            attackRing.transform.position = new Vector3(bounds.center.x, bounds.min.y + AttackRingLift, bounds.center.z);
            attackRing.transform.localScale = new Vector3(diameter, diameter, 1f);
        }

        private void OnDestroy()
        {
            if (attackRing != null) Destroy(attackRing.gameObject);
        }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        private void OnDamaged(GameObject source)
        {
            PlayImpact(source);
            if (owner == null) return;
            VillagerUnit attacker = source != null && source.TryGetComponent(out VillagerUnit unit) ? unit : null;
            owner.ReportAttack(transform.position, attacker);
        }

        private void PlayImpact(GameObject source)
        {
            if (!IsStanding || hitTween.isAlive) return;
            Vector3 point = transform.position + Vector3.up;
            if (mainCollider != null && source != null)
            {
                Vector3 from = source.transform.position + Vector3.up;
                point = mainCollider.ClosestPoint(from);
            }
            VfxPlayer.Play(impactEffect, point);
            hitTween = Tween.PunchScale(transform, hitPunch, hitPunchSeconds, frequency: 6f);
        }

        protected virtual void Collapse()
        {
            if (hitTween.isAlive) hitTween.Complete();
            if (attackRing != null) attackRing.enabled = false;
            StartCoroutine(SinkRoutine());
        }

        private IEnumerator SinkRoutine()
        {
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            foreach (Collider body in GetComponentsInChildren<Collider>()) bounds.Encapsulate(body.bounds);
            float drop = bounds.size.y + sinkExtraDepth;
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.8f;

            Tween.Position(transform, transform.position + Vector3.down * drop, sinkSeconds, Ease.InQuad, useUnscaledTime: true);
            Tween.Rotation(transform, transform.rotation * Quaternion.Euler(6f, 0f, 4f), sinkSeconds, Ease.Linear, useUnscaledTime: true);

            for (float t = 0f; t < sinkSeconds; t += puffInterval)
            {
                Vector2 around = Random.insideUnitCircle * radius;
                VfxPlayer.Play(collapseEffect, new Vector3(bounds.center.x + around.x, bounds.min.y + 0.3f, bounds.center.z + around.y));
                yield return new WaitForSecondsRealtime(puffInterval);
            }
            yield return new WaitForSecondsRealtime(0.2f);
            gameObject.SetActive(false);
        }
    }
}
