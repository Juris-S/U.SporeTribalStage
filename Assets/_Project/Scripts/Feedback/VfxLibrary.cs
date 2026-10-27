using System;
using UnityEngine;

namespace SporeTribal.Feedback
{
    [CreateAssetMenu(menuName = "SporeTribal/Feedback/Vfx Library", fileName = "VfxLibrary")]
    public class VfxLibrary : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            public VfxKey key;
            public GameObject prefab;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public GameObject Get(VfxKey key)
        {
            foreach (Entry entry in entries)
                if (entry.key == key) return entry.prefab;
            return null;
        }
    }
}
