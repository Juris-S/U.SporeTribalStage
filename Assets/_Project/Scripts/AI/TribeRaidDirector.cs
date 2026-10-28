using System;
using System.Collections.Generic;
using SporeTribal.Commands;
using SporeTribal.Definitions;
using SporeTribal.Economy;
using SporeTribal.Inputs;
using SporeTribal.Villagers;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace SporeTribal.AI
{
    [RequireComponent(typeof(Village))]
    public class TribeRaidDirector : MonoBehaviour
    {
        [SerializeField] private OrderDispatcher dispatcher;
        [Tooltip("The tribe this one raids.")]
        [SerializeField] private Village target;
        [SerializeField] private RaidDefinition[] plans;
        [Tooltip("Villagers always kept home to gather and defend.")]
        [SerializeField] private int minHomeGuard = 2;
        [SerializeField] private MusterSettings muster = new MusterSettings();

        [Header("Opportunism")]
        [Tooltip("A raid plan also becomes eligible, whatever its own relationship and population range, when the target dislikes us no more than this (a tribe we angered) and we outnumber it.")]
        [SerializeField, Range(-100, 100)] private int opportunisticMaxRelationship = -1;
        [Tooltip("Our head count must be at least this many times the target's to count as outnumbering it.")]
        [SerializeField, Min(1f)] private float outnumberRatio = 1.5f;

        public event Action<Village, RaidAlert, Vector3> Alerted;

        private Village village;
        private readonly List<RaidPlanSchedule> schedules = new List<RaidPlanSchedule>();
        private readonly Dictionary<RaidDefinition, IRaidRun> running = new Dictionary<RaidDefinition, IRaidRun>();
        private readonly List<RaidDefinition> finished = new List<RaidDefinition>();
        private readonly List<VillagerUnit> candidates = new List<VillagerUnit>();

        public Village Target => target;

        private RaidContext Context => new RaidContext(dispatcher, village, target);

        private void Awake()
        {
            village = GetComponent<Village>();
            foreach (RaidDefinition plan in plans) schedules.Add(new RaidPlanSchedule(plan));
        }

        private void OnEnable()
        {
            GameInput.Controls.DevTools.RaidNow.performed += OnRaidNow;
            GameInput.Controls.DevTools.StealNow.performed += OnStealNow;
        }

        private void OnDisable()
        {
            GameInput.Controls.DevTools.RaidNow.performed -= OnRaidNow;
            GameInput.Controls.DevTools.StealNow.performed -= OnStealNow;
        }

        private void OnRaidNow(InputAction.CallbackContext _) => Force(RaidDefinition.Objective.Raid, 0f);
        private void OnStealNow(InputAction.CallbackContext _) => Force(RaidDefinition.Objective.StealFood, 0f);

        public bool Owns(VillagerUnit unit)
        {
            foreach (IRaidRun run in running.Values)
                if (run.Owns(unit)) return true;
            return false;
        }

        public void Revenge(float delay) => Force(RaidDefinition.Objective.Raid, delay);

        public void StandDown()
        {
            foreach (IRaidRun run in running.Values) run.End();
            running.Clear();
        }

        public void Tick(List<VillagerUnit> members)
        {
            TickRunning();
            SchedulePlans(members);
        }

        private void TickRunning()
        {
            finished.Clear();
            foreach (KeyValuePair<RaidDefinition, IRaidRun> pair in running)
            {
                pair.Value.Tick();
                if (pair.Value.IsOver) finished.Add(pair.Key);
            }
            foreach (RaidDefinition plan in finished)
            {
                running[plan].End();
                running.Remove(plan);
            }
        }

        private void SchedulePlans(List<VillagerUnit> members)
        {
            foreach (RaidPlanSchedule schedule in schedules)
            {
                if (!schedule.IsForced && !TryArm(schedule)) continue;
                if (!schedule.IsDue || running.ContainsKey(schedule.Plan)) continue;
                if (TryLaunch(schedule.Plan, members, schedule.IsForced)) schedule.MarkLaunched();
                else schedule.MarkRefused();
            }
        }

        private bool TryArm(RaidPlanSchedule schedule)
        {
            if (!IsEligible(schedule.Plan))
            {
                schedule.Disarm();
                return false;
            }
            schedule.Arm();
            return true;
        }

        private bool IsEligible(RaidDefinition plan) =>
            HasHostileTarget
            && ((plan.RelationshipAllows(village.RelationshipTo(target)) && plan.PopulationAllows(target.Population())) || SeesOpening(plan));

        private bool SeesOpening(RaidDefinition plan) =>
            plan.Goal == RaidDefinition.Objective.Raid
            && village.RelationshipTo(target) <= opportunisticMaxRelationship
            && village.Population() >= target.Population() * outnumberRatio;

        private bool HasHostileTarget => target != null && village.IsHostileTo(target);

        private void Force(RaidDefinition.Objective objective, float delay)
        {
            foreach (RaidPlanSchedule schedule in schedules)
            {
                if (schedule.Plan.Goal != objective) continue;
                schedule.Force(delay);
                return;
            }
        }

        private bool TryLaunch(RaidDefinition plan, List<VillagerUnit> members, bool forced)
        {
            if (!HasHostileTarget)
            {
                if (forced) Debug.LogWarning($"{name}: forced {plan.DisplayName} refused, no hostile target (target={(target != null ? target.name : "none")}).", this);
                return false;
            }
            if (!forced && !RollsToLaunch(plan)) return false;

            PickCandidates(plan, members);
            if (candidates.Count < plan.RequiredMembers)
            {
                if (forced) Debug.LogWarning($"{name}: forced {plan.DisplayName} waiting, {candidates.Count} free of {plan.RequiredMembers} needed ({members.Count} fit members, {minHomeGuard} kept home).", this);
                return false;
            }

            IRaidRun run = plan.Goal == RaidDefinition.Objective.StealFood ? StartTheft(plan) : StartRaid(plan);
            running[plan] = run;
            run.Begin();
            return true;
        }

        private bool RollsToLaunch(RaidDefinition plan) => plan.FoodAllows(village.Food) && Random.Range(0f, 100f) < plan.Frequency;

        private void PickCandidates(RaidDefinition plan, List<VillagerUnit> members)
        {
            candidates.Clear();
            int size = Mathf.Min(plan.DesiredMembers, members.Count - minHomeGuard);
            foreach (VillagerUnit unit in members)
            {
                if (candidates.Count >= size) break;
                if (IsFree(unit)) candidates.Add(unit);
            }
        }

        private bool IsFree(VillagerUnit unit) => !unit.IsAttacking && !unit.IsAttending && !Owns(unit);

        private IRaidRun StartTheft(RaidDefinition plan)
        {
            VillagerUnit thief = candidates[0];
            Alerted?.Invoke(target, RaidAlert.FoodRaidersComing, thief.transform.position);
            return new FoodThiefController(Context, thief, plan.StealAmount, OnThiefIntercepted);
        }

        private IRaidRun StartRaid(RaidDefinition plan)
        {
            var squad = new List<VillagerUnit>(candidates);
            return new RaidSquad(Context, plan, squad, muster, OnRaidSetOff);
        }

        private void OnThiefIntercepted(Vector3 where) => Alerted?.Invoke(target, RaidAlert.ThiefIntercepted, where);
        private void OnRaidSetOff(Vector3 from) => Alerted?.Invoke(target, RaidAlert.RaidersComing, from);
    }
}
