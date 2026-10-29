using System.Collections.Generic;
using SporeTribal.Buildings;
using SporeTribal.Combat;
using SporeTribal.Villagers;
using UnityEngine;
using UnityEngine.UIElements;

namespace SporeTribal.UI
{
    public class HealthBars : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(44f, 6f);
        [SerializeField] private float heightAboveUnit = 2.4f;
        [Tooltip("Seconds a bar stays visible after the last hit (selected units always show theirs while hurt).")]
        [SerializeField] private float showSeconds = 4f;
        [Tooltip("Damaged buildings always show their bar, this much wider than a villager's.")]
        [SerializeField] private float buildingWidthScale = 2f;
        [Tooltip("Seconds the pale damage trail waits before catching up with the fill.")]
        [SerializeField] private float trailDelay = 0.4f;
        [SerializeField] private float trailSpeed = 0.8f;

        private static readonly Color Low = new Color(1f, 0.43f, 0.43f);
        private static readonly Color High = new Color(0.55f, 0.9f, 0.43f);

        private sealed class Bar
        {
            public VisualElement Root;
            public VisualElement Trail;
            public VisualElement Fill;
            public float Shown = 1f;
            public float TrailFraction = 1f;
            public float TrailHoldUntil;
            public bool Seen;
        }

        private readonly Dictionary<Health, Bar> active = new Dictionary<Health, Bar>();
        private readonly Stack<Bar> pool = new Stack<Bar>();
        private readonly List<Health> stale = new List<Health>();

        private void OnDisable()
        {
            foreach (Bar bar in active.Values) bar.Root.RemoveFromHierarchy();
            active.Clear();
            pool.Clear();
        }

        private void LateUpdate()
        {
            VisualElement layer = HudLayer.Root;
            if (layer == null) return;

            foreach (VillagerUnit unit in VillagerUnit.All)
            {
                Health health = unit.Health;
                if (health.Current >= health.Max) continue;
                if (!unit.IsSelected && Time.time - health.LastHitTime > showSeconds) continue;
                Place(layer, health, unit.transform.position + Vector3.up * heightAboveUnit, size.x);
            }

            foreach (Building building in Building.All)
            {
                Health health = building.Health;
                if (!building.IsStanding || health.Current >= health.Max) continue;
                Bounds bounds = building.Bounds;
                Place(layer, health, new Vector3(bounds.center.x, bounds.max.y + 0.5f, bounds.center.z), size.x * buildingWidthScale);
            }

            Sweep();
        }

        private void Place(VisualElement layer, Health health, Vector3 world, float width)
        {
            if (!active.TryGetValue(health, out Bar bar))
            {
                bar = pool.Count > 0 ? pool.Pop() : Create();
                bar.Shown = bar.TrailFraction = health.Fraction;
                bar.Root.style.display = DisplayStyle.None;
                layer.Add(bar.Root);
                active[health] = bar;
            }
            bar.Seen = true;
            if (!HudLayer.TryWorldToPanel(world, bar.Root, out Vector2 point))
            {
                bar.Root.style.display = DisplayStyle.None;
                return;
            }

            bar.Root.style.display = DisplayStyle.Flex;
            bar.Root.style.left = point.x;
            bar.Root.style.top = point.y;
            bar.Root.style.width = width;
            bar.Root.style.height = size.y;

            float fraction = health.Fraction;
            if (fraction < bar.Shown)
            {
                bar.TrailHoldUntil = Time.time + trailDelay;
                HudLayer.Punch(bar.Root, 0.08f);
            }
            bar.Shown = fraction;
            bar.TrailFraction = Time.time < bar.TrailHoldUntil ? Mathf.Max(bar.TrailFraction, fraction) : Mathf.MoveTowards(bar.TrailFraction, fraction, trailSpeed * Time.deltaTime);
            bar.Trail.style.width = Length.Percent(bar.TrailFraction * 100f);
            bar.Fill.style.width = Length.Percent(fraction * 100f);
            bar.Fill.style.backgroundColor = Color.Lerp(Low, High, fraction);
        }

        private void Sweep()
        {
            stale.Clear();
            foreach (KeyValuePair<Health, Bar> pair in active)
            {
                if (pair.Value.Seen) pair.Value.Seen = false;
                else stale.Add(pair.Key);
            }
            foreach (Health health in stale)
            {
                Bar bar = active[health];
                bar.Root.RemoveFromHierarchy();
                active.Remove(health);
                pool.Push(bar);
            }
        }

        private static Bar Create()
        {
            var bar = new Bar
            {
                Root = new VisualElement { pickingMode = PickingMode.Ignore },
                Trail = new VisualElement { pickingMode = PickingMode.Ignore },
                Fill = new VisualElement { pickingMode = PickingMode.Ignore },
            };
            bar.Root.AddToClassList("health-bar");
            bar.Trail.AddToClassList("health-bar__ghost");
            bar.Fill.AddToClassList("health-bar__fill");
            bar.Root.Add(bar.Trail);
            bar.Root.Add(bar.Fill);
            return bar;
        }
    }
}
