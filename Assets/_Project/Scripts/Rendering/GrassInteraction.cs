using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.Rendering
{
    [DefaultExecutionOrder(50)]
    public class GrassInteraction : MonoBehaviour
    {
        private const int MaxPushers = 64;
        private static readonly int PushersId = Shader.PropertyToID("_GrassPushers");
        private static readonly int CountId = Shader.PropertyToID("_GrassPusherCount");
        private static readonly int FadeId = Shader.PropertyToID("_GrassFade");

        [Tooltip("Pusher radius multiplier. Raise for a larger footprint, e.g. for a bigger creature later.")]
        [SerializeField] private float radiusScale = 1f;

        [Header("Zoom fade")]
        [Tooltip("Parent of the grass chunks. Only these renderers are switched off when zoomed out.")]
        [SerializeField] private Transform grassRoot;
        [Tooltip("Camera distance to the ground point it looks at where grass starts to shrink.")]
        [SerializeField] private float fadeStartDistance = 20f;
        [Tooltip("Camera distance at which grass is fully gone (renderers are switched off).")]
        [SerializeField] private float fadeEndDistance = 38f;
        [SerializeField] private float fadeSmoothTime = 0.15f;

        private readonly Vector4[] pushers = new Vector4[MaxPushers];
        private Renderer[] renderers;
        private float fade;
        private float fadeVelocity;
        private bool renderersOn = true;

        private void Awake()
        {
            renderers = grassRoot != null ? grassRoot.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        }

        public void SetGrassRoot(Transform root) => grassRoot = root;

        private void LateUpdate()
        {
            var units = VillagerUnit.All;
            int count = Mathf.Min(units.Count, MaxPushers);
            for (int i = 0; i < count; i++)
            {
                Vector3 p = units[i].transform.position;
                pushers[i] = new Vector4(p.x, p.y, p.z, radiusScale);
            }

            Shader.SetGlobalVectorArray(PushersId, pushers);
            Shader.SetGlobalFloat(CountId, count);

            UpdateZoomFade();
        }

        private void UpdateZoomFade()
        {
            var cam = Camera.main;
            if (cam == null) return;

            Transform t = cam.transform;
            float distance = t.forward.y < -0.01f ? t.position.y / -t.forward.y : fadeStartDistance;
            float target = Mathf.InverseLerp(fadeStartDistance, fadeEndDistance, distance);
            fade = Mathf.SmoothDamp(fade, target, ref fadeVelocity, fadeSmoothTime);
            Shader.SetGlobalFloat(FadeId, fade);

            bool visible = fade < 0.98f;
            if (visible == renderersOn) return;
            renderersOn = visible;
            foreach (var r in renderers)
                if (r != null) r.enabled = visible;
        }

        private void OnDisable()
        {
            Shader.SetGlobalFloat(CountId, 0f);
            Shader.SetGlobalFloat(FadeId, 0f);
        }
    }
}
