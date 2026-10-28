using UnityEngine;

namespace SporeTribal.Utilities
{
    public static class VectorExtensions
    {
        public static Vector3 Flat(this Vector3 v) => new Vector3(v.x, 0f, v.z);

        public static float FlatDistance(this Vector3 from, Vector3 to) => (to - from).Flat().magnitude;

        public static float FlatSqrDistance(this Vector3 from, Vector3 to) => (to - from).Flat().sqrMagnitude;

        public static bool IsWithinFlat(this Vector3 from, Vector3 to, float radius) => from.FlatSqrDistance(to) <= radius * radius;

        public static Vector3 FromBearing(float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        }
    }
}
