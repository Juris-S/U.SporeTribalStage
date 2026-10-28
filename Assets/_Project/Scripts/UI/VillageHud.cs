using SporeTribal.AI;
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
        private static readonly Vector2 FoodSize = new Vector2(150f, 44f);
        private static readonly Vector2 TooltipSize = new Vector2(130f, 48f);
        private static readonly Vector2 HutPopupSize = new Vector2(190f, 76f);
        private const float DebugWidth = 270f;
        private const float HutCloseDelay = 0.3f;

        private SelectionController selection;
        private SocialPerformanceDirector director;
        private MainHut hut;
        private bool hutOpen;
        private float hutCloseAt;
        private Rect foodChip;
        private Rect hutPopup;
        private Rect debugPanel;
        private bool debugVisible;

        private static InputAction DebugToggle => GameInput.Controls.DevTools.ToggleVillageDebug;

        private void OnEnable()
        {
            input.PointerBlocked = Blocked;
            DebugToggle.performed += OnDebugToggle;
        }

        private void OnDisable()
        {
            input.PointerBlocked = null;
            DebugToggle.performed -= OnDebugToggle;
        }

        private void OnDebugToggle(InputAction.CallbackContext _) => debugVisible = !debugVisible;

        private void Update() => UpdateHutHover();

        private void UpdateHutHover()
        {
            if (Mouse.current == null) return;
            Vector2 mouse = Mouse.current.position.ReadValue();
            bool overPopup = hutOpen && hutPopup.Contains(new Vector2(mouse.x, Screen.height - mouse.y));
            MainHut hovered = !Blocked(mouse) && ScreenRaycast.TryRaycast(mouse, ~0, out RaycastHit hit) ? hit.collider.GetComponentInParent<MainHut>() : null;
            bool overHut = hovered != null && hovered.Owner == village && hovered.IsStanding;
            if (overHut) hut = hovered;
            if (overHut || overPopup) hutCloseAt = Time.unscaledTime + HutCloseDelay;
            hutOpen = Time.unscaledTime < hutCloseAt && hut != null && hut.IsStanding;
        }

        private bool Blocked(Vector2 screen)
        {
            var p = new Vector2(screen.x, Screen.height - screen.y);
            return foodChip.Contains(p) || (hutOpen && hutPopup.Contains(p)) || (debugVisible && debugPanel.Contains(p));
        }

        private void OnGUI()
        {
            foodChip = new Rect(Margin, Margin, FoodSize.x, FoodSize.y);

            DrawToolTooltip();
            if (hutOpen) DrawHutPopup();
            if (debugVisible) DrawDebug();
        }

        private static bool AboveBounds(Bounds b, Vector2 size, out Rect rect)
        {
            Vector3 screen = ScreenRaycast.WorldToScreen(new Vector3(b.center.x, b.max.y, b.center.z));
            rect = new Rect(screen.x - size.x * 0.5f, Screen.height - screen.y - size.y - 8f, size.x, size.y);
            return screen.z > 0f;
        }

        private void DrawToolTooltip()
        {
            if (selection == null) selection = FindAnyObjectByType<SelectionController>();
            if (selection == null || selection.Selected.Count == 0 || Mouse.current == null) return;

            Vector2 mouse = Mouse.current.position.ReadValue();
            if (Blocked(mouse)) return;
            if (!ScreenRaycast.TryRaycast(mouse, ~0, out RaycastHit hit)) return;
            ToolBuilding building = hit.collider.GetComponentInParent<ToolBuilding>();
            if (building == null || building.Tool == null || building.Owner != village || !building.IsStanding || !AboveBounds(hit.collider.bounds, TooltipSize, out Rect rect)) return;

            Color previous = GUI.color;
            GUI.color = village.Food >= building.Tool.Cost ? Color.white : new Color(1f, 0.5f, 0.5f);
            GUI.Box(rect, $"{building.Tool.DisplayName}\n{building.Tool.Cost} food");
            GUI.color = previous;
        }

        private void DrawHutPopup()
        {
            if (hut == null || !AboveBounds(hut.Bounds, HutPopupSize, out hutPopup)) return;

            GUI.Box(hutPopup, "Main Hut");
            bool roomToRecruit = VillagerUnit.CountIn(village) < maxVillagers;
            GUI.enabled = roomToRecruit && village.Food >= recruitCost;
            var button = new Rect(hutPopup.x + 8f, hutPopup.y + 30f, hutPopup.width - 16f, Row);
            if (GUI.Button(button, roomToRecruit ? $"Recruit villager ({recruitCost})" : "Tribe is full")) Recruit();
            GUI.enabled = true;
        }

        private void DrawDebug()
        {
            debugPanel = new Rect(Margin, foodChip.yMax + Margin, DebugWidth, Row * 3 + Margin);
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

        private void Recruit()
        {
            if (!TryFindSpawnPoint(out Vector3 point)) return;
            if (village.TrySpend(recruitCost)) spawner.SpawnAt(point);
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
