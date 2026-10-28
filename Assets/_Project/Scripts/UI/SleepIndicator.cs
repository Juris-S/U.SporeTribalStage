using SporeTribal.Buildings;
using SporeTribal.Utilities;
using UnityEngine;

namespace SporeTribal.UI
{
    public class SleepIndicator : MonoBehaviour
    {
        [SerializeField] private float heightAboveHut = 1f;
        [SerializeField] private float bobPixels = 6f;
        [SerializeField] private int fontSize = 22;

        private HutSanctuary[] huts;
        private GUIStyle style;

        private void OnEnable() => huts = FindObjectsByType<HutSanctuary>();

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = fontSize, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            foreach (HutSanctuary hut in huts)
            {
                if (hut == null || !hut.IsSleeping) continue;
                Bounds bounds = hut.Bounds;
                Vector3 screen = ScreenRaycast.WorldToScreen(new Vector3(bounds.center.x, bounds.max.y + heightAboveHut, bounds.center.z));
                if (screen.z <= 0f) continue;
                float bob = Mathf.Sin(Time.time * 2f) * bobPixels;
                GUI.Label(new Rect(screen.x - 40f, Screen.height - screen.y - 20f + bob, 80f, 40f), "z Z z", style);
            }
        }
    }
}
