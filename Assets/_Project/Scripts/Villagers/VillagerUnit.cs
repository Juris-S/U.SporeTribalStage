using System.Collections;
using System.Collections.Generic;
using SporeTribal.Buildings;
using SporeTribal.Combat;
using SporeTribal.Definitions;
using SporeTribal.Economy;
using UnityEngine;
using UnityEngine.AI;

namespace SporeTribal.Villagers
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Health))]
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

        [Header("Combat")]
        [SerializeField] private int unarmedDamage = 3;
        [SerializeField] private float unarmedCooldown = 1.5f;
        [Tooltip("Damage per hit against buildings with bare hands (or a tool that isn't made for razing).")]
        [SerializeField] private int unarmedRazeDamage = 2;
        [Tooltip("Centre-to-centre distance at which a villager can hit its target.")]
        [SerializeField] private float attackRange = 2f;
        [Tooltip("Idle or gathering villagers drop their task to fight an enemy who is fighting within this radius.")]
        [SerializeField] private float alertRadius = 10f;
        [Tooltip("A target with this many attackers already is skipped for a free enemy nearby, so fights split into duels instead of one pile.")]
        [SerializeField] private int maxAttackersPerTarget = 2;
        [SerializeField] private float swingSeconds = 0.6f;
        [Tooltip("Tool pitch in degrees over the swing (normalised time). Negative winds up, positive strikes.")]
        [SerializeField] private AnimationCurve swingCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.45f, -60f), new Keyframe(0.6f, 80f), new Keyframe(1f, 0f));
        [Tooltip("Normalised swing time at which the blow lands and damage is applied.")]
        [SerializeField, Range(0f, 1f)] private float hitMoment = 0.6f;
        [Tooltip("How far the body steps forward at the peak of the strike.")]
        [SerializeField] private float lungeDistance = 0.4f;
        [SerializeField] private Color hitFlashColour = Color.white;
        [Tooltip("Degrees the body slumps forward when the tribe surrenders.")]
        [SerializeField] private float mournSlump = 25f;

        private const int WorkPriority = 40;
        private const int WanderPriority = 60;
        private const float AlertScanSeconds = 0.5f;
        private const float RepathSeconds = 0.2f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private NavMeshAgent agent;
        private Renderer body;
        private MaterialPropertyBlock block;
        private GameObject heldTool;
        private Vector3 toolRestPosition;
        private Quaternion toolRestRotation;
        private float nextGlanceTime;
        private float glanceYaw;
        private bool glancing;
        private string stage;
        private Coroutine routine;
        private StandingSpots spots;
        private FoodNode workNode;
        private Vector3? wanderCentre;
        private float nextWanderTime;
        private TribalOrder? order;
        private TribalOrder? resumeOrder;
        private int carried;
        private float interruptedAt = -10f;
        private OrderKind interruptedKind;
        private float nextAlertScan;
        private float nextSwingTime;
        private bool mourning;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            body = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
            Health = GetComponent<Health>();
            Health.Damaged += OnDamaged;
            Health.Died += Die;
            agent.speed *= 1f + Random.Range(-movementVariance, movementVariance);
            agent.angularSpeed *= 1f + Random.Range(-movementVariance, movementVariance);
            ScheduleGlance();
            SetSelected(false);
        }

        private void Start()
        {
            if (Home == null) return;
            Home.Surrendered += Mourn;
            SetBodyColour(Home.TribeColour);
            WanderNear(Home, Random.Range(wanderInterval.x, wanderInterval.y));
        }

        private void OnDestroy()
        {
            if (Home != null) Home.Surrendered -= Mourn;
        }

        private void Update()
        {
            if (mourning) return;
            agent.avoidancePriority = wanderCentre != null ? WanderPriority : WorkPriority;

            if (Time.time >= nextAlertScan)
            {
                nextAlertScan = Time.time + AlertScanSeconds;
                if (CanAutoDefend && NearestHostile(alertRadius, attackingOnly: true) is VillagerUnit enemy) Defend(enemy);
            }

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

        public static int CountIn(Village village)
        {
            int count = 0;
            foreach (VillagerUnit unit in all)
                if (unit.Home == village) count++;
            return count;
        }

        public bool IsMoving => agent.isOnNavMesh && (agent.pathPending || agent.remainingDistance > agent.stoppingDistance);

        public Vector3[] PathCorners => agent.hasPath ? agent.path.corners : System.Array.Empty<Vector3>();

        public bool IsSelected { get; private set; }
        public Village Home { get; set; }
        public ToolDefinition Tool { get; private set; }
        public Health Health { get; private set; }
        public bool IsAlive => Health.IsAlive;

        public void Equip(ToolDefinition tool)
        {
            if (heldTool != null) Destroy(heldTool);
            Tool = tool;
            heldTool = tool.SpawnHeld(hand);
            toolRestPosition = heldTool.transform.localPosition;
            toolRestRotation = heldTool.transform.localRotation;
        }
        public bool IsGathering => order is TribalOrder o && o.Kind == OrderKind.Gather;
        public bool IsFighting => order is TribalOrder o && o.Kind == OrderKind.Attack;
        public bool IsRazing => order is TribalOrder o && o.Kind == OrderKind.Raze;
        public bool IsAttacking => IsFighting || IsRazing;
        public Building RazeTarget => IsRazing ? order.Value.Structure : null;
        public bool HasOrder => order is TribalOrder o && (o.Kind != OrderKind.Move || IsMoving);
        public float SearchRadius => searchRadius;
        public FoodNode GatherTarget => workNode;

        private int Damage => Tool != null && Tool.Damage > 0 ? Tool.Damage : unarmedDamage;
        private float AttackCooldown => Tool != null && Tool.Damage > 0 ? Tool.AttackCooldown : unarmedCooldown;
        private int RazeDamage => Tool != null && Tool.RazeDamage > 0 ? Tool.RazeDamage : unarmedRazeDamage;

        private bool CanAutoDefend => !mourning && (!HasOrder || IsGathering || IsRazing);

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            selectionRing.SetActive(selected);
        }

        public string OrderLabel => HasOrder ? order.Value.Kind.ToString() : "none";
        public string BehaviourLabel
        {
            get
            {
                string step = stage ?? (IsMoving ? (wanderCentre != null ? "wandering" : "walking") : glancing ? "looking around" : "idle");
                return carried > 0 ? $"{step} | carrying {carried}" : step;
            }
        }
        public string InterruptLabel => Time.time - interruptedAt < 2.5f ? $"{interruptedKind} cancelled" : null;

        public void Accept(TribalOrder order)
        {
            if (mourning) return;
            if (this.order is TribalOrder previous && (routine != null || IsMoving))
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
                    routine = StartCoroutine(GatherRoutine(order.Node));
                    break;
                case OrderKind.Equip:
                    routine = StartCoroutine(EquipRoutine(order.Building));
                    break;
                case OrderKind.Attack:
                    routine = StartCoroutine(AttackRoutine(order.Target));
                    break;
                case OrderKind.Raze:
                    routine = StartCoroutine(RazeRoutine(order.Structure));
                    break;
            }
        }

        private void Cancel()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            stage = null;
            carried = 0;
            order = null;
            resumeOrder = null;
            workNode = null;
            wanderCentre = null;
            ReleaseSpot();
            RestTool();
        }

        private void WanderNear(Village home, float delay)
        {
            wanderCentre = home.DropOffCentre;
            nextWanderTime = Time.time + delay;
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
                        stage = "waiting for a spot";
                        yield return new WaitForSeconds(0.5f);
                        continue;
                    }
                    else break;
                }

                stage = "to food";
                spots = node.Spots;
                yield return WalkTo(spots.Claim(this, transform.position, arriveDistance));

                carried = 0;
                while (carried < carryCapacity && node.Available)
                {
                    stage = $"picking {carried + 1}/{carryCapacity}";
                    yield return new WaitForSeconds(gatherSeconds);
                    carried += node.Take(1);
                }
                ReleaseSpot();
                if (carried == 0) continue;

                stage = $"home with {carried}";
                spots = home.Spots;
                yield return WalkTo(spots.Claim(this, transform.position, home.DropOffRadius));
                ReleaseSpot();

                home.Deposit(carried);
                carried = 0;
            }
            ReleaseSpot();
            stage = "no food left, returning to village";
            spots = home.Spots;
            yield return WalkTo(spots.Claim(this, transform.position, home.DropOffRadius));
            ReleaseSpot();
            order = null;
            workNode = null;
            routine = null;
            stage = null;
            WanderNear(home, Random.Range(wanderInterval.x, wanderInterval.y));
        }

        private IEnumerator EquipRoutine(ToolBuilding building)
        {
            spots = building.Spots;
            yield return WalkTo(spots.Claim(this, transform.position, building.StandRadius));
            ReleaseSpot();

            Village home = Home;
            if (Tool != building.Tool && home.TrySpend(building.Tool.Cost)) Equip(building.Tool);
            order = null;
            routine = null;
            WanderNear(home, Random.Range(wanderInterval.x, wanderInterval.y));
        }

        private void Defend(VillagerUnit enemy)
        {
            TribalOrder? resume = IsGathering || IsRazing ? order : null;
            Accept(TribalOrder.Attack(enemy));
            resumeOrder = resume;
        }

        private void OnDamaged(GameObject source)
        {
            StartCoroutine(Flash());
            if (Home != null) Home.ReportAttack(transform.position);
            if (!IsAlive || !CanAutoDefend || source == null) return;
            if (source.GetComponent<VillagerUnit>() is VillagerUnit attacker && attacker.IsAlive && Home != null && Home.IsHostileTo(attacker.Home))
                Defend(attacker);
        }

        private IEnumerator AttackRoutine(VillagerUnit target)
        {
            while (true)
            {
                if (!CanStrike(target) || OthersAttacking(target) >= maxAttackersPerTarget)
                {
                    target = PickTarget(target);
                    if (target == null) break;
                    order = TribalOrder.Attack(target);
                }

                if (!InReach(target, attackRange))
                {
                    stage = "charging";
                    Vector3 away = transform.position - target.transform.position;
                    away.y = 0f;
                    agent.SetDestination(target.transform.position + away.normalized * (attackRange * 0.8f));
                    yield return new WaitForSeconds(RepathSeconds);
                    continue;
                }

                agent.ResetPath();
                Face(target.transform.position);
                if (Time.time < nextSwingTime)
                {
                    stage = "squaring up";
                    yield return null;
                    continue;
                }
                nextSwingTime = Time.time + AttackCooldown;
                VillagerUnit victim = target;
                yield return Swing(victim.transform, () =>
                {
                    if (CanStrike(victim) && InReach(victim, attackRange + 0.5f)) victim.Health.Damage(Damage, gameObject);
                });
            }

            TribalOrder? resume = resumeOrder;
            routine = null;
            order = null;
            resumeOrder = null;
            stage = null;
            if (resume is TribalOrder job && job.CanResume) Accept(job);
            else WanderNear(Home, 0f);
        }

        private static bool CanStrike(VillagerUnit target) => target != null && target.IsAlive && target.isActiveAndEnabled;

        private IEnumerator RazeRoutine(Building building)
        {
            stage = "to building";
            spots = building.Spots;
            yield return WalkTo(spots.Claim(this, transform.position, building.StandRadius));

            while (building != null && building.IsStanding && Home != null && Home.IsHostileTo(building.Owner))
            {
                agent.ResetPath();
                Face(building.transform.position);
                if (Time.time < nextSwingTime)
                {
                    stage = "squaring up";
                    yield return null;
                    continue;
                }
                nextSwingTime = Time.time + AttackCooldown;
                yield return Swing(building.transform, () =>
                {
                    if (building != null && building.IsStanding) building.Health.Damage(RazeDamage, gameObject);
                });
            }

            ReleaseSpot();
            routine = null;
            order = null;
            stage = null;
        }

        private IEnumerator Swing(Transform target, System.Action strike)
        {
            stage = "attacking";
            float peak = Mathf.Max(1f, MaxKeyValue(swingCurve));
            float lunged = 0f;
            bool struck = false;
            for (float t = 0f; t < swingSeconds; t += Time.deltaTime)
            {
                float n = t / swingSeconds;
                float angle = swingCurve.Evaluate(n);
                PoseTool(angle);

                float lunge = Mathf.Clamp01(angle / peak) * lungeDistance;
                if (agent.enabled) agent.Move(transform.forward * (lunge - lunged));
                lunged = lunge;

                if (target != null) Face(target.position);
                if (!struck && n >= hitMoment)
                {
                    struck = true;
                    strike();
                }
                yield return null;
            }
            if (agent.enabled) agent.Move(-transform.forward * lunged);
            RestTool();
        }

        private static float MaxKeyValue(AnimationCurve curve)
        {
            float max = 0f;
            foreach (Keyframe key in curve.keys) max = Mathf.Max(max, key.value);
            return max;
        }

        private void PoseTool(float pitch)
        {
            if (heldTool == null) return;
            Quaternion swing = Quaternion.Euler(pitch, 0f, 0f);
            heldTool.transform.SetLocalPositionAndRotation(swing * toolRestPosition, swing * toolRestRotation);
        }

        private void RestTool() => PoseTool(0f);

        private void Face(Vector3 point)
        {
            Vector3 to = point - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), agent.angularSpeed * Time.deltaTime);
        }

        private bool InReach(VillagerUnit other, float range)
        {
            Vector3 to = other.transform.position - transform.position;
            to.y = 0f;
            return to.sqrMagnitude <= range * range;
        }

        private int OthersAttacking(VillagerUnit target)
        {
            int count = 0;
            foreach (VillagerUnit other in all)
                if (other != this && other.order is TribalOrder o && o.Kind == OrderKind.Attack && o.Target == target) count++;
            return count;
        }

        private VillagerUnit PickTarget(VillagerUnit current)
        {
            if (Home == null) return null;
            const float CrowdPenalty = 6f;
            VillagerUnit best = null;
            float bestScore = float.MaxValue;
            foreach (VillagerUnit other in all)
            {
                if (!other.IsAlive || !Home.IsHostileTo(other.Home)) continue;
                float distance = Vector3.Distance(other.transform.position, transform.position);
                if (distance > alertRadius && other != current) continue;
                int crowd = OthersAttacking(other);
                float score = distance + crowd * CrowdPenalty + (crowd >= maxAttackersPerTarget ? 1000f : 0f);
                if (score < bestScore) { bestScore = score; best = other; }
            }
            return best;
        }

        private VillagerUnit NearestHostile(float radius, bool attackingOnly)
        {
            if (Home == null) return null;
            VillagerUnit best = null;
            float bestSqr = radius * radius;
            foreach (VillagerUnit other in all)
            {
                if (!other.IsAlive || !Home.IsHostileTo(other.Home)) continue;
                if (attackingOnly && !other.IsAttacking) continue;
                float sqr = (other.transform.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = other; }
            }
            return best;
        }

        public void Shelter()
        {
            Cancel();
            SetSelected(false);
            gameObject.SetActive(false);
        }

        public void Unshelter(Vector3 at)
        {
            transform.position = at;
            gameObject.SetActive(true);
            agent.Warp(at);
            SetBodyColour(Home != null ? Home.TribeColour : hitFlashColour);
            if (Home == null) return;
            if (Home.HasSurrendered) Mourn(Home);
            else WanderNear(Home, Random.Range(wanderInterval.x, wanderInterval.y));
        }

        private void Mourn(Village _)
        {
            if (!IsAlive || mourning || !gameObject.activeInHierarchy) return;
            Cancel();
            mourning = true;
            stage = "mourning";
            agent.ResetPath();
            agent.isStopped = true;
            agent.updateRotation = false;
            DropTool();
            transform.rotation = Quaternion.Euler(mournSlump, transform.eulerAngles.y, 0f);
        }

        private void DropTool()
        {
            if (heldTool == null) return;
            heldTool.transform.SetParent(null, true);
            heldTool.transform.SetPositionAndRotation(
                new Vector3(heldTool.transform.position.x, 0.05f, heldTool.transform.position.z),
                Quaternion.Euler(0f, Random.Range(0f, 360f), 90f));
            heldTool = null;
            Tool = null;
        }

        private void Die()
        {
            Cancel();
            SetSelected(false);
            enabled = false;
            agent.enabled = false;
            foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
            StartCoroutine(Topple());
        }

        private IEnumerator Topple()
        {
            Quaternion from = transform.rotation;
            Quaternion to = from * Quaternion.Euler(90f, 0f, 0f);
            Vector3 start = transform.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f)
            {
                transform.SetPositionAndRotation(start + Vector3.down * (0.6f * t), Quaternion.Slerp(from, to, t * t));
                yield return null;
            }
            yield return new WaitForSeconds(1.5f);
            for (float t = 0f; t < 1f; t += Time.deltaTime)
            {
                transform.position += Vector3.down * Time.deltaTime;
                yield return null;
            }
            Destroy(gameObject);
        }

        private IEnumerator Flash()
        {
            SetBodyColour(hitFlashColour);
            yield return new WaitForSeconds(0.08f);
            SetBodyColour(Home != null ? Home.TribeColour : hitFlashColour);
        }

        private void SetBodyColour(Color colour)
        {
            body.GetPropertyBlock(block);
            block.SetColor(BaseColorId, colour);
            body.SetPropertyBlock(block);
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
