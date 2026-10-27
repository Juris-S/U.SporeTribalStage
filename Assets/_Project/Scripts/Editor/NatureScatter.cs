using System.Collections.Generic;
using System.IO;
using SporeTribal.Economy;
using SporeTribal.Rendering;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace SporeTribal.EditorTools
{
    public static class NatureScatter
    {
        private const string NatureFolder = "Assets/_Project/Art/KayKit/Nature/";
        private const string RootName = "Scenery";
        private const int Seed = 7;
        private const float HalfSize = 72f;
        private const float ForestEdge = 52f;
        private const int IgnoreRaycastLayer = 2;
        private const float GrassChunk = 24f;
        private const string GrassMeshFolder = "Assets/_Project/Art/Scenery";
        private const string GrassMaterialPath = "Assets/_Project/Materials/Environment/Grass.mat";
        private const string GroundMaterialPath = "Assets/_Project/Materials/Environment/Ground.mat";
        private const string TreeFoliagePath = "Assets/_Project/Materials/Environment/Foliage_Trees.mat";
        private const string BushFoliagePath = "Assets/_Project/Materials/Environment/Foliage_Bushes.mat";
        private const string MaskPath ="Assets/_Project/Materials/Environment/GroundMask.png";

        private const int MaskRes = 512;
        private const float MaskSize = 150f;
        private const float MaskOrigin = -75f;

        private const float BuildingClearance = 7f;
        private const float VillageClearance = 9f;
        private const float FoodClearance = 4f;
        private const float PathHalfWidth = 5f;
        private const float GrassClearanceTrim = -3.5f;

        private struct Keepout
        {
            public Vector2 Center;
            public float Radius;
        }

        private static readonly string[] Trees = { "Tree_1_A", "Tree_1_B", "Tree_1_C", "Tree_2_A", "Tree_2_B", "Tree_2_C", "Tree_3_A", "Tree_3_B", "Tree_3_C" };
        private static readonly string[] BareTrees = { "Tree_Bare_1_A", "Tree_Bare_1_B", "Tree_Bare_2_A" };
        private static readonly string[] Rocks = { "Rock_1_A", "Rock_1_B", "Rock_1_C", "Rock_2_A", "Rock_2_B", "Rock_3_A", "Rock_3_B", "Rock_3_C" };
        private static readonly string[] Bushes = { "Bush_3_A", "Bush_3_B" };
        private static readonly string[] Grass = { "Grass_1_A_Singlesided", "Grass_1_B_Singlesided", "Grass_2_A_Singlesided", "Grass_2_B_Singlesided", "Grass_1_A", "Grass_2_A", "Grass_1_B" };

        [MenuItem("Tools/Spore/Scatter Nature")]
        public static void Run()
        {
            var old = GameObject.Find(RootName);
            if (old != null) Undo.DestroyObjectImmediate(old);

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Scatter Nature");
            var keepouts = CollectKeepouts(out var villageCenters);
            var rng = new Random(Seed);
            var mask = BakeGroundMask(villageCenters, keepouts);
            var groundRenderer = GameObject.Find("Ground")?.GetComponent<MeshRenderer>();
            if (groundRenderer != null)
            {
                Undo.RecordObject(groundRenderer, "Scatter Nature");
                groundRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
                if (groundRenderer.GetComponent<GroundFootprintPainter>() == null)
                    Undo.AddComponent<GroundFootprintPainter>(groundRenderer.gameObject);
            }

            var forest = NewGroup(root, "Forest");
            var rocks = NewGroup(root, "Rocks");
            var bushes = NewGroup(root, "Bushes");
            var grass = NewGroup(root, "Grass");

            ScatterForest(forest, rng, keepouts);
            ScatterRocks(rocks, rng, keepouts, villageCenters);
            ScatterBushes(bushes, rng, keepouts, villageCenters);
            int tufts = ScatterGrass(grass, rng, keepouts, mask);
            root.AddComponent<GrassInteraction>().SetGrassRoot(grass.transform);

            foreach (var surface in Object.FindObjectsByType<NavMeshSurface>())
            {
                surface.layerMask = ~(1 << IgnoreRaycastLayer);
                surface.BuildNavMesh();
                SaveNavMeshAsset(surface);
                EditorUtility.SetDirty(surface);
            }

            Debug.Log($"Nature scattered: {forest.transform.childCount} trees, {rocks.transform.childCount} rocks, " +
                      $"{bushes.transform.childCount} bushes, {tufts} grass tufts in {grass.transform.childCount} chunks.");
        }

        private static void SaveNavMeshAsset(NavMeshSurface surface)
        {
            var scenePath = surface.gameObject.scene.path;
            if (string.IsNullOrEmpty(scenePath) || surface.navMeshData == null) return;
            var folder = Path.Combine(Path.GetDirectoryName(scenePath), Path.GetFileNameWithoutExtension(scenePath)).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(scenePath).Replace('\\', '/'), Path.GetFileNameWithoutExtension(scenePath));
            AssetDatabase.CreateAsset(surface.navMeshData, $"{folder}/NavMesh-{surface.name}.asset");
        }

        private static void ScatterForest(GameObject group, Random rng, List<Keepout> keepouts)
        {
            var treeMaterial = EnsureFoliageMaterial(TreeFoliagePath, 0.012f, 0.025f);
            for (int i = 0; i < 700; i++)
            {
                var p = RandomPoint(rng, HalfSize);
                float edge = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y));
                if (edge < ForestEdge || IsBlocked(p, keepouts, 1.5f)) continue;
                Place(group, rng.NextDouble() < 0.12 ? BareTrees : Trees, p, rng, 0.9f, 1.4f, treeMaterial);
            }

            for (int i = 0; i < 500; i++)
            {
                var p = RandomPoint(rng, ForestEdge);
                float n = Mathf.PerlinNoise(p.x * 0.05f + 40f, p.y * 0.05f + 40f);
                if (n < 0.68f || rng.NextDouble() > 0.45 || IsBlocked(p, keepouts, 2f)) continue;
                Place(group, Trees, p, rng, 0.8f, 1.2f, treeMaterial);
            }
        }

        private static void ScatterRocks(GameObject group, Random rng, List<Keepout> keepouts, List<Vector2> villages)
        {
            for (int i = 0; i < 90; i++)
            {
                var p = RandomPoint(rng, HalfSize - 4f);
                if (IsBlocked(p, keepouts, 1f)) continue;
                Place(group, Rocks, p, rng, 0.8f, 1.6f, null);
            }

            foreach (var v in villages)
                for (int i = 0; i < 14; i++)
                {
                    var p = v + RingOffset(rng, 17f, 28f);
                    if (IsBlocked(p, keepouts, 1f)) continue;
                    Place(group, Rocks, p, rng, 0.6f, 1.1f, null);
                }
        }

        private static void ScatterBushes(GameObject group, Random rng, List<Keepout> keepouts, List<Vector2> villages)
        {
            var bushMaterial = EnsureFoliageMaterial(BushFoliagePath, 0.07f, 0.015f);
            for (int i = 0; i < 160; i++)
            {
                var p = RandomPoint(rng, HalfSize - 2f);
                float n = Mathf.PerlinNoise(p.x * 0.08f + 7f, p.y * 0.08f + 19f);
                if (n < 0.55f || IsBlocked(p, keepouts, 1f)) continue;
                Place(group, Bushes, p, rng, 0.8f, 1.3f, bushMaterial);
            }

            foreach (var v in villages)
                for (int i = 0; i < 20; i++)
                {
                    var p = v + RingOffset(rng, 15f, 26f);
                    if (IsBlocked(p, keepouts, 1f)) continue;
                    Place(group, Bushes, p, rng, 0.7f, 1.1f, bushMaterial);
                }
        }

        private static int ScatterGrass(GameObject group, Random rng, List<Keepout> keepouts, GroundMask mask)
        {
            var material = EnsureGrassMaterial();
            var chunks = new Dictionary<Vector2Int, List<CombineInstance>>();
            int tufts = 0;

            for (int i = 0; i < 160000; i++)
            {
                var p = RandomPoint(rng, HalfSize);
                float density = GrassDensity(p, mask, keepouts);
                if (rng.NextDouble() > density) continue;

                string name = Grass[rng.Next(Grass.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureFolder}{name}_Color1.fbx");
                var filter = prefab != null ? prefab.GetComponentInChildren<MeshFilter>() : null;
                if (filter == null) continue;

                float scale = Mathf.Lerp(0.55f, 1.35f, density) * Mathf.Lerp(0.85f, 1.15f, (float)rng.NextDouble());
                var rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                var placed = Matrix4x4.TRS(new Vector3(p.x, 0f, p.y), rotation, Vector3.one * scale);
                var local = prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;

                var key = new Vector2Int(Mathf.FloorToInt(p.x / GrassChunk), Mathf.FloorToInt(p.y / GrassChunk));
                if (!chunks.TryGetValue(key, out var list)) chunks[key] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = filter.sharedMesh, transform = placed * local });
                tufts++;
            }

            AssetDatabase.DeleteAsset(GrassMeshFolder);
            AssetDatabase.CreateFolder("Assets/_Project/Art", "Scenery");
            foreach (var kv in chunks)
            {
                var mesh = new Mesh { name = $"GrassChunk_{kv.Key.x}_{kv.Key.y}", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(kv.Value.ToArray(), true, true);
                mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, $"{GrassMeshFolder}/{mesh.name}.asset");

                var go = new GameObject(mesh.name);
                go.transform.SetParent(group.transform, false);
                go.layer = IgnoreRaycastLayer;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = material;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            AssetDatabase.SaveAssets();
            return tufts;
        }

        private static float GrassDensity(Vector2 p, GroundMask mask, List<Keepout> keepouts)
        {
            if (GroundFootprint.ClearingAtPoint(p) > 0.05f || mask.Sample(mask.Trail, p) > 0.15f) return 0f;
            if (IsBlocked(p, keepouts, GrassClearanceTrim)) return 0f;

            float clumps = Mathf.PerlinNoise(p.x * 0.24f + 100f, p.y * 0.24f + 100f);
            float detail = Mathf.PerlinNoise(p.x * 0.9f + 50f, p.y * 0.9f + 50f);
            float regions = Mathf.PerlinNoise(p.x * 0.05f + 300f, p.y * 0.05f + 300f);
            float n = clumps * 0.8f + detail * 0.2f + (regions - 0.5f) * 0.35f;
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.56f, 0.7f, n));
        }

        private sealed class GroundMask
        {
            public readonly float[] Trail = new float[MaskRes * MaskRes];
            public readonly float[] Cover = new float[MaskRes * MaskRes];

            public float Sample(float[] channel, Vector2 p)
            {
                int x = Mathf.Clamp(Mathf.FloorToInt((p.x - MaskOrigin) / MaskSize * MaskRes), 0, MaskRes - 1);
                int y = Mathf.Clamp(Mathf.FloorToInt((p.y - MaskOrigin) / MaskSize * MaskRes), 0, MaskRes - 1);
                return channel[y * MaskRes + x];
            }
        }

        private static GroundMask BakeGroundMask(List<Vector2> centers, List<Keepout> keepouts)
        {
            var mask = new GroundMask();

            if (centers.Count == 2)
            {
                const int steps = 40;
                Vector2 a = centers[0], b = centers[1];
                for (int i = 0; i < steps; i++)
                {
                    float t0 = i / (float)steps, t1 = (i + 1) / (float)steps;
                    float gap = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 0.6f, Mathf.Abs((t0 + t1) - 1f)));
                    if (gap <= 0.01f) continue;
                    StampSegment(mask.Trail, WobblePoint(a, b, t0, 1.8f), WobblePoint(a, b, t1, 1.8f), 0.9f, 2.4f, 0.55f * gap);
                }
            }

            BakeCover(mask, keepouts);
            SaveMask(mask);
            return mask;
        }

        private static void BakeCover(GroundMask mask, List<Keepout> keepouts)
        {
            float texel = MaskSize / MaskRes;
            var raw = new float[MaskRes * MaskRes];
            for (int y = 0; y < MaskRes; y++)
                for (int x = 0; x < MaskRes; x++)
                {
                    var p = new Vector2(MaskOrigin + (x + 0.5f) * texel, MaskOrigin + (y + 0.5f) * texel);
                    raw[y * MaskRes + x] = GrassDensity(p, mask, keepouts);
                }

            const int radius = 3;
            var tmp = new float[raw.Length];
            for (int y = 0; y < MaskRes; y++)
                for (int x = 0; x < MaskRes; x++)
                {
                    float sum = 0f;
                    for (int k = -radius; k <= radius; k++) sum += raw[y * MaskRes + Mathf.Clamp(x + k, 0, MaskRes - 1)];
                    tmp[y * MaskRes + x] = sum / (radius * 2 + 1);
                }
            for (int y = 0; y < MaskRes; y++)
                for (int x = 0; x < MaskRes; x++)
                {
                    float sum = 0f;
                    for (int k = -radius; k <= radius; k++) sum += tmp[Mathf.Clamp(y + k, 0, MaskRes - 1) * MaskRes + x];
                    mask.Cover[y * MaskRes + x] = Mathf.Clamp01(sum / (radius * 2 + 1) * 1.3f);
                }
        }

        private static Vector2 WobblePoint(Vector2 a, Vector2 b, float t, float amount)
        {
            var dir = (b - a).normalized;
            var side = new Vector2(-dir.y, dir.x);
            float offset = Mathf.Sin(t * 9f) * amount + Mathf.Sin(t * 23f + 1.3f) * amount * 0.35f;
            return Vector2.Lerp(a, b, t) + side * offset * Mathf.Sin(t * Mathf.PI);
        }

        private static void StampSegment(float[] channel, Vector2 a, Vector2 b, float core, float edge, float strength)
        {
            float texel = MaskSize / MaskRes;
            int x0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, b.x) - edge - MaskOrigin) / texel));
            int x1 = Mathf.Min(MaskRes - 1, Mathf.CeilToInt((Mathf.Max(a.x, b.x) + edge - MaskOrigin) / texel));
            int y0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.y, b.y) - edge - MaskOrigin) / texel));
            int y1 = Mathf.Min(MaskRes - 1, Mathf.CeilToInt((Mathf.Max(a.y, b.y) + edge - MaskOrigin) / texel));
            var ab = b - a;
            float len2 = Mathf.Max(ab.sqrMagnitude, 1e-6f);

            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    var p = new Vector2(MaskOrigin + (x + 0.5f) * texel, MaskOrigin + (y + 0.5f) * texel);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                    float d = Vector2.Distance(p, a + ab * t);
                    float v = strength * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(core, edge, d)));
                    int idx = y * MaskRes + x;
                    if (v > channel[idx]) channel[idx] = v;
                }
        }

        private static void SaveMask(GroundMask mask)
        {
            var pixels = new Color32[MaskRes * MaskRes];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(0, (byte)(mask.Trail[i] * 255f), 0, (byte)(mask.Cover[i] * 255f));

            var tex = new Texture2D(MaskRes, MaskRes, TextureFormat.RGBA32, false, true);
            tex.SetPixels32(pixels);
            System.IO.File.WriteAllBytes(MaskPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(MaskPath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(MaskPath);
            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();

            var ground = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            if (ground == null)
            {
                ground = new Material(Shader.Find("Project/Terrain")) { name = "Ground" };
                AssetDatabase.CreateAsset(ground, GroundMaterialPath);
            }
            ground.SetTexture("_MaskMap", AssetDatabase.LoadAssetAtPath<Texture2D>(MaskPath));
            ground.SetVector("_MaskRect", new Vector4(MaskOrigin, MaskOrigin, MaskSize, 0f));
            EditorUtility.SetDirty(ground);
        }

        private static Material EnsureGrassMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(GrassMaterialPath);
            if (mat != null) return mat;

            mat = new Material(Shader.Find("Project/BentGrass")) { name = "Grass" };
            var forest = AssetDatabase.LoadAssetAtPath<Material>(NatureFolder + "KayKit_Forest.mat");
            mat.SetTexture("_BaseMap", forest.GetTexture("_BaseMap"));
            AssetDatabase.CreateAsset(mat, GrassMaterialPath);
            return mat;
        }

        private static List<Keepout> CollectKeepouts(out List<Vector2> villageCenters)
        {
            var list = new List<Keepout>();
            villageCenters = new List<Vector2>();

            foreach (var rootName in new[] { "Village Buildings", "Enemy Village" })
            {
                var root = GameObject.Find(rootName);
                if (root == null) continue;
                foreach (Transform child in root.transform)
                    list.Add(new Keepout { Center = Flat(child.position), Radius = BuildingClearance });
            }

            foreach (var village in Object.FindObjectsByType<Village>())
                list.Add(new Keepout { Center = Flat(village.transform.position), Radius = VillageClearance });

            foreach (var footprint in GroundFootprint.All)
                list.Add(new Keepout { Center = footprint.Position, Radius = footprint.Reach });

            foreach (var food in Object.FindObjectsByType<FoodNode>())
                list.Add(new Keepout { Center = Flat(food.transform.position), Radius = FoodClearance });

            villageCenters.Add(Centroid(GameObject.Find("Village Buildings")));
            villageCenters.Add(Centroid(GameObject.Find("Enemy Village")));

            if (villageCenters.Count == 2)
            {
                var a = villageCenters[0];
                var b = villageCenters[1];
                int steps = Mathf.CeilToInt(Vector2.Distance(a, b) / PathHalfWidth);
                for (int i = 0; i <= steps; i++)
                    list.Add(new Keepout { Center = Vector2.Lerp(a, b, i / (float)steps), Radius = PathHalfWidth });
            }

            return list;
        }

        private static Vector2 Centroid(GameObject root)
        {
            if (root == null || root.transform.childCount == 0) return Vector2.zero;
            var sum = Vector2.zero;
            foreach (Transform child in root.transform) sum += Flat(child.position);
            return sum / root.transform.childCount;
        }

        private static bool IsBlocked(Vector2 p, List<Keepout> keepouts, float margin)
        {
            foreach (var k in keepouts)
                if ((p - k.Center).sqrMagnitude < (k.Radius + margin) * (k.Radius + margin))
                    return true;
            return false;
        }

        private static Material EnsureFoliageMaterial(string path, float flex, float flutter)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(AssetDatabase.LoadAssetAtPath<Material>(NatureFolder + "KayKit_Forest.mat"));
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetFloat("_Wind", 1f);
            mat.EnableKeyword("_WIND");
            mat.SetFloat("_WindFlex", flex);
            mat.SetFloat("_WindFlutter", flutter);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void Place(GameObject group, string[] names, Vector2 p, Random rng, float minScale, float maxScale, Material material)
        {
            string name = names[rng.Next(names.Length)];
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{NatureFolder}{name}_Color1.fbx");
            if (prefab == null)
            {
                Debug.LogWarning($"Missing nature model {name}");
                return;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group.transform);
            go.transform.position = new Vector3(p.x, 0f, p.y);
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            go.transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, (float)rng.NextDouble());
            go.isStatic = true;
            if (material == null) return;
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = material;
                r.sharedMaterials = mats;
            }
        }

        private static GameObject NewGroup(GameObject root, string name)
        {
            var g = new GameObject(name);
            g.transform.SetParent(root.transform, false);
            g.isStatic = true;
            return g;
        }

        private static Vector2 RandomPoint(Random rng, float half) =>
            new Vector2((float)(rng.NextDouble() * 2 - 1) * half, (float)(rng.NextDouble() * 2 - 1) * half);

        private static Vector2 RingOffset(Random rng, float min, float max)
        {
            float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
            float r = Mathf.Lerp(min, max, (float)rng.NextDouble());
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
        }

        private static Vector2 Flat(Vector3 v) => new Vector2(v.x, v.z);
    }
}
