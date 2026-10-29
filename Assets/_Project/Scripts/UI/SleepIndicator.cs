using SporeTribal.Buildings;
using UnityEngine;
using UnityEngine.UIElements;

namespace SporeTribal.UI
{
    public class SleepIndicator : MonoBehaviour
    {
        [SerializeField] private float heightAboveHut = 1f;
        [SerializeField] private float bobPixels = 6f;

        private HutSanctuary[] huts;
        private Label[] labels;
        private bool[] shown;

        private void OnEnable() => huts = FindObjectsByType<HutSanctuary>();

        private void OnDisable()
        {
            if (labels == null) return;
            foreach (Label label in labels) label?.RemoveFromHierarchy();
            labels = null;
        }

        private void LateUpdate()
        {
            VisualElement layer = HudLayer.Root;
            if (layer == null || huts.Length == 0) return;
            if (labels == null || labels[0].panel == null) Build(layer);

            for (int i = 0; i < huts.Length; i++)
            {
                HutSanctuary hut = huts[i];
                Label label = labels[i];
                Vector2 point = default;
                bool show = hut != null && hut.IsSleeping;
                if (show)
                {
                    Bounds bounds = hut.Bounds;
                    show = HudLayer.TryWorldToPanel(new Vector3(bounds.center.x, bounds.max.y + heightAboveHut, bounds.center.z), label, out point);
                }

                if (show)
                {
                    label.style.left = point.x;
                    label.style.top = point.y + Mathf.Sin(Time.time * 2f) * bobPixels;
                }
                if (show == shown[i]) continue;
                shown[i] = show;
                if (show) HudLayer.PopIn(label, 0.3f, 0.5f);
                else HudLayer.FadeOut(label, 0.2f);
            }
        }

        private void Build(VisualElement layer)
        {
            labels = new Label[huts.Length];
            shown = new bool[huts.Length];
            for (int i = 0; i < huts.Length; i++)
            {
                labels[i] = HudLayer.MakeLabel("z Z z", "sleep-indicator");
                layer.Add(labels[i]);
            }
        }
    }
}
