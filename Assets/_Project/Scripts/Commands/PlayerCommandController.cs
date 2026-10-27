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

            ClickTarget target;
            if (ScreenRaycast.TryRaycast(screenPosition, unitMask, out RaycastHit unitHit) && unitHit.collider.GetComponentInParent<VillagerUnit>() is VillagerUnit unit)
            {
                Issue(ClickTarget.Villager(unit));
                return;
            }

            bool onResource = ScreenRaycast.TryRaycast(screenPosition, resourceMask, out RaycastHit resourceHit);
            if (onResource && resourceHit.collider.GetComponentInParent<FoodNode>() is FoodNode node)
            {
                target = ClickTarget.Resource(node, resourceHit.point);
            }
            else if (onResource && resourceHit.collider.GetComponentInParent<Building>() is Building building)
            {
                target = ClickTarget.AtBuilding(building, resourceHit.point);
            }
            else if (onResource) return;
            else if (ScreenRaycast.TryRaycast(screenPosition, groundMask, out RaycastHit hit))
            {
                target = ClickTarget.Ground(hit.point);
                if (clickPing != null) clickPing.Play(hit.point);
            }
            else return;

            Issue(target);
        }

        private void Issue(ClickTarget target)
        {
            OrderBatch batch = dispatcher.Issue(selection.Selected, target);
            if (batch.AllRefusedReason() is RefusalReason reason) Debug.Log($"[Order] All refused: {reason.Describe()}");
            else if (batch.GroupRefusal != RefusalReason.None) Debug.Log($"[Order] Performance refused: {batch.GroupRefusal.Describe()}");
        }
    }
}
