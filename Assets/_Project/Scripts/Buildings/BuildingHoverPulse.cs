using SporeTribal.Inputs;
using SporeTribal.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SporeTribal.Buildings
{
    public class BuildingHoverPulse : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private HoverPulseSettings pulse = new HoverPulseSettings();

        private struct TintedRenderer
        {
            public Renderer Renderer;
            public Color BaseColor;
        }

        private MaterialPropertyBlock block;
        private TintedRenderer[] tinted = new TintedRenderer[0];
        private Transform active;
        private float phase;
        private float weight;
        private float lostFor;

        private void Awake() => block = new MaterialPropertyBlock();

        private void Update()
        {
            Transform target = HoverTarget(Time.unscaledDeltaTime);
            if (target != null && target != active) Switch(target);
            if (active == null) return;

            float dt = Time.unscaledDeltaTime;
            phase += dt / pulse.Period;
            weight = Mathf.MoveTowards(weight, target != null ? 1f : 0f, dt / pulse.FadeTime);
            float breath = 0.5f - 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);
            Apply(weight * pulse.PeakBrightness * breath);

            if (target == null && weight == 0f) Clear();
        }

        private Transform HoverTarget(float dt)
        {
            Transform hit = FindHovered();
            if (hit != null)
            {
                lostFor = 0f;
                return hit;
            }
            lostFor += dt;
            return lostFor < pulse.LostGrace ? active : null;
        }

        private void Switch(Transform target)
        {
            if (active != null) Apply(0f);
            active = target;
            Cache(active);
            phase = 0f;
            weight = 0f;
        }

        private void Clear()
        {
            Apply(0f);
            active = null;
        }

        private static Transform FindHovered()
        {
            if (GameInput.IsLocked || Mouse.current == null) return null;
            if (!ScreenRaycast.TryPick(Mouse.current.position.ReadValue(), ~0, out Building building)) return null;
            return building.IsStanding ? building.transform : null;
        }

        private void Cache(Transform building)
        {
            Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
            tinted = new TintedRenderer[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                tinted[i] = new TintedRenderer { Renderer = renderers[i], BaseColor = BaseColorOf(renderers[i]) };
        }

        private Color BaseColorOf(Renderer renderer)
        {
            renderer.GetPropertyBlock(block);
            if (block.HasColor(BaseColor)) return block.GetColor(BaseColor);
            Material material = renderer.sharedMaterial;
            return material.HasProperty(BaseColor) ? material.GetColor(BaseColor) : Color.white;
        }

        private void Apply(float boost)
        {
            foreach (TintedRenderer entry in tinted)
            {
                if (entry.Renderer == null) continue;
                Color c = entry.BaseColor;
                entry.Renderer.GetPropertyBlock(block);
                block.SetColor(BaseColor, new Color(c.r * (1f + boost), c.g * (1f + boost), c.b * (1f + boost), c.a));
                entry.Renderer.SetPropertyBlock(block);
            }
        }
    }
}
