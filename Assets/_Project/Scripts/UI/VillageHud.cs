using System.Collections.Generic;
using SporeTribal.AI;
using SporeTribal.Audio;
using SporeTribal.Buildings;
using SporeTribal.Definitions;
using SporeTribal.Economy;
using SporeTribal.Inputs;
using SporeTribal.Selection;
using SporeTribal.Utilities;
using SporeTribal.Villagers;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SporeTribal.UI
{
    public class VillageHud : MonoBehaviour
    {
        [SerializeField] private Village village;
        [SerializeField] private VillagerSpawner spawner;
        [SerializeField] private RTSInputController input;
        [Tooltip("Where recruits appear (in front of the main hut).")]
        [SerializeField] private Transform recruitPoint;
        [SerializeField] private int recruitCost = 10;
        [SerializeField] private int maxVillagers = 12;
        [SerializeField] private float recruitRadius = 2.5f;

        [Header("Debug")]
        [SerializeField] private int[] debugFoodAmounts = { 10, 50, 100 };

        private const float Margin = 12f;
        private const float SpawnClearance = 1f;
        private const float Row = 30f;
        private static readonly Vector2 FoodSize = new Vector2(170f, 44f);
        private const float DebugWidth = 270f;
        private const float HutCloseDelay = 0.3f;

        private SelectionController selection;
        private SocialPerformanceDirector director;
        private TribeProgressionDirector progression;
        private MainHut hut;
        private bool hutOpen;
        private float hutCloseAt;
        private Rect foodChip;
        private Rect debugPanel;
        private VisualElement tooltip;
        private Label tooltipName;
        private Label tooltipCost;
        private ToolBuilding tooltipBuilding;
        private VisualElement popup;
        private Button recruitButton;
        private bool popupShown;
        private bool debugVisible;

        public int RecruitCost => recruitCost;

        private static InputAction DebugToggle => GameInput.Controls.DevTools.ToggleVillageDebug;

        private void OnEnable()
        {
            foodChip = new Rect(Margin, Margin, FoodSize.x, FoodSize.y);
            input.PointerBlocked = Blocked;
            if (!TryGetComponent(out TribePanel _)) gameObject.AddComponent<TribePanel>();
            if (!TryGetComponent(out ProgressionBar _)) gameObject.AddComponent<ProgressionBar>();
            if (!TryGetComponent(out Minimap _)) gameObject.AddComponent<Minimap>();
            DebugToggle.performed += OnDebugToggle;
        }

        private void OnDisable()
        {
            input.PointerBlocked = null;
            DebugToggle.performed -= OnDebugToggle;
        }

        private void OnDebugToggle(InputAction.CallbackContext _) => debugVisible = !debugVisible;

        private void Update()
        {
            UpdateHutHover();
            if (!EnsureUi()) return;
            UpdatePopup();
            UpdateTooltip();
        }

        private void UpdateHutHover()
        {
            if (Mouse.current == null) return;
            Vector2 mouse = Mouse.current.position.ReadValue();
            bool overPopup = hutOpen && OverPopup(mouse);
            MainHut hovered = !Blocked(mouse) && ScreenRaycast.TryRaycast(mouse, ~0, out RaycastHit hit) ? hit.collider.GetComponentInParent<MainHut>() : null;
            bool overHut = hovered != null && hovered.Owner == village && hovered.IsStanding;
            if (overHut) hut = hovered;
            if (overHut || overPopup) hutCloseAt = Time.unscaledTime + HutCloseDelay;
            hutOpen = Time.unscaledTime < hutCloseAt && hut != null && hut.IsStanding;
        }

        private bool Blocked(Vector2 screen)
        {
            var p = new Vector2(screen.x, Screen.height - screen.y);
            return foodChip.Contains(p) || HudLayer.IsOverBlocker(screen) || (hutOpen && OverPopup(screen)) || (debugVisible && debugPanel.Contains(p));
        }

        private bool OverPopup(Vector2 screen)
        {
            if (popup == null || popup.panel == null) return false;
            Vector2 point = RuntimePanelUtils.ScreenToPanel(popup.panel, new Vector2(screen.x, Screen.height - screen.y));
            return popup.worldBound.Contains(point);
        }

        private void OnGUI()
        {
            if (debugVisible) DrawDebug();
        }

        private bool EnsureUi()
        {
            VisualElement layer = HudLayer.Root;
            if (layer == null) return false;
            if (popup != null && popup.panel != null) return true;

            tooltip = new VisualElement { pickingMode = PickingMode.Ignore };
            tooltip.AddToClassList("tooltip");
            HudLayer.ApplyBackdrop(tooltip);
            tooltipName = HudLayer.MakeLabel(null, "tooltip__name");
            tooltipCost = HudLayer.MakeLabel(null, "tooltip__cost");
            tooltip.Add(tooltipName);
            tooltip.Add(tooltipCost);
            layer.Add(tooltip);
            tooltipBuilding = null;

            popup = new VisualElement();
            popup.AddToClassList("hut-popup");
            HudLayer.ApplyBackdrop(popup);
            popup.Add(HudLayer.MakeLabel("Main Hut", "hut-popup__title"));
            recruitButton = HudLayer.MakeButton(null, OnRecruitClicked);
            popup.Add(recruitButton);
            layer.Add(popup);
            popupShown = false;
            return true;
        }

        private void UpdatePopup()
        {
            Vector2 anchor = default;
            bool show = hutOpen && hut != null && HudLayer.TryWorldToPanel(hut.Bounds.center, popup, out anchor);
            if (show)
            {
                popup.style.left = anchor.x;
                popup.style.top = anchor.y;
                bool roomToRecruit = VillagerUnit.CountIn(village) < maxVillagers;
                string text = roomToRecruit ? $"Recruit villager ({recruitCost})" : "Tribe is full";
                if (recruitButton.text != text) recruitButton.text = text;
                recruitButton.EnableInClassList("hud-button--off", !CanRecruit());
            }
            if (show == popupShown) return;
            popupShown = show;
            if (show) HudLayer.PopIn(popup);
            else HudLayer.FadeOut(popup);
        }

        private void UpdateTooltip()
        {
            ToolBuilding building = HoveredToolBuilding(out Bounds bounds);
            Vector2 anchor = default;
            if (building == null || !HudLayer.TryWorldToPanel(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z), tooltip, out anchor))
            {
                if (tooltipBuilding != null) HudLayer.FadeOut(tooltip, 0.08f);
                tooltipBuilding = null;
                return;
            }

            tooltip.style.left = anchor.x;
            tooltip.style.top = anchor.y;
            tooltip.EnableInClassList("tooltip--short", village.Food < building.Tool.Cost);
            if (building == tooltipBuilding) return;
            tooltipBuilding = building;
            tooltipName.text = building.Tool.DisplayName;
            tooltipCost.text = $"{building.Tool.Cost} food";
            HudLayer.PopIn(tooltip, 0.18f);
        }

        private ToolBuilding HoveredToolBuilding(out Bounds bounds)
        {
            bounds = default;
            if (selection == null) selection = FindAnyObjectByType<SelectionController>();
            if (selection == null || selection.Selected.Count == 0 || Mouse.current == null) return null;

            Vector2 mouse = Mouse.current.position.ReadValue();
            if (Blocked(mouse) || !ScreenRaycast.TryRaycast(mouse, ~0, out RaycastHit hit)) return null;
            ToolBuilding building = hit.collider.GetComponentInParent<ToolBuilding>();
            if (building == null || building.Tool == null || building.Owner != village || !building.IsStanding) return null;
            bounds = hit.collider.bounds;
            return building;
        }

        private bool CanRecruit() => VillagerUnit.CountIn(village) < maxVillagers && village.Food >= recruitCost;

        private void OnRecruitClicked()
        {
            if (!CanRecruit() || !Recruit())
            {
                HudLayer.Shake(recruitButton);
                return;
            }
            AudioManager.PlayUi(SfxId.Recruit);
            HudLayer.Punch(popup);
        }

        private void DrawDebug()
        {
            debugPanel = new Rect(Margin, foodChip.yMax + Margin, DebugWidth, Row * 6 + Margin);
            GUI.Box(debugPanel, GUIContent.none);

            float y = debugPanel.y + 6f;
            GUI.Label(new Rect(debugPanel.x + 6f, y, DebugWidth - 12f, Row - 4f), $"Debug ({DebugToggle.GetBindingDisplayString()} hides)");
            y += Row;

            float w = (DebugWidth - 12f) / (debugFoodAmounts.Length + 1);
            for (int i = 0; i < debugFoodAmounts.Length; i++)
                if (GUI.Button(new Rect(debugPanel.x + 6f + w * i, y, w - 4f, Row - 4f), $"+{debugFoodAmounts[i]}"))
                    village.Deposit(debugFoodAmounts[i]);
            if (GUI.Button(new Rect(debugPanel.x + 6f + w * debugFoodAmounts.Length, y, w - 4f, Row - 4f), "Zero"))
                village.TrySpend(village.Food);
            y += Row;

            float half = (DebugWidth - 12f) / 2f;
            if (GUI.Button(new Rect(debugPanel.x + 6f, y, half - 4f, Row - 4f), "Equip band")) DebugEquipBand();
            if (GUI.Button(new Rect(debugPanel.x + 6f + half, y, half - 4f, Row - 4f), "Go to rival")) DebugWarpToRival();
            y += Row;

            if (progression == null) progression = FindAnyObjectByType<TribeProgressionDirector>();
            GUI.enabled = progression != null && progression.HasPendingWave;
            if (GUI.Button(new Rect(debugPanel.x + 6f, y, DebugWidth - 16f, Row - 4f), "Spawn next tribe wave")) progression.SettleNext();
            GUI.enabled = true;
            y += Row;

            GUI.enabled = selection != null && selection.Selected.Count > 0;
            if (GUI.Button(new Rect(debugPanel.x + 6f, y, half - 4f, Row - 4f), $"Hurt {DebugHealthStep}")) DebugHealSelected(-DebugHealthStep);
            if (GUI.Button(new Rect(debugPanel.x + 6f + half, y, half - 4f, Row - 4f), $"Heal {DebugHealthStep}")) DebugHealSelected(DebugHealthStep);
            GUI.enabled = true;
            y += Row;

            GUI.enabled = progression != null;
            if (GUI.Button(new Rect(debugPanel.x + 6f, y, half - 4f, Row - 4f), "Conquer next")) progression.ResolveNext(true);
            if (GUI.Button(new Rect(debugPanel.x + 6f + half, y, half - 4f, Row - 4f), "Ally next")) progression.ResolveNext(false);
            GUI.enabled = true;
        }

        private const int DebugHealthStep = 10;
        private readonly List<VillagerUnit> debugTargets = new List<VillagerUnit>();

        private void DebugHealSelected(int amount)
        {
            debugTargets.Clear();
            debugTargets.AddRange(selection.Selected);
            foreach (VillagerUnit unit in debugTargets)
            {
                if (unit == null || !unit.IsAlive) continue;
                if (amount < 0) unit.Health.Damage(-amount, null);
                else unit.Health.Heal(amount);
            }
        }

        private static readonly InstrumentKind[] DebugHandout = { InstrumentKind.Didgeridoo, InstrumentKind.Horn, InstrumentKind.Maraca };

        private void DebugEquipBand()
        {
            if (director == null) director = FindAnyObjectByType<SocialPerformanceDirector>();
            if (director == null) return;
            int given = 0;
            foreach (VillagerUnit unit in VillagerUnit.All)
            {
                if (unit.Home != village || !unit.IsAlive || unit.IsChieftain) continue;
                ToolDefinition tool = director.ToolFor(DebugHandout[given++ % DebugHandout.Length]);
                if (tool != null) unit.Equip(tool);
            }
        }

        private void DebugWarpToRival()
        {
            if (director == null) director = FindAnyObjectByType<SocialPerformanceDirector>();
            Village rival = director != null ? director.Rival : null;
            if (rival == null) return;
            Vector3 away = village.DropOffCentre - rival.DropOffCentre;
            away.y = 0f;
            away = away.sqrMagnitude < 0.01f ? Vector3.back : away.normalized;
            Vector3 side = Vector3.Cross(Vector3.up, away);
            Vector3 spot = rival.DropOffCentre + away * (rival.WanderRadius + 14f);

            int placed = 0;
            foreach (VillagerUnit unit in VillagerUnit.All)
            {
                if (unit.Home != village || !unit.IsAlive) continue;
                Vector3 slot = spot + side * ((placed++ - 3) * 1.2f);
                if (NavMesh.SamplePosition(slot, out NavMeshHit hit, 4f, NavMesh.AllAreas)) slot = hit.position;
                unit.Warp(slot);
            }
        }

        private bool Recruit()
        {
            if (!TryFindSpawnPoint(out Vector3 point) || !village.TrySpend(recruitCost)) return false;
            VillagerUnit recruit = spawner.SpawnAt(point);
            if (!village.HasLivingChieftain()) recruit.MakeChieftain();
            return true;
        }

        private bool TryFindSpawnPoint(out Vector3 point)
        {
            for (int i = 0; i < 16; i++)
            {
                Vector2 offset = Random.insideUnitCircle * recruitRadius;
                if (NavMesh.SamplePosition(recruitPoint.position + new Vector3(offset.x, 0f, offset.y), out NavMeshHit hit, 0.5f, NavMesh.AllAreas) && !VillagerUnit.IsCrowded(hit.position, SpawnClearance))
                {
                    point = hit.position;
                    return true;
                }
            }
            bool found = NavMesh.SamplePosition(recruitPoint.position, out NavMeshHit fallback, 4f, NavMesh.AllAreas);
            point = fallback.position;
            return found;
        }
    }
}
