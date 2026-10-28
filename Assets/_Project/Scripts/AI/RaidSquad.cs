using System;
using System.Collections.Generic;
using SporeTribal.Buildings;
using SporeTribal.Definitions;
using SporeTribal.Orders;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.AI
{
    public class RaidSquad : IRaidRun
    {
        private readonly RaidContext context;
        private readonly RaidDefinition plan;
        private readonly List<VillagerUnit> members;
        private readonly MusterSettings muster;
        private readonly Vector3 rally;
        private readonly Action<Vector3> setOff;
        private readonly Dictionary<VillagerUnit, ToolDefinition> loadout = new Dictionary<VillagerUnit, ToolDefinition>();
        private readonly HashSet<VillagerUnit> rallying = new HashSet<VillagerUnit>();
        private readonly List<VillagerUnit> toRally = new List<VillagerUnit>();
        private float musterUntil;
        private bool assaulting;

        public RaidSquad(RaidContext context, RaidDefinition plan, List<VillagerUnit> members, MusterSettings muster, Action<Vector3> setOff)
        {
            this.context = context;
            this.plan = plan;
            this.members = members;
            this.muster = muster;
            this.setOff = setOff;
            rally = muster.RallyPositionOr(context.Home.DropOffCentre);
        }

        public bool IsOver => members.Count == 0 || !context.IsStillHostile || (assaulting && NextTarget(rally) == null);

        public bool Owns(VillagerUnit unit) => members.Contains(unit);

        public void Begin()
        {
            musterUntil = Time.time + muster.Timeout;
            foreach (VillagerUnit unit in members) loadout[unit] = plan.RollTool();
        }

        public void Tick()
        {
            members.RemoveAll(unit => unit == null || !unit.IsAlive);
            if (assaulting) Assault();
            else Muster();
        }

        public void End() => members.Clear();

        private void Muster()
        {
            toRally.Clear();
            bool allRallied = members.Count > 0;
            foreach (VillagerUnit unit in members) allRallied &= MusterMember(unit);
            if (toRally.Count > 0) context.Dispatcher.Issue(toRally, ClickTarget.Ground(rally));

            if (!allRallied && Time.time < musterUntil) return;
            assaulting = true;
            setOff?.Invoke(rally);
            Assault();
        }

        private bool MusterMember(VillagerUnit unit)
        {
            if (IsAtRally(unit)) return true;
            if (unit.HasOrder && !unit.IsGathering) return false;

            ToolDefinition tool = loadout[unit];
            ToolBuilding armoury = ToolBuilding.Find(context.Home, tool);
            if (CanArm(unit, tool, armoury))
            {
                context.Dispatcher.Issue(unit, ClickTarget.AtBuilding(armoury, armoury.transform.position));
                return false;
            }

            rallying.Add(unit);
            toRally.Add(unit);
            return false;
        }

        private bool IsAtRally(VillagerUnit unit) =>
            rallying.Contains(unit) && !unit.IsMoving && (unit.transform.position - rally).sqrMagnitude < muster.RallyRadius * muster.RallyRadius;

        private bool CanArm(VillagerUnit unit, ToolDefinition tool, ToolBuilding armoury) =>
            !unit.IsChieftain && armoury != null && unit.Tool != tool && context.Home.Food >= tool.Cost;

        private void Assault()
        {
            foreach (VillagerUnit unit in members) SendToNextTarget(unit);
        }

        private void SendToNextTarget(VillagerUnit unit)
        {
            if (unit.IsFighting) return;
            Building goal = NextTarget(unit.transform.position);
            if (goal == null || unit.RazeTarget == goal) return;
            context.Dispatcher.Issue(unit, ClickTarget.AtBuilding(goal, goal.transform.position));
        }

        private Building NextTarget(Vector3 from)
        {
            Building outbuilding = Building.NearestStanding(context.Target, from, includeMainHut: false);
            return outbuilding != null ? outbuilding : Building.NearestStanding(context.Target, from, includeMainHut: true);
        }
    }
}
