using UnityEngine;
using SporeTribal.Rendering;

namespace SporeTribal.Utilities
{
    public static class ScreenRaycast
    {
        private const float MaxDistance = 500f;
        private const float BentStep = 4f;

        private static Camera cached;

        public static Camera Camera => cached != null ? cached : (cached = Camera.main);

        public static Vector3 WorldToScreen(Vector3 world) => Camera.WorldToScreenPoint(WorldBend.Apply(world));

        public static bool TryRaycast(Vector2 screenPosition, LayerMask mask, out RaycastHit hit)
        {
            Ray ray = Camera.ScreenPointToRay(screenPosition);
            return WorldBend.IsActive
                ? TryRaycastBent(ray, mask, out hit)
                : Physics.Raycast(ray, out hit, MaxDistance, mask);
        }

        public static bool TryPick<T>(Vector2 screenPosition, LayerMask mask, out T picked, out RaycastHit hit) where T : Component
        {
            picked = null;
            return TryRaycast(screenPosition, mask, out hit)
                && (picked = hit.collider.GetComponentInParent<T>()) != null;
        }

        public static bool TryPick<T>(Vector2 screenPosition, LayerMask mask, out T picked) where T : Component =>
            TryPick(screenPosition, mask, out picked, out _);

        private static bool TryRaycastBent(Ray ray, LayerMask mask, out RaycastHit hit)
        {
            Vector3 previous = WorldBend.Remove(ray.origin);
            for (float d = BentStep; d <= MaxDistance; d += BentStep)
            {
                Vector3 next = WorldBend.Remove(ray.GetPoint(d));
                Vector3 segment = next - previous;
                if (Physics.Raycast(previous, segment, out hit, segment.magnitude, mask)) return true;
                previous = next;
            }

            hit = default;
            return false;
        }
    }
}
