using SporeTribal.Economy;
using UnityEngine;

namespace SporeTribal.Villagers
{
    public enum OrderKind { Move, Gather }

    public readonly struct TribalOrder
    {
        public readonly OrderKind Kind;
        public readonly Vector3 Point;
        public readonly FoodNode Node;

        private TribalOrder(OrderKind kind, Vector3 point, FoodNode node)
        {
            Kind = kind;
            Point = point;
            Node = node;
        }

        public static TribalOrder Move(Vector3 point) => new TribalOrder(OrderKind.Move, point, null);
        public static TribalOrder Gather(FoodNode node) => new TribalOrder(OrderKind.Gather, default, node);
    }
}
