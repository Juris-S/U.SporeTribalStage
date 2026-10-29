using System;
using System.Collections.Generic;
using SporeTribal.Definitions;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.Economy
{
    public enum RelationshipTier { AtWar, Unfriendly, Neutral, Friendly, Allied }

    public class Village : MonoBehaviour
    {
        public const int MinRelationship = -100;
        public const int MaxRelationship = 100;
        public const int AtWarThreshold = -31;
        public const int FriendlyThreshold = 30;
        public const int AllianceThreshold = 70;
        public const int LargeGiftFood = 100;

        [Tooltip("Distance from the village centre where villagers stand to hand over food. Keep it outside the building footprint.")]
        [SerializeField] private float dropOffRadius = 6f;
        [Tooltip("Where food is delivered and villagers idle (the market). Falls back to this object if empty.")]
        [SerializeField] private Transform dropOffPoint;
        [Tooltip("How far from the drop-off point idle villagers wander.")]
        [SerializeField] private float wanderRadius = 10f;
        [Tooltip("Tints this tribe's villagers so the two sides read apart on screen.")]
        [SerializeField] private Color tribeColour = new Color(0.95f, 0.62f, 0.18f);

        [Header("Relationships")]
        [Tooltip("How much a single hit on this tribe sours its opinion of the attacker's tribe.")]
        [SerializeField, Min(0)] private int offencePerHit = 10;

        [Header("Diplomacy")]
        [Tooltip("Relationship gained per 50 food gifted.")]
        [SerializeField, Min(0)] private int smallGiftBoost = 20;
        [Tooltip("Relationship gained for a gift of 100 food or more.")]
        [SerializeField, Min(0)] private int largeGiftBoost = 30;
        [SerializeField, Min(0)] private float soothingSongSeconds = 60f;
        [Tooltip("Instruments this tribe asks for during a recital. The first tribe only knows the didgeridoo; later tribes add more. A recital never asks for an instrument the band does not hold.")]
        [SerializeField] private InstrumentKind[] calledInstruments = { InstrumentKind.Didgeridoo };

        private readonly HashSet<Village> allies = new HashSet<Village>();
        private readonly Dictionary<Village, int> opinions = new Dictionary<Village, int>();
        private float soothedUntil;

        public event Action<Vector3> Attacked;
        public event Action<Village> Surrendered;
        public event Action<Village, Village> AllianceDeclared;
        public event Action<Village, int> OffendedOther;
        public event Action<Village, int> BefriendedOther;

        public float WanderRadius => wanderRadius;
        public float DropOffRadius => dropOffRadius;
        public Vector3 DropOffCentre => (dropOffPoint != null ? dropOffPoint : transform).position;
        public StandingSpots Spots { get; private set; }
        public int Food { get; private set; }
        public Color TribeColour => tribeColour;
        public IReadOnlyList<InstrumentKind> CalledInstruments => calledInstruments;
        public bool HasSurrendered { get; private set; }
        public bool IsSoothed => Time.time < soothedUntil;
        public float SoothingSongSeconds => soothingSongSeconds;

        private void Awake() => Spots = new StandingSpots(dropOffPoint != null ? dropOffPoint : transform);

        public bool IsAlliedWith(Village other) => other != null && allies.Contains(other);

        public bool IsHostileTo(Village other) =>
            other != null && other != this && !HasSurrendered && !other.HasSurrendered && !IsAlliedWith(other) && !IsSoothed && !other.IsSoothed;

        public int RelationshipTo(Village other) => other != null && opinions.TryGetValue(other, out int value) ? value : 0;

        public RelationshipTier TierTowards(Village other)
        {
            if (IsAlliedWith(other)) return RelationshipTier.Allied;
            int value = RelationshipTo(other);
            if (value <= AtWarThreshold) return RelationshipTier.AtWar;
            if (value < 0) return RelationshipTier.Unfriendly;
            if (value < FriendlyThreshold) return RelationshipTier.Neutral;
            return value < AllianceThreshold ? RelationshipTier.Friendly : RelationshipTier.Allied;
        }

        public void SetOpening(Village towards, int value)
        {
            if (towards != null && towards != this) opinions[towards] = ClampRelationship(value);
        }

        public void Soothe() => soothedUntil = Time.time + soothingSongSeconds;

        public void Offend(Village by, int amount)
        {
            if (by == null || by == this) return;
            opinions[by] = ClampRelationship(RelationshipTo(by) - amount);
            by.OffendedOther?.Invoke(this, amount);
        }

        public void Befriend(Village by, int amount)
        {
            if (by == null || by == this) return;
            opinions[by] = ClampRelationship(RelationshipTo(by) + amount);
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
            int boost = amount >= LargeGiftFood ? largeGiftBoost : smallGiftBoost;
            to.Befriend(this, boost);
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

        public void ReportAttack(Vector3 where, VillagerUnit attacker = null)
        {
            Village attackerTribe = attacker != null ? attacker.Home : null;
            Offend(attackerTribe, offencePerHit);
            Attacked?.Invoke(where);
            if (attacker != null && IsHostileTo(attackerTribe)) VillagerUnit.CallForHelp(this, where, attacker);
        }

        public void Surrender()
        {
            if (HasSurrendered) return;
            HasSurrendered = true;
            Surrendered?.Invoke(this);
        }

        private static int ClampRelationship(int value) => Mathf.Clamp(value, MinRelationship, MaxRelationship);
    }
}
