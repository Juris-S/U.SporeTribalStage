using System.Collections.Generic;
using SporeTribal.Definitions;
using SporeTribal.Economy;
using SporeTribal.Orders;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.AI
{
    public class SocialPerformanceDirector : MonoBehaviour
    {
        public enum State { Idle, Approaching, Mustering, Waiting, Calling, Playing }
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
        [SerializeField] private PerformanceStage stage = new PerformanceStage();
        [Tooltip("If the band is still on its way after this many seconds, the crowd meets it wherever the chieftain is.")]
        [SerializeField] private float approachTimeout = 45f;
        [SerializeField] private int maxAudience = 6;
        [Tooltip("The recital starts once everyone is in place, or after this many seconds.")]
        [SerializeField] private float musterTimeout = 20f;

        [Header("Rounds")]
        [SerializeField] private float callSeconds = 5f;
        [SerializeField] private float pauseSeconds = 1.5f;
        [Tooltip("How long the band plays its answer before the crowd reacts to it.")]
        [SerializeField] private float playSeconds = 4f;
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
        private VillagerUnit contact;
        private TierTuning tuning;
        private float stateUntil;
        private float callEnd;
        private float callLength = 1f;
        private InstrumentKind played;
        private int playedBy;
        private float playEnd;
        public float PlayRemaining => Current == State.Playing ? Mathf.Max(0f, playEnd - Time.time) : 0f;
        public float ProjectedProgress => Current == State.Playing && played == Demanded
            ? Mathf.Clamp01(Progress + baseIncrement * (1f + 0.5f * (playedBy - 1)) * tuning.gain)
            : Progress;

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

        public RefusalReason TryBegin(IReadOnlyList<VillagerUnit> performers, Village target, VillagerUnit contact = null)
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
            stage.Set(target, mean / band.Count);
            stage.PlaceBand(band, Target);
            foreach (VillagerUnit unit in band) unit.Health.Damaged += OnBandHit;
            target.Attacked += OnTargetAttacked;

            tuning = TuningFor(target.TierTowards(village));
            Progress = startProgress;
            Current = State.Approaching;
            stateUntil = Time.time + approachTimeout;
            this.contact = contact;
            if (contact != null) contact.SetTalkTarget(true);
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
            stage.PlaceBand(band, Target);
            return RefusalReason.None;
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
            return stage.IsNoticedFrom(Target, chieftain.transform.position);
        }

        private void Meet(VillagerUnit chieftain)
        {
            stage.Set(Target, chieftain.transform.position);
            if (FindCrowd(Target) == 0)
            {
                End(Outcome.Interrupted);
                return;
            }
            candidates.Sort((a, b) => stage.SqrDistanceToAudience(a.transform.position).CompareTo(stage.SqrDistanceToAudience(b.transform.position)));
            VillagerUnit leader = candidates.Find(unit => unit.IsChieftain);
            if (leader != null) candidates.Remove(leader);
            for (int i = 0; i < Mathf.Min(maxAudience - (leader != null ? 1 : 0), candidates.Count); i++) audience.Add(candidates[i]);
            if (leader != null) audience.Insert(audience.Count / 2, leader);
            if (contact != null && !audience.Contains(contact)) contact.SetTalkTarget(false);
            contact = null;
            foreach (VillagerUnit unit in audience) unit.SetTalkTarget(true);

            stage.PlaceBand(band, Target);
            stage.PlaceAudience(audience);
            Current = State.Mustering;
            stateUntil = Time.time + musterTimeout;
        }

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
                case State.Playing:
                    if (Time.time >= playEnd) Resolve();
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
            audience.RemoveAll(unit =>
            {
                bool gone = unit == null || !unit.IsAlive || !unit.IsAttending;
                if (gone && unit != null) unit.SetTalkTarget(false);
                return gone;
            });
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
            Demanded = PickDemand();
            Caller = audience[Random.Range(0, audience.Count)];
            callLength = callSeconds * tuning.time;
            callEnd = Time.time + callLength;
            Current = State.Calling;
            Play(ToolFor(Demanded)?.CallClip, Caller.transform.position);
            Caller.Excited();
            CallStarted?.Invoke(Demanded, Caller);
        }

        private InstrumentKind PickDemand()
        {
            var called = Target.CalledInstruments;
            int playable = 0;
            foreach (InstrumentKind kind in called) if (MatchingCount(kind) > 0) playable++;
            if (playable > 0)
            {
                int pick = Random.Range(0, playable);
                foreach (InstrumentKind kind in called)
                    if (MatchingCount(kind) > 0 && pick-- == 0) return kind;
            }
            foreach (VillagerUnit unit in band)
                if (!unit.IsChieftain && unit.Tool != null && unit.Tool.IsInstrument) return unit.Tool.Instrument;
            return called.Count > 0 ? called[0] : InstrumentKind.Didgeridoo;
        }

        public void Submit(InstrumentKind kind)
        {
            if (Current != State.Calling || kind == InstrumentKind.None) return;
            int matching = MatchingCount(kind);
            if (matching == 0)
            {
                Fail();
                return;
            }
            played = kind;
            playedBy = matching;
            playEnd = Time.time + playSeconds;
            Current = State.Playing;
            bool liked = kind == Demanded;
            foreach (VillagerUnit unit in band)
            {
                if (unit.IsChieftain || (unit.Tool != null && unit.Tool.Instrument == kind)) unit.Bounce(playSeconds, 0.15f, true);
                else if (liked) unit.Dance(playSeconds, true);
            }
            Play(ToolFor(kind)?.ResponseClip, Chieftain().transform.position);
            foreach (VillagerUnit unit in audience) unit.Dance(playSeconds, liked);
        }

        private void Resolve()
        {
            if (played == Demanded) Succeed(playedBy);
            else Fail();
        }

        private void Succeed(int matching)
        {
            Progress = Mathf.Clamp01(Progress + baseIncrement * (1f + 0.5f * (matching - 1)) * tuning.gain);
            foreach (VillagerUnit unit in audience)
                unit.Happy();
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
            foreach (VillagerUnit unit in band)
            {
                unit.Bounce(1.2f, 0.3f, true);
                unit.Excited();
            }
            foreach (VillagerUnit unit in audience)
            {
                unit.Bounce(1.2f, 0.3f, false);
                unit.Excited();
            }
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
            foreach (VillagerUnit unit in audience)
            {
                if (unit == null) continue;
                unit.SetTalkTarget(false);
                unit.Release(true);
            }
            if (contact != null) contact.SetTalkTarget(false);
            contact = null;

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
