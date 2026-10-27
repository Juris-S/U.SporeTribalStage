using SporeTribal.Villagers;

namespace SporeTribal.Orders
{
    public enum RefusalReason { None, TargetGone, AlreadyEquipped, CannotAfford, NeedChieftain, NoInstrument, NoAudience, WarringTribe, ChieftainKeepsStaff }

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
            RefusalReason.NeedChieftain => "You must bring your chieftain to conduct the band!",
            RefusalReason.NoInstrument => "Your band needs at least one instrument.",
            RefusalReason.NoAudience => "There must be at least one tribe member present to socialize.",
            RefusalReason.WarringTribe => "They are at war with you. Send a gift or sing the soothing song first.",
            RefusalReason.ChieftainKeepsStaff => "The chieftain keeps their staff and never takes tools.",
            _ => string.Empty
        };
    }
}
