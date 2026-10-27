using UnityEngine;

namespace SporeTribal.Rendering
{
    [ExecuteAlways]
    public class GroundFootprintPainter : MonoBehaviour
    {
        private static readonly int MaskId = Shader.PropertyToID("_GroundFootprints");
        private static readonly int RectId = Shader.PropertyToID("_GroundFootprintRect");

        [Tooltip("World X/Z of the mask's lower-left corner.")]
        [SerializeField] private Vector2 origin = new Vector2(-75f, -75f);
        [Tooltip("World size of the square the mask covers. Match the ground plane.")]
        [SerializeField] private float size = 150f;
        [SerializeField] private int resolution = 512;

        private Texture2D mask;
        private Color32[] pixels;
        private readonly System.Collections.Generic.List<Vector2> neighbours = new System.Collections.Generic.List<Vector2>();
        private bool dirty = true;

        private void OnEnable()
        {
            GroundFootprint.Changed += MarkDirty;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += EditorTick;
#endif
            MarkDirty();
        }

        private void OnDisable()
        {
            GroundFootprint.Changed -= MarkDirty;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= EditorTick;
#endif
            Shader.SetGlobalTexture(MaskId, Texture2D.blackTexture);
            if (mask != null) DestroyImmediate(mask);
            mask = null;
        }

        private void OnValidate() => dirty = true;

        private void MarkDirty() => dirty = true;

        private void LateUpdate() => Tick();

        private void EditorTick()
        {
            if (!Application.isPlaying && this != null) Tick();
        }

        private void Tick()
        {
            foreach (var f in GroundFootprint.All)
                if (f.ConsumeMoved()) dirty = true;
            if (dirty) Repaint();
        }

        public void Repaint()
        {
            dirty = false;
            int res = Mathf.Max(16, resolution);
            if (mask == null || mask.width != res)
            {
                if (mask != null) DestroyImmediate(mask);
                mask = new Texture2D(res, res, TextureFormat.RGBA32, false, true)
                {
                    name = "GroundFootprints",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.DontSave
                };
                pixels = new Color32[res * res];
            }

            System.Array.Clear(pixels, 0, pixels.Length);
            float texel = size / res;
            foreach (var f in GroundFootprint.All)
            {
                var c = f.Position;
                float reach = f.Reach;
                int x0 = Mathf.Max(0, Mathf.FloorToInt((c.x - reach - origin.x) / texel));
                int x1 = Mathf.Min(res - 1, Mathf.CeilToInt((c.x + reach - origin.x) / texel));
                int y0 = Mathf.Max(0, Mathf.FloorToInt((c.y - reach - origin.y) / texel));
                int y1 = Mathf.Min(res - 1, Mathf.CeilToInt((c.y + reach - origin.y) / texel));

                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        var p = new Vector2(origin.x + (x + 0.5f) * texel, origin.y + (y + 0.5f) * texel);
                        int i = y * res + x;
                        byte clearing = (byte)(f.ClearingAt(p) * 255f);
                        byte stone = (byte)(f.StoneAt(p) * 255f);
                        if (clearing > pixels[i].r) pixels[i].r = clearing;
                        if (stone > pixels[i].g) pixels[i].g = stone;
                    }

                if (f.HasStar) StampStar(f, res, texel);
            }

            mask.SetPixels32(pixels);
            mask.Apply(false);
            Shader.SetGlobalTexture(MaskId, mask);
            Shader.SetGlobalVector(RectId, new Vector4(origin.x, origin.y, size, 0f));
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.SceneView.RepaintAll();
#endif
        }

        private void StampStar(GroundFootprint star, int res, float texel)
        {
            var c = star.Position;
            float reach = star.Reach;
            neighbours.Clear();
            foreach (var other in GroundFootprint.All)
                if (other != star && Vector2.Distance(other.Position, c) < reach) neighbours.Add(other.Position);

            int x0 = Mathf.Max(0, Mathf.FloorToInt((c.x - reach - origin.x) / texel));
            int x1 = Mathf.Min(res - 1, Mathf.CeilToInt((c.x + reach - origin.x) / texel));
            int y0 = Mathf.Max(0, Mathf.FloorToInt((c.y - reach - origin.y) / texel));
            int y1 = Mathf.Min(res - 1, Mathf.CeilToInt((c.y + reach - origin.y) / texel));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    var p = new Vector2(origin.x + (x + 0.5f) * texel, origin.y + (y + 0.5f) * texel);
                    int i = y * res + x;
                    byte stone = (byte)(star.StarAt(p, neighbours) * 255f);
                    if (stone > pixels[i].g) pixels[i].g = stone;
                }
        }
    }
}
