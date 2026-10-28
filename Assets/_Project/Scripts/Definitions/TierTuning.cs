using UnityEngine;

namespace SporeTribal.Definitions
{
    [System.Serializable]
    public struct TierTuning
    {
        [Tooltip("Multiplies the time the audience gives for each call.")]
        public float time;
        [Tooltip("Multiplies the progress gained per correct answer.")]
        public float gain;
        [Tooltip("Multiplies the progress lost per mistake.")]
        public float penalty;
    }
}
