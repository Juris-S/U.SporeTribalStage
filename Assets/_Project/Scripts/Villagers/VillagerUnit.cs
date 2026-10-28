using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using SporeTribal.Buildings;
using SporeTribal.Combat;
using SporeTribal.Definitions;
using SporeTribal.Economy;
using SporeTribal.Feedback;
using UnityEngine;
using UnityEngine.AI;

namespace SporeTribal.Villagers
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Health))]
    public class VillagerUnit : MonoBehaviour
    {
        private static readonly List<VillagerUnit> all = new List<VillagerUnit>();
        public static IReadOnlyList<VillagerUnit> All => all;

        [Tooltip("Ground ring under the villager: the material's colour when selected, the talk colours when the player is about to perform for them.")]
        [UnityEngine.Serialization.FormerlySerializedAs("selectionRing")]
        [SerializeField] private GameObject markerRing;
        [SerializeField, ColorUsage(true, true)] private Color talkRim = new Color(0.3f, 1f, 0.3f, 0.9f);
        [SerializeField] private Color talkFill = new Color(0.3f, 1f, 0.3f, 0.12f);
        [SerializeField, ColorUsage(true, true)] private Color attackRim = new Color(1f, 0.2f, 0.28f, 0.95f);
        [SerializeField] private Color attackFill = new Color(1f, 0.2f, 0.28f, 0.18f);
        [Tooltip("The tribe leader and band conductor: a performance cannot start without the chieftain.")]
        [SerializeField] private bool isChieftain;
        [Tooltip("Hat, feathers or mask shown only on the chieftain.")]
        [SerializeField] private GameObject chieftainHeadgear;
        [Tooltip("Headgear parts painted in the tribe colour so the chieftain matches its people (the rest keeps its own colours).")]
        [SerializeField] private Renderer[] headgearTribeParts;
        [Tooltip("Brightness of the second part relative to the tribe colour, so the band reads against the crown.")]
        [SerializeField, Range(0.3f, 1f)] private float headgearBandShade = 0.7f;
        [Tooltip("The chieftain's own weapon, held at all times outside a performance. Chieftains never take tools from buildings.")]
        [SerializeField] private ToolDefinition chieftainStaff;
        [Tooltip("Raised in place of the staff while the chieftain conducts a performance.")]
        [SerializeField] private ToolDefinition conductorBaton;
        [Tooltip("Humanoid model spawned under the visual rig. Its right hand holds the equipped tool.")]
        [SerializeField] private VillagerModel modelPrefab;
        [Tooltip("Skin tones from light to dark. Each tribe draws a random point along the range (kept apart from other tribes), each villager varies slightly around it.")]
        [SerializeField] private Color[] skinTones =
        {
            new Color(0.99f, 0.88f, 0.80f), new Color(0.96f, 0.80f, 0.69f), new Color(0.87f, 0.68f, 0.52f),
            new Color(0.73f, 0.53f, 0.37f), new Color(0.55f, 0.37f, 0.25f), new Color(0.36f, 0.23f, 0.15f)
        };
        [Tooltip("Per-villager brightness variation around the tribe's skin tone (fraction).")]
        [SerializeField, Range(0f, 0.2f)] private float skinVariance = 0.05f;
        [Tooltip("Offset of the chieftain headgear from the head bone, in world-sized units.")]
        [SerializeField] private Vector3 headgearOffset = new Vector3(0f, -0.02f, 0f);
        [SerializeField] private float headgearScale = 0.3f;
        [Tooltip("Local position and Euler rotation of the tool socket on the right hand bone.")]
        [SerializeField] private Vector3 socketPosition;
        [SerializeField] private Vector3 socketEuler;
        [Tooltip("Each villager rolls its speed and turn speed within +/- this fraction, once, so a group never moves in lockstep.")]
        [SerializeField, Range(0f, 0.2f)] private float movementVariance = 0.05f;
        [Tooltip("Pace multiplier while carrying out an order or charging at an enemy. Idle wandering and watching walk at the agent speed, which matches the walk clip.")]
        [SerializeField] private float runSpeedMultiplier = 2.5f;
        [Tooltip("Metres to the destination below which a villager walks instead of running, so short hops and arrivals are not a sprint.")]
        [SerializeField] private float runMinDistance = 6f;
        [Tooltip("Seconds between idle glances while standing still (min, max).")]
        [SerializeField] private Vector2 idleGlanceInterval = new Vector2(10f, 24f);
        [SerializeField] private float idleTurnSpeed = 90f;
        [Tooltip("Degrees per second a villager turns on the spot toward its route before it starts walking.")]
        [SerializeField] private float turnSpeed = 280f;
        [Tooltip("Deceleration in metres per second squared while a villager brakes to turn on the spot. Higher stops sooner, lower slides.")]
        [SerializeField] private float turnBraking = 14f;
        [Tooltip("Seconds between fidgets (glance or breath) while attending a performance (min, max).")]
        [SerializeField] private Vector2 attendFidgetInterval = new Vector2(4f, 9f);
        [SerializeField] private float gatherSeconds = 1.5f;
        [SerializeField] private int carryCapacity = 10;
        [SerializeField] private float arriveDistance = 1.4f;
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
        [Tooltip("A hit villager or building cries out: free tribemates within this radius of the victim join the fight.")]
        [SerializeField] private float helpRadius = 18f;
        [Tooltip("Defenders keep picking targets anywhere within this radius of their own village centre, not just near themselves.")]
        [SerializeField] private float leashRadius = 30f;
        [Tooltip("A target with this many attackers already is skipped for a free enemy nearby, so fights split into duels instead of one pile.")]
        [SerializeField] private int maxAttackersPerTarget = 2;
        [SerializeField] private float swingSeconds = 0.6f;
        [Tooltip("Normalised swing time at which the blow lands and damage is applied.")]
        [SerializeField, Range(0f, 1f)] private float hitMoment = 0.6f;
        [SerializeField] private Color hitFlashColour = Color.white;
        [Tooltip("Degrees the body slumps forward when the tribe surrenders.")]
        [SerializeField] private float mournSlump = 25f;

        private const float TurnStopDegrees = 50f;
        private const float TurnResumeDegrees = 15f;
        private const int WorkPriority = 40;
        private const int WanderPriority = 60;
        private const float StallGiveUpSeconds = 1.5f;
        private const float AlertScanSeconds = 0.5f;
        private const float RepathSeconds = 0.2f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        private static readonly int FillColorId = Shader.PropertyToID("_FillColor");

        private NavMeshAgent agent;
        private VillagerModel model;
        private float baseSpeed;
        private bool turning;
        private bool charging;
        private Color skinTone;
        private float clipUntil;
        private static readonly Dictionary<Village, float> tribeTones = new Dictionary<Village, float>();
        private const float TribeToneGap = 0.4f;
        private MaterialPropertyBlock markerBlock;
        private Renderer markerRenderer;
        private GameObject heldTool;
        private Vector3 toolRestPosition;
        private Quaternion toolRestRotation;
        private float nextGlanceTime;
        private float nextFidgetTime;
        private float fidgetYaw;
        private const float RingLift = 0.03f;
        private const float RingGroundSearch = 3f;
        private VillagerVisualRig rig;
        private float glanceYaw;
        private bool glancing;
        private float glanceStarted;
        private const float GlanceGiveUpSeconds = 4f;
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
        private VillagerFeedback feedback;
        private WaitForSeconds gatherWait;
        private WaitForSeconds swingWait;
        private Tween swingTween;
        private VillagerUnit swingVictim;
        private Building swingStructure;
        private bool swingStruck;
        private static readonly WaitForSeconds RepathWait = new WaitForSeconds(RepathSeconds);
        private static readonly WaitForSeconds SpotWait = new WaitForSeconds(0.5f);

        private void Awake()
        {
            priorityJitter = Random.Range(0, 15);
            agent = GetComponent<NavMeshAgent>();
            feedback = new VillagerFeedback(agent, PoseTool, SetTilt);
            gatherWait = new WaitForSeconds(gatherSeconds);
            swingWait = new WaitForSeconds(swingSeconds);
            BuildVisualRig();
            markerBlock = new MaterialPropertyBlock();
            markerRenderer = markerRing.GetComponent<Renderer>();
            Health =GetComponent<Health>();
            Health.Damaged += OnDamaged;
            Health.Died += Die;
            agent.speed *= 1f + Random.Range(-movementVariance, movementVariance);
            agent.angularSpeed *= 1f + Random.Range(-movementVariance, movementVariance);
            baseSpeed = agent.speed;
            turnSpeed *= 1f + Random.Range(-movementVariance, movementVariance);
            ScheduleGlance();
            SetSelected(false);
        }

        private void Start()
        {
            if (Home == null) return;
            Home.Surrendered += Mourn;
            skinTone = RollSkinTone(Home);
            Repaint(false);
            TintHeadgear();
            WanderNear(Home, Random.Range(wanderInterval.x, wanderInterval.y));
        }

        private void OnDestroy()
        {
            if (Home != null) Home.Surrendered -= Mourn;
            if (rig != null) Destroy(rig.gameObject);
        }

        private void BuildVisualRig()
        {
            var rootRenderer = GetComponent<MeshRenderer>();
            float feet = GetComponent<MeshFilter>().sharedMesh.bounds.min.y;

            rig = new GameObject(name + " Visual").AddComponent<VillagerVisualRig>();
            rig.Init(transform, feet);

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child != markerRing.transform) child.SetParent(rig.transform, true);
            }
            rootRenderer.enabled = false;

            model = Instantiate(modelPrefab, rig.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            rig.SetModel(model.transform, model.ModelScale);
            model.Init(transform);
            model.AlignToolSocket(socketPosition, socketEuler);
            skinTone = skinTones[0];
            Repaint(false);
        }

        private Color RollSkinTone(Village tribe)
        {
            if (!tribeTones.TryGetValue(tribe, out float position))
            {
                position = PickTribeTonePosition();
                tribeTones[tribe] = position;
            }
            Color tone = SampleSkinTones(position) * (1f + Random.Range(-skinVariance, skinVariance));
            tone.a = 1f;
            return tone;
        }

        private static float PickTribeTonePosition()
        {
            float best = Random.value;
            float bestGap = -1f;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                float candidate = Random.value;
                float gap = 1f;
                foreach (float taken in tribeTones.Values) gap = Mathf.Min(gap, Mathf.Abs(candidate - taken));
                if (gap >= TribeToneGap) return candidate;
                if (gap > bestGap) { best = candidate; bestGap = gap; }
            }
            return best;
        }

        private Color SampleSkinTones(float position)
        {
            float scaled = Mathf.Clamp01(position) * (skinTones.Length - 1);
            int low = Mathf.FloorToInt(scaled);
            int high = Mathf.Min(low + 1, skinTones.Length - 1);
            return Color.Lerp(skinTones[low], skinTones[high], scaled - low);
        }

        private void Repaint(bool flash)
        {
            Color tribe = Home != null ? Home.TribeColour : hitFlashColour;
            model.SetSkin(flash ? hitFlashColour : skinTone);
            model.SetClothing(flash ? hitFlashColour : tribe);
        }

        private void Update()
        {
            if (mourning) return;
            agent.avoidancePriority = (wanderCentre != null ? WanderPriority : WorkPriority) + priorityJitter;
            WatchStall();
            agent.updateRotation = false;
            TurnToTravel();

            if (Time.time >= nextAlertScan)
            {
                nextAlertScan = Time.time + AlertScanSeconds;
                if (IsAttackTarget) { if (!IsFighting && !IsBeingAttacked()) SetAttackTarget(false); }
                else if (IsFighting && TribeHasAttackTarget()) SetAttackTarget(true);
                if (CanAutoDefend && NearestHostile(alertRadius, attackingOnly: true) is VillagerUnit enemy) Defend(enemy);
            }

            if (IsMoving)
            {
                glancing = false;
                ScheduleGlance();
                return;
            }

            if (IsAttending)
            {
                glancing = false;
                ScheduleGlance();
                Fidget();
                Face(order.Value.Look, fidgetYaw);
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
                if (Quaternion.Angle(transform.rotation, target) < 0.5f || Time.time - glanceStarted > GlanceGiveUpSeconds)
                {
                    glancing = false;
                    ScheduleGlance();
                }
            }
            else if (Time.time >= nextGlanceTime)
            {
                glanceYaw = transform.eulerAngles.y + Random.Range(-30f, 30f);
                glanceStarted = Time.time;
                glancing = true;
            }
        }

        private bool Hurrying =>
            (charging || (wanderCentre == null && order is TribalOrder current && current.Kind != OrderKind.Watch))
            && (agent.destination - transform.position).sqrMagnitude > runMinDistance * runMinDistance;

        private float Pace => Hurrying ? baseSpeed * runSpeedMultiplier : baseSpeed;

        private void TurnToTravel()
        {
            bool travelling = agent.isOnNavMesh && agent.hasPath && !agent.isStopped && !glancing && !feedback.IsBusy;
            if (!travelling)
            {
                agent.speed = Pace;
                turning = false;
                return;
            }

            Vector3 to = agent.steeringTarget - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.04f)
            {
                agent.speed = Pace;
                return;
            }

            Quaternion want = Quaternion.LookRotation(to);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
            float off = Quaternion.Angle(transform.rotation, want);
            turning = turning ? off > TurnResumeDegrees : off > TurnStopDegrees;
            if (turning) agent.velocity = Vector3.MoveTowards(agent.velocity, Vector3.zero, turnBraking * Time.deltaTime);
            agent.speed = turning ? 0f : Pace * Mathf.Lerp(1f, 0.6f, Mathf.InverseLerp(0f, TurnResumeDegrees, off));
        }

        private void Fidget()
        {
            if (feedback.IsBusy || Time.time < clipUntil)
            {
                fidgetYaw = 0f;
                nextFidgetTime = Time.time + 1f;
                return;
            }
            if (Time.time < nextFidgetTime) return;
            if (fidgetYaw != 0f)
            {
                fidgetYaw = 0f;
                nextFidgetTime = Time.time + Random.Range(attendFidgetInterval.x, attendFidgetInterval.y);
            }
            else
            {
                fidgetYaw = Random.Range(10f, 30f) * (Random.value < 0.5f ? -1f : 1f);
                nextFidgetTime = Time.time + Random.Range(0.8f, 1.6f);
            }
        }

        private void SetTilt(float degrees) => rig.Tilt = degrees;

        private void LateUpdate()
        {
            if (!markerRing.activeSelf) return;
            Transform ring = markerRing.transform;
            Vector3 at = transform.position;
            if (NavMesh.SamplePosition(at, out NavMeshHit ground, RingGroundSearch, NavMesh.AllAreas)) at.y = ground.position.y;
            ring.SetPositionAndRotation(
                new Vector3(at.x, at.y + RingLift, at.z),
                Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * Quaternion.Euler(90f, 0f, 0f));
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

        private float stalledFor;
        private int priorityJitter;

        private void WatchStall()
        {
            if (turning || !agent.isOnNavMesh || agent.pathPending || !agent.hasPath || agent.remainingDistance <= agent.stoppingDistance || agent.velocity.sqrMagnitude > 0.01f)
            {
                stalledFor = 0f;
                return;
            }
            stalledFor += Time.deltaTime;
            if (stalledFor < StallGiveUpSeconds) return;
            stalledFor = 0f;
            agent.ResetPath();
        }

        public bool IsMoving => agent.isOnNavMesh && (agent.pathPending || agent.remainingDistance > agent.stoppingDistance);

        public Vector3[] PathCorners => agent.hasPath ? agent.path.corners : System.Array.Empty<Vector3>();

        public bool IsChieftain => isChieftain;
        public void MakeChieftain()
        {
            isChieftain = true;
            if (chieftainHeadgear != null)
            {
                chieftainHeadgear.transform.SetParent(model.HeadAnchor, false);
                chieftainHeadgear.transform.SetLocalPositionAndRotation(headgearOffset, Quaternion.identity);
                chieftainHeadgear.transform.localScale = Vector3.one * headgearScale;
                chieftainHeadgear.SetActive(true);
            }
            TintHeadgear();
            HoldChieftainTool();
        }

        private void HoldChieftainTool()
        {
            if (!isChieftain) return;
            bool conducting = order is TribalOrder o && o.Kind == OrderKind.Perform && conductorBaton != null;
            ToolDefinition wanted = conducting ? conductorBaton : chieftainStaff;
            if (wanted != null && Tool != wanted) Equip(wanted);
        }
        public bool IsAttending => order is TribalOrder o && (o.Kind == OrderKind.Perform || o.Kind == OrderKind.Watch);
        public int OrderSerial { get; private set; }
        public bool IsIdle => IsFree && !IsChieftain;
        public bool IsFree => IsAlive && !mourning && !HasOrder && !IsTalkTarget && !IsAttackTarget;
        public bool IsSelected { get; private set; }
        public bool IsTalkTarget { get; private set; }
        public bool IsAttackTarget { get; private set; }
        public Village Home { get; set; }
        public ToolDefinition Tool { get; private set; }
        public Health Health { get; private set; }
        public bool IsAlive => Health.IsAlive;

        public void Equip(ToolDefinition tool)
        {
            if (heldTool != null) Destroy(heldTool);
            Tool = tool;
            heldTool = tool.SpawnHeld(model.ToolSocket);
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
            if (selected && !IsSelected) feedback.Selected();
            IsSelected = selected;
            RefreshMarker();
        }

        public void SetTalkTarget(bool talkTarget)
        {
            IsTalkTarget = talkTarget;
            RefreshMarker();
        }

        public void SetAttackTarget(bool attackTarget)
        {
            IsAttackTarget = attackTarget;
            RefreshMarker();
        }

        private bool TribeHasAttackTarget()
        {
            foreach (VillagerUnit other in all)
                if (other.Home == Home && other.IsAttackTarget) return true;
            return false;
        }

        private bool IsBeingAttacked()
        {
            foreach (VillagerUnit other in all)
                if (other.IsAlive && other.order is TribalOrder o && o.Kind == OrderKind.Attack && o.Target == this) return true;
            return false;
        }

        private void RefreshMarker()
        {
            bool tinted = !IsSelected && (IsTalkTarget || IsAttackTarget);
            markerRing.SetActive(IsSelected || tinted);
            if (tinted)
            {
                markerBlock.SetColor(RimColorId, IsAttackTarget ? attackRim : talkRim);
                markerBlock.SetColor(FillColorId, IsAttackTarget ? attackFill : talkFill);
                markerRenderer.SetPropertyBlock(markerBlock);
            }
            else markerRenderer.SetPropertyBlock(null);
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
            OrderSerial++;
            if (order.Kind == OrderKind.Attack && TribeHasAttackTarget()) SetAttackTarget(true);
            HoldChieftainTool();
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
                case OrderKind.Perform:
                case OrderKind.Watch:
                    routine = StartCoroutine(AttendRoutine(order));
                    break;
            }
        }

        public void Warp(Vector3 position)
        {
            Cancel();
            order = null;
            agent.Warp(position);
        }

        public void Release(bool resumeWander)
        {
            if (!IsAttending) return;
            Cancel();
            HoldChieftainTool();
            if (resumeWander && Home != null && !mourning) WanderNear(Home, Random.Range(wanderInterval.x, wanderInterval.y));
        }

        public void Bounce(float seconds, float height, bool playTool)
        {
            if (!CanAnimate) return;
            feedback.Stop();
            PlayFor(VillagerModel.Clip.Dance, seconds, Random.Range(0, 6));
        }

        public void Dance(float seconds, bool liked)
        {
            if (!CanAnimate) return;
            feedback.Stop();
            if (liked) PlayFor(VillagerModel.Clip.Dance, seconds, Random.Range(0, 6));
            else PlayFor(VillagerModel.Clip.CombatIdle, seconds);
        }

        public void Shove(Vector3 direction, float distance, float seconds, float tilt)
        {
            if (CanAnimate) feedback.Shove(direction, distance, seconds, tilt);
        }

        public void Circle(Vector3 centre, float seconds, float pulse, float beats)
        {
            if (CanAnimate) feedback.Circle(centre, seconds, pulse, beats);
        }

        public void Happy()
        {
            nextFidgetTime = Time.time + 1f;
            if (CanAnimate) feedback.Happy();
        }

        public void Excited()
        {
            nextFidgetTime = Time.time + 1f;
            if (CanAnimate) feedback.Excited();
        }

        private bool CanAnimate => isActiveAndEnabled && !mourning;

        private void PlayFor(VillagerModel.Clip clip, float seconds, int variant = 0)
        {
            model.Play(clip, variant);
            clipUntil = Time.time + seconds;
            Tween.Delay(this, seconds, static unit => unit.EndClip());
        }

        private void EndClip()
        {
            if (Time.time + 0.02f < clipUntil || !CanAnimate) return;
            clipUntil = 0f;
            model.Play(VillagerModel.Clip.Locomotion);
        }

        private void Cancel()
        {
            feedback.Stop();
            clipUntil = 0f;
            charging = false;
            model.Play(VillagerModel.Clip.Locomotion);
            fidgetYaw = 0f;
            SetTilt(0f);
            if (swingTween.isAlive) swingTween.Stop();
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
                        yield return SpotWait;
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
                    model.Play(VillagerModel.Clip.Gather);
                    for (float t = 0f; t < gatherSeconds; t += Time.deltaTime)
                    {
                        if (node != null) Face(node.transform.position);
                        yield return null;
                    }
                    carried += node.Take(1);
                }
                ReleaseSpot();
                model.Play(VillagerModel.Clip.Locomotion);
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

        private IEnumerator AttendRoutine(TribalOrder order)
        {
            bool performing = order.Kind == OrderKind.Perform;
            stage = performing ? "to the stage" : "gathering to watch";
            yield return WalkTo(order.Point);
            if (agent.isOnNavMesh) agent.ResetPath();
            stage = performing ? "performing" : "watching";
            while (true) yield return null;
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
            Flash();
            if (IsAlive && !swingTween.isAlive) model.Play(VillagerModel.Clip.Hit);
            if (Home == null) return;
            VillagerUnit attacker = source != null && source.TryGetComponent(out VillagerUnit hitBy) ? hitBy : null;
            if (ShouldFightBack(attacker)) Defend(attacker);
            Home.ReportAttack(transform.position, attacker);
        }

        private bool ShouldFightBack(VillagerUnit attacker) =>
            IsAlive && !mourning && !IsFighting && attacker != null && attacker.IsAlive && Home.IsHostileTo(attacker.Home);

        public static void CallForHelp(Village tribe, Vector3 where, VillagerUnit attacker)
        {
            if (tribe == null || attacker == null || !attacker.IsAlive) return;
            foreach (VillagerUnit ally in all)
            {
                if (ally.Home != tribe || !ally.IsAlive || ally.IsFighting || !ally.CanAutoDefend) continue;
                float range = ally.helpRadius;
                if ((ally.transform.position - where).sqrMagnitude > range * range) continue;
                ally.Defend(attacker);
            }
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
                    charging = true;
                    model.Play(VillagerModel.Clip.Locomotion);
                    Vector3 away = transform.position - target.transform.position;
                    away.y = 0f;
                    agent.SetDestination(target.transform.position + away.normalized * (attackRange * 0.8f));
                    yield return RepathWait;
                    continue;
                }

                charging = false;
                agent.ResetPath();
                Face(target.transform.position);
                if (Time.time < nextSwingTime)
                {
                    stage = "squaring up";
                    model.Play(VillagerModel.Clip.CombatIdle);
                    yield return null;
                    continue;
                }
                nextSwingTime = Time.time + AttackCooldown;
                yield return Swing(target, null);
            }

            charging = false;
            model.Play(VillagerModel.Clip.Locomotion);
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
                    model.Play(VillagerModel.Clip.CombatIdle);
                    yield return null;
                    continue;
                }
                nextSwingTime = Time.time + AttackCooldown;
                yield return Swing(null, building);
            }

            model.Play(VillagerModel.Clip.Locomotion);
            ReleaseSpot();
            routine = null;
            order = null;
            stage = null;
        }

        private WaitForSeconds Swing(VillagerUnit victim, Building structure)
        {
            if (swingTween.isAlive) swingTween.Complete();
            stage = "attacking";
            model.Play(VillagerModel.Clip.Attack, seconds: swingSeconds);
            swingVictim = victim;
            swingStructure = structure;
            swingStruck = false;
            swingTween = Tween.Custom(this, 0f, 1f, swingSeconds, static (unit, n) => unit.SwingStep(n), Ease.Linear);
            return swingWait;
        }

        private void SwingStep(float n)
        {
            Transform aim = swingVictim != null ? swingVictim.transform : swingStructure != null ? swingStructure.transform : null;
            if (aim != null) Face(aim.position);
            if (swingStruck || n < hitMoment) return;
            swingStruck = true;
            if (swingVictim != null)
            {
                if (CanStrike(swingVictim) && InReach(swingVictim, attackRange + 0.5f)) Hit(swingVictim.Health, Damage);
            }
            else if (swingStructure != null && swingStructure.IsStanding) Hit(swingStructure.Health, RazeDamage);
        }

        private void Hit(IDamageable target, int amount) => target.Damage(amount, gameObject);

        private void PoseTool(float pitch)
        {
            if (heldTool == null) return;
            Quaternion swing = Quaternion.Euler(pitch, 0f, 0f);
            heldTool.transform.SetLocalPositionAndRotation(swing * toolRestPosition, swing * toolRestRotation);
        }

        private void RestTool() => PoseTool(0f);

        private void Face(Vector3 point, float yaw = 0f)
        {
            Vector3 to = point - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(Quaternion.Euler(0f, yaw, 0f) * to), agent.angularSpeed * Time.deltaTime);
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
                if (other != current && distance > alertRadius && !InLeash(other)) continue;
                int crowd = OthersAttacking(other);
                float score = distance + crowd * CrowdPenalty + (crowd >= maxAttackersPerTarget ? 1000f : 0f);
                if (score < bestScore) { bestScore = score; best = other; }
            }
            return best;
        }

        private bool InLeash(VillagerUnit other) =>
            (other.transform.position - Home.DropOffCentre).sqrMagnitude <= leashRadius * leashRadius;

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
            rig.gameObject.SetActive(false);
        }

        public void Unshelter(Vector3 at)
        {
            transform.position = at;
            gameObject.SetActive(true);
            rig.gameObject.SetActive(true);
            rig.Follow();
            agent.Warp(at);
            Repaint(false);
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
            model.Die();
            agent.enabled = false;
            foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
            Topple();
        }

        private void Topple()
        {
            float y = transform.position.y;
            Sequence.Create()
                .ChainDelay(2f)
                .Chain(Tween.PositionY(transform, y - 1.6f, 1f, Ease.Linear))
                .ChainCallback(gameObject, static go => Destroy(go));
        }

        private void Flash()
        {
            Repaint(true);
            Tween.Delay(this, 0.08f, static unit => unit.Repaint(false));
        }

        private void TintHeadgear()
        {
            if (headgearTribeParts == null || Home == null) return;
            for (int i = 0; i < headgearTribeParts.Length; i++)
            {
                Renderer part = headgearTribeParts[i];
                if (part == null) continue;
                Color colour = i == 0 ? Home.TribeColour : Home.TribeColour * headgearBandShade;
                colour.a = 1f;
                var partBlock = new MaterialPropertyBlock();
                partBlock.SetColor(BaseColorId, colour);
                part.SetPropertyBlock(partBlock);
            }
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
                stalled = !turning && !agent.pathPending && agent.velocity.sqrMagnitude < 0.01f ? stalled + Time.deltaTime : 0f;
                if (stalled > 2f) break;
                yield return null;
            }
        }
    }
}
