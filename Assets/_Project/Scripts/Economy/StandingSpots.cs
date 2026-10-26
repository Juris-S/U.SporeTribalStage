using SporeTribal.Villagers;
using UnityEngine;
using UnityEngine.AI;

namespace SporeTribal.Economy
{
    public class StandingSpots
    {
        private const int Count = 8;
        private static readonly NavMeshPath path = new NavMeshPath();
        private readonly Transform centre;
        private readonly VillagerUnit[] taken = new VillagerUnit[Count];

        private readonly int capacity;

        public StandingSpots(Transform centre, int capacity = Count)
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
            int best = -1;
            float bestCost = float.MaxValue;
            for (int i = 0; i < Count; i++)
            {
                if (taken[i] != null) continue;
                float cost = PathCost(from, SpotPosition(i, distance));
                if (cost < bestCost) { bestCost = cost; best = i; }
            }
            if (best < 0) best = Closest(from, distance);
            taken[best] = who;
            return SpotPosition(best, distance);
        }

        private Vector3 SpotPosition(int index, float distance)
        {
            float a = index * Mathf.PI * 2f / Count;
            Vector3 p = centre.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * distance;
            return NavMesh.SamplePosition(p, out NavMeshHit hit, 2f, NavMesh.AllAreas) ? hit.position : p;
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
