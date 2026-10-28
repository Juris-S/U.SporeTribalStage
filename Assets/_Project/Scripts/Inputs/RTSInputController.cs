using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SporeTribal.Inputs
{
    public class RTSInputController : MonoBehaviour
    {
        [SerializeField] private float dragThresholdPixels = 8f;

        public event Action<Vector2> SelectClicked;
        public event Action<Rect> DragSelected;
        public event Action<Vector2> CommandClicked;

        public event Action<Vector2> PanDragged;
        public event Action<Vector2> PanKeyed;
        public event Action<Vector2> OrbitDragged;
        public event Action<float> Zoomed;

        [Tooltip("Set by HUDs: presses that start over a HUD are ignored here.")]
        public Func<Vector2, bool> PointerBlocked;

        [Tooltip("Off while something else (e.g. the auto orbit) owns the arrow keys.")]
        public bool KeyPanEnabled = true;

        public bool IsDragging { get; private set; }
        public Rect DragRect { get; private set; }

        private Vector2 pressPosition;
        private Vector2 rightPressPosition;
        private bool leftHeld;
        private bool rightHeld;
        private bool rightPanning;
        private bool chordActive;
        private bool pressedOnUi;
        private bool middleHeld;

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || GameInput.IsLocked) return;

            ReadCameraInput(mouse);

            bool left = mouse.leftButton.isPressed;
            bool right = mouse.rightButton.isPressed;

            if (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
                pressedOnUi = PointerBlocked != null && PointerBlocked(mouse.position.ReadValue());
            if (pressedOnUi)
            {
                if (!left && !right) pressedOnUi = false;
                return;
            }

            if (chordActive)
            {
                if (!left && !right) chordActive = false;
                else if (left && right) OrbitDragged?.Invoke(mouse.delta.ReadValue());
                return;
            }
            if (left && right)
            {
                chordActive = true;
                leftHeld = rightHeld = rightPanning = IsDragging = false;
                return;
            }

            Vector2 position = mouse.position.ReadValue();
            UpdateLeft(mouse, position);
            UpdateRight(mouse, position);
        }

        private void ReadCameraInput(Mouse mouse)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f) Zoomed?.Invoke(Mathf.Sign(scroll));

            ReadMiddleDrag(mouse);

            if (!KeyPanEnabled) return;
            Vector2 dir = GameInput.Controls.Camera.Pan.ReadValue<Vector2>();
            if (dir != Vector2.zero) PanKeyed?.Invoke(dir);
        }

        private void ReadMiddleDrag(Mouse mouse)
        {
            if (mouse.middleButton.wasPressedThisFrame)
                middleHeld = PointerBlocked == null || !PointerBlocked(mouse.position.ReadValue());
            if (!mouse.middleButton.isPressed) middleHeld = false;
            if (!middleHeld) return;

            Vector2 delta = mouse.delta.ReadValue();
            bool orbit = GameInput.Controls.Camera.OrbitModifier.IsPressed();
            if (orbit) OrbitDragged?.Invoke(delta);
            else PanDragged?.Invoke(delta);
        }

        private void UpdateLeft(Mouse mouse, Vector2 position)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                pressPosition = position;
                leftHeld = true;
                IsDragging = false;
            }

            if (!leftHeld) return;

            if (!IsDragging && (position - pressPosition).sqrMagnitude > dragThresholdPixels * dragThresholdPixels)
                IsDragging = true;

            if (IsDragging)
                DragRect = Rect.MinMaxRect(
                    Mathf.Min(pressPosition.x, position.x), Mathf.Min(pressPosition.y, position.y),
                    Mathf.Max(pressPosition.x, position.x), Mathf.Max(pressPosition.y, position.y));

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                if (IsDragging) DragSelected?.Invoke(DragRect);
                else SelectClicked?.Invoke(position);

                IsDragging = false;
                leftHeld = false;
            }
        }

        private void UpdateRight(Mouse mouse, Vector2 position)
        {
            if (mouse.rightButton.wasPressedThisFrame)
            {
                rightPressPosition = position;
                rightHeld = true;
                rightPanning = false;
            }

            if (!rightHeld) return;

            if (!rightPanning && (position - rightPressPosition).sqrMagnitude > dragThresholdPixels * dragThresholdPixels)
                rightPanning = true;

            if (rightPanning) PanDragged?.Invoke(mouse.delta.ReadValue());

            if (mouse.rightButton.wasReleasedThisFrame)
            {
                if (!rightPanning) CommandClicked?.Invoke(position);

                rightHeld = false;
                rightPanning = false;
            }
        }
    }
}
