using System.Collections.Generic;
using SporeTribal.Utilities;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.Commands
{
    public static class SlotAssignment
    {
        public static int[] Match(IReadOnlyList<VillagerUnit> units, Vector3[] slots)
        {
            int n = units.Count;
            var assignment = new int[n];
            for (int i = 0; i < n; i++) assignment[i] = i;

            bool improved = true;
            while (improved)
            {
                improved = false;
                for (int a = 0; a < n; a++)
                {
                    for (int b = a + 1; b < n; b++)
                    {
                        Vector3 pa = units[a].transform.position, pb = units[b].transform.position;
                        float current = pa.FlatDistance(slots[assignment[a]]) + pb.FlatDistance(slots[assignment[b]]);
                        float swapped = pa.FlatDistance(slots[assignment[b]]) + pb.FlatDistance(slots[assignment[a]]);
                        if (swapped < current - 0.001f)
                        {
                            (assignment[a], assignment[b]) = (assignment[b], assignment[a]);
                            improved = true;
                        }
                    }
                }
            }

            return assignment;
        }
    }
}
