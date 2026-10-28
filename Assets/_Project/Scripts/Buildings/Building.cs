using System.Collections.Generic;
using PrimeTween;
using SporeTribal.Combat;
using SporeTribal.Economy;
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

        protected virtual void Awake()
        {
            Spots = new StandingSpots(transform);
            Health = GetComponent<Health>();
            Health.Damaged += OnDamaged;
            Health.Died += Collapse;
        }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        private void OnDamaged(GameObject source)
        {
            if (owner == null) return;
            VillagerUnit attacker = source != null && source.TryGetComponent(out VillagerUnit unit) ? unit : null;
            owner.ReportAttack(transform.position, attacker);
        }

        protected virtual void Collapse()
        {
            float drop = GetComponentInChildren<Collider>() is Collider c ? c.bounds.size.y * 0.4f : 1f;
            Tween.Position(transform, transform.position + Vector3.down * drop, 1.2f, Ease.Linear);
            Tween.Rotation(transform, transform.rotation * Quaternion.Euler(6f, 0f, 4f), 1.2f, Ease.Linear);
        }
    }
}
