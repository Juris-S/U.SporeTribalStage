using SporeTribal.Commands;
using SporeTribal.Economy;

namespace SporeTribal.AI
{
    public readonly struct RaidContext
    {
        public readonly OrderDispatcher Dispatcher;
        public readonly Village Home;
        public readonly Village Target;

        public RaidContext(OrderDispatcher dispatcher, Village home, Village target)
        {
            Dispatcher = dispatcher;
            Home = home;
            Target = target;
        }

        public bool IsStillHostile => Home.IsHostileTo(Target);
    }
}
