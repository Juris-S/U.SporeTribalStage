using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SporeTribal.EditorTools
{
    [CreateAssetMenu(menuName = "Tribal/Editor/Environment Bake Settings", fileName = "EnvironmentBakeSettings")]
    public class EnvironmentBakeSettings : ScriptableObject
    {
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
        public float HalfSize => halfSize;
        public float ForestEdge => forestEdge;
        public float GrassChunk => grassChunk;
        public int MaskResolution => maskResolution;
        public float MaskSize => maskSize;
        public float MaskOrigin => -maskSize * 0.5f;
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
