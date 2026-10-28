using System.Collections.Generic;
using SporeTribal.Feedback;
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

        private const float RingLift = 0.03f;
        private const float MinScale = 0.01f;

        private int food;
        private float regrowAt;
        private GameObject ring;

        public StandingSpots Spots { get; private set; }
        public int Food => food;
        public bool Available => food > 0;
        public bool HasRoom => Available && Spots.HasFree;

        private void Awake()
        {
            food = maxFood;
            Spots = new StandingSpots(transform, maxGatherers);
            ring = CreateTargetRing();
        }

        private GameObject CreateTargetRing()
        {
            Transform target = GroundRing.Create("TargetRing", ringMaterial, ringColor, transform).transform;
            target.position = new Vector3(transform.position.x, RingLift, transform.position.z);
            target.localScale = Vector3.one * ringSize / Mathf.Max(transform.lossyScale.x, MinScale);
            target.gameObject.SetActive(false);
            return target.gameObject;
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
