using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.Feedback
{
    public class ActivityEffect : MonoBehaviour
    {
        private const float EndGrace = 3f;

        private VillagerUnit[] units;
        private float endTime;
        private bool ending;

        public static void Spawn(VfxKey key, Vector3 position, float seconds, params VillagerUnit[] units)
        {
            GameObject instance = VfxPlayer.Play(key, position, seconds + EndGrace);
            if (instance == null) return;
            var effect = instance.AddComponent<ActivityEffect>();
            effect.units = units;
            effect.endTime = Time.time + seconds;
        }

        private void Update()
        {
            if (ending) return;
            if (Time.time < endTime && Performing()) return;
            ending = true;
            foreach (ParticleSystem system in GetComponentsInChildren<ParticleSystem>())
                system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(gameObject, EndGrace);
        }

        private bool Performing()
        {
            foreach (VillagerUnit unit in units)
                if (unit == null || !unit.IsAlive || !unit.IsAttending) return false;
            return true;
        }
    }
}
