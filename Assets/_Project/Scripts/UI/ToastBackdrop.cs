using UnityEngine;

namespace SporeTribal.UI
{
    public static class ToastBackdrop
    {
        private const int Width = 128;
        private const float Opacity = 0.7f;
        private const float EdgeFade = 0.3f;

        private const float PopSeconds = 0.4f;
        private const float FadeSeconds = 0.5f;
        private const float DropPixels = 28f;
        private const float StartScale = 0.85f;

        private static Texture2D gradient;
        private static Matrix4x4 savedMatrix;
        private static Color savedColour;

        public static void Begin(Rect rect, float age, float remaining)
        {
            savedMatrix = GUI.matrix;
            savedColour = GUI.color;
            float t = Mathf.Clamp01(age / PopSeconds);
            float pop = OutBack(t);
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t * 3f) * Mathf.Clamp01(remaining / FadeSeconds));
            GUI.matrix *= Matrix4x4.Translate(new Vector3(0f, -(1f - pop) * DropPixels, 0f));
            GUIUtility.ScaleAroundPivot(Vector2.one * Mathf.LerpUnclamped(StartScale, 1f, pop), rect.center);
        }

        public static void End()
        {
            GUI.matrix = savedMatrix;
            GUI.color = savedColour;
        }

        private static float OutBack(float t)
        {
            const float c = 1.70158f;
            float u = t - 1f;
            return 1f + (c + 1f) * u * u * u + c * u * u;
        }

        public static void Draw(Rect rect)
        {
            if (gradient == null) gradient = Build();
            GUI.DrawTexture(rect, gradient);
        }

        private static Texture2D Build()
        {
            var texture = new Texture2D(Width, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            for (int x = 0; x < Width; x++)
            {
                float t = (x + 0.5f) / Width;
                float edge = Mathf.Min(t, 1f - t) / EdgeFade;
                texture.SetPixel(x, 0, new Color(0f, 0f, 0f, Opacity * Mathf.SmoothStep(0f, 1f, edge)));
            }
            texture.Apply();
            return texture;
        }
    }
}
