using UnityEditor;
using UnityEngine;

namespace SporeTribal.EditorTools
{
    public static class CloudTextureBuilder
    {
        private const string TexturePath = "Assets/_Project/Art/Textures/CloudShadows.png";
        private const int Size = 256;
        private const int BasePeriod = 4;
        private const int Seed = 11;

        private const float ShadowFloor = 0.35f;

        [MenuItem("Tools/Spore/Build Cloud Texture")]
        public static Texture2D Build()
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size, v = y / (float)Size;
                    float n = 0f, amp = 0.5f;
                    int period = BasePeriod;
                    for (int o = 0; o < 5; o++)
                    {
                        n += PeriodicNoise(u * period, v * period, period, o) * amp;
                        period *= 2;
                        amp *= 0.5f;
                    }

                    float cloud = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 0.6f, n));
                    byte b = (byte)(Mathf.Lerp(1f, ShadowFloor, cloud) * 255f);
                    pixels[y * Size + x] = new Color32(b, b, b, 255);
                }

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
            tex.SetPixels32(pixels);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(TexturePath));
            System.IO.File.WriteAllBytes(TexturePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }

        private static float PeriodicNoise(float x, float y, int period, int octave)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);

            float a = Hash(x0, y0, period, octave);
            float b = Hash(x0 + 1, y0, period, octave);
            float c = Hash(x0, y0 + 1, period, octave);
            float d = Hash(x0 + 1, y0 + 1, period, octave);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static float Hash(int x, int y, int period, int octave)
        {
            x = ((x % period) + period) % period;
            y = ((y % period) + period) % period;
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + octave * 2147483647 + Seed * 144665);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }
    }
}
