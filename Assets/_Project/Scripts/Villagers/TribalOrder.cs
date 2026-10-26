using SporeTribal.Buildings;
using SporeTribal.Economy;
using UnityEngine;

namespace SporeTribal.Villagers
{
    public enum OrderKind { Move, Gather, Equip, Attack, Raze }

    public readonly struct TribalOrder
    {
        public readonly OrderKind Kind;
        public readonly Vector3 Point;
        public readonly FoodNode Node;
        public readonly ToolBuilding Building;
        public readonly VillagerUnit Target;
        public readonly Building Structure;

        private TribalOrder(OrderKind kind, Vector3 point, FoodNode node, ToolBuilding building, VillagerUnit target, Building structure)
        {
            Kind = kind;
            Point = point;
            Node = node;
            Building = building;
            Target = target;
            Structure = structure;
        }

        public static TribalOrder Move(Vector3 point) => new TribalOrder(OrderKind.Move, point, null, null, null, null);
        public static TribalOrder Gather(FoodNode node) => new TribalOrder(OrderKind.Gather, default, node, null, null, null);
        public static TribalOrder Equip(ToolBuilding building) => new TribalOrder(OrderKind.Equip, default, null, building, null, null);
        public static TribalOrder Attack(VillagerUnit target) => new TribalOrder(OrderKind.Attack, default, null, null, target, null);
        public static TribalOrder Raze(Building structure) => new TribalOrder(OrderKind.Raze, default, null, null, null, structure);

        public bool CanResume => Kind switch
        {
            OrderKind.Gather => Node != null,
            OrderKind.Raze => Structure != null && Structure.IsStanding,
            _ => false
        };
    }
}
