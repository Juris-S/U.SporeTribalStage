using System.Collections.Generic;
using SporeTribal.Definitions;
using SporeTribal.Economy;
using SporeTribal.Orders;
using SporeTribal.Villagers;
using UnityEngine;
using UnityEngine.AI;

namespace SporeTribal.AI
{
    public class SocialPerformanceDirector : MonoBehaviour
    {
        public enum State { Idle, Approaching, Mustering, Waiting, Calling }
        public enum Outcome { Allied, Pleased, Disappointed, Interrupted }

        [System.Serializable]
        public struct TierTuning
        {
            [Tooltip("Multiplies the time the audience gives for each call.")]
            public float time;
            [Tooltip("Multiplies the progress gained per correct answer.")]
            public float gain;
            [Tooltip("Multiplies the progress lost per mistake.")]
            public float penalty;
        }

        [SerializeField] private Village village;
        [Tooltip("The tribe the gift and soothing song hotkeys are aimed at.")]
        [SerializeField] private Village rival;
        [Tooltip("The three instrument tools: they supply the icons and sounds for the calls.")]
        [SerializeField] private ToolDefinition[] instruments;
        [SerializeField] private AudioClip failClip;

        [Header("Stage")]
        [Tooltip("Metres between the chieftain and the audience centre.")]
        [SerializeField] private float stageDistance = 5f;
        [Tooltip("How far out the crowd comes to meet the band, as a fraction of their village's wander radius, so they never leave home.")]
        [SerializeField, Range(0f, 1f)] private float crowdReach = 0.8f;
        [Tooltip("The crowd notices the chieftain this many metres beyond the meeting point and walks out to meet the band.")]
        [SerializeField] private float noticeDistance = 8f;
        [Tooltip("If the band is still on its way after this many seconds, the crowd meets it wherever the chieftain is.")]
        [SerializeField] private float approachTimeout = 45f;
        [SerializeField] private float bandRadius = 2.2f;
        [SerializeField] private float bandArc = 120f;
        [SerializeField] private float audienceRadius = 2.5f;
        [SerializeField] private int maxAudience = 6;
        [Tooltip("The recital starts once everyone is in place, or after this many seconds.")]
        [SerializeField] private float musterTimeout = 20f;

        [Header("Rounds")]
        [SerializeField] private float callSeconds = 5f;
        [SerializeField] private float pauseSeconds = 1.5f;
        [Tooltip("Where the friendship meter starts, so a few mistakes end the recital.")]
        [SerializeField, Range(0f, 0.9f)] private float startProgress = 0.35f;
        [Tooltip("Meter gained by one musician answering correctly. Each extra matching musician adds 50%.")]
        [SerializeField] private float baseIncrement = 0.25f;
        [SerializeField] private float failPenalty = 0.15f;
        [Tooltip("Relationship the rival gains when the meter fills. 70 is an alliance.")]
        [SerializeField] private int rewardAtFull = 70;
        [Tooltip("Relationship lost when the crowd leaves disappointed.")]
        [SerializeField] private int disappointment = 5;

        [Header("Tier tolerance")]
        [SerializeField] private TierTuning harsh = new TierTuning { time = 0.7f, gain = 1f, penalty = 1.5f };
        [SerializeField] private TierTuning neutral = new TierTuning { time = 1f, gain = 1f, penalty = 1f };
        [SerializeField] private TierTuning warm = new TierTuning { time = 1.25f, gain = 1.25f, penalty = 0.6f };

        [Header("Gifts")]
        [SerializeField] private int smallGift = 50;
        [SerializeField] private int largeGift = 100;

        private readonly List<VillagerUnit> band = new List<VillagerUnit>();
        private readonly List<VillagerUnit> audience = new List<VillagerUnit>();
        private readonly List<VillagerUnit> candidates = new List<VillagerUnit>();
        private TierTuning tuning;
        private Vector3 audienceCentre;
        private Vector3 stagePoint;
        private Vector3 toBand;
        private float stateUntil;
        private float callEnd;
        private float callLength = 1f;

        public Village Village => village;
        public Village Rival => rival;
        public IReadOnlyList<ToolDefinition> Instruments => instruments;
        public State Current { get; private set; }
        public bool IsActive => Current != State.Idle;
        public Village Target { get; private set; }
        public float Progress { get; private set; }
        public InstrumentKind Demanded { get; private set; }
        public VillagerUnit Caller { get; private set; }
        public float CallRemaining01 => Current == State.Calling ? Mathf.Clamp01((callEnd - Time.time) / callLength) : 0f;
        public int SmallGift => smallGift;
        public int LargeGift => largeGift;

        public event System.Action<Village> Started;
        public event System.Action<InstrumentKind, VillagerUnit> CallStarted;
        public event System.Action<bool, InstrumentKind> Answered;
        public event System.Action<Outcome, Village> Ended;

        public ToolDefinition ToolFor(InstrumentKind kind)
        {
            foreach (ToolDefinition tool in instruments)
                if (tool != null && tool.Instrument == kind) return tool;
            return null;
        }

        public int MatchingCount(InstrumentKind kind)
        {
            int count = 0;
            foreach (VillagerUnit unit in band)
                if (!unit.IsChieftain && unit.Tool != null && unit.Tool.Instrument == kind) count++;
            return count;
        }

        public RefusalReason TryBegin(IReadOnlyList<VillagerUnit> performers, Village target)
        {
            if (target == null || target.HasSurrendered) return RefusalReason.TargetGone;
            if (IsActive && target == Target) return Join(performers);
            bool chieftain = false;
            bool instrument = false;
            foreach (VillagerUnit unit in performers)
            {
                chieftain |= unit.IsChieftain;
                instrument |= unit.Tool != null && unit.Tool.IsInstrument;
            }
            if (!chieftain) return RefusalReason.NeedChieftain;
            if (!instrument) return RefusalReason.NoInstrument;
            if (target.TierTowards(village) == RelationshipTier.AtWar && !target.IsSoothed) return RefusalReason.WarringTribe;

            if (FindCrowd(target) == 0) return RefusalReason.NoAudience;

            if (IsActive) End(Outcome.Interrupted);
            Target = target;
            foreach (VillagerUnit unit in performers) band.Add(unit);

            Vector3 mean = Vector3.zero;
            foreach (VillagerUnit unit in band) mean += unit.transform.position;
            Stage(mean / band.Count);
            PlaceBand();
            foreach (VillagerUnit unit in band) unit.Health.Damaged += OnBandHit;
            target.Attacked += OnTargetAttacked;

            tuning = TuningFor(target.TierTowards(village));
            Progress = startProgress;
            Current = State.Approaching;
            stateUntil = Time.time + approachTimeout;
            Started?.Invoke(target);
            return RefusalReason.None;
        }

        private RefusalReason Join(IReadOnlyList<VillagerUnit> performers)
        {
            foreach (VillagerUnit unit in performers)
            {
                if (band.Contains(unit)) continue;
                band.Add(unit);
                unit.Health.Damaged += OnBandHit;
            }
            PlaceBand();
            return RefusalReason.None;
        }

        private void Stage(Vector3 from)
        {
            Vector3 centre = Target.DropOffCentre;
            toBand = from - centre;
            toBand.y = 0f;
            float distance = toBand.magnitude;
            toBand = distance < 0.1f ? Vector3.back : toBand / distance;
            float reach = Mathf.Clamp(distance - stageDistance, 0f, Target.WanderRadius * crowdReach);
            audienceCentre = OnNavMesh(centre + toBand * reach);
            stagePoint = audienceCentre + toBand * stageDistance;
        }

        private int FindCrowd(Village target)
        {
            candidates.Clear();
            foreach (VillagerUnit unit in VillagerUnit.All)
                if (unit.Home == target && unit.IsAlive && !unit.IsAttacking && !unit.IsAttending) candidates.Add(unit);
            return candidates.Count;
        }

        private bool Noticed(VillagerUnit chieftain)
        {
            if (Time.time >= stateUntil || !chieftain.IsMoving) return true;
            Vector3 to = chieftain.transform.position - Target.DropOffCentre;
            to.y = 0f;
            float notice = Target.WanderRadius * crowdReach + stageDistance + noticeDistance;
            return to.sqrMagnitude <= notice * notice;
        }

        private void Meet(VillagerUnit chieftain)
        {
            Stage(chieftain.transform.position);
            if (FindCrowd(Target) == 0)
            {
                End(Outcome.Interrupted);
                return;
            }
            candidates.Sort((a, b) => (a.transform.position - audienceCentre).sqrMagnitude.CompareTo((b.transform.position - audienceCentre).sqrMagnitude));
            for (int i = 0; i < Mathf.Min(maxAudience, candidates.Count); i++) audience.Add(candidates[i]);

            PlaceBand();
            PlaceAudience();
            Current = State.Mustering;
            stateUntil = Time.time + musterTimeout;
        }

        private void PlaceBand()
        {
            int musicians = 0;
            foreach (VillagerUnit unit in band) if (!unit.IsChieftain) musicians++;
            int placed = 0;
            foreach (VillagerUnit unit in band)
            {
                Vector3 slot = stagePoint;
                if (!unit.IsChieftain)
                {
                    float t = musicians < 2 ? 0.5f : (float)placed / (musicians - 1);
                    slot = stagePoint + Quaternion.AngleAxis(Mathf.Lerp(-bandArc * 0.5f, bandArc * 0.5f, t), Vector3.up) * toBand * bandRadius;
                    placed++;
                }
                unit.Accept(TribalOrder.Perform(Target, OnNavMesh(slot), audienceCentre));
            }
        }

        private void PlaceAudience()
        {
            for (int i = 0; i < audience.Count; i++)
            {
                float t = audience.Count < 2 ? 0.5f : (float)i / (audience.Count - 1);
                Vector3 slot = audienceCentre + Quaternion.AngleAxis(Mathf.Lerp(-90f, 90f, t), Vector3.up) * toBand * audienceRadius;
                audience[i].Accept(TribalOrder.Watch(OnNavMesh(slot), stagePoint));
            }
        }

        private static Vector3 OnNavMesh(Vector3 slot) =>
            NavMesh.SamplePosition(slot, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : slot;

        private TierTuning TuningFor(RelationshipTier tier) => tier switch
        {
            RelationshipTier.AtWar => harsh,
            RelationshipTier.Unfriendly => harsh,
            RelationshipTier.Friendly => warm,
            _ => neutral
        };

        private void Update()
        {
            if (!IsActive) return;
            Prune();
            if (!IsActive) return;

            switch (Current)
            {
                case State.Approaching:
                    VillagerUnit chieftain = Chieftain();
                    if (Noticed(chieftain)) Meet(chieftain);
                    break;
                case State.Mustering:
                    if (Time.time >= stateUntil || AllInPlace()) BeginRecital();
                    break;
                case State.Waiting:
                    if (Time.time >= stateUntil) StartCall();
                    break;
                case State.Calling:
                    if (Time.time >= callEnd) Fail();
                    break;
            }
        }

        private bool AllInPlace()
        {
            foreach (VillagerUnit unit in band) if (unit.IsMoving) return false;
            foreach (VillagerUnit unit in audience) if (unit.IsMoving) return false;
            return true;
        }

        private void Prune()
        {
            band.RemoveAll(unit => unit == null || !unit.IsAlive || !unit.IsAttending);
            audience.RemoveAll(unit => unit == null || !unit.IsAlive || !unit.IsAttending);
            bool crowdGone = Current != State.Approaching && audience.Count == 0;
            if (Target == null || Target.HasSurrendered || crowdGone || Chieftain() == null || !HasInstrument())
                End(Outcome.Interrupted);
        }

        private VillagerUnit Chieftain()
        {
            foreach (VillagerUnit unit in band) if (unit.IsChieftain) return unit;
            return null;
        }

        private bool HasInstrument()
        {
            foreach (VillagerUnit unit in band) if (unit.Tool != null && unit.Tool.IsInstrument) return true;
            return false;
        }

        private void BeginRecital()
        {
            Current = State.Waiting;
            stateUntil = Time.time + pauseSeconds;
        }

        private void StartCall()
        {
            Demanded = (InstrumentKind)Random.Range(1, 4);
            Caller = audience[Random.Range(0, audience.Count)];
            callLength = callSeconds * tuning.time;
            callEnd = Time.time + callLength;
            Current = State.Calling;
            Play(ToolFor(Demanded)?.CallClip, Caller.transform.position);
            CallStarted?.Invoke(Demanded, Caller);
        }

        public void Submit(InstrumentKind kind)
        {
            if (Current != State.Calling || kind == InstrumentKind.None) return;
            int matching = MatchingCount(kind);
            if (kind == Demanded && matching > 0) Succeed(matching);
            else Fail();
        }

        private void Succeed(int matching)
        {
            Progress = Mathf.Clamp01(Progress + baseIncrement * (1f + 0.5f * (matching - 1)) * tuning.gain);
            foreach (VillagerUnit unit in band)
                if (unit.IsChieftain || (unit.Tool != null && unit.Tool.Instrument == Demanded)) unit.Bounce(0.8f, 0.15f, true);
            foreach (VillagerUnit unit in audience) unit.Bounce(0.6f, 0.2f, false);
            Play(ToolFor(Demanded)?.ResponseClip, Caller.transform.position);
            Answered?.Invoke(true, Demanded);
            Caller = null;

            if (Progress >= 1f) Finish();
            else Pause();
        }

        private void Fail()
        {
            Progress = Mathf.Max(0f, Progress - failPenalty * tuning.penalty);
            Play(failClip, Caller != null ? Caller.transform.position : transform.position);
            Answered?.Invoke(false, Demanded);
            Caller = null;

            if (Progress <= 0f) End(Outcome.Disappointed);
            else Pause();
        }

        private void Pause()
        {
            Current = State.Waiting;
            stateUntil = Time.time + pauseSeconds;
        }

        private void Finish()
        {
            foreach (VillagerUnit unit in band) unit.Bounce(1.2f, 0.3f, true);
            foreach (VillagerUnit unit in audience) unit.Bounce(1.2f, 0.3f, false);
            End(Outcome.Pleased);
        }

        private void OnBandHit(GameObject source) => End(Outcome.Interrupted);
        private void OnTargetAttacked(Vector3 where) => End(Outcome.Interrupted);

        public void End(Outcome outcome)
        {
            if (!IsActive) return;
            Village target = Target;
            foreach (VillagerUnit unit in band)
            {
                if (unit == null) continue;
                unit.Health.Damaged -= OnBandHit;
                unit.Release(false);
            }
            if (target != null) target.Attacked -= OnTargetAttacked;
            foreach (VillagerUnit unit in audience) if (unit != null) unit.Release(true);

            if (target != null)
            {
                float earned = outcome == Outcome.Pleased ? 1f : Mathf.Clamp01((Progress - startProgress) / (1f - startProgress));
                if (outcome == Outcome.Disappointed) target.Offend(village, disappointment);
                else if (earned > 0f) target.Befriend(village, Mathf.RoundToInt(rewardAtFull * earned));
                if (village.IsAlliedWith(target)) outcome = Outcome.Allied;
            }

            band.Clear();
            audience.Clear();
            Caller = null;
            Target = null;
            Current = State.Idle;
            Ended?.Invoke(outcome, target);
        }

        public bool SingSoothingSong()
        {
            if (rival == null || !AnyChieftainAlive()) return false;
            rival.Soothe();
            return true;
        }

        public bool Gift(bool large) => rival != null && village.GiftFood(rival, large ? largeGift : smallGift);

        private bool AnyChieftainAlive()
        {
            foreach (VillagerUnit unit in VillagerUnit.All)
                if (unit.Home == village && unit.IsChieftain && unit.IsAlive) return true;
            return false;
        }

        private static void Play(AudioClip clip, Vector3 where)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, where);
        }
    }
}
