using SporeTribal.Buildings;
using SporeTribal.Economy;
using SporeTribal.Feedback;
using SporeTribal.Inputs;
using SporeTribal.Orders;
using SporeTribal.Selection;
using SporeTribal.Utilities;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.Commands
{
    public class PlayerCommandController : MonoBehaviour
    {
        [SerializeField] private RTSInputController input;
        [SerializeField] private SelectionController selection;
        [SerializeField] private OrderDispatcher dispatcher;
        [SerializeField] private ClickPing clickPing;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private LayerMask resourceMask;
        [SerializeField] private LayerMask unitMask;

        private void OnEnable() => input.CommandClicked += HandleCommand;
        private void OnDisable() => input.CommandClicked -= HandleCommand;

        private void HandleCommand(Vector2 screenPosition)
        {
            if (selection.Selected.Count == 0) return;
            if (TryResolveTarget(screenPosition, out ClickTarget target)) Issue(target);
        }

        private bool TryResolveTarget(Vector2 screenPosition, out ClickTarget target)
        {
            if (ScreenRaycast.TryPick(screenPosition, unitMask, out VillagerUnit unit))
            {
                target = ClickTarget.Villager(unit);
                return true;
            }

            if (ScreenRaycast.TryRaycast(screenPosition, resourceMask, out RaycastHit resourceHit))
                return TryResolveResource(resourceHit, out target);

            if (ScreenRaycast.TryRaycast(screenPosition, groundMask, out RaycastHit groundHit))
            {
                target = ClickTarget.Ground(groundHit.point);
                if (clickPing != null) clickPing.Play(groundHit.point);
                return true;
            }

            target = default;
            return false;
        }

        private static bool TryResolveResource(RaycastHit hit, out ClickTarget target)
        {
            if (hit.collider.GetComponentInParent<FoodNode>() is FoodNode node)
            {
                target = ClickTarget.Resource(node, hit.point);
                return true;
            }
            if (hit.collider.GetComponentInParent<Building>() is Building building)
            {
                target = ClickTarget.AtBuilding(building, hit.point);
                return true;
            }
            target = default;
            return false;
        }

        private void Issue(ClickTarget target)
        {
            OrderBatch batch = dispatcher.Issue(selection.Selected, target);
            if (batch.AllRefusedReason() is RefusalReason reason)
            {
                Debug.Log($"[Order] All refused: {reason.Describe()}");
                return;
            }
            if (batch.GroupRefusal != RefusalReason.None)
                Debug.Log($"[Order] Performance refused: {batch.GroupRefusal.Describe()}");
        }
    }
}
