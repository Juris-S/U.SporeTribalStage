using UnityEngine;

namespace SporeTribal.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        float Fraction { get; }
        void Damage(int amount, GameObject source);
    }
}
