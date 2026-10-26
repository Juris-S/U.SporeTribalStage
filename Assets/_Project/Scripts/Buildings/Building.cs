using System.Collections;
using System.Collections.Generic;
using SporeTribal.Combat;
using SporeTribal.Economy;
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
            if (owner != null) owner.ReportAttack(transform.position);
        }

        protected virtual void Collapse() => StartCoroutine(Sink());

        private IEnumerator Sink()
        {
            Vector3 start = transform.position;
            Quaternion from = transform.rotation;
            Quaternion to = from * Quaternion.Euler(6f, 0f, 4f);
            float drop = GetComponentInChildren<Collider>() is Collider c ? c.bounds.size.y * 0.4f : 1f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f)
            {
                transform.SetPositionAndRotation(start + Vector3.down * (drop * t), Quaternion.Slerp(from, to, t));
                yield return null;
            }
        }
    }
}
