using UnityEngine;

namespace SporeTribal.Culture
{
    [System.Serializable]
    public struct ShoveMotion
    {
        [Tooltip("Metres the villager is moved along the push.")]
        public float distance;
        [Tooltip("Degrees the body tips while moving: positive leans in, negative reels back.")]
        public float tilt;
    }
}
