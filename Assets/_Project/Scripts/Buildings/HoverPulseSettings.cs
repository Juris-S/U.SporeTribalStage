using UnityEngine;

namespace SporeTribal.Buildings
{
    [System.Serializable]
    public class HoverPulseSettings
    {
        [Tooltip("Extra albedo multiplier at the peak of the pulse (0.35 = 35% brighter).")]
        [SerializeField] private float peakBrightness = 0.35f;
        [Tooltip("Seconds for one full breath.")]
        [SerializeField] private float period = 0.8f;
        [Tooltip("Seconds for the pulse to fade in on hover and fade out after the mouse leaves.")]
        [SerializeField] private float fadeTime = 0.35f;
        [Tooltip("Seconds the hover survives a missed raycast (a villager walking past, a gap between colliders) before fading out.")]
        [SerializeField] private float lostGrace = 0.15f;

        public float PeakBrightness => peakBrightness;
        public float Period => period;
        public float FadeTime => fadeTime;
        public float LostGrace => lostGrace;
    }
}
