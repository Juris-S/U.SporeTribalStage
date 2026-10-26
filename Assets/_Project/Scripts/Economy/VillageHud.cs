using SporeTribal.Inputs;
using SporeTribal.Selection;
using SporeTribal.Utilities;
using SporeTribal.Villagers;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace SporeTribal.Economy
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
        [SerializeField] private Key debugToggleKey = Key.F1;
        [SerializeField] private int[] debugFoodAmounts = { 10, 50, 100 };

        private const float Margin = 12f;
        private const float Row = 30f;
        private static readonly Vector2 FoodSize = new Vector2(150f, 44f);
        private static readonly Vector2 TooltipSize = new Vector2(130f, 48f);
        private static readonly Vector2 HutPopupSize = new Vector2(190f, 76f);
        private const float DebugWidth = 270f;
        private const float HutCloseDelay = 0.3f;

        private SelectionController selection;
        private MainHut hut;
        private bool hutOpen;
        private float hutCloseAt;
        private Rect foodChip;
        private Rect hutPopup;
        private Rect debugPanel;
        private bool debugVisible;

        private void OnEnable()
        {
            input.PointerBlocked = Blocked;
        }

        private void OnDisable()
        {
            input.PointerBlocked = null;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[debugToggleKey].wasPressedThisFrame) debugVisible = !debugVisible;
            UpdateHutHover();
        }

        private void UpdateHutHover()
        {
            if (Mouse.current == null) return;
            Vector2 mouse = Mouse.current.position.ReadValue();
            bool overPopup = hutOpen && hutPopup.Contains(new Vector2(mouse.x, Screen.height - mouse.y));
            bool overHut = !Blocked(mouse) && ScreenRaycast.TryRaycast(mouse, ~0, out RaycastHit hit) && hit.collider.GetComponentInParent<MainHut>() != null;
            if (overHut || overPopup) hutCloseAt = Time.unscaledTime + HutCloseDelay;
            hutOpen = Time.unscaledTime < hutCloseAt;
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

        private static bool AboveCollider(Collider c, Vector2 size, out Rect rect)
        {
            Bounds b = c.bounds;
            Vector3 screen = ScreenRaycast.Camera.WorldToScreenPoint(new Vector3(b.center.x, b.max.y, b.center.z));
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
            if (building == null || building.Tool == null || !AboveCollider(hit.collider, TooltipSize, out Rect rect)) return;

            Color previous = GUI.color;
            GUI.color = village.Food >= building.Tool.Cost ? Color.white : new Color(1f, 0.5f, 0.5f);
            GUI.Box(rect, $"{building.Tool.DisplayName}\n{building.Tool.Cost} food");
            GUI.color = previous;
        }

        private void DrawHutPopup()
        {
            if (hut == null) hut = FindAnyObjectByType<MainHut>();
            Collider c = hut != null ? hut.GetComponentInChildren<Collider>() : null;
            if (c == null || !AboveCollider(c, HutPopupSize, out hutPopup)) return;

            GUI.Box(hutPopup, "Main Hut");
            bool roomToRecruit = VillagerUnit.All.Count < maxVillagers;
            GUI.enabled = roomToRecruit && village.Food >= recruitCost;
            var button = new Rect(hutPopup.x + 8f, hutPopup.y + 30f, hutPopup.width - 16f, Row);
            if (GUI.Button(button, roomToRecruit ? $"Recruit villager ({recruitCost})" : "Tribe is full")) Recruit();
            GUI.enabled = true;
        }

        private void DrawDebug()
        {
            debugPanel = new Rect(Margin, foodChip.yMax + Margin, DebugWidth, Row * 2 + Margin);
            GUI.Box(debugPanel, GUIContent.none);

            float y = debugPanel.y + 6f;
            GUI.Label(new Rect(debugPanel.x + 6f, y, DebugWidth - 12f, Row - 4f), $"Debug food ({debugToggleKey} hides)");
            y += Row;

            float w = (DebugWidth - 12f) / (debugFoodAmounts.Length + 1);
            for (int i = 0; i < debugFoodAmounts.Length; i++)
                if (GUI.Button(new Rect(debugPanel.x + 6f + w * i, y, w - 4f, Row - 4f), $"+{debugFoodAmounts[i]}"))
                    village.Deposit(debugFoodAmounts[i]);
            if (GUI.Button(new Rect(debugPanel.x + 6f + w * debugFoodAmounts.Length, y, w - 4f, Row - 4f), "Zero"))
                village.TrySpend(village.Food);
        }

        private void Recruit()
        {
            if (!TryFindSpawnPoint(out Vector3 point)) return;
            if (village.TrySpend(recruitCost)) spawner.SpawnAt(point);
        }

        private bool TryFindSpawnPoint(out Vector3 point)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = Random.insideUnitCircle * recruitRadius;
                if (NavMesh.SamplePosition(recruitPoint.position + new Vector3(offset.x, 0f, offset.y), out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
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
