using SporeTribal.Villagers;

namespace SporeTribal.Orders
{
    public enum RefusalReason { None, TargetGone, AlreadyEquipped, CannotAfford }

    public readonly struct OrderResult
    {
        public readonly TribalOrder? Order;
        public readonly RefusalReason Refusal;

        private OrderResult(TribalOrder? order, RefusalReason refusal)
        {
            Order = order;
            Refusal = refusal;
        }

        public bool Accepted => Order.HasValue;

        public static OrderResult Accept(TribalOrder order) => new OrderResult(order, RefusalReason.None);
        public static OrderResult Refuse(RefusalReason reason) => new OrderResult(null, reason);
    }

    public static class RefusalText
    {
        public static string Describe(this RefusalReason reason) => reason switch
        {
            RefusalReason.TargetGone => "That target is gone.",
            RefusalReason.AlreadyEquipped => "Already carrying that tool.",
            RefusalReason.CannotAfford => "Not enough food.",
            _ => string.Empty
        };
    }
}
