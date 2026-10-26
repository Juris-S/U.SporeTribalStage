using System.Collections;
using System.Collections.Generic;
using SporeTribal.Economy;
using UnityEngine;
using UnityEngine.AI;

namespace SporeTribal.Villagers
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class VillagerUnit : MonoBehaviour
    {
        private static readonly List<VillagerUnit> all = new List<VillagerUnit>();
        public static IReadOnlyList<VillagerUnit> All => all;

        [SerializeField] private GameObject selectionRing;
        [Tooltip("Unscaled anchor under the body where the equipped tool is held.")]
        [SerializeField] private Transform hand;
        [Tooltip("Each villager rolls its speed and turn speed within +/- this fraction, once, so a group never moves in lockstep.")]
        [SerializeField, Range(0f, 0.2f)] private float movementVariance = 0.05f;
        [Tooltip("Seconds between idle glances while standing still (min, max).")]
        [SerializeField] private Vector2 idleGlanceInterval = new Vector2(4f, 10f);
        [SerializeField] private float idleTurnSpeed = 90f;
        [SerializeField] private float gatherSeconds = 1.5f;
        [SerializeField] private int carryCapacity = 10;
        [SerializeField] private float arriveDistance = 2.2f;
        [SerializeField] private float searchRadius = 25f;
        [SerializeField] private Vector2 wanderInterval = new Vector2(5f, 15f);

        private const int WorkPriority = 40;
        private const int WanderPriority = 60;

        private NavMeshAgent agent;
        private GameObject heldTool;
        private float nextGlanceTime;
        private float glanceYaw;
        private bool glancing;
        private string gatherStage;
        private Coroutine gatherRoutine;
        private StandingSpots spots;
        private FoodNode workNode;
        private Vector3? wanderCentre;
        private float nextWanderTime;
        private TribalOrder? order;
        private int carried;
        private float interruptedAt = -10f;
        private OrderKind interruptedKind;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.speed *= 1f + Random.Range(-movementVariance, movementVariance);
            agent.angularSpeed *= 1f + Random.Range(-movementVariance, movementVariance);
            ScheduleGlance();
            SetSelected(false);
        }

        private void Start()
        {
            if (Home == null) return;
            wanderCentre = Home.DropOffCentre;
            nextWanderTime = Time.time + Random.Range(wanderInterval.x, wanderInterval.y);
        }

        private void Update()
        {
            agent.avoidancePriority = wanderCentre != null ? WanderPriority : WorkPriority;

            if (IsMoving)
            {
                glancing = false;
                ScheduleGlance();
                return;
            }

            if (wanderCentre is Vector3 centre && Time.time >= nextWanderTime)
            {
                nextWanderTime = Time.time + Random.Range(wanderInterval.x, wanderInterval.y);
                Vector2 offset = Random.insideUnitCircle * Home.WanderRadius;
                if (NavMesh.SamplePosition(centre + new Vector3(offset.x, 0f, offset.y), out NavMeshHit hit, 4f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
                return;
            }

            if (glancing)
            {
                var target = Quaternion.Euler(0f, glanceYaw, 0f);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, idleTurnSpeed * Time.deltaTime);
                if (Quaternion.Angle(transform.rotation, target) < 0.5f)
                {
                    glancing = false;
                    ScheduleGlance();
                }
            }
            else if (Time.time >= nextGlanceTime)
            {
                glanceYaw = Random.Range(0f, 360f);
                glancing = true;
            }
        }

        private void ScheduleGlance() => nextGlanceTime = Time.time + Random.Range(idleGlanceInterval.x, idleGlanceInterval.y);

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        public bool IsMoving => agent.pathPending || agent.remainingDistance > agent.stoppingDistance;

        public Vector3[] PathCorners => agent.hasPath ? agent.path.corners : System.Array.Empty<Vector3>();

        public bool IsSelected { get; private set; }
        public Village Home { get; set; }
        public ToolDefinition Tool { get; private set; }

        public void Equip(ToolDefinition tool)
        {
            if (heldTool != null) Destroy(heldTool);
            Tool = tool;
            heldTool = tool.SpawnHeld(hand);
        }
        public bool IsGathering => gatherStage != null;
        public float SearchRadius => searchRadius;
        public FoodNode GatherTarget => workNode;

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            selectionRing.SetActive(selected);
        }

        public string OrderLabel => order is TribalOrder o && (o.Kind != OrderKind.Move || IsMoving) ? o.Kind.ToString() : "none";
        public string BehaviourLabel
        {
            get
            {
                string step = gatherStage ?? (IsMoving ? (wanderCentre != null ? "wandering" : "walking") : glancing ? "looking around" : "idle");
                return carried > 0 ? $"{step} | carrying {carried}" : step;
            }
        }
        public string InterruptLabel => Time.time - interruptedAt < 2.5f ? $"{interruptedKind} cancelled" : null;

        public void Accept(TribalOrder order)
        {
            if (this.order is TribalOrder previous && (gatherRoutine != null || IsMoving))
            {
                interruptedAt = Time.time;
                interruptedKind = previous.Kind;
            }
            Cancel();
            this.order = order;
            switch (order.Kind)
            {
                case OrderKind.Move:
                    agent.SetDestination(order.Point);
                    break;
                case OrderKind.Gather:
                    workNode = order.Node;
                    gatherRoutine = StartCoroutine(GatherRoutine(order.Node));
                    break;
                case OrderKind.Equip:
                    gatherRoutine = StartCoroutine(EquipRoutine(order.Building));
                    break;
            }
        }

        private void Cancel()
        {
            if (gatherRoutine != null) StopCoroutine(gatherRoutine);
            gatherRoutine = null;
            gatherStage = null;
            carried = 0;
            order = null;
            workNode = null;
            wanderCentre = null;
            ReleaseSpot();
        }

        private IEnumerator GatherRoutine(FoodNode node)
        {
            Village home = Home;
            while (node != null)
            {
                if (!node.HasRoom)
                {
                    FoodNode next = FoodNode.Nearest(node.transform.position, searchRadius);
                    if (next != null)
                    {
                        node = next;
                        workNode = node;
                    }
                    else if (node.Available)
                    {
                        gatherStage = "waiting for a spot";
                        yield return new WaitForSeconds(0.5f);
                        continue;
                    }
                    else break;
                }

                gatherStage = "to food";
                spots = node.Spots;
                yield return WalkTo(spots.Claim(this, transform.position, arriveDistance));

                carried = 0;
                while (carried < carryCapacity && node.Available)
                {
                    gatherStage = $"picking {carried + 1}/{carryCapacity}";
                    yield return new WaitForSeconds(gatherSeconds);
                    carried += node.Take(1);
                }
                ReleaseSpot();
                if (carried == 0) continue;

                gatherStage = $"home with {carried}";
                spots = home.Spots;
                yield return WalkTo(spots.Claim(this, transform.position, home.DropOffRadius));
                ReleaseSpot();

                home.Deposit(carried);
                carried = 0;
            }
            ReleaseSpot();
            gatherStage = "no food left, returning to village";
            spots = home.Spots;
            yield return WalkTo(spots.Claim(this, transform.position, home.DropOffRadius));
            ReleaseSpot();
            order = null;
            workNode = null;
            gatherRoutine = null;
            gatherStage = null;
            wanderCentre = home.DropOffCentre;
            nextWanderTime = Time.time + Random.Range(wanderInterval.x, wanderInterval.y);
        }

        private IEnumerator EquipRoutine(ToolBuilding building)
        {
            spots = building.Spots;
            yield return WalkTo(spots.Claim(this, transform.position, building.StandRadius));
            ReleaseSpot();

            Village home = Home;
            if (Tool != building.Tool && home.TrySpend(building.Tool.Cost)) Equip(building.Tool);
            order = null;
            gatherRoutine = null;
            wanderCentre = home.DropOffCentre;
            nextWanderTime = Time.time + Random.Range(wanderInterval.x, wanderInterval.y);
        }

        private void ReleaseSpot()
        {
            spots?.Release(this);
            spots = null;
        }

        private IEnumerator WalkTo(Vector3 stop)
        {
            if (NavMesh.SamplePosition(stop, out NavMeshHit hit, 2f, NavMesh.AllAreas)) stop = hit.position;
            agent.SetDestination(stop);
            yield return null;
            float stalled = 0f;
            while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
            {
                stalled = !agent.pathPending && agent.velocity.sqrMagnitude < 0.01f ? stalled + Time.deltaTime : 0f;
                if (stalled > 2f) break;
                yield return null;
            }
        }
    }
}
