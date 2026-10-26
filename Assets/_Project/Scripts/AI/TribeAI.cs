using System.Collections.Generic;
using SporeTribal.Buildings;
using SporeTribal.Commands;
using SporeTribal.Economy;
using SporeTribal.Orders;
using SporeTribal.Villagers;
using UnityEngine;

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

        private Village village;
        private TribeRaidDirector raids;
        private readonly List<VillagerUnit> members = new List<VillagerUnit>();
        private readonly List<VillagerUnit> fit = new List<VillagerUnit>();
        private float nextThink;
        private bool provoked;

        private void Awake()
        {
            village = GetComponent<Village>();
            raids = GetComponent<TribeRaidDirector>();
            village.Attacked += _ => provoked = true;
        }

        private void Update()
        {
            if (Time.time < nextThink) return;
            nextThink = Time.time + thinkSeconds;

            if (village.HasSurrendered)
            {
                raids.StandDown();
                enabled = false;
                return;
            }

            members.Clear();
            fit.Clear();
            foreach (VillagerUnit unit in VillagerUnit.All)
            {
                if (unit.Home != village) continue;
                members.Add(unit);
                if (!NeedsShelter(unit)) fit.Add(unit);
            }

            if (temper == Temper.Peaceful && provoked)
            {
                temper = Temper.Aggressive;
                raids.Revenge(Random.Range(revengeDelay.x, revengeDelay.y));
            }

            if (temper == Temper.Aggressive) raids.Tick(fit);
            Shelter();
            if (Defend()) return;
            Gather();
        }

        private bool Busy(VillagerUnit unit) => raids.Owns(unit) || NeedsShelter(unit);

        private bool NeedsShelter(VillagerUnit unit) => sanctuary != null && sanctuary.NeedsShelter(unit);

        private void Shelter()
        {
            if (sanctuary == null) return;
            foreach (VillagerUnit unit in members)
            {
                if (raids.Owns(unit) || !sanctuary.NeedsShelter(unit)) continue;
                if (!unit.IsMoving && sanctuary.AtDoor(unit)) sanctuary.Admit(unit);
                else if (unit.IsAttacking || !unit.IsMoving) dispatcher.Issue(new[] { unit }, ClickTarget.Ground(sanctuary.Door));
            }
        }

        private bool Defend()
        {
            VillagerUnit intruder = NearestHostile(village.DropOffCentre, defendRadius);
            if (intruder == null) return false;

            var defenders = new List<VillagerUnit>();
            foreach (VillagerUnit unit in members)
                if (!unit.IsFighting && !Busy(unit)) defenders.Add(unit);
            if (defenders.Count > 0) dispatcher.Issue(defenders, ClickTarget.Villager(intruder));
            return true;
        }

        private void Gather()
        {
            foreach (VillagerUnit unit in members)
            {
                if (unit.HasOrder || Busy(unit)) continue;
                FoodNode node = FoodNode.Nearest(village.DropOffCentre, gatherRadius);
                if (node == null) return;
                dispatcher.Issue(new[] { unit }, ClickTarget.Resource(node, node.transform.position));
            }
        }

        private VillagerUnit NearestHostile(Vector3 from, float radius)
        {
            VillagerUnit best = null;
            float bestSqr = radius * radius;
            foreach (VillagerUnit unit in VillagerUnit.All)
            {
                if (!village.IsHostileTo(unit.Home) || !unit.IsAttacking) continue;
                float sqr = (unit.transform.position - from).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = unit; }
            }
            return best;
        }
    }
}
