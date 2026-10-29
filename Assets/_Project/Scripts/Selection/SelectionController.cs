using System.Collections.Generic;
using SporeTribal.CameraControl;
using SporeTribal.Economy;
using SporeTribal.Inputs;
using SporeTribal.Utilities;
using SporeTribal.Villagers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SporeTribal.Selection
{
    public class SelectionController : MonoBehaviour
    {
        [SerializeField] private RTSInputController input;
        [SerializeField] private LayerMask unitMask = ~0;
        [Tooltip("Only this tribe's villagers can be selected.")]
        [SerializeField] private Village playerVillage;

        private RTSCameraController cameraRig;
        private readonly List<VillagerUnit> selected = new List<VillagerUnit>();

        public IReadOnlyList<VillagerUnit> Selected => selected;

        private void OnEnable()
        {
            input.SelectClicked += HandleClick;
            input.DragSelected += HandleDrag;
        }

        private void OnDisable()
        {
            input.SelectClicked -= HandleClick;
            input.DragSelected -= HandleDrag;
        }

        private void HandleClick(Vector2 screenPosition)
        {
            bool additive = ShiftHeld;
            if (!additive) Clear();

            if (!ScreenRaycast.TryRaycast(screenPosition, unitMask, out RaycastHit hit)) return;

            var unit = hit.collider.GetComponentInParent<VillagerUnit>();
            if (unit == null) return;
            if (additive)
            {
                Toggle(unit);
                return;
            }
            Add(unit);
            if (unit.IsSelected && AltHeld) FocusCamera(unit);
        }

        public static bool ShiftHeld => Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
        public static bool AltHeld => Keyboard.current != null && Keyboard.current.altKey.isPressed;

        public void Toggle(VillagerUnit unit)
        {
            if (unit == null || !unit.IsAlive) return;
            if (selected.Remove(unit)) unit.SetSelected(false);
            else Add(unit);
        }

        public void FocusCamera(VillagerUnit unit)
        {
            if (cameraRig == null) cameraRig = FindAnyObjectByType<RTSCameraController>();
            if (cameraRig != null) cameraRig.GlideTo(unit.transform.position);
        }

        public void Include(VillagerUnit unit)
        {
            if (unit != null && unit.IsAlive && !selected.Contains(unit)) Add(unit);
        }

        public void SelectTribe()
        {
            Clear();
            foreach (VillagerUnit unit in VillagerUnit.All)
                if (unit.IsAlive) Add(unit);
        }

        private void HandleDrag(Rect screenRect)
        {
            if (!ShiftHeld) Clear();

            foreach (VillagerUnit unit in VillagerUnit.All)
            {
                Vector3 screen = ScreenRaycast.WorldToScreen(unit.transform.position);
                if (screen.z > 0f && screenRect.Contains(screen)) Include(unit);
            }
        }

        public void SelectOnly(VillagerUnit unit)
        {
            Clear();
            if (unit != null && unit.IsAlive) Add(unit);
        }

        private void Update() => selected.RemoveAll(unit => unit == null || !unit.IsAlive);

        private void Add(VillagerUnit unit)
        {
            if (unit.Home != playerVillage) return;
            selected.Add(unit);
            unit.SetSelected(true);
        }

        private void Clear()
        {
            foreach (VillagerUnit unit in selected)
                if (unit != null) unit.SetSelected(false);
            selected.Clear();
        }
    }
}
