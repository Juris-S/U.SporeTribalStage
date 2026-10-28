using System.Collections.Generic;
using SporeTribal.Buildings;
using SporeTribal.Definitions;
using SporeTribal.Economy;
using UnityEngine;

namespace SporeTribal.Villagers
{
    public static class VillagerQueries
    {
        public static bool IsMusician(this VillagerUnit unit) => !unit.IsChieftain && unit.Tool != null && unit.Tool.IsInstrument;

        public static bool Plays(this VillagerUnit unit, InstrumentKind kind) => unit.IsMusician() && unit.Tool.Instrument == kind;

        public static bool NeedsShelterIn(this VillagerUnit unit, HutSanctuary sanctuary) => sanctuary != null && sanctuary.NeedsShelter(unit);

        public static void CollectMembers(this Village village, List<VillagerUnit> into)
        {
            into.Clear();
            foreach (VillagerUnit unit in VillagerUnit.All)
                if (unit.Home == village) into.Add(unit);
        }

        public static int Population(this Village village)
        {
            int count = 0;
            foreach (VillagerUnit unit in VillagerUnit.All)
                if (unit.Home == village) count++;
            return count;
        }

        public static bool HasLivingChieftain(this Village village)
        {
            foreach (VillagerUnit unit in VillagerUnit.All)
                if (unit.Home == village && unit.IsChieftain && unit.IsAlive) return true;
            return false;
        }

        public static VillagerUnit NearestHostileAttacker(this Village village, Vector3 from, float radius)
        {
            VillagerUnit nearest = null;
            float nearestSqr = radius * radius;
            foreach (VillagerUnit unit in VillagerUnit.All)
            {
                if (!unit.IsAttacking || !village.IsHostileTo(unit.Home)) continue;
                float sqr = (unit.transform.position - from).sqrMagnitude;
                if (sqr >= nearestSqr) continue;
                nearestSqr = sqr;
                nearest = unit;
            }
            return nearest;
        }

        public static int CountFollowers(this IReadOnlyList<VillagerUnit> group)
        {
            int count = 0;
            foreach (VillagerUnit unit in group)
                if (!unit.IsChieftain) count++;
            return count;
        }
    }
}
