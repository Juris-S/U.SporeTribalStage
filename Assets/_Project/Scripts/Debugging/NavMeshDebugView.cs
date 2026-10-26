using System.Collections.Generic;
using SporeTribal.Commands;
using SporeTribal.Selection;
using SporeTribal.Villagers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SporeTribal.Debugging
{
    public class NavMeshDebugView : MonoBehaviour
    {
        [SerializeField] private SelectionController selection;
        [SerializeField] private OrderDispatcher dispatcher;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private Key toggleKey = Key.N;
        [SerializeField] private Key allKey = Key.M;
        [SerializeField] private float lineWidth = 0.12f;
        [SerializeField] private float lift = 0.06f;
        [SerializeField] private float slotRadius = 0.4f;

        private const int RingSegments = 24;

        private struct Label
        {
            public Vector3 World;
            public string Text;
            public Color Colour;
            public bool Small;
        }

        private readonly List<LineRenderer> pathLines = new List<LineRenderer>();
        private readonly List<LineRenderer> slotRings = new List<LineRenderer>();
        private readonly List<Label> labels = new List<Label>();
        private readonly List<Vector3> points = new List<Vector3>();

        private Transform root;
        private bool visible;
        private bool showAll;
        private VillagerUnit[] orderUnits = System.Array.Empty<VillagerUnit>();
        private Vector3[] orderSlots;
        private int[] orderAssignment;

        private void Awake()
        {
            root = new GameObject("NavMesh Debug Lines").transform;
            root.SetParent(transform, false);
            root.gameObject.SetActive(false);
        }

        private void OnEnable() => dispatcher.OrderIssued += HandleOrder;
        private void OnDisable() => dispatcher.OrderIssued -= HandleOrder;

        private void HandleOrder(IReadOnlyList<VillagerUnit> units, Vector3[] slots, int[] assignment)
        {
            var copy = new VillagerUnit[units.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = units[i];
            orderUnits = copy;
            orderSlots = slots;
            orderAssignment = assignment;
        }

        private void Update()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current[toggleKey].wasPressedThisFrame) Toggle(false);
                if (Keyboard.current[allKey].wasPressedThisFrame) Toggle(true);
            }

            if (!visible) return;

            labels.Clear();
            int paths = 0, rings = 0;

            IEnumerable<VillagerUnit> units = showAll ? VillagerUnit.All : selection.Selected;
            foreach (VillagerUnit unit in units)
            {
                Color colour = ColourFor(unit);
                labels.Add(new Label { World = unit.transform.position + Vector3.up * 3.0f, Text = $"Order: {unit.OrderLabel}", Colour = colour, Small = true });
                labels.Add(new Label { World = unit.transform.position + Vector3.up * 2.5f, Text = unit.BehaviourLabel, Colour = Color.Lerp(colour, Color.white, 0.5f), Small = true });
                if (unit.InterruptLabel != null)
                    labels.Add(new Label { World = unit.transform.position + Vector3.up * 3.5f, Text = unit.InterruptLabel, Colour = new Color(1f, 0.35f, 0.3f), Small = true });

                if (unit.IsMoving && BuildRemainingPath(unit))
                    ShowPath(GetLine(pathLines, ref paths, false), colour);

                int index = System.Array.IndexOf(orderUnits, unit);
                if (orderSlots == null || index < 0) continue;

                int slot = orderAssignment[index];
                ShowRing(GetLine(slotRings, ref rings, true), orderSlots[slot], colour);

                string number = (slot + 1).ToString();
                labels.Add(new Label { World = orderSlots[slot] + Vector3.up * 0.3f, Text = number, Colour = colour });
                labels.Add(new Label { World = unit.transform.position + Vector3.up * 1.6f, Text = number, Colour = colour });
            }

            HideFrom(pathLines, paths);
            HideFrom(slotRings, rings);
        }

        private void Toggle(bool all)
        {
            visible = !(visible && showAll == all);
            showAll = all;
            root.gameObject.SetActive(visible);
        }

        private bool BuildRemainingPath(VillagerUnit unit)
        {
            Vector3[] corners = unit.PathCorners;
            if (corners.Length < 2) return false;

            Vector3 pos = unit.transform.position;
            int segment = 0;
            float best = float.MaxValue;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                float d = DistanceToSegment(pos, corners[i], corners[i + 1]);
                if (d < best) { best = d; segment = i; }
            }

            points.Clear();
            points.Add(new Vector3(pos.x, corners[segment].y, pos.z));
            for (int i = segment + 1; i < corners.Length; i++) points.Add(corners[i]);
            return true;
        }

        private void ShowPath(LineRenderer line, Color colour)
        {
            Style(line, colour, false);
            line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++) line.SetPosition(i, points[i] + Vector3.up * lift);
        }

        private void ShowRing(LineRenderer line, Vector3 centre, Color colour)
        {
            Style(line, colour, true);
            line.positionCount = RingSegments;
            for (int i = 0; i < RingSegments; i++)
            {
                float a = i * Mathf.PI * 2f / RingSegments;
                line.SetPosition(i, centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * slotRadius + Vector3.up * lift);
            }
        }

        private void Style(LineRenderer line, Color colour, bool loop)
        {
            line.loop = loop;
            line.startWidth = line.endWidth = lineWidth;
            line.startColor = line.endColor = colour;
        }

        private LineRenderer GetLine(List<LineRenderer> pool, ref int used, bool ring)
        {
            if (used == pool.Count)
            {
                var go = new GameObject(ring ? "Slot Ring" : "Path");
                go.transform.SetParent(root, false);
                var line = go.AddComponent<LineRenderer>();
                line.sharedMaterial = lineMaterial;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.numCornerVertices = 4;
                pool.Add(line);
            }

            LineRenderer result = pool[used++];
            result.gameObject.SetActive(true);
            return result;
        }

        private static void HideFrom(List<LineRenderer> pool, int firstUnused)
        {
            for (int i = firstUnused; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
        }

        private static Color ColourFor(VillagerUnit unit)
        {
            IReadOnlyList<VillagerUnit> all = VillagerUnit.All;
            int index = 0;
            for (int i = 0; i < all.Count; i++) if (all[i] == unit) { index = i; break; }
            return Color.HSVToRGB(index / (float)Mathf.Max(all.Count, 1), 0.6f, 0.85f);
        }

        private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            p.y = a.y = b.y = 0f;
            Vector3 ab = b - a;
            float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector3.Distance(p, a + ab * t);
        }

        private GUIStyle[] styles;

        private GUIStyle StyleFor(bool small)
        {
            if (styles == null)
            {
                styles = new GUIStyle[2];
                for (int i = 0; i < 2; i++)
                {
                    styles[i] = new GUIStyle(GUI.skin.label) { fontSize = i == 0 ? 26 : 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
                    styles[i].normal.textColor = Color.white;
                }
            }
            return styles[small ? 1 : 0];
        }

        private void OnGUI()
        {
            if (!visible || Event.current.type != EventType.Repaint) return;

            Camera cam = Camera.main;

            foreach (Label label in labels)
            {
                Vector3 screen = cam.WorldToScreenPoint(label.World);
                if (screen.z <= 0f) continue;

                GUIStyle style = StyleFor(label.Small);
                float width = label.Small ? 200f : 80f;
                var rect = new Rect(screen.x - width * 0.5f, Screen.height - screen.y - 20f, width, 40f);
                GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), $"<color=#000000CC>{label.Text}</color>", style);
                GUI.Label(rect, $"<color=#{ColorUtility.ToHtmlStringRGB(label.Colour)}>{label.Text}</color>", style);
            }
        }
    }
}
