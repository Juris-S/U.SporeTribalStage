using System;
using UnityEngine;

namespace SporeTribal.Feedback
{
    [CreateAssetMenu(menuName = "Tribal/Feedback/Vfx Library", fileName = "VfxLibrary")]
    public class VfxLibrary : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            public VfxId key;
            public GameObject prefab;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public GameObject Get(VfxId key)
        {
            foreach (Entry entry in entries)
                if (entry.key == key) return entry.prefab;
            return null;
        }
    }
}
