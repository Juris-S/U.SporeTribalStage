using System.Collections.Generic;
using SporeTribal.Buildings;
using SporeTribal.Utilities;
using UnityEngine;
using UnityEngine.AI;

namespace SporeTribal.Economy
{
    [DefaultExecutionOrder(-100)]
    public class BerryBushSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject[] prefabs;
        [Tooltip("The hills collider. Its bounds set the area, so it follows the ground scale.")]
        [SerializeField] private Collider ground;
        [SerializeField, Min(0)] private int count = 30;
        [Tooltip("Guaranteed bushes around each village, on top of the scatter.")]
        [SerializeField, Min(0)] private int perVillage = 5;
        [Tooltip("Distance band from a village centre for its bushes (min, max metres).")]
        [SerializeField] private Vector2 villageRing = new Vector2(18f, 34f);
        [SerializeField] private float minSpacing = 14f;
        [SerializeField] private float edgeMargin = 8f;
        [SerializeField] private float villageClearance = 12f;
        [SerializeField] private float buildingClearance = 8f;
        [Tooltip("Radius around a spot that must be free of trees, rocks and other colliders.")]
        [SerializeField] private float sceneryClearance = 1.5f;
        [SerializeField, Range(0f, 1f)] private float minSlope = 0.92f;
        [Tooltip("Metres sunk into the ground per unit of prefab scale, so the bush base sits on the surface.")]
        [SerializeField] private float sink = 0.12f;
        [SerializeField, Min(1)] private int attemptsPerBush = 30;
        [Header("Seed")]
        [Tooltip("Off: a fresh seed every play and restart. On: the same layout every time.")]
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private uint fixedSeed = 1;

        private const float RayHeight = 100f;
        private const float NavMeshSnap = 1.5f;

        private struct Keepout
        {
            public Vector2 Centre;
            public float RadiusSqr;
        }

        private void Awake()
        {
            if (useFixedSeed) GameSeed.Set(fixedSeed); else GameSeed.Roll();
            Mulberry32 rng = GameSeed.Stream("berry-bushes");

            List<Keepout> keepouts = CollectKeepouts();
            Bounds area = ground.bounds;
            int groundMask = LayerMask.GetMask("Ground");
            int nearVillages = 0, scattered = 0;

            Village[] villages = FindObjectsByType<Village>();
            System.Array.Sort(villages, (a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (Village village in villages)
            {
                Vector3 centre = village.transform.position;
                for (int i = 0; i < perVillage; i++)
                {
                    bool placed = TryPlace(rng, keepouts, area, groundMask, () =>
                    {
                        float angle = rng.Range(0f, Mathf.PI * 2f);
                        float radius = rng.Range(villageRing.x, villageRing.y);
                        return new Vector2(centre.x + Mathf.Cos(angle) * radius, centre.z + Mathf.Sin(angle) * radius);
                    });
                    if (placed) nearVillages++;
                }
            }

            for (int i = 0; i < count; i++)
            {
                bool placed = TryPlace(rng, keepouts, area, groundMask, () => new Vector2(
                    rng.Range(area.min.x + edgeMargin, area.max.x - edgeMargin),
                    rng.Range(area.min.z + edgeMargin, area.max.z - edgeMargin)));
                if (placed) scattered++;
            }

            Debug.Log($"Berry bushes: seed {GameSeed.Current}, {nearVillages}/{perVillage * villages.Length} near villages, {scattered}/{count} scattered");
        }

        private bool TryPlace(Mulberry32 rng, List<Keepout> keepouts, Bounds area, int groundMask, System.Func<Vector2> candidate)
        {
            for (int attempt = 0; attempt < attemptsPerBush; attempt++)
            {
                Vector2 flat = candidate();
                bool inside = flat.x > area.min.x + edgeMargin && flat.x < area.max.x - edgeMargin
                    && flat.y > area.min.z + edgeMargin && flat.y < area.max.z - edgeMargin;
                if (!inside || IsBlocked(keepouts, flat) || !TryFindSurface(flat.x, flat.y, area.max.y, groundMask, out Vector3 point)) continue;

                GameObject prefab = rng.Pick(prefabs);
                float lift = -sink * prefab.transform.localScale.y;
                Quaternion yaw = Quaternion.Euler(0f, rng.Range(0f, 360f), 0f);
                Instantiate(prefab, point + Vector3.up * lift, yaw, transform);
                keepouts.Add(new Keepout { Centre = flat, RadiusSqr = minSpacing * minSpacing });
                return true;
            }
            return false;
        }

        private List<Keepout> CollectKeepouts()
        {
            var list = new List<Keepout>();
            foreach (Village village in FindObjectsByType<Village>())
                list.Add(Make(village.transform.position, villageClearance));
            foreach (VillageLayout layout in FindObjectsByType<VillageLayout>())
                foreach (Transform piece in layout.Pieces)
                    list.Add(Make(piece.position, buildingClearance));
            foreach (FoodNode node in FindObjectsByType<FoodNode>())
                list.Add(Make(node.transform.position, minSpacing));
            return list;
        }

        private static Keepout Make(Vector3 position, float radius) =>
            new Keepout { Centre = new Vector2(position.x, position.z), RadiusSqr = radius * radius };

        private static bool IsBlocked(List<Keepout> keepouts, Vector2 point)
        {
            foreach (Keepout k in keepouts)
                if ((k.Centre - point).sqrMagnitude < k.RadiusSqr) return true;
            return false;
        }

        private bool TryFindSurface(float x, float z, float topY, int groundMask, out Vector3 point)
        {
            point = default;
            var origin = new Vector3(x, topY + RayHeight, z);
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, RayHeight * 2f, groundMask)) return false;
            if (hit.normal.y < minSlope) return false;
            if (!NavMesh.SamplePosition(hit.point, out _, NavMeshSnap, NavMesh.AllAreas)) return false;

            Vector3 probe = hit.point + Vector3.up * (sceneryClearance + 0.5f);
            if (Physics.CheckSphere(probe, sceneryClearance, ~groundMask, QueryTriggerInteraction.Ignore)) return false;

            point = hit.point;
            return true;
        }
    }
}
