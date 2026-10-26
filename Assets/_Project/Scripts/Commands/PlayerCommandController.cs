using SporeTribal.Feedback;
using SporeTribal.Inputs;
using SporeTribal.Selection;
using SporeTribal.Utilities;
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

        private void OnEnable() => input.CommandClicked += HandleCommand;
        private void OnDisable() => input.CommandClicked -= HandleCommand;

        private void HandleCommand(Vector2 screenPosition)
        {
            if (selection.Selected.Count == 0) return;

            if (!ScreenRaycast.TryRaycast(screenPosition, groundMask, out RaycastHit hit)) return;

            dispatcher.MoveTo(selection.Selected, hit.point);
            if (clickPing != null) clickPing.Play(hit.point);
        }
    }
}
