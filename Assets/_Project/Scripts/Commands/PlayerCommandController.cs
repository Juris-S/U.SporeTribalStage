using SporeTribal.Economy;
using SporeTribal.Feedback;
using SporeTribal.Inputs;
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
        [SerializeField] private MoveOrderDispatcher dispatcher;
        [SerializeField] private ClickPing clickPing;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private LayerMask resourceMask;

        private void OnEnable() => input.CommandClicked += HandleCommand;
        private void OnDisable() => input.CommandClicked -= HandleCommand;

        private void HandleCommand(Vector2 screenPosition)
        {
            if (selection.Selected.Count == 0) return;

            if (ScreenRaycast.TryRaycast(screenPosition, resourceMask, out RaycastHit resourceHit))
            {
                FoodNode node = resourceHit.collider.GetComponentInParent<FoodNode>();
                if (node != null)
                {
                    foreach (VillagerUnit unit in selection.Selected) unit.Accept(TribalOrder.Gather(node));
                    return;
                }
            }

            if (!ScreenRaycast.TryRaycast(screenPosition, groundMask, out RaycastHit hit)) return;

            dispatcher.MoveTo(selection.Selected, hit.point);
            if (clickPing != null) clickPing.Play(hit.point);
        }
    }
}
