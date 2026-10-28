using System.Collections.Generic;
using SporeTribal.Economy;
using SporeTribal.Orders;
using SporeTribal.Utilities;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.AI
{
    [System.Serializable]
    public class PerformanceStage
    {
        private const float AudienceArc = 180f;

        [Tooltip("Metres between the chieftain and the audience centre.")]
        [SerializeField] private float stageDistance = 5f;
        [Tooltip("How far out the crowd comes to meet the band, as a fraction of their village's wander radius, so they never leave home.")]
        [SerializeField, Range(0f, 1f)] private float crowdReach = 0.8f;
        [Tooltip("The crowd notices the chieftain this many metres beyond the meeting point and walks out to meet the band.")]
        [SerializeField] private float noticeDistance = 8f;
        [SerializeField] private float bandRadius = 2.2f;
        [SerializeField] private float bandArc = 120f;
        [SerializeField] private float audienceRadius = 3f;
        [Tooltip("Minimum metres between neighbours on the band and audience arcs. The arc radius grows to keep it as the crowd gets bigger.")]
        [SerializeField] private float slotSpacing = 1.6f;

        private Vector3 toBand;

        public Vector3 AudienceCentre { get; private set; }
        public Vector3 Point { get; private set; }

        public void Set(Village target, Vector3 from)
        {
            Vector3 centre = target.DropOffCentre;
            Vector3 offset = (from - centre).Flat();
            float distance = offset.magnitude;
            toBand = distance < 0.1f ? Vector3.back : offset / distance;
            float reach = Mathf.Clamp(distance - stageDistance, 0f, target.WanderRadius * crowdReach);
            AudienceCentre = NavMeshUtility.Snap(centre + toBand * reach);
            Point = AudienceCentre + toBand * stageDistance;
        }

        public bool IsNoticedFrom(Village target, Vector3 position)
        {
            float notice = target.WanderRadius * crowdReach + stageDistance + noticeDistance;
            return position.IsWithinFlat(target.DropOffCentre, notice);
        }

        public void PlaceBand(IReadOnlyList<VillagerUnit> band, Village target)
        {
            int followers = band.CountFollowers();
            int placed = 0;
            foreach (VillagerUnit unit in band)
            {
                Vector3 slot = unit.IsChieftain ? Point : ArcSlot(Point, bandRadius, bandArc, placed++, followers);
                unit.Accept(TribalOrder.Perform(target, NavMeshUtility.Snap(slot), AudienceCentre));
            }
        }

        public void PlaceAudience(IReadOnlyList<VillagerUnit> audience)
        {
            for (int i = 0; i < audience.Count; i++)
            {
                Vector3 slot = ArcSlot(AudienceCentre, audienceRadius, AudienceArc, i, audience.Count);
                audience[i].Accept(TribalOrder.Watch(NavMeshUtility.Snap(slot), Point));
            }
        }

        public float SqrDistanceToAudience(Vector3 position) => (position - AudienceCentre).sqrMagnitude;

        private Vector3 ArcSlot(Vector3 centre, float radius, float arc, int index, int count)
        {
            float t = count < 2 ? 0.5f : (float)index / (count - 1);
            if (count > 1) radius = Mathf.Max(radius, slotSpacing * (count - 1) / (arc * Mathf.Deg2Rad));
            float angle = Mathf.Lerp(-arc * 0.5f, arc * 0.5f, t);
            return centre + Quaternion.AngleAxis(angle, Vector3.up) * toBand * radius;
        }
    }
}
