using System.Collections.Generic;
using SporeTribal.Villagers;
using UnityEngine;
using UnityEngine.AI;

namespace SporeTribal.Commands
{
    public class MoveOrderDispatcher : MonoBehaviour
    {
        [SerializeField] private float slotSpacing = 1.5f;
        [SerializeField] private float navMeshSampleRadius = 3f;

        public event System.Action<IReadOnlyList<VillagerUnit>, Vector3[], int[]> OrderIssued;

        public void MoveTo(IReadOnlyList<VillagerUnit> units, Vector3 point)
        {
            Vector3[] slots = CircleFormation.GetSlots(point, units.Count, slotSpacing);

            for (int i = 0; i < slots.Length; i++)
            {
                if (NavMesh.SamplePosition(slots[i], out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                    slots[i] = hit.position;
            }

            int[] assignment = SlotAssignment.Match(units, slots);
            for (int i = 0; i < units.Count; i++)
                units[i].Accept(TribalOrder.Move(slots[assignment[i]]));

            OrderIssued?.Invoke(units, slots, assignment);
        }
    }
}
