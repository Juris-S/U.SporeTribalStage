using SporeTribal.Rendering;
using SporeTribal.Villagers;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SporeTribal.UI
{
    public sealed class HeadshotCamera
    {
        private const int Size = 128;
        private const float Distance = 1.5f;
        private const float HalfHeight = 0.34f;
        private const float RaiseAboveFace = 0.005f;
        private const float Slab = 0.35f;

        private static readonly Color LightColour = new Color(1f, 0.96f, 0.9f);

        private readonly Camera camera;
        private readonly Light key;

        public HeadshotCamera()
        {
            var go = new GameObject("Headshot Camera") { hideFlags = HideFlags.HideAndDontSave };
            camera = go.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = HalfHeight;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;

            key = new GameObject("Headshot Light") { hideFlags = HideFlags.HideAndDontSave }.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = LightColour;
            key.intensity = 1.6f;
            key.shadows = LightShadows.None;
            key.enabled = false;
        }

        public static RenderTexture CreateTarget() => new RenderTexture(Size, Size, 16, RenderTextureFormat.ARGB32)
        {
            name = "Headshot",
            antiAliasing = 1,
            filterMode = FilterMode.Bilinear,
        };

        public void Capture(VillagerUnit unit, RenderTexture target)
        {
            Vector3 forward = unit.FaceForward;
            Vector3 face = WorldBend.Apply(unit.FaceCentre) + Vector3.up * RaiseAboveFace;
            camera.transform.SetPositionAndRotation(face + forward * Distance, Quaternion.LookRotation(-forward, Vector3.up));
            camera.nearClipPlane = Distance - Slab;
            camera.farClipPlane = Distance + Slab;
            camera.backgroundColor = Color.clear;
            camera.targetTexture = target;

            key.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up) * Quaternion.Euler(30f, -25f, 0f);
            Light sun = RenderSettings.sun;
            bool sunWasOn = sun != null && sun.enabled;
            if (sunWasOn) sun.enabled = false;
            key.enabled = true;
            camera.Render();
            key.enabled = false;
            if (sunWasOn) sun.enabled = true;
            camera.targetTexture = null;
        }

        public void Dispose()
        {
            if (camera != null) Object.Destroy(camera.gameObject);
            if (key != null) Object.Destroy(key.gameObject);
        }
    }
}
