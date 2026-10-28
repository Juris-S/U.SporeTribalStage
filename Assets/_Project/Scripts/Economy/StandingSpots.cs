using SporeTribal.Villagers;
using UnityEngine;
using UnityEngine.AI;

namespace SporeTribal.Economy
{
    public class StandingSpots
    {
        private const int PerRing = 8;
        private const int Count = PerRing * 2;
        private const float RingStep = 1.5f;
        private const float MinGap = 1.2f;
        private static readonly NavMeshPath path = new NavMeshPath();
        private readonly Transform centre;
        private readonly VillagerUnit[] taken = new VillagerUnit[Count];

        private readonly int capacity;

        public StandingSpots(Transform centre, int capacity = PerRing)
        {
            this.centre = centre;
            this.capacity = Mathf.Clamp(capacity, 1, Count);
        }

        public bool HasFree
        {
            get
            {
                int used = 0;
                foreach (VillagerUnit v in taken) if (v != null) used++;
                return used < capacity;
            }
        }

        public Vector3 Claim(VillagerUnit who, Vector3 from, float distance)
        {
            Release(who);
            int best = Cheapest(who, from, distance, keepGap: true);
            if (best < 0) best = Cheapest(who, from, distance, keepGap: false);
            if (best < 0) best = Closest(from, distance);
            taken[best] = who;
            return SpotPosition(best, distance);
        }

        private int Cheapest(VillagerUnit who, Vector3 from, float distance, bool keepGap)
        {
            for (int ring = 0; ring < Count / PerRing; ring++)
            {
                int best = -1;
                float bestCost = float.MaxValue;
                for (int i = ring * PerRing; i < (ring + 1) * PerRing; i++)
                {
                    if (taken[i] != null) continue;
                    Vector3 spot = SpotPosition(i, distance);
                    if (keepGap && (NearTaken(spot, distance) || VillagerUnit.IsCrowded(spot, MinGap, who))) continue;
                    float cost = PathCost(from, spot);
                    if (cost < bestCost) { bestCost = cost; best = i; }
                }
                if (best >= 0) return best;
            }
            return -1;
        }

        private Vector3 SpotPosition(int index, float distance)
        {
            int ring = index / PerRing;
            float a = (index % PerRing + ring * 0.5f) * Mathf.PI * 2f / PerRing;
            Vector3 p = centre.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (distance + ring * RingStep);
            return NavMesh.SamplePosition(p, out NavMeshHit hit, 2f, NavMesh.AllAreas) ? hit.position : p;
        }

        private bool NearTaken(Vector3 spot, float distance)
        {
            for (int i = 0; i < Count; i++)
                if (taken[i] != null && (SpotPosition(i, distance) - spot).sqrMagnitude < MinGap * MinGap) return true;
            return false;
        }

        private static float PathCost(Vector3 from, Vector3 to)
        {
            if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) return float.MaxValue;
            float length = 0f;
            Vector3[] corners = path.corners;
            for (int i = 1; i < corners.Length; i++) length += Vector3.Distance(corners[i - 1], corners[i]);
            return length;
        }

        private int Closest(Vector3 from, float distance)
        {
            int best = 0;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < Count; i++)
            {
                float sqr = (SpotPosition(i, distance) - from).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = i; }
            }
            return best;
        }

        public void Release(VillagerUnit who)
        {
            for (int i = 0; i < Count; i++) if (taken[i] == who) taken[i] = null;
        }
    }
}
