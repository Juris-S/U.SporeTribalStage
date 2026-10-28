using UnityEngine;

namespace SporeTribal.AI
{
    [System.Serializable]
    public class MusterSettings
    {
        [Tooltip("Where raid squads group up before setting off. Empty uses the village centre.")]
        [SerializeField] private Transform rallyPoint;
        [SerializeField] private float rallyRadius = 4f;
        [Tooltip("A squad sets off once everyone has rallied, or after this many seconds.")]
        [SerializeField] private float timeout = 25f;

        public float RallyRadius => rallyRadius;
        public float Timeout => timeout;

        public Vector3 RallyPositionOr(Vector3 fallback) => rallyPoint != null ? rallyPoint.position : fallback;
    }
}
