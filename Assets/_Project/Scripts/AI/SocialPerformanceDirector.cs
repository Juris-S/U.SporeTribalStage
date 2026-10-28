using System;
using System.Collections.Generic;
using SporeTribal.Definitions;
using SporeTribal.Economy;
using SporeTribal.Feedback;
using SporeTribal.Orders;
using SporeTribal.Villagers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SporeTribal.AI
{
    public class SocialPerformanceDirector : MonoBehaviour
    {
        public enum State { Idle, Approaching, Mustering, Waiting, Calling, Playing }
        public enum Outcome { Allied, Pleased, Disappointed, Interrupted }

        private const float FinaleBounceSeconds = 1.2f;
        private const float FinaleBounceHeight = 0.3f;
        private const float MusicianBounceHeight = 0.15f;

        [SerializeField] private Village village;
        [Tooltip("The tribe the gift and soothing song hotkeys are aimed at.")]
        [SerializeField] private Village rival;
        [Tooltip("The three instrument tools: they supply the icons and sounds for the calls.")]
        [SerializeField] private ToolDefinition[] instruments;
        [Tooltip("Timing, scoring, tier tolerance and gift sizes.")]
        [SerializeField] private RecitalDefinition recital;
        [SerializeField] private AudioClip failClip;
        [Tooltip("Rises from each musician while they play an answer.")]
        [SerializeField] private VfxId playEffect = VfxId.Music;
        [SerializeField] private float playEffectHeight = 1.5f;
        [SerializeField] private PerformanceStage stage = new PerformanceStage();

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

        public event Action<Village> Started;
        public event Action<InstrumentKind, VillagerUnit> CallStarted;
        public event Action<bool, InstrumentKind> Answered;
        public event Action<Outcome, Village> Ended;

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
        public float PlayRemaining => Current == State.Playing ? Mathf.Max(0f, playEnd - Time.time) : 0f;
        public float ProjectedProgress => Current == State.Playing && played == Demanded ? ProgressAfterSuccess(playedBy) : Progress;
        public int SmallGift => recital.SmallGift;
        public int LargeGift => recital.LargeGift;

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
                if (unit.Plays(kind)) count++;
            return count;
        }

        public RefusalReason TryBegin(IReadOnlyList<VillagerUnit> performers, Village target, VillagerUnit contact = null)
        {
            if (target == null || target.HasSurrendered) return RefusalReason.TargetGone;
            if (IsActive && target == Target) return Join(performers);

            RefusalReason refusal = CheckCanPerform(performers, target);
            if (refusal != RefusalReason.None) return refusal;

            if (IsActive) End(Outcome.Interrupted);
            Target = target;
            band.AddRange(performers);
            stage.Set(target, MeanPosition(band));
            stage.PlaceBand(band, Target);
            foreach (VillagerUnit unit in band) unit.Health.Damaged += OnBandHit;
            target.Attacked += OnTargetAttacked;

            tuning = recital.TuningFor(target.TierTowards(village));
            Progress = recital.StartProgress;
            EnterState(State.Approaching, recital.ApproachTimeout);
            this.contact = contact;
            if (contact != null) contact.SetTalkTarget(true);
            Started?.Invoke(target);
            return RefusalReason.None;
        }

        private RefusalReason CheckCanPerform(IReadOnlyList<VillagerUnit> performers, Village target)
        {
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
            return RefusalReason.None;
        }

        private static Vector3 MeanPosition(IReadOnlyList<VillagerUnit> units)
        {
            Vector3 sum = Vector3.zero;
            foreach (VillagerUnit unit in units) sum += unit.transform.position;
            return sum / units.Count;
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

            GatherAudience();
            stage.PlaceBand(band, Target);
            stage.PlaceAudience(audience);
            EnterState(State.Mustering, recital.MusterTimeout);
        }

        private void GatherAudience()
        {
            candidates.Sort((a, b) => stage.SqrDistanceToAudience(a.transform.position).CompareTo(stage.SqrDistanceToAudience(b.transform.position)));
            VillagerUnit leader = candidates.Find(unit => unit.IsChieftain);
            if (leader != null) candidates.Remove(leader);

            int seats = recital.MaxAudience - (leader != null ? 1 : 0);
            for (int i = 0; i < Mathf.Min(seats, candidates.Count); i++) audience.Add(candidates[i]);
            if (leader != null) audience.Insert(audience.Count / 2, leader);

            if (contact != null && !audience.Contains(contact)) contact.SetTalkTarget(false);
            contact = null;
            foreach (VillagerUnit unit in audience) unit.SetTalkTarget(true);
        }

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
                    if (Time.time >= stateUntil || AllInPlace()) Pause();
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

        private void EnterState(State next, float seconds)
        {
            Current = next;
            stateUntil = Time.time + seconds;
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

        private void StartCall()
        {
            Demanded = PickDemand();
            Caller = audience[Random.Range(0, audience.Count)];
            callLength = recital.CallSeconds * tuning.time;
            callEnd = Time.time + callLength;
            Current = State.Calling;
            Play(ToolFor(Demanded)?.CallClip, Caller.transform.position);
            Caller.Excited();
            CallStarted?.Invoke(Demanded, Caller);
        }

        private InstrumentKind PickDemand()
        {
            IReadOnlyList<InstrumentKind> called = Target.CalledInstruments;
            int playable = 0;
            foreach (InstrumentKind kind in called) if (MatchingCount(kind) > 0) playable++;
            if (playable > 0)
            {
                int pick = Random.Range(0, playable);
                foreach (InstrumentKind kind in called)
                    if (MatchingCount(kind) > 0 && pick-- == 0) return kind;
            }
            foreach (VillagerUnit unit in band)
                if (unit.IsMusician()) return unit.Tool.Instrument;
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

            float seconds = recital.PlaySeconds;
            played = kind;
            playedBy = matching;
            playEnd = Time.time + seconds;
            Current = State.Playing;
            bool liked = kind == Demanded;
            foreach (VillagerUnit unit in band) PerformAnswer(unit, kind, liked, seconds);
            Play(ToolFor(kind)?.ResponseClip, Chieftain().transform.position);
            foreach (VillagerUnit unit in audience) unit.Dance(seconds, liked);
        }

        private void PerformAnswer(VillagerUnit unit, InstrumentKind kind, bool liked, float seconds)
        {
            bool musician = unit.Plays(kind);
            if (unit.IsChieftain || musician) unit.Bounce(seconds, MusicianBounceHeight, true);
            else if (liked) unit.Dance(seconds, true);
            if (musician) ActivityEffect.Spawn(playEffect, unit.transform.position + Vector3.up * playEffectHeight, seconds, unit);
        }

        private void Resolve()
        {
            if (played == Demanded) Succeed(playedBy);
            else Fail();
        }

        private float ProgressAfterSuccess(int matching) => Mathf.Clamp01(Progress + recital.GainFor(matching, tuning));

        private void Succeed(int matching)
        {
            Progress = ProgressAfterSuccess(matching);
            foreach (VillagerUnit unit in audience) unit.Happy();
            Answered?.Invoke(true, Demanded);
            Caller = null;

            if (Progress >= 1f) Finish();
            else Pause();
        }

        private void Fail()
        {
            Progress = Mathf.Max(0f, Progress - recital.FailPenalty * tuning.penalty);
            Play(failClip, Caller != null ? Caller.transform.position : transform.position);
            Answered?.Invoke(false, Demanded);
            Caller = null;

            if (Progress <= 0f) End(Outcome.Disappointed);
            else Pause();
        }

        private void Pause() => EnterState(State.Waiting, recital.PauseSeconds);

        private void Finish()
        {
            foreach (VillagerUnit unit in band) Celebrate(unit, true);
            foreach (VillagerUnit unit in audience) Celebrate(unit, false);
            End(Outcome.Pleased);
        }

        private static void Celebrate(VillagerUnit unit, bool performer)
        {
            unit.Bounce(FinaleBounceSeconds, FinaleBounceHeight, performer);
            unit.Excited();
        }

        private void OnBandHit(GameObject source) => End(Outcome.Interrupted);
        private void OnTargetAttacked(Vector3 where) => End(Outcome.Interrupted);

        public void End(Outcome outcome)
        {
            if (!IsActive) return;
            Village target = Target;
            ReleaseBand();
            ReleaseAudience();
            if (target != null)
            {
                target.Attacked -= OnTargetAttacked;
                outcome = Settle(target, outcome);
            }

            band.Clear();
            audience.Clear();
            Caller = null;
            Target = null;
            Current = State.Idle;
            Ended?.Invoke(outcome, target);
        }

        private void ReleaseBand()
        {
            foreach (VillagerUnit unit in band)
            {
                if (unit == null) continue;
                unit.Health.Damaged -= OnBandHit;
                unit.Release(false);
            }
        }

        private void ReleaseAudience()
        {
            foreach (VillagerUnit unit in audience)
            {
                if (unit == null) continue;
                unit.SetTalkTarget(false);
                unit.Release(true);
            }
            if (contact != null) contact.SetTalkTarget(false);
            contact = null;
        }

        private Outcome Settle(Village target, Outcome outcome)
        {
            float earned = outcome == Outcome.Pleased ? 1f : recital.EarnedFraction(Progress);
            if (outcome == Outcome.Disappointed) target.Offend(village, recital.Disappointment);
            else if (earned > 0f) target.Befriend(village, Mathf.RoundToInt(recital.RewardAtFull * earned));
            return village.IsAlliedWith(target) ? Outcome.Allied : outcome;
        }

        public bool SingSoothingSong()
        {
            if (rival == null || !village.HasLivingChieftain()) return false;
            rival.Soothe();
            return true;
        }

        public bool Gift(bool large) => rival != null && village.GiftFood(rival, large ? recital.LargeGift : recital.SmallGift);

        private static void Play(AudioClip clip, Vector3 where)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, where);
        }
    }
}
