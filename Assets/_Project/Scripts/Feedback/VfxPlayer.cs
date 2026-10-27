using UnityEngine;

namespace SporeTribal.Feedback
{
    public class VfxPlayer : MonoBehaviour
    {
        [SerializeField] private VfxLibrary library;
        [Tooltip("Seconds before a spawned effect is destroyed, unless the caller passes its own.")]
        [SerializeField] private float defaultLifetime = 5f;

        private static VfxPlayer instance;

        private void Awake() => instance = this;

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public static GameObject Play(VfxKey key, Vector3 position, float lifetime = -1f)
        {
            if (instance == null || key == VfxKey.None) return null;
            GameObject prefab = instance.library != null ? instance.library.Get(key) : null;
            if (prefab == null)
            {
                Debug.LogWarning($"[Vfx] No prefab bound to {key} in the VfxLibrary.");
                return null;
            }
            GameObject spawned = Instantiate(prefab, position, Quaternion.identity, instance.transform);
            if (lifetime < 0f) lifetime = instance.defaultLifetime;
            if (lifetime > 0f) Destroy(spawned, lifetime);
            return spawned;
        }
    }
}
