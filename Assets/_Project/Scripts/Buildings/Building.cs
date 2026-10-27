using PrimeTween;
using SporeTribal.Villagers;
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
            if (owner == null) return;
            VillagerUnit attacker = source != null ? source.GetComponent<VillagerUnit>() : null;
            owner.ReportAttack(transform.position, attacker != null ? attacker.Home : null);
            if (attacker != null && owner.IsHostileTo(attacker.Home)) VillagerUnit.CallForHelp(owner, transform.position, attacker);
        }

        protected virtual void Collapse()
        {
            float drop = GetComponentInChildren<Collider>() is Collider c ? c.bounds.size.y * 0.4f : 1f;
            Tween.Position(transform, transform.position + Vector3.down * drop, 1.2f, Ease.Linear);
            Tween.Rotation(transform, transform.rotation * Quaternion.Euler(6f, 0f, 4f), 1.2f, Ease.Linear);
        }
    }
}
