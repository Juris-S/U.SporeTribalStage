using System.Collections.Generic;
using UnityEngine;

namespace SporeTribal.Rendering
{
    [ExecuteAlways]
    public class GroundFootprint : MonoBehaviour
    {
        private static readonly List<GroundFootprint> all = new List<GroundFootprint>();
        public static IReadOnlyList<GroundFootprint> All => all;
        public static event System.Action Changed;

        [Tooltip("Radius of fully worn ground around this object.")]
        [SerializeField, Min(0f)] private float clearingRadius = 5f;
        [Tooltip("How far past the clearing radius the worn ground fades back into grass.")]
        [SerializeField, Min(0.01f)] private float clearingFeather = 4f;
        [Tooltip("Radius of the stony patch under the building. 0 leaves bare sand, like the plaza centre.")]
        [SerializeField, Min(0f)] private float stoneRadius;
        [SerializeField, Min(0.01f)] private float stoneFeather = 2f;

        [Header("Plaza Star")]
        [Tooltip("Outer radius of the stony ring around a plaza centre. 0 turns the star off.")]
        [SerializeField, Min(0f)] private float starRadius;
        [Tooltip("Radius of the bare sand left in the middle of the star.")]
        [SerializeField, Min(0f)] private float starHole = 3.5f;
        [Tooltip("Half width of the sandy spoke worn towards each neighbouring footprint.")]
        [SerializeField, Min(0f)] private float spokeWidth = 1.5f;
        [SerializeField, Min(0.01f)] private float starFeather = 2f;

        private Vector2 lastPosition;

        public Vector2 Position => new Vector2(transform.position.x, transform.position.z);
        public bool HasStar => starRadius > 0f;
        public float Reach => Mathf.Max(clearingRadius + clearingFeather, stoneRadius + stoneFeather, starRadius + starFeather);

        public void Configure(float clearing, float feather, float stone, float stoneEdge)
        {
            clearingRadius = clearing;
            clearingFeather = feather;
            stoneRadius = stone;
            stoneFeather = stoneEdge;
            Changed?.Invoke();
        }

        public void ConfigureStar(float radius, float hole, float spoke, float feather)
        {
            starRadius = radius;
            starHole = hole;
            spokeWidth = spoke;
            starFeather = feather;
            Changed?.Invoke();
        }

        public float ClearingAt(Vector2 p) => Falloff(p, clearingRadius, clearingFeather);
        public float StoneAt(Vector2 p) => stoneRadius > 0f ? Falloff(p, stoneRadius, stoneFeather) : 0f;

        public float StarAt(Vector2 p, List<Vector2> neighbours)
        {
            if (!HasStar) return 0f;
            var c = Position;
            float d = Vector2.Distance(p, c);
            float band = Falloff(p, starRadius, starFeather) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(starHole, starHole + starFeather, d));
            if (band <= 0f) return 0f;
            float path = 0f;
            foreach (var n in neighbours)
            {
                var seg = n - c;
                float t = Mathf.Clamp01(Vector2.Dot(p - c, seg) / Mathf.Max(seg.sqrMagnitude, 0.0001f));
                float off = Vector2.Distance(p, c + seg * t);
                path = Mathf.Max(path, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(spokeWidth, spokeWidth + starFeather, off)));
            }
            return band * (1f - path);
        }

        public static float ClearingAtPoint(Vector2 p)
        {
            float v = 0f;
            foreach (var f in all) v = Mathf.Max(v, f.ClearingAt(p));
            return v;
        }

        private float Falloff(Vector2 p, float radius, float feather)
        {
            float d = Vector2.Distance(p, Position);
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(radius, radius + feather, d));
        }

        private void OnEnable()
        {
            all.Add(this);
            lastPosition = Position;
            Changed?.Invoke();
        }

        private void OnDisable()
        {
            all.Remove(this);
            Changed?.Invoke();
        }

        private void OnValidate() => Changed?.Invoke();

        public bool ConsumeMoved()
        {
            var p = Position;
            if ((p - lastPosition).sqrMagnitude < 0.0001f) return false;
            lastPosition = p;
            return true;
        }
    }
}
