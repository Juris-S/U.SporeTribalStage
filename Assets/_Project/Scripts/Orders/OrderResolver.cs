using SporeTribal.Villagers;

namespace SporeTribal.Orders
{
    public static class OrderResolver
    {
        public static OrderResult Resolve(VillagerUnit villager, ClickTarget target)
        {
            if (!target.OnResource) return OrderResult.Accept(TribalOrder.Move(target.Point));
            if (target.Node == null) return OrderResult.Refuse(RefusalReason.TargetGone);
            return OrderResult.Accept(TribalOrder.Gather(target.Node));
        }
    }
}
