using System.Collections.Generic;
using SporeTribal.Buildings;
using SporeTribal.Commands;
using SporeTribal.Definitions;
using SporeTribal.Economy;
using SporeTribal.Orders;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.AI
{
    public class RaidSquad : IRaidRun
    {
        private readonly OrderDispatcher dispatcher;
        private readonly Village home;
        private readonly Village target;
        private readonly Vector3 rally;
        private readonly float rallyRadius;
        private readonly float musterUntil;
        private readonly System.Action<Vector3> setOff;
        private readonly List<VillagerUnit> members;
        private readonly Dictionary<VillagerUnit, ToolDefinition> loadout = new Dictionary<VillagerUnit, ToolDefinition>();
        private readonly HashSet<VillagerUnit> rallying = new HashSet<VillagerUnit>();
        private bool assaulting;

        public RaidSquad(RaidDefinition plan, List<VillagerUnit> members, OrderDispatcher dispatcher, Village home, Village target,
            Vector3 rally, float rallyRadius, float musterTimeout, System.Action<Vector3> setOff)
        {
            this.members = members;
            this.dispatcher = dispatcher;
            this.home = home;
            this.target = target;
            this.rally = rally;
            this.rallyRadius = rallyRadius;
            this.setOff = setOff;
            musterUntil = Time.time + musterTimeout;
            foreach (VillagerUnit unit in members) loadout[unit] = plan.RollTool();
        }

        public bool IsOver => members.Count == 0 || !home.IsHostileTo(target) || (assaulting && NextTarget(rally) == null);

        public bool Owns(VillagerUnit unit) => members.Contains(unit);

        public void Tick()
        {
            members.RemoveAll(unit => unit == null || !unit.IsAlive);
            if (assaulting) Assault();
            else Muster();
        }

        public void End() => members.Clear();

        private void Muster()
        {
            var toRally = new List<VillagerUnit>();
            bool allRallied = members.Count > 0;
            foreach (VillagerUnit unit in members)
            {
                bool atRally = rallying.Contains(unit) && !unit.IsMoving && (unit.transform.position - rally).sqrMagnitude < rallyRadius * rallyRadius;
                allRallied &= atRally;
                if (atRally || (unit.HasOrder && !unit.IsGathering)) continue;

                ToolDefinition tool = loadout[unit];
                ToolBuilding armoury = ToolBuilding.Find(home, tool);
                bool canArm = !unit.IsChieftain && armoury != null && unit.Tool != tool && home.Food >= tool.Cost;
                if (canArm) dispatcher.Issue(new[] { unit }, ClickTarget.AtBuilding(armoury, armoury.transform.position));
                else
                {
                    rallying.Add(unit);
                    toRally.Add(unit);
                }
            }
            if (toRally.Count > 0) dispatcher.Issue(toRally, ClickTarget.Ground(rally));

            if (!allRallied && Time.time < musterUntil) return;
            assaulting = true;
            setOff?.Invoke(rally);
            Assault();
        }

        private void Assault()
        {
            foreach (VillagerUnit unit in members)
            {
                if (unit.IsFighting) continue;
                Building goal = NextTarget(unit.transform.position);
                if (goal == null || unit.RazeTarget == goal) continue;
                dispatcher.Issue(new[] { unit }, ClickTarget.AtBuilding(goal, goal.transform.position));
            }
        }

        private Building NextTarget(Vector3 from)
        {
            Building best = null;
            float bestSqr = float.MaxValue;
            MainHut hut = null;
            foreach (Building building in Building.All)
            {
                if (building.Owner != target || !building.IsStanding) continue;
                if (building is MainHut main) { hut = main; continue; }
                float sqr = (building.transform.position - from).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = building; }
            }
            return best != null ? best : hut;
        }
    }
}
