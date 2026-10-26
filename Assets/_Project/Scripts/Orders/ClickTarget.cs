using SporeTribal.Economy;
using UnityEngine;

namespace SporeTribal.Orders
{
    public readonly struct ClickTarget
    {
        public readonly Vector3 Point;
        public readonly FoodNode Node;
        public readonly bool OnResource;

        private ClickTarget(Vector3 point, FoodNode node, bool onResource)
        {
            Point = point;
            Node = node;
            OnResource = onResource;
        }

        public static ClickTarget Ground(Vector3 point) => new ClickTarget(point, null, false);
        public static ClickTarget Resource(FoodNode node, Vector3 point) => new ClickTarget(point, node, true);
    }
}
