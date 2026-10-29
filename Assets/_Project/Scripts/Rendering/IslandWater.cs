using UnityEngine;
using UnityEngine.Rendering;

namespace SporeTribal.Rendering
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class IslandWater : MonoBehaviour
    {
        [SerializeField] private Material material;
        [Tooltip("Side length of the sea square in metres. Must reach past the horizon from anywhere the camera can look.")]
        [SerializeField] private float size = 1200f;
        [Tooltip("Quads per side. The bend is applied per vertex, so cells must stay small.")]
        [SerializeField, Range(8, 250)] private int cells = 192;

        [Tooltip("Surface drift in m/s along the wind, so the waves travel the same way as the cloud shadows.")]
        [SerializeField] private float driftSpeed = 1.2f;
        [Tooltip("Used when there is no WindController in the scene. Match the cloud shadows.")]
        [SerializeField] private Vector2 fallbackDirection = new Vector2(0.6f, 0.8f);

        private static readonly int SeaId = Shader.PropertyToID("_IslandSea");

        private Mesh mesh;
        private Vector3[] flat;
        private Vector3[] bent;
        private Vector3[] normals;
        private Vector2[] uvs;
        private Vector2 drift;
        private float bentAmount = -1f;
        private Vector3 bentPivot;

        private void OnEnable()
        {
            mesh = Build();
            GetComponent<MeshFilter>().sharedMesh = mesh;

            var meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            WorldBend.CameraBendSet += OnCameraBendSet;
            Shader.SetGlobalVector(SeaId, new Vector4(1f, transform.position.y, 0f, 0f));
        }

        private void OnValidate()
        {
            if (TryGetComponent(out MeshRenderer meshRenderer)) meshRenderer.sharedMaterial = material;
        }

        private void OnDisable()
        {
            WorldBend.CameraBendSet -= OnCameraBendSet;
            Shader.SetGlobalVector(SeaId, Vector4.zero);
            if (mesh == null) return;
            if (Application.isPlaying) Destroy(mesh);
            else DestroyImmediate(mesh);
        }

        private void Update()
        {
            if (mesh == null) return;

            var wind = WindController.Current;
            Vector2 velocity = wind != null
                ? wind.Direction * driftSpeed * Mathf.Max(wind.Strength, 0.3f)
                : fallbackDirection.normalized * driftSpeed;
            drift += velocity * Time.deltaTime;
            drift.x %= 1000f;
            drift.y %= 1000f;

            Vector3 origin = transform.position;
            for (int i = 0; i < flat.Length; i++)
                uvs[i] = new Vector2(flat[i].x + origin.x - drift.x, flat[i].z + origin.z - drift.y);
            mesh.SetUVs(0, uvs);
        }

        private void OnCameraBendSet(float amount, Vector3 pivot)
        {
            if (mesh == null || (amount == bentAmount && pivot == bentPivot)) return;
            bentAmount = amount;
            bentPivot = pivot;

            Vector3 origin = transform.position;
            for (int i = 0; i < flat.Length; i++)
            {
                float dx = flat[i].x + origin.x - pivot.x;
                float dz = flat[i].z + origin.z - pivot.z;
                bent[i] = new Vector3(flat[i].x, -(dx * dx + dz * dz) * amount, flat[i].z);
                normals[i] = new Vector3(2f * amount * dx, 1f, 2f * amount * dz).normalized;
            }
            mesh.SetVertices(bent);
            mesh.SetNormals(normals);
        }

        private Mesh Build()
        {
            int stride = cells + 1;
            flat = new Vector3[stride * stride];
            bent = new Vector3[flat.Length];
            normals = new Vector3[flat.Length];
            uvs = new Vector2[flat.Length];
            var colors = new Color[flat.Length];
            Vector3 origin = transform.position;
            for (int j = 0; j < stride; j++)
                for (int i = 0; i < stride; i++)
                {
                    int v = i + j * stride;
                    flat[v] = new Vector3((i / (float)cells - 0.5f) * size, 0f, (j / (float)cells - 0.5f) * size);
                    bent[v] = flat[v];
                    normals[v] = Vector3.up;
                    uvs[v] = new Vector2(flat[v].x + origin.x, flat[v].z + origin.z);
                    colors[v] = Color.black;
                }

            var triangles = new int[cells * cells * 6];
            int t = 0;
            for (int j = 0; j < cells; j++)
                for (int i = 0; i < cells; i++)
                {
                    int a = i + j * stride;
                    triangles[t++] = a;
                    triangles[t++] = a + stride;
                    triangles[t++] = a + 1;
                    triangles[t++] = a + 1;
                    triangles[t++] = a + stride;
                    triangles[t++] = a + stride + 1;
                }

            var built = new Mesh { name = "IslandWater", hideFlags = HideFlags.HideAndDontSave };
            built.MarkDynamic();
            built.vertices = bent;
            built.normals = normals;
            built.uv = uvs;
            built.colors = colors;
            built.triangles = triangles;
            built.bounds = new Bounds(Vector3.zero, new Vector3(size, 2000f, size));
            bentAmount = 0f;
            bentPivot = Vector3.zero;
            return built;
        }
    }
}
