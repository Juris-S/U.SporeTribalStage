using UnityEditor;
using UnityEngine;

namespace SporeTribal.EditorTools
{
    public static class CloudTextureBuilder
    {
        private const int Size = 256;
        private const int BasePeriod = 4;
        private const int Octaves = 5;
        private const int Seed = 11;
        private const float ShadowFloor = 0.35f;

        [MenuItem("Tools/Tribal/Build Cloud Texture")]
        public static void Build()
        {
            if (!EnvironmentBakeSettings.TryFind(out EnvironmentBakeSettings settings)) return;
            string path = AssetDatabase.GetAssetPath(settings.CloudShadows);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("EnvironmentBakeSettings has no Cloud Shadows texture to overwrite.", settings);
                return;
            }

            var shade = new float[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    shade[y * Size + x] = Shade(x / (float)Size, y / (float)Size);
            TextureBaking.SaveTileableGreyscale(path, shade, Size);
        }

        private static float Shade(float u, float v)
        {
            float n = 0f, amp = 0.5f;
            int period = BasePeriod;
            for (int octave = 0; octave < Octaves; octave++)
            {
                n += TileableNoise.Value(u * period, v * period, period, octave, Seed) * amp;
                period *= 2;
                amp *= 0.5f;
            }
            float cloud = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 0.6f, n));
            return Mathf.Lerp(1f, ShadowFloor, cloud);
        }
    }
}
