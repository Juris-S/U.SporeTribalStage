using SporeTribal.Economy;
using UnityEngine;

namespace SporeTribal.Villagers
{
    public enum OrderKind { Move, Gather, Equip }

    public readonly struct TribalOrder
    {
        public readonly OrderKind Kind;
        public readonly Vector3 Point;
        public readonly FoodNode Node;
        public readonly ToolBuilding Building;

        private TribalOrder(OrderKind kind, Vector3 point, FoodNode node, ToolBuilding building)
        {
            Kind = kind;
            Point = point;
            Node = node;
            Building = building;
        }

        public static TribalOrder Move(Vector3 point) => new TribalOrder(OrderKind.Move, point, null, null);
        public static TribalOrder Gather(FoodNode node) => new TribalOrder(OrderKind.Gather, default, node, null);
        public static TribalOrder Equip(ToolBuilding building) => new TribalOrder(OrderKind.Equip, default, null, building);
    }
}
