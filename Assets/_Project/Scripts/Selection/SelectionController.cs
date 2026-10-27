using System.Collections.Generic;
using SporeTribal.Economy;
using SporeTribal.Inputs;
using SporeTribal.Utilities;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.Selection
{
    public class SelectionController : MonoBehaviour
    {
        [SerializeField] private RTSInputController input;
        [SerializeField] private LayerMask unitMask = ~0;
        [Tooltip("Only this tribe's villagers can be selected.")]
        [SerializeField] private Village playerVillage;

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
            Clear();

            if (!ScreenRaycast.TryRaycast(screenPosition, unitMask, out RaycastHit hit)) return;

            var unit = hit.collider.GetComponentInParent<VillagerUnit>();
            if (unit != null) Add(unit);
        }

        private void HandleDrag(Rect screenRect)
        {
            Clear();

            foreach (VillagerUnit unit in VillagerUnit.All)
            {
                Vector3 screen = ScreenRaycast.WorldToScreen(unit.transform.position);
                if (screen.z > 0f && screenRect.Contains(screen)) Add(unit);
            }
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
