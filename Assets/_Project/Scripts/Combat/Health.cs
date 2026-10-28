using UnityEngine;

namespace SporeTribal.Combat
{
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private int max = 50;
        [Tooltip("Seconds without taking damage before health starts to regenerate.")]
        [SerializeField] private float regenDelay = 20f;
        [SerializeField] private float regenPerSecond = 2f;

        private float current;
        private float lastHitTime = -999f;

        public event System.Action<GameObject> Damaged;
        public event System.Action Died;

        public int Max => max;
        public int Current => Mathf.CeilToInt(current);
        public float Fraction => current / max;
        public bool IsAlive => current > 0f;
        public float LastHitTime => lastHitTime;

        private void Awake() => current = max;

        private void Update()
        {
            if (IsAlive && current < max && Time.time - lastHitTime >= regenDelay)
                current = Mathf.Min(max, current + regenPerSecond * Time.deltaTime);
        }

        public void Damage(int amount, GameObject source)
        {
            if (!IsAlive || amount <= 0) return;
            current = Mathf.Max(0f, current - amount);
            lastHitTime = Time.time;
            Damaged?.Invoke(source);
            if (!IsAlive) Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsAlive && amount > 0f) current = Mathf.Min(max, current + amount);
        }
    }
}
