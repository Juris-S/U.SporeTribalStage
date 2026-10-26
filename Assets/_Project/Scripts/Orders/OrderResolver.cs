using SporeTribal.Buildings;
using SporeTribal.Economy;
using SporeTribal.Villagers;

namespace SporeTribal.Orders
{
    public static class OrderResolver
    {
        public static OrderResult Resolve(VillagerUnit villager, ClickTarget target)
        {
            if (target.Unit != null) return ResolveVillager(villager, target);
            if (target.Building != null) return ResolveBuilding(villager, target);
            if (!target.OnResource) return OrderResult.Accept(TribalOrder.Move(target.Point));
            if (target.Node == null) return OrderResult.Refuse(RefusalReason.TargetGone);
            return OrderResult.Accept(TribalOrder.Gather(target.Node));
        }

        private static OrderResult ResolveVillager(VillagerUnit villager, ClickTarget target)
        {
            if (!target.Unit.IsAlive) return OrderResult.Refuse(RefusalReason.TargetGone);
            if (villager.Home == null || !villager.Home.IsHostileTo(target.Unit.Home)) return OrderResult.Accept(TribalOrder.Move(target.Point));
            return OrderResult.Accept(TribalOrder.Attack(target.Unit));
        }

        private static OrderResult ResolveBuilding(VillagerUnit villager, ClickTarget target)
        {
            Building building = target.Building;
            if (!building.IsStanding) return OrderResult.Refuse(RefusalReason.TargetGone);
            if (villager.Home != null && villager.Home.IsHostileTo(building.Owner)) return OrderResult.Accept(TribalOrder.Raze(building));
            if (building is ToolBuilding tools && tools.Owner == villager.Home) return ResolveToolBuilding(villager, tools);
            return OrderResult.Accept(TribalOrder.Move(target.Point));
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
