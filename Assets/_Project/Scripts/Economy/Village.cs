using UnityEngine;

namespace SporeTribal.Economy
{
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

        public bool IsHostileTo(Village other) => other != null && other != this && !HasSurrendered && !other.HasSurrendered;

        private readonly System.Collections.Generic.Dictionary<Village, int> opinions = new System.Collections.Generic.Dictionary<Village, int>();

        public int RelationshipTo(Village other) => other != null && opinions.TryGetValue(other, out int value) ? value : 0;

        public void Offend(Village by, int amount)
        {
            if (by == null || by == this) return;
            opinions[by] = Mathf.Clamp(RelationshipTo(by) - amount, -100, 100);
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
