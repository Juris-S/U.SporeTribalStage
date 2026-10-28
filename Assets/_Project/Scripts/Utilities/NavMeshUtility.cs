using UnityEngine;
using UnityEngine.AI;

namespace SporeTribal.Utilities
{
    public static class NavMeshUtility
    {
        private const float DefaultSnapDistance = 3f;

        public static Vector3 Snap(Vector3 point, float maxDistance = DefaultSnapDistance) =>
            NavMesh.SamplePosition(point, out NavMeshHit hit, maxDistance, NavMesh.AllAreas) ? hit.position : point;
    }
}
