using System.Collections.Generic;
using System.IO;
using SporeTribal.Buildings;
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
        private const string RootName = "Scenery";
        private const int IgnoreRaycastLayer = 2;

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MaskMapId = Shader.PropertyToID("_MaskMap");
        private static readonly int MaskRectId = Shader.PropertyToID("_MaskRect");
        private static readonly int WindId = Shader.PropertyToID("_Wind");
        private static readonly int WindFlexId = Shader.PropertyToID("_WindFlex");
        private static readonly int WindFlutterId = Shader.PropertyToID("_WindFlutter");

        private struct Keepout
        {
            public Vector2 Center;
            public float Radius;
        }

        private sealed class GroundMask
        {
            public readonly float[] Trail;
            public readonly float[] Cover;
            private readonly EnvironmentBakeSettings settings;

            public GroundMask(EnvironmentBakeSettings settings)
            {
                this.settings = settings;
                int texels = settings.MaskResolution * settings.MaskResolution;
                Trail = new float[texels];
                Cover = new float[texels];
            }

            public float Sample(float[] channel, Vector2 p)
            {
                int res = settings.MaskResolution;
                int x = Mathf.Clamp(Mathf.FloorToInt((p.x - settings.MaskOrigin) / settings.MaskSize * res), 0, res - 1);
                int y = Mathf.Clamp(Mathf.FloorToInt((p.y - settings.MaskOrigin) / settings.MaskSize * res), 0, res - 1);
                return channel[y * res + x];
            }
        }

        private static EnvironmentBakeSettings settings;

        [MenuItem("Tools/Tribal/Scatter Nature")]
        public static void Run()
        {
            if (!EnvironmentBakeSettings.TryFind(out settings) || !HasRequiredAssets() || !TerrainHills.Build(settings)) return;

            GrassInteraction old = Object.FindAnyObjectByType<GrassInteraction>();
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Scatter Nature");
            var keepouts = CollectKeepouts(out var villageCenters);
            var rng = new Random(settings.Seed);
            var mask = BakeGroundMask(villageCenters, keepouts);
            AssignGroundMaterial();

            var forest = NewGroup(root, "Forest");
            var rocks = NewGroup(root, "Rocks");
            var bushes = NewGroup(root, "Bushes");
            var grass = NewGroup(root, "Grass");

            ScatterForest(forest, rng, keepouts);
            ScatterRocks(rocks, rng, keepouts, villageCenters);
            ScatterBushes(bushes, rng, keepouts, villageCenters);
            int tufts = ScatterGrass(grass, rng, keepouts, mask);
            root.AddComponent<GrassInteraction>().SetGrassRoot(grass.transform);

            RebakeNavMesh();

            Debug.Log($"Nature scattered: {forest.transform.childCount} trees, {rocks.transform.childCount} rocks, " +
                      $"{bushes.transform.childCount} bushes, {tufts} grass tufts in {grass.transform.childCount} chunks.");
        }

        private static bool HasRequiredAssets()
        {
            bool ok = settings.ForestMaterial != null && settings.GroundMaterial != null && settings.GrassMaterial != null
                && settings.TreeFoliage != null && settings.BushFoliage != null && settings.GroundMask != null
                && !string.IsNullOrEmpty(settings.GrassMeshFolder);
            if (!ok) Debug.LogError("EnvironmentBakeSettings is missing a material, the ground mask or the grass mesh folder.", settings);
            return ok;
        }

        private static void AssignGroundMaterial()
        {
            GroundFootprintPainter painter = Object.FindAnyObjectByType<GroundFootprintPainter>();
            if (painter == null || !painter.TryGetComponent(out MeshRenderer groundRenderer))
            {
                Debug.LogWarning("No ground with a GroundFootprintPainter in the scene: the ground material was not assigned.");
                return;
            }
            Undo.RecordObject(groundRenderer, "Scatter Nature");
            groundRenderer.sharedMaterial = settings.GroundMaterial;
        }

        private static void RebakeNavMesh()
        {
            foreach (var surface in Object.FindObjectsByType<NavMeshSurface>())
            {
                surface.layerMask = ~(1 << IgnoreRaycastLayer);
                surface.BuildNavMesh();
                SaveNavMeshAsset(surface);
                EditorUtility.SetDirty(surface);
            }
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
            var treeMaterial = PrepareFoliage(settings.TreeFoliage, 0.012f, 0.025f);
            for (int i = 0; i < 700; i++)
            {
                var p = RandomPoint(rng, settings.HalfSize);
                float edge = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y));
                if (edge < settings.ForestEdge || IsBlocked(p, keepouts, 1.5f)) continue;
                Place(group, rng.NextDouble() < 0.12 ? settings.BareTrees : settings.Trees, p, rng, 0.9f, 1.4f, treeMaterial);
            }

            for (int i = 0; i < 500; i++)
            {
                var p = RandomPoint(rng, settings.ForestEdge);
                float n = Mathf.PerlinNoise(p.x * 0.05f + 40f, p.y * 0.05f + 40f);
                if (n < 0.68f || rng.NextDouble() > 0.45 || IsBlocked(p, keepouts, 2f)) continue;
                Place(group, settings.Trees, p, rng, 0.8f, 1.2f, treeMaterial);
            }
        }

        private static void ScatterRocks(GameObject group, Random rng, List<Keepout> keepouts, List<Vector2> villages)
        {
            for (int i = 0; i < 90; i++)
            {
                var p = RandomPoint(rng, settings.HalfSize - 4f);
                if (IsBlocked(p, keepouts, 1f)) continue;
                Place(group, settings.Rocks, p, rng, 0.8f, 1.6f, null);
            }

            foreach (var v in villages)
                for (int i = 0; i < 14; i++)
                {
                    var p = v + RingOffset(rng, 17f, 28f);
                    if (IsBlocked(p, keepouts, 1f)) continue;
                    Place(group, settings.Rocks, p, rng, 0.6f, 1.1f, null);
                }
        }

        private static void ScatterBushes(GameObject group, Random rng, List<Keepout> keepouts, List<Vector2> villages)
        {
            var bushMaterial = PrepareFoliage(settings.BushFoliage, 0.07f, 0.015f);
            for (int i = 0; i < 160; i++)
            {
                var p = RandomPoint(rng, settings.HalfSize - 2f);
                float n = Mathf.PerlinNoise(p.x * 0.08f + 7f, p.y * 0.08f + 19f);
                if (n < 0.55f || IsBlocked(p, keepouts, 1f)) continue;
                Place(group, settings.Bushes, p, rng, 0.8f, 1.3f, bushMaterial, settings.BushSink);
            }

            foreach (var v in villages)
                for (int i = 0; i < 20; i++)
                {
                    var p = v + RingOffset(rng, 15f, 26f);
                    if (IsBlocked(p, keepouts, 1f)) continue;
                    Place(group, settings.Bushes, p, rng, 0.7f, 1.1f, bushMaterial, settings.BushSink);
                }
        }

        private static int ScatterGrass(GameObject group, Random rng, List<Keepout> keepouts, GroundMask mask)
        {
            var material = PrepareGrassMaterial();
            var tuftFilters = TuftFilters();
            var chunks = new Dictionary<Vector2Int, List<CombineInstance>>();
            int tufts = 0;

            for (int i = 0; i < 160000; i++)
            {
                var p = RandomPoint(rng, settings.HalfSize);
                float density = GrassDensity(p, mask, keepouts);
                if (rng.NextDouble() > density) continue;

                int pick = rng.Next(tuftFilters.Count);
                var filter = tuftFilters[pick];
                if (filter == null) continue;
                var prefab = settings.GrassTufts[pick];

                float scale = Mathf.Lerp(0.55f, 1.35f, density) * Mathf.Lerp(0.85f, 1.15f, (float)rng.NextDouble());
                var rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                var placed = Matrix4x4.TRS(new Vector3(p.x, GroundY(p), p.y), rotation, Vector3.one * scale);
                var local = prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;

                var key = new Vector2Int(Mathf.FloorToInt(p.x / settings.GrassChunk), Mathf.FloorToInt(p.y / settings.GrassChunk));
                if (!chunks.TryGetValue(key, out var list)) chunks[key] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = filter.sharedMesh, transform = placed * local });
                tufts++;
            }

            ClearGrassMeshFolder();
            foreach (var kv in chunks) CreateGrassChunk(group, material, $"GrassChunk_{kv.Key.x}_{kv.Key.y}", kv.Value);

            AssetDatabase.SaveAssets();
            return tufts;
        }

        private static List<MeshFilter> TuftFilters()
        {
            var filters = new List<MeshFilter>();
            foreach (GameObject tuft in settings.GrassTufts)
                filters.Add(tuft != null ? tuft.GetComponentInChildren<MeshFilter>() : null);
            return filters;
        }

        private static void ClearGrassMeshFolder()
        {
            string folder = settings.GrassMeshFolder;
            foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { folder }))
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static void CreateGrassChunk(GameObject group, Material material, string name, List<CombineInstance> tufts)
        {
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(tufts.ToArray(), true, true);
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, $"{settings.GrassMeshFolder}/{mesh.name}.asset");

            var go = new GameObject(mesh.name);
            go.transform.SetParent(group.transform, false);
            go.layer = IgnoreRaycastLayer;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static float GrassDensity(Vector2 p, GroundMask mask, List<Keepout> keepouts)
        {
            if (GroundFootprint.ClearingAtPoint(p) > 0.05f || mask.Sample(mask.Trail, p) > 0.15f) return 0f;
            if (IsBlocked(p, keepouts, settings.GrassClearanceTrim)) return 0f;

            float clumps = Mathf.PerlinNoise(p.x * 0.24f + 100f, p.y * 0.24f + 100f);
            float detail = Mathf.PerlinNoise(p.x * 0.9f + 50f, p.y * 0.9f + 50f);
            float regions = Mathf.PerlinNoise(p.x * 0.05f + 300f, p.y * 0.05f + 300f);
            float n = clumps * 0.8f + detail * 0.2f + (regions - 0.5f) * 0.35f;
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.56f, 0.7f, n));
        }

        private static GroundMask BakeGroundMask(List<Vector2> centers, List<Keepout> keepouts)
        {
            var mask = new GroundMask(settings);

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
            int res = settings.MaskResolution;
            float texel = settings.MaskSize / res;
            var raw = new float[res * res];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    var p = new Vector2(settings.MaskOrigin + (x + 0.5f) * texel, settings.MaskOrigin + (y + 0.5f) * texel);
                    raw[y * res + x] = GrassDensity(p, mask, keepouts);
                }

            const int radius = 3;
            var tmp = new float[raw.Length];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float sum = 0f;
                    for (int k = -radius; k <= radius; k++) sum += raw[y * res + Mathf.Clamp(x + k, 0, res - 1)];
                    tmp[y * res + x] = sum / (radius * 2 + 1);
                }
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float sum = 0f;
                    for (int k = -radius; k <= radius; k++) sum += tmp[Mathf.Clamp(y + k, 0, res - 1) * res + x];
                    mask.Cover[y * res + x] = Mathf.Clamp01(sum / (radius * 2 + 1) * 1.3f);
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
            int res = settings.MaskResolution;
            float origin = settings.MaskOrigin;
            float texel = settings.MaskSize / res;
            int x0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, b.x) - edge - origin) / texel));
            int x1 = Mathf.Min(res - 1, Mathf.CeilToInt((Mathf.Max(a.x, b.x) + edge - origin) / texel));
            int y0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.y, b.y) - edge - origin) / texel));
            int y1 = Mathf.Min(res - 1, Mathf.CeilToInt((Mathf.Max(a.y, b.y) + edge - origin) / texel));
            var ab = b - a;
            float len2 = Mathf.Max(ab.sqrMagnitude, 1e-6f);

            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    var p = new Vector2(origin + (x + 0.5f) * texel, origin + (y + 0.5f) * texel);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                    float d = Vector2.Distance(p, a + ab * t);
                    float v = strength * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(core, edge, d)));
                    int idx = y * res + x;
                    if (v > channel[idx]) channel[idx] = v;
                }
        }

        private static void SaveMask(GroundMask mask)
        {
            int res = settings.MaskResolution;
            var pixels = new Color32[res * res];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(0, (byte)(mask.Trail[i] * 255f), 0, (byte)(mask.Cover[i] * 255f));

            string maskPath = AssetDatabase.GetAssetPath(settings.GroundMask);
            TextureBaking.WritePng(maskPath, pixels, res);

            var importer = (TextureImporter)AssetImporter.GetAtPath(maskPath);
            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();

            Material ground = settings.GroundMaterial;
            ground.SetTexture(MaskMapId, settings.GroundMask);
            ground.SetVector(MaskRectId, new Vector4(settings.MaskOrigin, settings.MaskOrigin, settings.MaskSize, 0f));
            EditorUtility.SetDirty(ground);
        }

        private static Material PrepareGrassMaterial()
        {
            Material grass = settings.GrassMaterial;
            if (grass.GetTexture(BaseMapId) == null)
            {
                grass.SetTexture(BaseMapId, settings.ForestMaterial.GetTexture(BaseMapId));
                EditorUtility.SetDirty(grass);
            }
            return grass;
        }

        private static Material PrepareFoliage(Material foliage, float flex, float flutter)
        {
            foliage.SetFloat(WindId, 1f);
            foliage.EnableKeyword("_WIND");
            foliage.SetFloat(WindFlexId, flex);
            foliage.SetFloat(WindFlutterId, flutter);
            EditorUtility.SetDirty(foliage);
            return foliage;
        }

        private static List<Keepout> CollectKeepouts(out List<Vector2> villageCenters)
        {
            var list = new List<Keepout>();
            villageCenters = new List<Vector2>();

            VillageLayout[] layouts = Object.FindObjectsByType<VillageLayout>();
            System.Array.Sort(layouts, (a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (VillageLayout layout in layouts)
            {
                foreach (Transform piece in layout.Pieces)
                    list.Add(new Keepout { Center = Flat(piece.position), Radius = settings.BuildingClearance });
                villageCenters.Add(Centroid(layout));
            }

            foreach (var village in Object.FindObjectsByType<Village>())
                list.Add(new Keepout { Center = Flat(village.transform.position), Radius = settings.VillageClearance });

            foreach (var footprint in GroundFootprint.All)
                list.Add(new Keepout { Center = footprint.Position, Radius = footprint.Reach });

            foreach (var food in Object.FindObjectsByType<FoodNode>())
                list.Add(new Keepout { Center = Flat(food.transform.position), Radius = settings.FoodClearance });

            if (villageCenters.Count == 2) AddPathKeepouts(list, villageCenters[0], villageCenters[1]);
            return list;
        }

        private static void AddPathKeepouts(List<Keepout> list, Vector2 a, Vector2 b)
        {
            float halfWidth = settings.PathHalfWidth;
            int steps = Mathf.CeilToInt(Vector2.Distance(a, b) / halfWidth);
            for (int i = 0; i <= steps; i++)
                list.Add(new Keepout { Center = Vector2.Lerp(a, b, i / (float)steps), Radius = halfWidth });
        }

        private static Vector2 Centroid(VillageLayout layout)
        {
            var sum = Vector2.zero;
            int count = 0;
            foreach (Transform piece in layout.Pieces)
            {
                sum += Flat(piece.position);
                count++;
            }
            return count > 0 ? sum / count : Flat(layout.Centre);
        }

        private static bool IsBlocked(Vector2 p, List<Keepout> keepouts, float margin)
        {
            foreach (var k in keepouts)
                if ((p - k.Center).sqrMagnitude < (k.Radius + margin) * (k.Radius + margin))
                    return true;
            return false;
        }

        private static float GroundY(Vector2 p) => TerrainHills.HeightAt(p);

        private static void Place(GameObject group, IReadOnlyList<GameObject> models, Vector2 p, Random rng, float minScale, float maxScale, Material material, float sink = 0f)
        {
            var prefab = models[rng.Next(models.Count)];
            if (prefab == null)
            {
                Debug.LogWarning("Empty slot in an EnvironmentBakeSettings model list.", settings);
                return;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group.transform);
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            go.transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, (float)rng.NextDouble());
            go.transform.position = new Vector3(p.x, GroundY(p) - sink * go.transform.localScale.y, p.y);
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
