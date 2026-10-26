using System.Collections.Generic;
using UnityEngine;

namespace SporeTribal.Economy
{
    public class ToolBuilding : MonoBehaviour
    {
        private static readonly List<ToolBuilding> all = new List<ToolBuilding>();
        public static IReadOnlyList<ToolBuilding> All => all;

        [SerializeField] private ToolDefinition tool;
        [Tooltip("Distance from the building centre where villagers stand to collect the tool. Keep it outside the footprint.")]
        [SerializeField] private float standRadius = 5f;

        public ToolDefinition Tool => tool;
        public float StandRadius => standRadius;
        public StandingSpots Spots { get; private set; }

        private void Awake() => Spots = new StandingSpots(transform);
        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);
    }
}
