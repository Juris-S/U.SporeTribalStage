using UnityEngine;

namespace SporeTribal.Utilities
{
    public static class ScreenRaycast
    {
        private const float MaxDistance = 500f;

        private static Camera cached;

        public static Camera Camera => cached != null ? cached : (cached = Camera.main);

        public static bool TryRaycast(Vector2 screenPosition, LayerMask mask, out RaycastHit hit)
        {
            return Physics.Raycast(Camera.ScreenPointToRay(screenPosition), out hit, MaxDistance, mask);
        }
    }
}
