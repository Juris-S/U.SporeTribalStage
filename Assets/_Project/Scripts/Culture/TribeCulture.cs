using System.Collections.Generic;
using SporeTribal.Economy;
using UnityEngine;

namespace SporeTribal.Culture
{
    public enum CultureTemper { Aggressive, Balanced, Friendly }

    [RequireComponent(typeof(Village))]
    public class TribeCulture : MonoBehaviour
    {
        private static readonly List<TribeCulture> all = new List<TribeCulture>();
        public static IReadOnlyList<TribeCulture> All => all;

        [SerializeField, Range(-1f, 1f)] private float startAlignment;
        [Tooltip("Alignment shift per relationship point lost to an attack (negative direction).")]
        [SerializeField, Min(0f)] private float perOffencePoint = 0.004f;
        [Tooltip("Alignment shift per relationship point gained by a gift or a performance.")]
        [SerializeField, Min(0f)] private float perFriendshipPoint = 0.006f;
        [Tooltip("Beyond this magnitude the tribe reads as aggressive or friendly rather than balanced.")]
        [SerializeField, Range(0f, 1f)] private float temperThreshold = 0.25f;

        private Village village;

        public Village Village => village;
        public float Alignment { get; private set; }
        public CultureTemper Temper => Alignment <= -temperThreshold ? CultureTemper.Aggressive
            : Alignment >= temperThreshold ? CultureTemper.Friendly : CultureTemper.Balanced;

        public event System.Action<float> Changed;

        private void Awake()
        {
            village = GetComponent<Village>();
            Alignment = startAlignment;
        }

        private void OnEnable()
        {
            all.Add(this);
            village.OffendedOther += OnOffended;
            village.BefriendedOther += OnBefriended;
        }

        private void OnDisable()
        {
            all.Remove(this);
            village.OffendedOther -= OnOffended;
            village.BefriendedOther -= OnBefriended;
        }

        public void Shift(float delta)
        {
            float next = Mathf.Clamp(Alignment + delta, -1f, 1f);
            if (Mathf.Approximately(next, Alignment)) return;
            Alignment = next;
            Changed?.Invoke(Alignment);
        }

        public void Set(float alignment) => Shift(alignment - Alignment);

        private void OnOffended(Village _, int points) => Shift(-points * perOffencePoint);
        private void OnBefriended(Village _, int points) => Shift(points * perFriendshipPoint);
    }
}
