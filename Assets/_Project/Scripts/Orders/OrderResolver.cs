using SporeTribal.Economy;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.Orders
{
    public static class OrderResolver
    {
        public static OrderResult Resolve(VillagerUnit villager, ClickTarget target)
        {
            if (target.Building != null) return ResolveToolBuilding(villager, target.Building);
            if (!target.OnResource) return OrderResult.Accept(TribalOrder.Move(target.Point));
            if (target.Node == null) return OrderResult.Refuse(RefusalReason.TargetGone);
            return OrderResult.Accept(TribalOrder.Gather(target.Node));
        }

        private static OrderResult ResolveToolBuilding(VillagerUnit villager, ToolBuilding building)
        {
            if (villager.Tool == building.Tool) return OrderResult.Refuse(RefusalReason.AlreadyEquipped);
            Village village = villager.Home;
            if (village == null || village.Food < building.Tool.Cost) return OrderResult.Refuse(RefusalReason.CannotAfford);
            return OrderResult.Accept(TribalOrder.Equip(building));
        }
    }
}
