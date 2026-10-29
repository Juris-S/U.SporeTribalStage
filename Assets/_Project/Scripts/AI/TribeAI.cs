using System.Collections.Generic;
using SporeTribal.Buildings;
using SporeTribal.Commands;
using SporeTribal.Economy;
using SporeTribal.Orders;
using SporeTribal.Villagers;
using UnityEngine;
using UnityEngine.AI;

namespace SporeTribal.AI
{
    [RequireComponent(typeof(Village), typeof(TribeRaidDirector))]
    public class TribeAI : MonoBehaviour
    {
        public enum Temper { Peaceful, Aggressive }

        [SerializeField] private OrderDispatcher dispatcher;
        [SerializeField] private Temper temper = Temper.Aggressive;
        [SerializeField] private float thinkSeconds = 1.5f;

        [Header("Gather")]
        [SerializeField] private float gatherRadius = 30f;

        [Header("Defend")]
        [Tooltip("Hostile fighters or razers inside this radius of the village pull every free villager into the fight.")]
        [SerializeField] private float defendRadius = 18f;
        [Tooltip("Optional: badly hurt villagers go here to heal instead of fighting on.")]
        [SerializeField] private HutSanctuary sanctuary;
        [Tooltip("Seconds between a peaceful tribe being attacked and its revenge raid (min, max).")]
        [SerializeField] private Vector2 revengeDelay = new Vector2(15f, 30f);

        [Header("Recruit")]
        [Tooltip("Spawns new villagers for this tribe. Found automatically from the tribe's spawner if left empty.")]
        [SerializeField] private VillagerSpawner spawner;
        [SerializeField, Min(0)] private int recruitCost = 10;
        [Tooltip("The tribe stops recruiting at this head count.")]
        [SerializeField, Min(1)] private int maxPopulation = 8;
        [Tooltip("Seconds between recruits while there is food to pay for one (min, max).")]
        [SerializeField] private Vector2 recruitSeconds = new Vector2(20f, 40f);
        [Tooltip("Multiplies the wait when the tribe has been wiped out or is under attack, so it rebuilds faster.")]
        [SerializeField, Range(0.1f, 1f)] private float crisisRecruitFactor = 0.4f;
        [SerializeField] private float recruitRadius = 2.5f;
        private const float SpawnClearance = 1f;

        private Village village;
        private TribeRaidDirector raids;
        private readonly List<VillagerUnit> members = new List<VillagerUnit>();
        private readonly List<VillagerUnit> fit = new List<VillagerUnit>();
        private readonly List<VillagerUnit> defenders = new List<VillagerUnit>();
        private float nextThink;
        private float nextRecruit;
        private bool underAttack;
        private bool provoked;

        private void Awake()
        {
            village = GetComponent<Village>();
            raids = GetComponent<TribeRaidDirector>();
            village.Attacked += _ => { provoked = true; underAttack = true; };
            if (spawner == null)
                foreach (VillagerSpawner candidate in FindObjectsByType<VillagerSpawner>())
                    if (candidate.Village == village) spawner = candidate;
        }

        public void SetTemper(Temper value) => temper = value;

        public void DelayRecruiting() => nextRecruit = Time.time + Random.Range(recruitSeconds.x, recruitSeconds.y);

        private void Update()
        {
            if (Time.time < nextThink) return;
            nextThink = Time.time + thinkSeconds;
            Think();
        }

        private void Think()
        {
            if (village.HasSurrendered)
            {
                StandDown();
                return;
            }

            RefreshMembers();
            RetaliateIfProvoked();
            Recruit();
            if (temper == Temper.Aggressive) raids.Tick(fit);
            Shelter();
            if (!Defend()) Gather();
        }

        private void Recruit()
        {
            if (spawner == null || Time.time < nextRecruit) return;

            bool crisis = members.Count == 0 || underAttack;
            underAttack = false;
            if (members.Count >= maxPopulation || village.Food < recruitCost) return;
            if (!TryFindSpawnPoint(out Vector3 point) || !village.TrySpend(recruitCost)) return;

            spawner.SpawnAt(point);
            nextRecruit = Time.time + Random.Range(recruitSeconds.x, recruitSeconds.y) * (crisis ? crisisRecruitFactor : 1f);
        }

        private Vector3 RecruitOrigin()
        {
            Building hut = null;
            foreach (Building building in Building.All)
                if (building is MainHut && building.Owner == village && building.IsStanding) hut = building;
            if (hut == null) return village.DropOffCentre;
            Vector3 toMarket = village.DropOffCentre - hut.transform.position;
            toMarket.y = 0f;
            return hut.transform.position + toMarket.normalized * hut.StandRadius;
        }

        private bool TryFindSpawnPoint(out Vector3 point)
        {
            Vector3 origin = RecruitOrigin();
            for (int i = 0; i < 16; i++)
            {
                Vector2 offset = Random.insideUnitCircle * recruitRadius;
                if (NavMesh.SamplePosition(origin + new Vector3(offset.x, 0f, offset.y), out NavMeshHit hit, 0.5f, NavMesh.AllAreas) && !VillagerUnit.IsCrowded(hit.position, SpawnClearance))
                {
                    point = hit.position;
                    return true;
                }
            }
            bool found = NavMesh.SamplePosition(origin, out NavMeshHit fallback, 4f, NavMesh.AllAreas);
            point = fallback.position;
            return found;
        }

        private void StandDown()
        {
            raids.StandDown();
            enabled = false;
        }

        private void RefreshMembers()
        {
            village.CollectMembers(members);
            fit.Clear();
            foreach (VillagerUnit unit in members)
                if (!unit.NeedsShelterIn(sanctuary)) fit.Add(unit);
        }

        private void RetaliateIfProvoked()
        {
            if (temper != Temper.Peaceful || !provoked) return;
            temper = Temper.Aggressive;
            raids.Revenge(Random.Range(revengeDelay.x, revengeDelay.y));
        }

        private bool IsBusy(VillagerUnit unit) => raids.Owns(unit) || unit.NeedsShelterIn(sanctuary) || unit.IsAttending;

        private void Shelter()
        {
            if (sanctuary == null) return;
            foreach (VillagerUnit unit in members)
            {
                if (raids.Owns(unit) || !unit.NeedsShelterIn(sanctuary)) continue;
                if (sanctuary.TryAdmit(unit)) continue;
                if (unit.IsAttacking || !unit.IsMoving) dispatcher.Issue(unit, ClickTarget.Ground(sanctuary.DoorFor(unit)));
            }
        }

        private bool Defend()
        {
            VillagerUnit intruder = village.NearestHostileAttacker(village.DropOffCentre, defendRadius);
            if (intruder == null) return false;

            defenders.Clear();
            foreach (VillagerUnit unit in members)
                if (!unit.IsFighting && !IsBusy(unit)) defenders.Add(unit);
            if (defenders.Count > 0) dispatcher.Issue(defenders, ClickTarget.Villager(intruder));
            return true;
        }

        private void Gather()
        {
            foreach (VillagerUnit unit in members)
            {
                if (unit.HasOrder || IsBusy(unit)) continue;
                FoodNode node = FoodNode.Nearest(village.DropOffCentre, gatherRadius);
                if (node == null) return;
                dispatcher.Issue(unit, ClickTarget.Resource(node, node.transform.position));
            }
        }
    }
}
