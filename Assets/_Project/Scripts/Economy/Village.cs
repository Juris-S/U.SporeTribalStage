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

        private void Awake() => Spots = new StandingSpots(dropOffPoint != null ? dropOffPoint : transform);

        public void Deposit(int amount) => Food += amount;

        public bool TrySpend(int amount)
        {
            if (amount > Food) return false;
            Food -= amount;
            return true;
        }
    }
}
