using SporeTribal.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SporeTribal.EditorTools
{
    public static class IslandWaterSetup
    {
        private const string MaterialPath = "Assets/_Project/Materials/IslandWater.mat";
        private const string SeabedMaterialPath = "Assets/_Project/Materials/Environment/Seabed.mat";
        private const string SeabedMeshPath = "Assets/_Project/Art/Seabed.asset";
        private const float SeaSize = 1200f;
        private const int SeabedCells = 120;
        private const int IgnoreRaycastLayer = 2;

        [MenuItem("Tools/Tribal/Add Island Water")]
        public static void Run()
        {
            if (!EnvironmentBakeSettings.TryFind(out EnvironmentBakeSettings settings)) return;

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Debug.LogError($"No water material at {MaterialPath}. Duplicate a stylised water material there first.");
                return;
            }

            IslandWater water = Object.FindAnyObjectByType<IslandWater>();
            if (water == null)
            {
                var go = new GameObject("Island Water");
                Undo.RegisterCreatedObjectUndo(go, "Add Island Water");
                water = go.AddComponent<IslandWater>();
            }

            water.gameObject.layer = IgnoreRaycastLayer;
            water.transform.position = new Vector3(0f, settings.SeaLevel, 0f);
            var serialized = new SerializedObject(water);
            serialized.FindProperty("material").objectReferenceValue = material;
            serialized.FindProperty("size").floatValue = SeaSize;
            serialized.FindProperty("cells").intValue = 192;
            serialized.ApplyModifiedProperties();
            water.enabled = false;
            water.enabled = true;

            BuildSeabed(water.transform, settings);

            EditorSceneManager.MarkSceneDirty(water.gameObject.scene);
            Debug.Log($"Island water placed at y {settings.SeaLevel:F2}. Run Build Terrain Hills, then Scatter Nature, so the ground and navmesh match the shore.");
        }

        private static void BuildSeabed(Transform water, EnvironmentBakeSettings settings)
        {
            Transform seabed = water.Find("Seabed");
            if (seabed == null)
            {
                var go = new GameObject("Seabed", typeof(MeshFilter), typeof(MeshRenderer));
                Undo.RegisterCreatedObjectUndo(go, "Add Island Water");
                seabed = go.transform;
                seabed.SetParent(water, false);
            }

            seabed.gameObject.layer = IgnoreRaycastLayer;
            seabed.position = new Vector3(0f, -settings.SeaDepth - 0.1f, 0f);
            seabed.GetComponent<MeshFilter>().sharedMesh = SeabedMesh();

            var meshRenderer = seabed.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = SeabedMaterial();
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static Mesh SeabedMesh()
        {
            int stride = SeabedCells + 1;
            var vertices = new Vector3[stride * stride];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            for (int j = 0; j < stride; j++)
                for (int i = 0; i < stride; i++)
                {
                    int v = i + j * stride;
                    vertices[v] = new Vector3((i / (float)SeabedCells - 0.5f) * SeaSize, 0f, (j / (float)SeabedCells - 0.5f) * SeaSize);
                    normals[v] = Vector3.up;
                    uvs[v] = new Vector2(i / (float)SeabedCells, j / (float)SeabedCells);
                }

            var triangles = new int[SeabedCells * SeabedCells * 6];
            int t = 0;
            for (int j = 0; j < SeabedCells; j++)
                for (int i = 0; i < SeabedCells; i++)
                {
                    int a = i + j * stride;
                    triangles[t++] = a;
                    triangles[t++] = a + stride;
                    triangles[t++] = a + 1;
                    triangles[t++] = a + 1;
                    triangles[t++] = a + stride;
                    triangles[t++] = a + stride + 1;
                }

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SeabedMeshPath);
            if (mesh == null)
            {
                mesh = new Mesh { name = "Seabed" };
                AssetDatabase.CreateAsset(mesh, SeabedMeshPath);
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(SeaSize, 2000f, SeaSize));
            EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssets();
            return mesh;
        }

        private static Material SeabedMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(SeabedMaterialPath);
            if (material != null) return material;

            material = new Material(Shader.Find("Project/BentLit")) { name = "Seabed" };
            material.SetColor("_BaseColor", new Color(0.06f, 0.2f, 0.32f));
            material.SetFloat("_Smoothness", 0f);
            AssetDatabase.CreateAsset(material, SeabedMaterialPath);
            return material;
        }
    }
}
