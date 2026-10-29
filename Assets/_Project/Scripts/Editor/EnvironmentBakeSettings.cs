using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SporeTribal.EditorTools
{
    [CreateAssetMenu(menuName = "Tribal/Editor/Environment Bake Settings", fileName = "EnvironmentBakeSettings")]
    public class EnvironmentBakeSettings : ScriptableObject
    {
        private const float ReferenceIslandRadius = 95f;

        [Header("Baked textures")]
        [Tooltip("Overwritten by Tools > Tribal > Build Cloud Texture.")]
        [SerializeField] private Texture2D cloudShadows;
        [Tooltip("Folder the terrain detail textures are written to.")]
        [SerializeField] private DefaultAsset terrainDetailFolder;

        [Header("Nature models")]
        [SerializeField] private GameObject[] trees;
        [SerializeField] private GameObject[] bareTrees;
        [SerializeField] private GameObject[] rocks;
        [SerializeField] private GameObject[] bushes;
        [SerializeField] private GameObject[] grassTufts;
        [Tooltip("Source of the foliage materials and the grass texture.")]
        [SerializeField] private Material forestMaterial;

        [Header("Nature output")]
        [SerializeField] private Material groundMaterial;
        [SerializeField] private Material grassMaterial;
        [SerializeField] private Material treeFoliage;
        [SerializeField] private Material bushFoliage;
        [Tooltip("Overwritten by Scatter Nature: trail mask (G) and grass cover (A).")]
        [SerializeField] private Texture2D groundMask;
        [Tooltip("Folder the combined grass chunk meshes are written to. Its contents are replaced on every scatter.")]
        [SerializeField] private DefaultAsset grassMeshFolder;

        [Header("Scatter area")]
        [SerializeField] private int seed = 7;
        [Tooltip("Half the width of the square that gets scattered.")]
        [SerializeField] private float halfSize = 72f;
        [Tooltip("Dense forest starts this far from the centre.")]
        [SerializeField] private float forestEdge = 52f;
        [Tooltip("Grass tufts are combined into square chunks this wide.")]
        [SerializeField] private float grassChunk = 24f;
        [SerializeField] private int maskResolution = 512;
        [Tooltip("World width covered by the ground mask, centred on the origin.")]
        [SerializeField] private float maskSize = 150f;

        [Tooltip("Bushes are planted this far into the ground (metres at scale 1) so they do not look perched.")]
        [SerializeField] private float bushSink = 0.12f;

        [Header("Hills")]
        [Tooltip("Tallest hill crest above the flat ground, in metres.")]
        [SerializeField] private float hillHeight = 1.6f;
        [Tooltip("Distance between neighbouring hills, in metres.")]
        [SerializeField] private float hillWavelength = 45f;
        [Tooltip("Noise below this (0-1) stays flat, so hills sit on open plains.")]
        [SerializeField, Range(0f, 0.9f)] private float hillThreshold = 0.35f;
        [Tooltip("Distance over which the ground rises from flat village ground to full height.")]
        [SerializeField] private float hillBlend = 12f;

        [Header("Island")]
        [Tooltip("Side length of the square ground mesh, in metres. Must exceed twice the island radius plus the shore width.")]
        [SerializeField] private float groundSize = 400f;
        [Tooltip("Radius of the dry land around the world origin, in metres. Keep it above the camera's max focus radius.")]
        [SerializeField] private float islandRadius = 150f;
        [Tooltip("Width of the slope from the island edge down to the sea floor, in metres.")]
        [SerializeField] private float shoreWidth = 36f;
        [Tooltip("How far below the flat ground the sea floor sits, in metres.")]
        [SerializeField] private float seaDepth = 3f;
        [Tooltip("Height of the water surface in world space.")]
        [SerializeField] private float seaLevel = -0.4f;
        [Tooltip("Width of the sand band that runs up the beach from the water line, in metres.")]
        [SerializeField] private float sandWidth = 14f;

        [Header("Clearances")]
        [SerializeField] private float buildingClearance = 7f;
        [SerializeField] private float villageClearance = 9f;
        [SerializeField] private float foodClearance = 4f;
        [Tooltip("Half width of the open path between the two villages.")]
        [SerializeField] private float pathHalfWidth = 5f;
        [Tooltip("Grass may grow this much closer to keepouts than trees and rocks (negative shrinks them).")]
        [SerializeField] private float grassClearanceTrim = -3.5f;

        public Texture2D CloudShadows => cloudShadows;
        public string TerrainDetailFolder => AssetDatabase.GetAssetPath(terrainDetailFolder);
        public IReadOnlyList<GameObject> Trees => trees;
        public IReadOnlyList<GameObject> BareTrees => bareTrees;
        public IReadOnlyList<GameObject> Rocks => rocks;
        public IReadOnlyList<GameObject> Bushes => bushes;
        public IReadOnlyList<GameObject> GrassTufts => grassTufts;
        public Material ForestMaterial => forestMaterial;
        public Material GroundMaterial => groundMaterial;
        public Material GrassMaterial => grassMaterial;
        public Material TreeFoliage => treeFoliage;
        public Material BushFoliage => bushFoliage;
        public Texture2D GroundMask => groundMask;
        public string GrassMeshFolder => AssetDatabase.GetAssetPath(grassMeshFolder);
        public int Seed => seed;
        public float AreaScale => islandRadius / ReferenceIslandRadius;
        public float HalfSize => islandRadius;
        public float ForestEdge => forestEdge * AreaScale;
        public float GrassChunk => grassChunk;
        public int Scaled(int count) => Mathf.RoundToInt(count * AreaScale * AreaScale);
        public int MaskResolution => Mathf.ClosestPowerOfTwo(Mathf.RoundToInt(maskResolution * AreaScale));
        public float MaskSize => groundSize;
        public float GroundSize => groundSize;
        public float MaskOrigin => -maskSize * 0.5f;
        public float BushSink => bushSink;
        public float HillHeight => hillHeight;
        public float HillWavelength => hillWavelength;
        public float HillThreshold => hillThreshold;
        public float HillBlend => hillBlend;
        public float IslandRadius => islandRadius;
        public float ShoreWidth => shoreWidth;
        public float SeaDepth => seaDepth;
        public float SeaLevel => seaLevel;
        public float SandWidth => sandWidth;

        public float WaterlineRadius
        {
            get
            {
                float target = Mathf.Clamp01(-seaLevel / Mathf.Max(seaDepth, 0.01f));
                float low = 0f, high = 1f;
                for (int i = 0; i < 24; i++)
                {
                    float mid = (low + high) * 0.5f;
                    if (mid * mid * (3f - 2f * mid) < target) low = mid;
                    else high = mid;
                }
                return islandRadius + low * shoreWidth;
            }
        }
        public float BuildingClearance => buildingClearance;
        public float VillageClearance => villageClearance;
        public float FoodClearance => foodClearance;
        public float PathHalfWidth => pathHalfWidth;
        public float GrassClearanceTrim => grassClearanceTrim;

        public static bool TryFind(out EnvironmentBakeSettings settings)
        {
            settings = null;
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(EnvironmentBakeSettings));
            if (guids.Length > 0) settings = AssetDatabase.LoadAssetAtPath<EnvironmentBakeSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (settings == null) Debug.LogError("No EnvironmentBakeSettings asset found. Create one via Create > Tribal > Editor > Environment Bake Settings.");
            return settings != null;
        }
    }
}
