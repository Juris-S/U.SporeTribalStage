using UnityEngine;

namespace SporeTribal.Feedback
{
    [System.Serializable]
    public class PingStyle
    {
        [SerializeField, ColorUsage(true, true)] private Color color = new Color(1f, 0.95f, 0.6f, 1f);
        [SerializeField] private float duration = 0.45f;
        [SerializeField] private float startSize = 0.5f;
        [SerializeField] private float endSize = 2.2f;
        [Tooltip("Height above the ground so the ring does not z-fight with it.")]
        [SerializeField] private float lift = 0.03f;

        public Color Color => color;
        public float Duration => duration;
        public float StartSize => startSize;
        public float EndSize => endSize;
        public float Lift => lift;
    }
}
