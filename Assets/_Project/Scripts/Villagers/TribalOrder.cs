using SporeTribal.Buildings;
using SporeTribal.Economy;
using UnityEngine;

namespace SporeTribal.Villagers
{
    public enum OrderKind { Move, Gather, Equip, Attack, Raze, Perform, Watch }

    public readonly struct TribalOrder
    {
        public readonly OrderKind Kind;
        public readonly Vector3 Point;
        public readonly FoodNode Node;
        public readonly ToolBuilding Building;
        public readonly VillagerUnit Target;
        public readonly Building Structure;
        public readonly Village Audience;
        public readonly Vector3 Look;

        private TribalOrder(OrderKind kind, Vector3 point, FoodNode node, ToolBuilding building, VillagerUnit target, Building structure, Village audience = null, Vector3 look = default)
        {
            Kind = kind;
            Point = point;
            Node = node;
            Building = building;
            Target = target;
            Structure = structure;
            Audience = audience;
            Look = look;
        }

        public static TribalOrder Move(Vector3 point) => new TribalOrder(OrderKind.Move, point, null, null, null, null);
        public static TribalOrder Gather(FoodNode node) => new TribalOrder(OrderKind.Gather, default, node, null, null, null);
        public static TribalOrder Equip(ToolBuilding building) => new TribalOrder(OrderKind.Equip, default, null, building, null, null);
        public static TribalOrder Attack(VillagerUnit target) => new TribalOrder(OrderKind.Attack, default, null, null, target, null);
        public static TribalOrder Raze(Building structure) => new TribalOrder(OrderKind.Raze, default, null, null, null, structure);

        public static TribalOrder Perform(Village audience) => new TribalOrder(OrderKind.Perform, default, null, null, null, null, audience);
        public static TribalOrder Perform(Village audience, Vector3 slot, Vector3 look) => new TribalOrder(OrderKind.Perform, slot, null, null, null, null, audience, look);
        public static TribalOrder Watch(Vector3 slot, Vector3 look) => new TribalOrder(OrderKind.Watch, slot, null, null, null, null, null, look);

        public bool CanResume => Kind switch
        {
            OrderKind.Gather => Node != null,
            OrderKind.Raze => Structure != null && Structure.IsStanding,
            _ => false
        };
    }
}
