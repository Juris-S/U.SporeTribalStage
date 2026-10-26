using SporeTribal.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SporeTribal.Buildings
{
    public class BuildingHoverPulse : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [Tooltip("Extra albedo multiplier at the peak of the pulse (0.35 = 35% brighter).")]
        [SerializeField] private float peakBrightness = 0.35f;
        [Tooltip("Seconds for one full breath.")]
        [SerializeField] private float period = 0.8f;
        [Tooltip("Seconds for the pulse to fade in on hover and fade out after the mouse leaves.")]
        [SerializeField] private float fadeTime = 0.35f;
        [Tooltip("Seconds the hover survives a missed raycast (a villager walking past, a gap between colliders) before fading out.")]
        [SerializeField] private float lostGrace = 0.15f;

        private MaterialPropertyBlock block;
        private Renderer[] renderers;
        private Color[] baseColors;
        private Transform active;
        private float phase;
        private float weight;
        private float lostFor;

        private void Awake() => block = new MaterialPropertyBlock();

        private void Update()
        {
            Transform hit = FindHovered();
            lostFor = hit != null ? 0f : lostFor + Time.unscaledDeltaTime;
            Transform target = hit != null || lostFor < lostGrace ? (hit ?? active) : null;
            if (target != null && target != active)
            {
                if (active != null) Apply(0f);
                active = target;
                Cache(active);
                phase = 0f;
                weight = 0f;
            }
            if (active == null) return;

            float dt = Time.unscaledDeltaTime;
            phase += dt / period;
            weight = Mathf.MoveTowards(weight, target != null ? 1f : 0f, dt / fadeTime);
            Apply(weight * peakBrightness * (0.5f - 0.5f * Mathf.Cos(phase * Mathf.PI * 2f)));

            if (target == null && weight == 0f) { Apply(0f); active = null; }
        }

        private static Transform FindHovered()
        {
            if (Mouse.current == null || !ScreenRaycast.TryRaycast(Mouse.current.position.ReadValue(), ~0, out RaycastHit hit)) return null;
            var building = hit.collider.GetComponentInParent<Building>();
            return building != null && building.IsStanding ? building.transform : null;
        }

        private void Cache(Transform building)
        {
            renderers = building.GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                baseColors[i] = renderers[i].sharedMaterial.HasProperty(BaseColor) ? renderers[i].sharedMaterial.GetColor(BaseColor) : Color.white;
        }

        private void Apply(float boost)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color c = baseColors[i];
                block.SetColor(BaseColor, new Color(c.r * (1f + boost), c.g * (1f + boost), c.b * (1f + boost), c.a));
                renderers[i].SetPropertyBlock(boost > 0f ? block : null);
            }
        }
    }
}
