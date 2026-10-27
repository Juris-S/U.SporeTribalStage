using System.Collections.Generic;
using SporeTribal.AI;
using SporeTribal.Economy;
using SporeTribal.Orders;
using SporeTribal.Villagers;
using UnityEngine;
using UnityEngine.AI;

namespace SporeTribal.Commands
{
    public class OrderDispatcher : MonoBehaviour
    {
        [SerializeField] private float slotSpacing = 1.5f;
        [SerializeField] private float navMeshSampleRadius = 3f;
        [Tooltip("Receives the band when a click on a rival resolves to a performance. Leave empty on tribes that never perform.")]
        [SerializeField] private SocialPerformanceDirector performances;

        public event System.Action<IReadOnlyList<VillagerUnit>, Vector3[], int[]> OrderIssued;

        public OrderBatch Issue(IReadOnlyList<VillagerUnit> units, ClickTarget target)
        {
            var batch = new OrderBatch();
            var movers = new List<VillagerUnit>();
            var performers = new List<VillagerUnit>();
            Village audience = null;

            foreach (VillagerUnit unit in units)
            {
                OrderResult result = OrderResolver.Resolve(unit, target);
                batch.Entries.Add(new OrderBatch.Entry(unit, result));
                if (!result.Accepted) continue;

                if (result.Order.Value.Kind == OrderKind.Perform)
                {
                    if (performances != null && unit.Home == performances.Village)
                    {
                        performers.Add(unit);
                        audience = result.Order.Value.Audience;
                    }
                    else movers.Add(unit);
                }
                else if (result.Order.Value.Kind == OrderKind.Move) movers.Add(unit);
                else unit.Accept(result.Order.Value);
            }

            if (movers.Count > 0) MoveInFormation(movers, target.Point);
            if (performers.Count > 0) batch.GroupRefusal = performances.TryBegin(performers, audience);
            return batch;
        }

        private void MoveInFormation(List<VillagerUnit> movers, Vector3 point)
        {
            Vector3[] slots = CircleFormation.GetSlots(point, movers.Count, slotSpacing);

            for (int i = 0; i < slots.Length; i++)
            {
                if (NavMesh.SamplePosition(slots[i], out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                    slots[i] = hit.position;
            }

            int[] assignment = SlotAssignment.Match(movers, slots);
            PutChieftainAtHead(movers, slots, assignment, point);
            for (int i = 0; i < movers.Count; i++)
                movers[i].Accept(TribalOrder.Move(slots[assignment[i]]));

            OrderIssued?.Invoke(movers, slots, assignment);
        }

        private static void PutChieftainAtHead(List<VillagerUnit> movers, Vector3[] slots, int[] assignment, Vector3 point)
        {
            int chief = movers.FindIndex(u => u.IsChieftain);
            if (chief < 0 || movers.Count < 2) return;

            Vector3 centroid = Vector3.zero;
            foreach (VillagerUnit unit in movers) centroid += unit.transform.position;
            Vector3 heading = point - centroid / movers.Count;
            heading.y = 0f;
            if (heading.sqrMagnitude < 0.01f) return;

            int head = 0;
            for (int i = 1; i < slots.Length; i++)
                if (Vector3.Dot(slots[i] - point, heading) > Vector3.Dot(slots[head] - point, heading)) head = i;

            int holder = System.Array.IndexOf(assignment, head);
            (assignment[chief], assignment[holder]) = (assignment[holder], assignment[chief]);
        }
    }
}
