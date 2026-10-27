using SporeTribal.Combat;
using UnityEngine;

namespace SporeTribal.Feedback
{
    [RequireComponent(typeof(Health))]
    public class HealthEffects : MonoBehaviour
    {
        [Tooltip("Played on every damage tick.")]
        [SerializeField] private VfxKey hitEffect = VfxKey.Hit;
        [Tooltip("Played once on death.")]
        [SerializeField] private VfxKey deathEffect = VfxKey.Death;
        [Tooltip("Height above the pivot where effects spawn.")]
        [SerializeField] private float spawnHeight = 1f;

        private Health health;

        private void Awake() => health = GetComponent<Health>();

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void OnDamaged(GameObject source) => VfxPlayer.Play(hitEffect, transform.position + Vector3.up * spawnHeight);

        private void OnDied() => VfxPlayer.Play(deathEffect, transform.position + Vector3.up * spawnHeight);
    }
}
