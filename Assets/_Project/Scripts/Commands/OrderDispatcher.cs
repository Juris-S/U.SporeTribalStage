using System.Collections.Generic;
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

        public event System.Action<IReadOnlyList<VillagerUnit>, Vector3[], int[]> OrderIssued;

        public OrderBatch Issue(IReadOnlyList<VillagerUnit> units, ClickTarget target)
        {
            var batch = new OrderBatch();
            var movers = new List<VillagerUnit>();

            foreach (VillagerUnit unit in units)
            {
                OrderResult result = OrderResolver.Resolve(unit, target);
                batch.Entries.Add(new OrderBatch.Entry(unit, result));
                if (!result.Accepted) continue;

                if (result.Order.Value.Kind == OrderKind.Move) movers.Add(unit);
                else unit.Accept(result.Order.Value);
            }

            if (movers.Count > 0) MoveInFormation(movers, target.Point);
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
            for (int i = 0; i < movers.Count; i++)
                movers[i].Accept(TribalOrder.Move(slots[assignment[i]]));

            OrderIssued?.Invoke(movers, slots, assignment);
        }
    }
}
