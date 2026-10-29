using System.Collections.Generic;
using SporeTribal.Buildings;
using SporeTribal.Economy;
using SporeTribal.Rendering;
using UnityEditor;
using UnityEngine;

namespace SporeTribal.EditorTools
{
    public static class TerrainHills
    {
        private const string HillsMeshPath = "Assets/_Project/Art/GroundPlane_Hills.asset";
        private const float BaseGroundWidth = 10f;
        private const float CellSize = 1f;

        private static EnvironmentBakeSettings active;
        private static List<FlatZone> activeZones;
        private static Transform grid;
        private static float[] gridHeights;
        private static int gridX;
        private static int gridZ;

        private struct FlatZone
        {
            public Vector2 Center;
            public float Radius;
        }

        [MenuItem("Tools/Tribal/Build Terrain Hills")]
        public static void Run()
        {
            if (!EnvironmentBakeSettings.TryFind(out EnvironmentBakeSettings settings)) return;
            if (Build(settings)) Debug.Log("Terrain hills built. Run Tools > Tribal > Scatter Nature to re-seat scenery and rebake the navmesh.");
        }

        public static float HeightAt(Vector2 p)
        {
            Vector3 local = grid.InverseTransformPoint(new Vector3(p.x, 0f, p.y));
            float fx = Mathf.Clamp01(local.x / BaseGroundWidth + 0.5f) * (gridX - 1);
            float fz = Mathf.Clamp01(local.z / BaseGroundWidth + 0.5f) * (gridZ - 1);
            int x0 = Mathf.Min((int)fx, gridX - 2), z0 = Mathf.Min((int)fz, gridZ - 2);
            float tx = fx - x0, tz = fz - z0;
            float low = Mathf.Lerp(gridHeights[x0 + z0 * gridX], gridHeights[x0 + 1 + z0 * gridX], tx);
            float high = Mathf.Lerp(gridHeights[x0 + (z0 + 1) * gridX], gridHeights[x0 + 1 + (z0 + 1) * gridX], tx);
            return Mathf.Lerp(low, high, tz);
        }

        public static bool Build(EnvironmentBakeSettings settings)
        {
            GroundFootprintPainter painter = Object.FindAnyObjectByType<GroundFootprintPainter>();
            if (painter == null || !painter.TryGetComponent(out MeshFilter filter) || !painter.TryGetComponent(out MeshCollider collider))
            {
                Debug.LogError("No ground with a GroundFootprintPainter, MeshFilter and MeshCollider in the scene.");
                return false;
            }

            active = settings;
            float groundScale = settings.GroundSize / BaseGroundWidth;
            painter.transform.localScale = new Vector3(groundScale, painter.transform.localScale.y, groundScale);
            activeZones = CollectFlatZones(settings);
            Mesh saved = Save(BuildGrid(painter.transform), HillsMeshPath);

            Undo.RecordObjects(new Object[] { filter, collider }, "Build Terrain Hills");
            filter.sharedMesh = saved;
            collider.sharedMesh = null;
            collider.sharedMesh = saved;
            EditorUtility.SetDirty(filter);
            EditorUtility.SetDirty(collider);
            Physics.SyncTransforms();
            ApplyIslandToMaterial(settings, painter.GetComponent<Renderer>());
            VerifyAgainstCollider(collider);
            return true;
        }

        private static void VerifyAgainstCollider(MeshCollider collider)
        {
            var rng = new System.Random(1);
            Bounds b = collider.bounds;
            float worst = 0f;
            int misses = 0;
            for (int i = 0; i < 200; i++)
            {
                var p = new Vector2(Mathf.Lerp(b.min.x, b.max.x, (float)rng.NextDouble()), Mathf.Lerp(b.min.z, b.max.z, (float)rng.NextDouble()));
                if (!collider.Raycast(new Ray(new Vector3(p.x, 100f, p.y), Vector3.down), out RaycastHit hit, 200f)) { misses++; continue; }
                worst = Mathf.Max(worst, Mathf.Abs(hit.point.y - HeightAt(p)));
            }
            Debug.Log($"Terrain hills: ground {b.size.x:F0} x {b.size.z:F0} m, crest {b.max.y:F2} m, collider vs mesh grid worst gap {worst:F3} m, {misses} misses.");
        }

        private static Mesh BuildGrid(Transform ground)
        {
            Vector3 scale = ground.lossyScale;
            float worldWidth = BaseGroundWidth * scale.x;
            float worldDepth = BaseGroundWidth * scale.z;
            int nx = Mathf.Max(2, Mathf.RoundToInt(worldWidth / CellSize) + 1);
            int nz = Mathf.Max(2, Mathf.RoundToInt(worldDepth / CellSize) + 1);

            grid = ground;
            gridX = nx;
            gridZ = nz;
            gridHeights = new float[nx * nz];
            var vertices = new Vector3[nx * nz];
            var uvs = new Vector2[nx * nz];
            for (int j = 0; j < nz; j++)
                for (int i = 0; i < nx; i++)
                {
                    float u = i / (float)(nx - 1), v = j / (float)(nz - 1);
                    var local = new Vector3((u - 0.5f) * BaseGroundWidth, 0f, (v - 0.5f) * BaseGroundWidth);
                    Vector3 world = ground.TransformPoint(local);
                    world.y = HeightAt(active, activeZones, world.x, world.z);
                    gridHeights[i + j * nx] = world.y;
                    vertices[i + j * nx] = ground.InverseTransformPoint(world);
                    uvs[i + j * nx] = new Vector2(u, v);
                }

            var triangles = new int[(nx - 1) * (nz - 1) * 6];
            int t = 0;
            for (int j = 0; j < nz - 1; j++)
                for (int i = 0; i < nx - 1; i++)
                {
                    int a = i + j * nx;
                    triangles[t++] = a;
                    triangles[t++] = a + nx;
                    triangles[t++] = a + 1;
                    triangles[t++] = a + 1;
                    triangles[t++] = a + nx;
                    triangles[t++] = a + nx + 1;
                }

            var mesh = new Mesh { name = "GroundPlane_Hills", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float HeightAt(EnvironmentBakeSettings settings, List<FlatZone> zones, float x, float z)
        {
            float frequency = 1f / settings.HillWavelength;
            float offset = settings.Seed * 17.3f;
            float n = Mathf.PerlinNoise(x * frequency + offset, z * frequency + offset) * 0.75f
                    + Mathf.PerlinNoise(x * frequency * 2.3f + offset, z * frequency * 2.3f + offset) * 0.25f;
            float hill = Mathf.Clamp01((n - settings.HillThreshold) / (1f - settings.HillThreshold));
            hill = hill * hill * (3f - 2f * hill);

            float nearest = float.MaxValue;
            foreach (FlatZone zone in zones)
                nearest = Mathf.Min(nearest, Vector2.Distance(new Vector2(x, z), zone.Center) - zone.Radius);
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(nearest / settings.HillBlend));

            float land = hill * rise * settings.HillHeight;

            float radius = Mathf.Sqrt(x * x + z * z);
            float inland = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((radius - (settings.IslandRadius - settings.ShoreWidth)) / settings.ShoreWidth));
            float shore = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((radius - settings.IslandRadius) / settings.ShoreWidth));
            return land * (1f - inland) - settings.SeaDepth * shore;
        }

        private static void ApplyIslandToMaterial(EnvironmentBakeSettings settings, Renderer ground)
        {
            Material material = ground.sharedMaterial;
            if (material == null) return;

            material.SetFloat("_IslandRadius", settings.IslandRadius);
            material.SetFloat("_SandWidth", settings.SandWidth);
            material.SetFloat("_SeaLevel", settings.SeaLevel);
            EditorUtility.SetDirty(material);
        }

        private static List<FlatZone> CollectFlatZones(EnvironmentBakeSettings settings)
        {
            var zones = new List<FlatZone>();

            foreach (VillageLayout layout in Object.FindObjectsByType<VillageLayout>())
                foreach (Transform piece in layout.Pieces)
                    zones.Add(Zone(piece.position, settings.BuildingClearance));

            foreach (Village village in Object.FindObjectsByType<Village>())
                zones.Add(Zone(village.transform.position, settings.VillageClearance));

            foreach (FoodNode food in Object.FindObjectsByType<FoodNode>())
                zones.Add(Zone(food.transform.position, settings.FoodClearance));

            foreach (GroundFootprint footprint in GroundFootprint.All)
                zones.Add(new FlatZone { Center = footprint.Position, Radius = footprint.Reach });

            return zones;
        }

        private static FlatZone Zone(Vector3 position, float radius) =>
            new FlatZone { Center = new Vector2(position.x, position.z), Radius = radius };

        private static Mesh Save(Mesh mesh, string path)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }

            existing.Clear();
            existing.indexFormat = mesh.indexFormat;
            existing.vertices = mesh.vertices;
            existing.uv = mesh.uv;
            existing.normals = mesh.normals;
            existing.triangles = mesh.triangles;
            existing.RecalculateBounds();
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            return existing;
        }
    }
}
