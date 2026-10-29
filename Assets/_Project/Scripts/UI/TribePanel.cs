using System.Collections.Generic;
using SporeTribal.Audio;
using SporeTribal.Definitions;
using SporeTribal.Economy;
using SporeTribal.Selection;
using SporeTribal.Villagers;
using UnityEngine;
using UnityEngine.UIElements;

namespace SporeTribal.UI
{
    public class TribePanel : MonoBehaviour
    {
        private const float SelectedTint = 0.9f;
        private const float IdleTint = 0.5f;

        private sealed class Chip
        {
            public VisualElement Root;
            public VisualElement Portrait;
            public VisualElement Health;
            public VisualElement HealthTrack;
            public VisualElement ToolIcon;
            public ToolDefinition Tool;
            public RenderTexture Headshot;
            public int CreatedFrame;
            public bool Captured;
            public bool Seen;
        }

        private readonly Dictionary<VillagerUnit, Chip> chips = new Dictionary<VillagerUnit, Chip>();
        private readonly List<VillagerUnit> gone = new List<VillagerUnit>();
        private HeadshotCamera headshots;
        private Village village;
        private SelectionController selection;
        private VisualElement panel;
        private VisualElement list;
        private bool dragging;

        private void OnDisable()
        {
            if (panel != null)
            {
                HudLayer.RemoveBlocker(panel);
                panel.RemoveFromHierarchy();
                panel = null;
            }
            ReleaseChips();
            headshots?.Dispose();
            headshots = null;
        }

        private void ReleaseChips()
        {
            foreach (Chip chip in chips.Values) Release(chip);
            chips.Clear();
        }

        private static void Release(Chip chip)
        {
            if (chip.Headshot == null) return;
            chip.Headshot.Release();
            Destroy(chip.Headshot);
        }

        private void Update()
        {
            if (!Resolve() || !EnsureUi()) return;
            Sync();
        }

        private bool Resolve()
        {
            if (village == null)
            {
                MatchDirector match = FindAnyObjectByType<MatchDirector>();
                village = match != null ? match.Player : null;
            }
            if (selection == null) selection = FindAnyObjectByType<SelectionController>();
            return village != null && selection != null;
        }

        private bool EnsureUi()
        {
            VisualElement layer = HudLayer.Root;
            if (layer == null) return false;
            if (panel != null && panel.panel != null) return true;

            ReleaseChips();
            headshots ??= new HeadshotCamera();
            panel = new VisualElement { name = "tribe-panel" };
            panel.AddToClassList("tribe-panel");
            panel.Add(HudLayer.MakeButton("All", OnSelectAllClicked, "tribe-panel__all"));
            list = new VisualElement { pickingMode = PickingMode.Ignore };
            list.AddToClassList("tribe-panel__list");
            panel.Add(list);
            panel.RegisterCallback<PointerMoveEvent>(OnPanelMoved);
            panel.RegisterCallback<PointerUpEvent>(OnPanelReleased);
            panel.RegisterCallback<PointerCaptureOutEvent>(_ => dragging = false);
            layer.Add(panel);
            HudLayer.AddBlocker(panel);
            return true;
        }

        private void Sync()
        {
            foreach (Chip chip in chips.Values) chip.Seen = false;

            int count = 0;
            foreach (VillagerUnit unit in VillagerUnit.All)
            {
                if (unit.Home != village || !unit.IsAlive) continue;
                count++;
                if (!chips.TryGetValue(unit, out Chip chip)) chip = Add(unit);
                chip.Seen = true;
                Refresh(unit, chip);
                if (!chip.Captured && chip.CreatedFrame < Time.frameCount) Capture(unit, chip);
            }

            gone.Clear();
            foreach (KeyValuePair<VillagerUnit, Chip> entry in chips)
                if (!entry.Value.Seen) gone.Add(entry.Key);
            foreach (VillagerUnit unit in gone)
            {
                Chip chip = chips[unit];
                chip.Root.RemoveFromHierarchy();
                Release(chip);
                chips.Remove(unit);
            }

            panel.style.display = count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private Chip Add(VillagerUnit unit)
        {
            var root = new VisualElement();
            root.AddToClassList("tribe-chip");
            root.EnableInClassList("tribe-chip--chief", unit.IsChieftain);

            var portrait = new VisualElement { pickingMode = PickingMode.Ignore };
            portrait.AddToClassList("tribe-chip__portrait");
            var toolIcon = new VisualElement { pickingMode = PickingMode.Ignore };
            toolIcon.AddToClassList("tribe-chip__tool");
            var track = new VisualElement { pickingMode = PickingMode.Ignore };
            track.AddToClassList("tribe-chip__track");
            var health = new VisualElement { pickingMode = PickingMode.Ignore };
            health.AddToClassList("tribe-chip__health");
            track.Add(health);
            root.Add(toolIcon);
            root.Add(portrait);
            root.Add(track);
            root.RegisterCallback<PointerDownEvent>(e => OnChipPressed(unit, root, e));

            if (unit.IsChieftain) list.Insert(0, root); else list.Add(root);
            if (Application.isPlaying) HudLayer.PopIn(root, 0.3f, 0.6f);

            var chip = new Chip { Root = root, Portrait = portrait, Health = health, HealthTrack = track, ToolIcon = toolIcon, Headshot = HeadshotCamera.CreateTarget(), CreatedFrame = Time.frameCount };
            portrait.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(chip.Headshot));
            chips.Add(unit, chip);
            return chip;
        }

        private void Capture(VillagerUnit unit, Chip chip)
        {
            chip.Captured = true;
            headshots.Capture(unit, chip.Headshot);
        }

        private Color Tint(bool selected)
        {
            Color tribe = village.TribeColour;
            float tint = selected ? SelectedTint : IdleTint;
            return new Color(tribe.r * tint, tribe.g * tint, tribe.b * tint, 1f);
        }

        private void Refresh(VillagerUnit unit, Chip chip)
        {
            chip.Root.style.backgroundColor = Tint(unit.IsSelected);
            chip.Root.EnableInClassList("tribe-chip--selected", unit.IsSelected);

            if (unit.Tool != chip.Tool)
            {
                chip.Tool = unit.Tool;
                Texture2D icon = unit.Tool != null ? unit.Tool.Icon : null;
                chip.ToolIcon.style.display = icon != null ? DisplayStyle.Flex : DisplayStyle.None;
                chip.ToolIcon.style.backgroundImage = icon != null ? new StyleBackground(icon) : StyleKeyword.None;
            }

            float fraction = Mathf.Clamp01(unit.Health.Fraction);
            chip.Health.style.width = Length.Percent(fraction * 100f);
            chip.Health.style.backgroundColor = Color.Lerp(new Color(1f, 0.43f, 0.43f), new Color(0.55f, 0.9f, 0.43f), fraction);
            chip.Root.EnableInClassList("tribe-chip--hurt", fraction < 1f);
        }

        private void OnChipPressed(VillagerUnit unit, VisualElement root, PointerDownEvent e)
        {
            if (e.button != 0) return;
            if (SelectionController.ShiftHeld) selection.Toggle(unit);
            else
            {
                selection.SelectOnly(unit);
                if (SelectionController.AltHeld) selection.FocusCamera(unit);
            }
            dragging = true;
            panel.CapturePointer(e.pointerId);
            HudLayer.Punch(root, 0.15f);
            AudioManager.PlayUi(SfxId.ButtonClick);
        }

        private void OnPanelMoved(PointerMoveEvent e)
        {
            if (!dragging) return;
            Vector2 point = panel.LocalToWorld((Vector2)e.localPosition);
            foreach (KeyValuePair<VillagerUnit, Chip> entry in chips)
                if (entry.Value.Root.worldBound.Contains(point)) selection.Include(entry.Key);
        }

        private void OnPanelReleased(PointerUpEvent e)
        {
            if (!dragging) return;
            dragging = false;
            panel.ReleasePointer(e.pointerId);
        }

        private void OnSelectAllClicked()
        {
            selection.SelectTribe();
        }
    }
}
