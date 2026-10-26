using SporeTribal.Economy;
using UnityEngine;

namespace SporeTribal.Orders
{
    public readonly struct ClickTarget
    {
        public readonly Vector3 Point;
        public readonly FoodNode Node;
        public readonly ToolBuilding Building;
        public readonly bool OnResource;

        private ClickTarget(Vector3 point, FoodNode node, ToolBuilding building, bool onResource)
        {
            Point = point;
            Node = node;
            Building = building;
            OnResource = onResource;
        }

        public static ClickTarget Ground(Vector3 point) => new ClickTarget(point, null, null, false);
        public static ClickTarget Resource(FoodNode node, Vector3 point) => new ClickTarget(point, node, null, true);
        public static ClickTarget ToolSource(ToolBuilding building, Vector3 point) => new ClickTarget(point, null, building, false);
    }
}
