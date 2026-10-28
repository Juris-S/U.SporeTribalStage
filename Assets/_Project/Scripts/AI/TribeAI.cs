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
        private readonly List<VillagerUnit> defenders = new List<VillagerUnit>();
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
            if (temper == Temper.Aggressive) raids.Tick(fit);
            Shelter();
            if (!Defend()) Gather();
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
                if (unit.IsAttacking || !unit.IsMoving) dispatcher.Issue(unit, ClickTarget.Ground(sanctuary.Door));
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
