using System;
using System.Collections.Generic;
using SporeTribal.AI;
using SporeTribal.Buildings;
using SporeTribal.Utilities;
using SporeTribal.Villagers;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace SporeTribal.Economy
{
    [DefaultExecutionOrder(-200)]
    public class TribeProgressionDirector : MonoBehaviour
    {
        [Serializable]
        public class Wave
        {
            [Tooltip("Rival tribes that settle together in this wave.")]
            public int tribes = 1;
            [Tooltip("Seconds from match start before the wave can appear.")]
            public float delaySeconds;
            [Tooltip("Rival tribes the player must have conquered or allied before the wave can appear.")]
            public int resolvedToTrigger;
            [Tooltip("Also appears early once the player's food reaches Food For New Tribes (the anti-turtle failsafe).")]
            public bool foodFailsafe;
            [Tooltip("Villagers each tribe starts with, chieftain included, one entry per tribe (the last repeats).")]
            public int[] villagers = { 3 };
            public int startingFood = 30;
            [Tooltip("Each tribe's opinion of the player on arrival, one entry per tribe (the last repeats). Below -31 is at war.")]
            public int[] openings = { 0 };
            public TribeAI.Temper temper = TribeAI.Temper.Aggressive;
        }

        [Header("Scene")]
        [SerializeField] private Village player;
        [Tooltip("Root of the player's tribe. Kept switched off in the scene; moved to its seeded site and switched on at the start of every match.")]
        [SerializeField] private Transform playerRoot;
        [Tooltip("Root of the rival tribe to copy. Kept switched off; each rival is a copy of it.")]
        [SerializeField] private Transform rivalTemplate;
        [SerializeField] private Transform cameraRig;
        [SerializeField] private SocialPerformanceDirector performance;
        [Tooltip("The hills collider. Its bounds set the area villages can settle in.")]
        [SerializeField] private Collider ground;

        [Header("Waves")]
        [SerializeField] private Wave[] waves =
        {
            new Wave { tribes = 1, delaySeconds = 60f, villagers = new[] { 3 }, startingFood = 30, openings = new[] { 0 }, temper = TribeAI.Temper.Peaceful },
            new Wave { tribes = 2, resolvedToTrigger = 1, foodFailsafe = true, villagers = new[] { 6 }, startingFood = 50, openings = new[] { -15, -40 } },
            new Wave { tribes = 2, resolvedToTrigger = 3, villagers = new[] { 6, 9 }, startingFood = 80, openings = new[] { -40 } },
        };
        [Tooltip("Player food that brings the second wave early if the first tribe is ignored.")]
        [SerializeField, Min(0)] private int foodForNewTribes = 100;

        [Header("Siting")]
        [Tooltip("Metres kept clear of the ground edge. Must stay well under half the ground width, or sites collapse to the centre.")]
        [SerializeField] private float edgeMargin = 24f;
        [Tooltip("Metres between villages. Spore uses 100 to 150; the usable ground here is about 200 m square.")]
        [SerializeField] private float minSpacing = 85f;
        [Tooltip("The first rival settles within this distance of the player.")]
        [SerializeField] private float firstRivalRange = 110f;
        [Tooltip("Radius around a site that must be free of trees, rocks and other colliders.")]
        [SerializeField] private float clearRadius = 20f;
        [Tooltip("Radius of the ring of height samples that must be level. Villages are about this wide.")]
        [SerializeField] private float flatRadius = 16f;
        [Tooltip("Largest height difference (metres) across the village ring.")]
        [SerializeField] private float flatTolerance = 1.2f;
        [SerializeField, Min(1)] private int attemptsPerSite = 250;

        [Header("Seed")]
        [Tooltip("Off: a fresh seed every play and restart. On: the same layout every time.")]
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private uint fixedSeed = 1;

        [Header("Messages")]
        [SerializeField] private int fontSize = 22;
        [SerializeField] private float messageSeconds = 6f;

        private const float RayHeight = 100f;
        private const float MinSpacingFloor = 0.85f;
        private static readonly string[] Compass = { "north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west" };

        private class Rival
        {
            public Village Village;
            public GameObject Root;
            public int Wave;
            public int Opening;
            public bool Arrived;
        }

        private readonly List<Rival> rivals = new List<Rival>();
        private static readonly string[] SceneryGroups = { "Forest", "Rocks", "Bushes" };
        private readonly List<Transform> scenery = new List<Transform>();
        private readonly Collider[] overlaps = new Collider[48];
        private int groundMask;
        private int nextWave;
        private float startTime;
        private string message;
        private float messageUntil;
        private GUIStyle style;

        public int Resolved
        {
            get
            {
                int count = 0;
                foreach (Rival rival in rivals)
                    if (rival.Arrived && IsResolved(rival.Village)) count++;
                return count;
            }
        }

        private void Awake()
        {
            if (useFixedSeed) GameSeed.Set(fixedSeed); else GameSeed.Roll();
            Mulberry32 rng = GameSeed.Stream("villages");
            groundMask = LayerMask.GetMask("Ground");
            rivalTemplate.gameObject.SetActive(false);
            CarveBuildings(playerRoot);
            CarveBuildings(rivalTemplate);
            CollectScenery();

            int total = 1;
            foreach (Wave wave in waves) total += wave.tribes;
            List<Vector3> sites = PlanSites(rng, total);
            if (sites.Count == 0)
            {
                playerRoot.gameObject.SetActive(true);
                return;
            }

            foreach (Vector3 site in sites) ClearScenery(site);

            Vector3 shift = sites[0] - player.transform.position;
            Relocate(playerRoot, player.transform.position, sites[0]);
            playerRoot.gameObject.SetActive(true);
            if (cameraRig != null) cameraRig.position += new Vector3(shift.x, 0f, shift.z);

            int next = 1;
            for (int w = 0; w < waves.Length; w++)
                for (int i = 0; i < waves[w].tribes && next < sites.Count; i++)
                    rivals.Add(BuildRival(w, i, sites[next++]));

            Debug.Log($"Villages: seed {GameSeed.Current}, player at {sites[0]}, {rivals.Count} rival sites");
        }

        private void Start()
        {
            startTime = Time.time;
            foreach (Rival rival in rivals) rival.Root.SetActive(false);
        }

        private static void CarveBuildings(Transform root)
        {
            int notWalkable = NavMesh.GetAreaFromName("Not Walkable");
            foreach (NavMeshModifier modifier in root.GetComponentsInChildren<NavMeshModifier>(true))
            {
                if (!modifier.overrideArea || modifier.area != notWalkable || !modifier.TryGetComponent(out BoxCollider box)) continue;
                NavMeshObstacle obstacle = modifier.gameObject.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.center = box.center;
                obstacle.size = box.size;
                obstacle.carving = true;
            }
        }

        private void Update()
        {
            if (nextWave < waves.Length && ShouldSettle(waves[nextWave])) Settle(nextWave++);
            if (performance != null && (performance.Rival == null || IsResolved(performance.Rival) || !performance.Rival.gameObject.activeInHierarchy))
                performance.SetRival(NearestOpenRival());
        }

        public bool HasPendingWave => nextWave < waves.Length;

        public void SettleNext()
        {
            if (HasPendingWave) Settle(nextWave++);
        }

        private bool ShouldSettle(Wave wave)
        {
            if (Time.time - startTime >= wave.delaySeconds && Resolved >= wave.resolvedToTrigger) return true;
            return wave.foodFailsafe && nextWave > 0 && player.Food >= foodForNewTribes;
        }

        private void Settle(int index)
        {
            Wave wave = waves[index];
            foreach (Rival rival in rivals)
            {
                if (rival.Wave != index) continue;
                rival.Arrived = true;
                rival.Root.SetActive(true);
                rival.Village.SetOpening(player, rival.Opening);
                rival.Village.Deposit(wave.startingFood);
                rival.Root.GetComponentInChildren<TribeAI>(true).DelayRecruiting();
                Announce($"A new tribe has settled to the {Direction(rival.Village.transform.position)}.");
            }
        }

        private static int Pick(int[] values, int index, int fallback) =>
            values == null || values.Length == 0 ? fallback : values[Mathf.Min(index, values.Length - 1)];

        private Rival BuildRival(int wave, int index, Vector3 site)
        {
            GameObject root = Instantiate(rivalTemplate.gameObject);
            root.name = $"Rival Tribe {rivals.Count + 1}";
            Village village = root.GetComponentInChildren<Village>(true);
            Relocate(root.transform, village.transform.position, site);

            Wave settings = waves[wave];
            root.GetComponentInChildren<VillagerSpawner>(true).SetRegulars(Mathf.Max(0, Pick(settings.villagers, index, 3) - 1));
            root.GetComponentInChildren<TribeAI>(true).SetTemper(settings.temper);

            root.SetActive(true);
            int opening = Pick(settings.openings, index, 0);
            return new Rival { Village = village, Root = root, Wave = wave, Opening = opening };
        }

        private void Relocate(Transform root, Vector3 from, Vector3 to)
        {
            root.position += to - from;
            foreach (VillageLayout layout in root.GetComponentsInChildren<VillageLayout>(true))
                foreach (Transform piece in layout.Pieces)
                {
                    Vector3 p = piece.position;
                    if (Physics.Raycast(new Vector3(p.x, to.y + RayHeight, p.z), Vector3.down, out RaycastHit hit, RayHeight * 2f, groundMask))
                        piece.position = new Vector3(p.x, p.y + hit.point.y - to.y, p.z);
                }
        }

        private List<Vector3> PlanSites(Mulberry32 rng, int count)
        {
            var sites = new List<Vector3>();
            Bounds area = ground.bounds;
            for (int i = 0; i < count; i++)
            {
                if (!TryPickSite(rng, sites, area, i == 1, out Vector3 site))
                {
                    Debug.LogWarning($"Villages: found room for {sites.Count} of {count} sites");
                    break;
                }
                sites.Add(site);
            }
            return sites;
        }

        private bool TryPickSite(Mulberry32 rng, List<Vector3> sites, Bounds area, bool nearFirst, out Vector3 site)
        {
            site = default;
            for (int relax = 0; relax < 4; relax++)
            {
                float spacing = minSpacing * Mathf.Max(MinSpacingFloor, 1f - 0.1f * relax);
                float clear = clearRadius * (1f - 0.25f * relax);
                float tolerance = flatTolerance * (1f + 0.5f * relax);
                float bestGap = -1f;
                for (int attempt = 0; attempt < attemptsPerSite; attempt++)
                {
                    float x = rng.Range(area.min.x + edgeMargin, area.max.x - edgeMargin);
                    float z = rng.Range(area.min.z + edgeMargin, area.max.z - edgeMargin);
                    if (!SpacedFrom(sites, x, z, spacing, nearFirst)) continue;
                    if (!IsSuitable(x, z, area.max.y, clear, tolerance, out Vector3 candidate)) continue;
                    float gap = NearestSite(sites, x, z);
                    if (gap <= bestGap) continue;
                    bestGap = gap;
                    site = candidate;
                }
                if (bestGap >= 0f) return true;
            }
            return false;
        }

        private static float NearestSite(List<Vector3> sites, float x, float z)
        {
            float nearest = float.MaxValue;
            foreach (Vector3 s in sites) nearest = Mathf.Min(nearest, Vector2.Distance(new Vector2(s.x, s.z), new Vector2(x, z)));
            return nearest;
        }

        private bool SpacedFrom(List<Vector3> sites, float x, float z, float spacing, bool nearFirst)
        {
            for (int i = 0; i < sites.Count; i++)
            {
                float distance = Vector2.Distance(new Vector2(sites[i].x, sites[i].z), new Vector2(x, z));
                if (distance < spacing || (nearFirst && i == 0 && distance > firstRivalRange)) return false;
            }
            return true;
        }

        private bool IsSuitable(float x, float z, float topY, float clear, float tolerance, out Vector3 point)
        {
            point = default;
            if (!Raycast(x, z, topY, out RaycastHit centre)) return false;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 0.25f;
                if (!Raycast(x + Mathf.Cos(angle) * flatRadius, z + Mathf.Sin(angle) * flatRadius, topY, out RaycastHit ring)) return false;
                if (Mathf.Abs(ring.point.y - centre.point.y) > tolerance) return false;
            }
            if (HasScenery(centre.point + Vector3.up * clear, clear)) return false;
            point = centre.point;
            return true;
        }

        private bool Raycast(float x, float z, float topY, out RaycastHit hit) =>
            Physics.Raycast(new Vector3(x, topY + RayHeight, z), Vector3.down, out hit, RayHeight * 2f, groundMask);

        private void CollectScenery()
        {
            GameObject root = GameObject.Find("Scenery");
            if (root == null) return;
            foreach (string group in SceneryGroups)
            {
                Transform parent = root.transform.Find(group);
                if (parent == null) continue;
                foreach (Transform item in parent) scenery.Add(item);
            }
        }

        private void ClearScenery(Vector3 site)
        {
            float sqr = clearRadius * clearRadius;
            foreach (Transform item in scenery)
            {
                Vector3 offset = item.position - site;
                offset.y = 0f;
                if (offset.sqrMagnitude < sqr) item.gameObject.SetActive(false);
            }
        }

        private bool HasScenery(Vector3 centre, float radius)
        {
            int hits = Physics.OverlapSphereNonAlloc(centre, radius, overlaps, ~groundMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits; i++)
                if (!overlaps[i].transform.IsChildOf(playerRoot)) return true;
            return false;
        }

        private bool IsResolved(Village village) => village.HasSurrendered || village.IsAlliedWith(player);

        private Village NearestOpenRival()
        {
            Village best = null;
            float bestSqr = float.MaxValue;
            foreach (Rival rival in rivals)
            {
                if (!rival.Arrived || IsResolved(rival.Village)) continue;
                float sqr = (rival.Village.transform.position - player.transform.position).sqrMagnitude;
                if (sqr >= bestSqr) continue;
                bestSqr = sqr;
                best = rival.Village;
            }
            return best;
        }

        private string Direction(Vector3 at)
        {
            Vector3 away = at - player.transform.position;
            float bearing = Mathf.Atan2(away.x, away.z) * Mathf.Rad2Deg;
            return Compass[Mathf.RoundToInt(Mathf.Repeat(bearing, 360f) / 45f) % Compass.Length];
        }

        private void Announce(string text)
        {
            message = text;
            messageUntil = Time.time + messageSeconds;
        }

        private void OnGUI()
        {
            style ??= new GUIStyle(GUI.skin.label) { fontSize = fontSize, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            if (message == null || Time.time >= messageUntil) return;
            GUI.Label(new Rect(0f, Screen.height * 0.12f, Screen.width, 50f), message, style);
        }
    }
}
