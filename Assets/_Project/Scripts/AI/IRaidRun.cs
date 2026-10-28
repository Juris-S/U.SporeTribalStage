using SporeTribal.Villagers;

namespace SporeTribal.AI
{
    public interface IRaidRun
    {
        bool IsOver { get; }
        bool Owns(VillagerUnit unit);
        void Begin();
        void Tick();
        void End();
    }
}
