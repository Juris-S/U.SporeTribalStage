using System.Collections.Generic;
using SporeTribal.Commands;
using SporeTribal.Definitions;
using SporeTribal.Economy;
using SporeTribal.Villagers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SporeTribal.AI
{
    [RequireComponent(typeof(Village))]
    public class TribeRaidDirector : MonoBehaviour
    {
        public enum Alert { FoodRaidersComing, RaidersComing, ThiefIntercepted }

        [SerializeField] private OrderDispatcher dispatcher;
        [Tooltip("The tribe this one raids.")]
        [SerializeField] private Village target;
        [SerializeField] private RaidDefinition[] plans;
        [Tooltip("Villagers always kept home to gather and defend.")]
        [SerializeField] private int minHomeGuard = 2;

        [Header("Muster")]
        [Tooltip("Where raid squads group up before setting off.")]
        [SerializeField] private Transform rallyPoint;
        [SerializeField] private float rallyRadius = 4f;
        [Tooltip("A squad sets off once everyone has rallied, or after this many seconds.")]
        [SerializeField] private float musterTimeout = 25f;

        [Header("Debug")]
        [Tooltip("Launch the first Raid plan straight away (handy when filming).")]
        [SerializeField] private Key raidNowKey = Key.F2;
        [Tooltip("Launch the first StealFood plan straight away.")]
        [SerializeField] private Key stealNowKey = Key.F3;

        public event System.Action<Village, Alert, Vector3> Alerted;

        private Village village;
        private float[] nextCheck;
        private bool[] forced;
        private readonly Dictionary<RaidDefinition, IRaidRun> running = new Dictionary<RaidDefinition, IRaidRun>();
        private readonly List<RaidDefinition> finished = new List<RaidDefinition>();

        public Village Target => target;

        private void Awake()
        {
            village = GetComponent<Village>();
            nextCheck = new float[plans.Length];
            forced = new bool[plans.Length];
            for (int i = 0; i < plans.Length; i++) nextCheck[i] = Time.time + plans[i].InitialDelay;
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current[raidNowKey].wasPressedThisFrame) Force(RaidDefinition.Objective.Raid, 0f);
            if (Keyboard.current[stealNowKey].wasPressedThisFrame) Force(RaidDefinition.Objective.StealFood, 0f);
        }

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

            for (int i = 0; i < plans.Length; i++)
            {
                if (Time.time < nextCheck[i] || running.ContainsKey(plans[i])) continue;
                bool launched = TryLaunch(plans[i], members, forced[i]);
                nextCheck[i] = Time.time + (launched ? plans[i].TimeBetweenExecution : plans[i].CheckTime);
                if (launched) forced[i] = false;
            }
        }

        private void Force(RaidDefinition.Objective objective, float delay)
        {
            for (int i = 0; i < plans.Length; i++)
            {
                if (plans[i].Goal != objective) continue;
                nextCheck[i] = Mathf.Min(nextCheck[i], Time.time + delay);
                forced[i] = true;
                return;
            }
        }

        private bool TryLaunch(RaidDefinition plan, List<VillagerUnit> members, bool force)
        {
            if (target == null || !village.IsHostileTo(target)) return false;
            if (!force && (!plan.FoodAllows(village.Food) || Random.Range(0f, 100f) >= plan.Frequency)) return false;

            var squad = new List<VillagerUnit>();
            int spare = members.Count - minHomeGuard;
            foreach (VillagerUnit unit in members)
            {
                if (squad.Count >= Mathf.Min(plan.DesiredMembers, spare)) break;
                if (!unit.IsAttacking && !Owns(unit)) squad.Add(unit);
            }
            if (squad.Count < plan.RequiredMembers) return false;

            Vector3 rally = rallyPoint != null ? rallyPoint.position : village.DropOffCentre;
            switch (plan.Goal)
            {
                case RaidDefinition.Objective.StealFood:
                    VillagerUnit thief = squad[0];
                    running[plan] = new FoodThiefController(thief, dispatcher, village, target, plan.StealAmount,
                        where => Alerted?.Invoke(target, Alert.ThiefIntercepted, where));
                    Alerted?.Invoke(target, Alert.FoodRaidersComing, thief.transform.position);
                    break;
                default:
                    running[plan] = new RaidSquad(plan, squad, dispatcher, village, target, rally, rallyRadius, musterTimeout,
                        from => Alerted?.Invoke(target, Alert.RaidersComing, from));
                    break;
            }
            return true;
        }
    }
}
