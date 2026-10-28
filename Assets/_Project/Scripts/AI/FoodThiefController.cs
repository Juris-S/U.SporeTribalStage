using System;
using SporeTribal.Economy;
using SporeTribal.Orders;
using SporeTribal.Utilities;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.AI
{
    public class FoodThiefController : IRaidRun
    {
        private enum Phase { ToStore, Fleeing, Done }

        private const float ArriveSlack = 2f;

        private readonly RaidContext context;
        private readonly VillagerUnit thief;
        private readonly int stealAmount;
        private readonly Action<Vector3> intercepted;
        private int carried;
        private Phase phase;

        public FoodThiefController(RaidContext context, VillagerUnit thief, int stealAmount, Action<Vector3> intercepted)
        {
            this.context = context;
            this.thief = thief;
            this.stealAmount = stealAmount;
            this.intercepted = intercepted;
        }

        public bool IsOver => phase == Phase.Done;

        public bool Owns(VillagerUnit unit) => unit == thief;

        public void Begin()
        {
            thief.Health.Damaged += OnHit;
            phase = Phase.ToStore;
            RunTo(context.Target);
        }

        public void Tick()
        {
            if (thief == null || !thief.IsAlive)
            {
                ReturnLoot();
                phase = Phase.Done;
                return;
            }

            switch (phase)
            {
                case Phase.ToStore: TickToStore(); break;
                case Phase.Fleeing: TickFleeing(); break;
            }
        }

        public void End()
        {
            if (thief != null) thief.Health.Damaged -= OnHit;
            phase = Phase.Done;
        }

        private void TickToStore()
        {
            if (!context.IsStillHostile) Flee();
            else if (HasArrived(context.Target)) Steal();
            else if (!thief.IsMoving) RunTo(context.Target);
        }

        private void TickFleeing()
        {
            if (HasArrived(context.Home)) Escape();
            else if (!thief.IsMoving) RunTo(context.Home);
        }

        private void Steal()
        {
            carried = context.Target.Withdraw(stealAmount);
            Flee();
        }

        private void Escape()
        {
            context.Home.Deposit(carried);
            carried = 0;
            phase = Phase.Done;
        }

        private void OnHit(GameObject source)
        {
            if (carried > 0)
            {
                ReturnLoot();
                intercepted?.Invoke(thief.transform.position);
            }
            if (phase == Phase.ToStore) Flee();
        }

        private void ReturnLoot()
        {
            if (carried > 0) context.Target.Deposit(carried);
            carried = 0;
        }

        private void Flee()
        {
            phase = Phase.Fleeing;
            RunTo(context.Home);
        }

        private void RunTo(Village village)
        {
            ClickTarget destination = ClickTarget.Ground(village.DropOffCentre);
            context.Dispatcher.Issue(thief, destination);
        }

        private bool HasArrived(Village village)
        {
            if (thief.IsMoving) return false;
            float reach = village.DropOffRadius + ArriveSlack;
            return thief.transform.position.IsWithinFlat(village.DropOffCentre, reach);
        }
    }
}
