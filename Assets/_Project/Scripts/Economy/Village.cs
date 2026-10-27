using SporeTribal.Definitions;
using UnityEngine;

namespace SporeTribal.Economy
{
    public enum RelationshipTier { AtWar, Unfriendly, Neutral, Friendly, Allied }

    public class Village : MonoBehaviour
    {
        [Tooltip("Distance from the village centre where villagers stand to hand over food. Keep it outside the building footprint.")]
        [SerializeField] private float dropOffRadius = 6f;

        [Tooltip("Where food is delivered and villagers idle (the market). Falls back to this object if empty.")]
        [SerializeField] private Transform dropOffPoint;

        [Tooltip("How far from the drop-off point idle villagers wander.")]
        [SerializeField] private float wanderRadius = 10f;

        public float WanderRadius => wanderRadius;
        public float DropOffRadius => dropOffRadius;
        public Vector3 DropOffCentre => (dropOffPoint != null ? dropOffPoint : transform).position;
        public StandingSpots Spots { get; private set; }
        public int Food { get; private set; }

        [Header("Relationships")]
        [Tooltip("How much a single hit on this tribe sours its opinion of the attacker's tribe.")]
        [SerializeField, Min(0)] private int offencePerHit = 10;

        public bool HasSurrendered { get; private set; }

        public event System.Action<Vector3> Attacked;
        public event System.Action<Village> Surrendered;

        private void Awake() => Spots = new StandingSpots(dropOffPoint != null ? dropOffPoint : transform);

        [Tooltip("Tints this tribe's villagers so the two sides read apart on screen.")]
        [SerializeField] private Color tribeColour = new Color(0.95f, 0.62f, 0.18f);

        public Color TribeColour => tribeColour;

        public const int AllianceThreshold = 70;

        [Header("Diplomacy")]
        [Tooltip("Relationship gained per 50 food gifted.")]
        [SerializeField, Min(0)] private int smallGiftBoost = 20;
        [Tooltip("Relationship gained for a gift of 100 food or more.")]
        [SerializeField, Min(0)] private int largeGiftBoost = 30;
        [SerializeField, Min(0)] private float soothingSongSeconds = 60f;
        [Tooltip("Instruments this tribe asks for during a recital. The first tribe only knows the didgeridoo; later tribes add more. A recital never asks for an instrument the band does not hold.")]
        [SerializeField] private InstrumentKind[] calledInstruments = { InstrumentKind.Didgeridoo };

        public System.Collections.Generic.IReadOnlyList<InstrumentKind> CalledInstruments => calledInstruments;

        private readonly System.Collections.Generic.HashSet<Village> allies = new System.Collections.Generic.HashSet<Village>();
        private float soothedUntil;

        public bool IsSoothed => Time.time < soothedUntil;
        public float SoothingSongSeconds => soothingSongSeconds;
        public bool IsAlliedWith(Village other) => other != null && allies.Contains(other);

        public event System.Action<Village, Village> AllianceDeclared;

        public bool IsHostileTo(Village other) =>
            other != null && other != this && !HasSurrendered && !other.HasSurrendered && !IsAlliedWith(other) && !IsSoothed && !other.IsSoothed;

        public RelationshipTier TierTowards(Village other)
        {
            if (IsAlliedWith(other)) return RelationshipTier.Allied;
            int value = RelationshipTo(other);
            if (value <= -31) return RelationshipTier.AtWar;
            if (value < 0) return RelationshipTier.Unfriendly;
            if (value < 30) return RelationshipTier.Neutral;
            return value < AllianceThreshold ? RelationshipTier.Friendly : RelationshipTier.Allied;
        }

        public void Soothe() => soothedUntil = Time.time + soothingSongSeconds;

        private readonly System.Collections.Generic.Dictionary<Village, int> opinions = new System.Collections.Generic.Dictionary<Village, int>();

        public int RelationshipTo(Village other) => other != null && opinions.TryGetValue(other, out int value) ? value : 0;

        public event System.Action<Village, int> OffendedOther;
        public event System.Action<Village, int> BefriendedOther;

        public void Offend(Village by, int amount)
        {
            if (by == null || by == this) return;
            opinions[by] = Mathf.Clamp(RelationshipTo(by) - amount, -100, 100);
            by.OffendedOther?.Invoke(this, amount);
        }

        public void Befriend(Village by, int amount)
        {
            if (by == null || by == this) return;
            opinions[by] = Mathf.Clamp(RelationshipTo(by) + amount, -100, 100);
            by.BefriendedOther?.Invoke(this, amount);
            if (RelationshipTo(by) >= AllianceThreshold) DeclareAlliance(by);
        }

        public void DeclareAlliance(Village with)
        {
            if (with == null || with == this || IsAlliedWith(with)) return;
            allies.Add(with);
            with.allies.Add(this);
            AllianceDeclared?.Invoke(this, with);
            with.AllianceDeclared?.Invoke(with, this);
        }

        public bool GiftFood(Village to, int amount)
        {
            if (to == null || to == this || amount <= 0 || !TrySpend(amount)) return false;
            to.Deposit(amount);
            to.Befriend(this, amount >= 100 ? largeGiftBoost : smallGiftBoost);
            return true;
        }

        public void Deposit(int amount) => Food += amount;

        public bool TrySpend(int amount)
        {
            if (amount > Food) return false;
            Food -= amount;
            return true;
        }

        public int Withdraw(int amount)
        {
            int taken = Mathf.Clamp(amount, 0, Food);
            Food -= taken;
            return taken;
        }

        public void ReportAttack(Vector3 where, Village attacker = null)
        {
            Offend(attacker, offencePerHit);
            Attacked?.Invoke(where);
        }

        public void Surrender()
        {
            if (HasSurrendered) return;
            HasSurrendered = true;
            Surrendered?.Invoke(this);
        }
    }
}
