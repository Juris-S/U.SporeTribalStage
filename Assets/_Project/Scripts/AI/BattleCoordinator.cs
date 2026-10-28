using System.Collections.Generic;
using SporeTribal.Buildings;
using SporeTribal.Economy;
using SporeTribal.Utilities;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.AI
{
    public class BattleCoordinator : MonoBehaviour
    {
        private const float TickSeconds = 0.1f;

        [SerializeField] private Village village;
        [Tooltip("Idle fighters pick up enemy villagers within this distance of themselves.")]
        [SerializeField, Min(0f)] private float engageRadius = 12f;
        [Tooltip("Idle fighters head for enemy buildings and fleeing enemies within this distance.")]
        [SerializeField, Min(0f)] private float pursuitRadius = 40f;

        private readonly HashSet<VillagerUnit> engaged = new HashSet<VillagerUnit>();
        private readonly List<VillagerUnit> idle = new List<VillagerUnit>();
        private readonly List<VillagerUnit> fleeing = new List<VillagerUnit>();
        private HutSanctuary[] huts;
        private float nextTick;

        private void OnEnable() => huts = FindObjectsByType<HutSanctuary>();

        private void Update()
        {
            if (Time.time < nextTick) return;
            nextTick = Time.time + TickSeconds;
            Refresh();
            if (idle.Count == 0) return;
            AssignChasers();
            for (int i = idle.Count - 1; i >= 0; i--) AssignFollowUp(idle[i]);
        }

        private void Refresh()
        {
            engaged.RemoveWhere(unit => unit == null || !unit.IsAlive || !unit.isActiveAndEnabled);
            idle.Clear();
            fleeing.Clear();
            bool siegeUnderway = false;
            foreach (VillagerUnit unit in VillagerUnit.All)
                siegeUnderway |= unit.Home == village && unit.IsRazing;

            foreach (VillagerUnit unit in VillagerUnit.All)
            {
                if (unit.Home == village)
                {
                    if (unit.IsAttacking) engaged.Add(unit);
                    else if (unit.IsChieftain && siegeUnderway && IsAvailable(unit)) engaged.Add(unit);
                    if (!engaged.Contains(unit)) continue;
                    if (unit.IsHurt && !unit.IsChieftain) continue;
                    if (IsAvailable(unit)) idle.Add(unit);
                    else if (unit.HasOrder && !unit.IsAttacking) engaged.Remove(unit);
                }
                else if (IsFleeing(unit)) fleeing.Add(unit);
            }
        }

        private static bool IsAvailable(VillagerUnit unit) => unit.IsAlive && !unit.HasOrder && !unit.IsTalkTarget;

        private bool IsFleeing(VillagerUnit enemy) =>
            enemy.IsAlive && village.IsHostileTo(enemy.Home) && enemy.IsHurt && enemy.IsMoving && !enemy.IsAttacking;

        private void AssignChasers()
        {
            foreach (VillagerUnit runner in fleeing)
            {
                if (IsBeingChased(runner)) continue;
                VillagerUnit chaser = Nearest(idle, runner.transform.position, pursuitRadius);
                if (chaser == null) continue;
                Send(chaser, TribalOrder.Attack(runner));
            }
        }

        private bool IsBeingChased(VillagerUnit runner)
        {
            foreach (VillagerUnit unit in engaged)
                if (unit.AttackVictim == runner) return true;
            return false;
        }

        private void AssignFollowUp(VillagerUnit unit)
        {
            Vector3 at = unit.transform.position;
            VillagerUnit enemy = NearestEnemy(at, engageRadius);
            if (enemy != null) { Send(unit, TribalOrder.Attack(enemy)); return; }

            VillagerUnit runner = Nearest(fleeing, at, pursuitRadius);
            if (runner != null) { Send(unit, TribalOrder.Attack(runner)); return; }

            Building target = PickBuilding(at);
            if (target != null) Send(unit, TribalOrder.Raze(target));
            else engaged.Remove(unit);
        }

        private Building PickBuilding(Vector3 from)
        {
            foreach (HutSanctuary sanctuary in huts)
                if (sanctuary != null && sanctuary.IsSleeping && IsRazable(sanctuary.Hut, from)) return sanctuary.Hut;

            Building nearest = null;
            float nearestSqr = float.MaxValue;
            foreach (Building building in Building.All)
            {
                if (!IsRazable(building, from)) continue;
                float sqr = building.transform.position.FlatSqrDistance(from);
                if (sqr >= nearestSqr) continue;
                nearestSqr = sqr;
                nearest = building;
            }
            return nearest;
        }

        private bool IsRazable(Building building, Vector3 from) =>
            building != null && building.IsStanding && village.IsHostileTo(building.Owner) && building.transform.position.IsWithinFlat(from, pursuitRadius);

        private VillagerUnit NearestEnemy(Vector3 from, float radius)
        {
            VillagerUnit best = null;
            float bestSqr = radius * radius;
            foreach (VillagerUnit other in VillagerUnit.All)
            {
                if (!other.IsAlive || !village.IsHostileTo(other.Home) || fleeing.Contains(other)) continue;
                float sqr = other.transform.position.FlatSqrDistance(from);
                if (sqr >= bestSqr) continue;
                bestSqr = sqr;
                best = other;
            }
            return best;
        }

        private static VillagerUnit Nearest(List<VillagerUnit> from, Vector3 point, float radius)
        {
            VillagerUnit best = null;
            float bestSqr = radius * radius;
            foreach (VillagerUnit unit in from)
            {
                float sqr = unit.transform.position.FlatSqrDistance(point);
                if (sqr >= bestSqr) continue;
                bestSqr = sqr;
                best = unit;
            }
            return best;
        }

        private void Send(VillagerUnit unit, TribalOrder order)
        {
            idle.Remove(unit);
            unit.Accept(order);
        }
    }
}
