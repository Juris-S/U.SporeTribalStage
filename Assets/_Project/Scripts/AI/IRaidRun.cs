using SporeTribal.Villagers;

namespace SporeTribal.AI
{
    public interface IRaidRun
    {
        bool IsOver { get; }
        bool Owns(VillagerUnit unit);
        void Tick();
        void End();
    }
}
