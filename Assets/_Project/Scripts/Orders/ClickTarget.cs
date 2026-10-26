using SporeTribal.Buildings;
using SporeTribal.Economy;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.Orders
{
    public readonly struct ClickTarget
    {
        public readonly Vector3 Point;
        public readonly FoodNode Node;
        public readonly Building Building;
        public readonly VillagerUnit Unit;
        public readonly bool OnResource;

        private ClickTarget(Vector3 point, FoodNode node, Building building, VillagerUnit unit, bool onResource)
        {
            Point = point;
            Node = node;
            Building = building;
            Unit = unit;
            OnResource = onResource;
        }

        public static ClickTarget Ground(Vector3 point) => new ClickTarget(point, null, null, null, false);
        public static ClickTarget Resource(FoodNode node, Vector3 point) => new ClickTarget(point, node, null, null, true);
        public static ClickTarget AtBuilding(Building building, Vector3 point) => new ClickTarget(point, null, building, null, false);
        public static ClickTarget Villager(VillagerUnit unit) => new ClickTarget(unit.transform.position, null, null, unit, false);
    }
}
