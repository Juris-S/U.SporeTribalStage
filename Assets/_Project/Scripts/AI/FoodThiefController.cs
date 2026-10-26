using SporeTribal.Commands;
using SporeTribal.Economy;
using SporeTribal.Orders;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.AI
{
    public class FoodThiefController : IRaidRun
    {
        private enum Phase { ToStore, Fleeing, Done }

        private const float ArriveSlack = 2f;

        private readonly VillagerUnit thief;
        private readonly OrderDispatcher dispatcher;
        private readonly Village home;
        private readonly Village target;
        private readonly int stealAmount;
        private readonly System.Action<Vector3> intercepted;
        private int carried;
        private Phase phase;

        public FoodThiefController(VillagerUnit thief, OrderDispatcher dispatcher, Village home, Village target, int stealAmount, System.Action<Vector3> intercepted)
        {
            this.thief = thief;
            this.dispatcher = dispatcher;
            this.home = home;
            this.target = target;
            this.stealAmount = stealAmount;
            this.intercepted = intercepted;
            thief.Health.Damaged += OnHit;
            RunTo(target);
            phase = Phase.ToStore;
        }

        public bool IsOver => phase == Phase.Done;

        public bool Owns(VillagerUnit unit) => unit == thief;

        public void Tick()
        {
            if (thief == null || !thief.IsAlive)
            {
                if (carried > 0) target.Deposit(carried);
                carried = 0;
                phase = Phase.Done;
                return;
            }

            switch (phase)
            {
                case Phase.ToStore:
                    if (!home.IsHostileTo(target)) Flee();
                    else if (Arrived(target))
                    {
                        carried = target.Withdraw(stealAmount);
                        Flee();
                    }
                    else if (!thief.IsMoving) RunTo(target);
                    break;
                case Phase.Fleeing:
                    if (Arrived(home))
                    {
                        home.Deposit(carried);
                        carried = 0;
                        phase = Phase.Done;
                    }
                    else if (!thief.IsMoving) RunTo(home);
                    break;
            }
        }

        public void End()
        {
            if (thief != null) thief.Health.Damaged -= OnHit;
            phase = Phase.Done;
        }

        private void OnHit(GameObject source)
        {
            if (carried > 0)
            {
                target.Deposit(carried);
                carried = 0;
                intercepted?.Invoke(thief.transform.position);
            }
            if (phase == Phase.ToStore) Flee();
        }

        private void Flee()
        {
            phase = Phase.Fleeing;
            RunTo(home);
        }

        private void RunTo(Village village) => dispatcher.Issue(new[] { thief }, ClickTarget.Ground(village.DropOffCentre));

        private bool Arrived(Village village)
        {
            if (thief.IsMoving) return false;
            Vector3 to = thief.transform.position - village.DropOffCentre;
            to.y = 0f;
            float reach = village.DropOffRadius + ArriveSlack;
            return to.sqrMagnitude <= reach * reach;
        }
    }
}
