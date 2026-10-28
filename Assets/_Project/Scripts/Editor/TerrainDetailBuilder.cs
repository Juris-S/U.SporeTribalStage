using UnityEditor;
using UnityEngine;

namespace SporeTribal.EditorTools
{
    public static class TerrainDetailBuilder
    {
        private const int Size = 512;

        [MenuItem("Tools/Tribal/Build Terrain Detail Textures")]
        public static void BuildAll()
        {
            if (!EnvironmentBakeSettings.TryFind(out EnvironmentBakeSettings settings)) return;
            string folder = settings.TerrainDetailFolder;
            if (string.IsNullOrEmpty(folder))
            {
                Debug.LogError("EnvironmentBakeSettings has no Terrain Detail Folder.", settings);
                return;
            }

            Save(folder, "TerrainDetail_Grass", Normalise(Grass(), 0.51f, 0.03f));
            Save(folder, "TerrainDetail_Ground", Normalise(Ground(), 0.48f, 0.1f));
            Save(folder, "TerrainDetail_Sand", Normalise(Sand(), 0.49f, 0.05f));
            Save(folder, "TerrainDetail_Gravel", Normalise(Gravel(), 0.49f, 0.11f));
            Save(folder, "TerrainDetail_Cliff", Normalise(Cliff(), 0.55f, 0.11f));
        }

        private static float[] Grass()
        {
            var h = Field((u, v) => Fbm(u, v, 64, 3, 0.6f, 1));
            return AddGrain(Blend(h, Emboss(h), 0.6f), 0.35f, 20);
        }

        private static float[] Ground()
        {
            var h = Field((u, v) => Fbm(u, v, 16, 5, 0.75f, 2) + Fbm(u, v, 4, 3, 0.5f, 3) * 0.08f);
            return AddGrain(Blend(h, Emboss(h), 0.7f), 0.45f, 21);
        }

        private static float[] Sand()
        {
            var h = Field((u, v) => Fbm(u, v, 32, 4, 0.6f, 4) + Fbm(u, v, 4, 3, 0.5f, 10) * 0.06f);
            return AddGrain(Blend(h, Emboss(h), 0.6f), 0.3f, 22);
        }

        private static float[] Gravel()
        {
            var blotch = Normalise(Field((u, v) => Fbm(u, v, 4, 7, 0.78f, 5)), 0f, 1f, false);
            var grit = Field((u, v) => Fbm(u, v, 32, 4, 0.7f, 6));
            grit = Normalise(Blend(grit, Emboss(grit), 0.5f), 0f, 1f, false);
            var cracks = Field((u, v) => CrackLine(u, v, 8, 7, 0.06f) * BrokenMask(u, v, 11));
            var o = new float[Size * Size];
            for (int i = 0; i < o.Length; i++)
                o[i] = blotch[i] * 0.6f + grit[i] - cracks[i] * 1.6f;
            return AddGrain(o, 0.45f, 23);
        }

        private static float[] Cliff()
        {
            var patches = Normalise(Field((u, v) => Fbm(u, v, 8, 6, 0.82f, 8) + Fbm(u, v, 2, 2, 0.5f, 15) * 0.1f), 0f, 1f, false);
            var cracks = Field((u, v) =>
            {
                float big = BrokenMask(u, v, 12), small = BrokenMask(u, v, 14);
                return Mathf.Max(
                    Mathf.Max(CrackLine(u, v, 6, 9, 0.06f), CrackLine(u, v, 6, 9, 0.25f) * 0.35f) * big,
                    Mathf.Max(CrackLine(u, v, 12, 13, 0.05f), CrackLine(u, v, 12, 13, 0.2f) * 0.3f) * small * 0.7f);
            });
            var o = new float[Size * Size];
            for (int i = 0; i < o.Length; i++)
                o[i] = patches[i] - cracks[i] * 2f;
            return AddGrain(o, 0.25f, 24);
        }

        private static float[] AddGrain(float[] a, float amount, int seed)
        {
            a = Normalise(a, 0f, 1f, false);
            var o = new float[a.Length];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    o[y * Size + x] = a[y * Size + x] + (TileableNoise.Hash(x, y, Size, seed) - 0.5f) * 3.46f * amount;
            return o;
        }

        private static float BrokenMask(float u, float v, int seed)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.58f, Fbm(u, v, 4, 3, 0.5f, seed)));
        }

        private static float[] Field(System.Func<float, float, float> f)
        {
            var o = new float[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    o[y * Size + x] = f(x / (float)Size, y / (float)Size);
            return o;
        }

        private static float[] Blend(float[] a, float[] b, float t)
        {
            a = Normalise(a, 0f, 1f, false);
            b = Normalise(b, 0f, 1f, false);
            var o = new float[a.Length];
            for (int i = 0; i < o.Length; i++) o[i] = Mathf.Lerp(a[i], b[i], t);
            return o;
        }

        private static float[] Emboss(float[] h)
        {
            var o = new float[h.Length];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    int x0 = (x - 1 + Size) % Size, x1 = (x + 1) % Size;
                    int y0 = (y - 1 + Size) % Size, y1 = (y + 1) % Size;
                    o[y * Size + x] = h[y1 * Size + x0] - h[y0 * Size + x1];
                }
            return o;
        }

        private static float[] Normalise(float[] a, float mean, float std, bool clamp = true)
        {
            double sum = 0, sq = 0;
            foreach (var v in a) { sum += v; sq += v * v; }
            float m = (float)(sum / a.Length);
            float s = Mathf.Sqrt(Mathf.Max(1e-8f, (float)(sq / a.Length) - m * m));
            var o = new float[a.Length];
            for (int i = 0; i < o.Length; i++)
            {
                float v = mean + (a[i] - m) / s * std;
                o[i] = clamp ? Mathf.Clamp01(v) : v;
            }
            return o;
        }

        private static void Save(string folder, string name, float[] a) =>
            TextureBaking.SaveTileableGreyscale($"{folder}/{name}.png", a, Size);

        private static float Fbm(float u, float v, int basePeriod, int octaves, float gain, int seed)
        {
            float n = 0f, amp = 0.5f, total = 0f;
            int period = basePeriod;
            for (int o = 0; o < octaves; o++)
            {
                n += TileableNoise.Value(u * period, v * period, period, seed * 16 + o) * amp;
                total += amp;
                period *= 2;
                amp *= gain;
            }
            return n / total;
        }

        private static float CrackLine(float u, float v, int cells, int seed, float width)
        {
            float wu = u + (Fbm(u, v, 6, 3, 0.5f, seed + 40) - 0.5f) * 0.12f;
            float wv = v + (Fbm(u, v, 6, 3, 0.5f, seed + 41) - 0.5f) * 0.12f;
            float px = wu * cells, py = wv * cells;
            int cx = Mathf.FloorToInt(px), cy = Mathf.FloorToInt(py);
            float f1 = 8f, f2 = 8f;
            for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    float fx = cx + i + TileableNoise.Hash(cx + i, cy + j, cells, seed) - px;
                    float fy = cy + j + TileableNoise.Hash(cx + i, cy + j, cells, seed + 1) - py;
                    float d = Mathf.Sqrt(fx * fx + fy * fy);
                    if (d < f1) { f2 = f1; f1 = d; }
                    else if (d < f2) f2 = d;
                }
            return 1f - Mathf.SmoothStep(0f, 1f, (f2 - f1) / width);
        }
    }
}
