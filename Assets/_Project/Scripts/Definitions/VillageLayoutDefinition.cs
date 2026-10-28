using UnityEngine;

namespace SporeTribal.Definitions
{
    [CreateAssetMenu(menuName = "Tribal/Village Layout", fileName = "VillageLayout")]
    public class VillageLayoutDefinition : ScriptableObject
    {
        [Header("Shape")]
        [SerializeField, Min(0f)] private float hutDistance = 12f;
        [SerializeField, Min(0f)] private float ringRadius = 14f;
        [Tooltip("Width of the open entrance at the front of the ring, in degrees.")]
        [SerializeField, Range(0f, 180f)] private float frontGap = 80f;
        [SerializeField, Min(0f)] private float plazaRadius = 6f;
        [Tooltip("Width of the arc the plaza pieces spread over, in degrees.")]
        [SerializeField, Range(0f, 180f)] private float plazaArc = 90f;

        [Header("Footprints")]
        [SerializeField] private Vector2 centreClearing = new Vector2(10f, 6f);
        [SerializeField] private Vector2 hutClearing = new Vector2(5f, 4f);
        [SerializeField] private Vector2 ringClearing = new Vector2(5.5f, 4f);
        [Tooltip("Radius and feather of a solid stony patch under each ring building. 0 leaves them on the star.")]
        [SerializeField] private Vector2 ringStone = Vector2.zero;
        [Tooltip("Stone wedges between the paths from the centre to each building: sand hole radius, spoke half width, feather.")]
        [SerializeField] private Vector3 star = new Vector3(3.5f, 1.4f, 2f);
        [Tooltip("How far past the ring the stone wedges reach.")]
        [SerializeField] private float starOverhang = 0.5f;

        public float HutDistance => hutDistance;
        public float RingRadius => ringRadius;
        public float FrontGap => frontGap;
        public float PlazaRadius => plazaRadius;
        public float PlazaArc => plazaArc;
        public Vector2 CentreClearing => centreClearing;
        public Vector2 HutClearing => hutClearing;
        public Vector2 RingClearing => ringClearing;
        public Vector2 RingStone => ringStone;
        public Vector3 Star => star;
        public float StarOverhang => starOverhang;
    }
}
