using System;
using UnityEngine;

namespace SporeTribal.Audio
{
    [CreateAssetMenu(menuName = "Tribal/Audio/Sfx Library", fileName = "SfxLibrary")]
    public class SfxLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public SfxId key;
            [Tooltip("One is picked at random per play, never the same twice in a row.")]
            public AudioClip[] clips = Array.Empty<AudioClip>();
            [Range(0f, 1f)] public float volume = 1f;
            [Tooltip("Pitch is randomised within 1 +/- this.")]
            [Range(0f, 0.3f)] public float pitchVariance = 0.05f;
            [Tooltip("Minimum seconds between two plays of this id, so a drag-select does not stack ten copies.")]
            [Min(0f)] public float cooldown = 0.1f;

            [NonSerialized] public int lastIndex = -1;
            [NonSerialized] public float nextAllowed;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public Entry Get(SfxId key)
        {
            foreach (Entry entry in entries)
                if (entry.key == key) return entry;
            return null;
        }
    }
}
