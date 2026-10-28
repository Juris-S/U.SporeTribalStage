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

        public static GameObject Play(VfxId id, Vector3 position) =>
            instance != null ? Play(id, position, instance.defaultLifetime) : null;

        public static GameObject Play(VfxId id, Vector3 position, float lifetime)
        {
            GameObject spawned = Spawn(id, position);
            if (spawned != null) Destroy(spawned, lifetime);
            return spawned;
        }

        public static GameObject PlayPersistent(VfxId id, Vector3 position) => Spawn(id, position);

        private static GameObject Spawn(VfxId id, Vector3 position)
        {
            if (instance == null || id == VfxId.None) return null;
            GameObject prefab = instance.library != null ? instance.library.Get(id) : null;
            if (prefab == null)
            {
                Debug.LogWarning($"[Vfx] No prefab bound to {id} in the VfxLibrary.");
                return null;
            }
            return Instantiate(prefab, position, Quaternion.identity, instance.transform);
        }
    }
}
