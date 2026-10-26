using System.Collections.Generic;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.Economy
{
    public class FoodNode : MonoBehaviour
    {
        private static readonly List<FoodNode> all = new List<FoodNode>();
        public static IReadOnlyList<FoodNode> All => all;

        [SerializeField] private int maxFood = 12;
        [Tooltip("Physical clearance around the bush. Extra villagers spill over to a nearby node.")]
        [SerializeField, Range(1, 8)] private int maxGatherers = 3;
        [SerializeField] private float regrowSeconds = 25f;
        [SerializeField] private Renderer[] fruit;
        [SerializeField] private Material ringMaterial;
        [SerializeField] private float ringSize = 3f;
        [SerializeField, ColorUsage(true, true)] private Color ringColor = new Color(1f, 1f, 1f, 0.35f);

        private int food;
        private float regrowAt;
        private GameObject ring;
        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");

        public StandingSpots Spots { get; private set; }
        public int Food => food;
        public bool Available => food > 0;
        public bool HasRoom => Available && Spots.HasFree;

        private void Awake()
        {
            food = maxFood;
            Spots = new StandingSpots(transform, maxGatherers);
            ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(ring.GetComponent<Collider>());
            ring.name = "TargetRing";
            ring.transform.SetParent(transform, false);
            ring.transform.SetPositionAndRotation(new Vector3(transform.position.x, 0.03f, transform.position.z), Quaternion.Euler(90f, 0f, 0f));
            ring.transform.localScale = Vector3.one * ringSize / Mathf.Max(transform.lossyScale.x, 0.01f);
            MeshRenderer r = ring.GetComponent<MeshRenderer>();
            r.sharedMaterial = ringMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor(RimColorId, ringColor);
            r.SetPropertyBlock(block);
            ring.SetActive(false);
        }
        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        private void Update()
        {
            ring.SetActive(IsTargetOfSelected());

            if (food == 0 && Time.time >= regrowAt)
            {
                food = maxFood;
                SetFruitVisible(true);
            }
        }

        private bool IsTargetOfSelected()
        {
            foreach (VillagerUnit v in VillagerUnit.All)
            {
                if (!v.IsSelected) continue;
                if (v.GatherTarget == this) return true;
                if (!v.IsGathering) continue;
                float r = v.SearchRadius;
                if ((v.transform.position - transform.position).sqrMagnitude <= r * r) return true;
            }
            return false;
        }

        public int Take(int amount)
        {
            int taken = Mathf.Min(amount, food);
            food -= taken;
            if (food == 0 && taken > 0)
            {
                regrowAt = Time.time + regrowSeconds;
                SetFruitVisible(false);
            }
            return taken;
        }

        public static FoodNode Nearest(Vector3 from, float maxRange)
        {
            FoodNode best = null;
            float bestSqr = maxRange * maxRange;
            foreach (FoodNode node in all)
            {
                if (!node.HasRoom) continue;
                float sqr = (node.transform.position - from).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = node; }
            }
            return best;
        }

        private void SetFruitVisible(bool visible)
        {
            foreach (Renderer r in fruit) r.enabled = visible;
        }
    }
}
