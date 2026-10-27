using SporeTribal.Rendering;
using UnityEngine;

namespace SporeTribal.Buildings
{
    public class VillageLayout : MonoBehaviour
    {
        [Tooltip("The plaza centre. The village's drop-off point sits here.")]
        [SerializeField] private Transform centre;
        [SerializeField] private Transform hut;
        [Tooltip("Tool buildings and other structures, placed around the outer ring in this order.")]
        [SerializeField] private Transform[] ring = new Transform[0];
        [Tooltip("Small pieces that stand inside the plaza, such as the dance spot, spread across the front.")]
        [SerializeField] private Transform[] plaza = new Transform[0];

        [Header("Shape")]
        [Tooltip("Compass direction the village opens towards, in degrees. 180 faces -Z, towards the default camera.")]
        [SerializeField] private float facing = 180f;
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

        public Vector3 Centre => (centre != null ? centre : transform).position;

        public Vector3 Slot(float angle, float radius)
        {
            float rad = angle * Mathf.Deg2Rad;
            return Centre + new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * radius;
        }

        [ContextMenu("Arrange")]
        public void Arrange()
        {
            float back = facing + 180f;
            if (hut != null) Place(hut, Slot(back, hutDistance), hutClearing, Vector2.zero);

            var active = System.Array.FindAll(ring, b => b != null && b.gameObject.activeInHierarchy);
            int slots = active.Length + (hut != null ? 1 : 0);
            float span = 360f - frontGap;
            int hutSlot = slots / 2;
            int next = 0;
            for (int s = 0; s < slots && next < active.Length; s++)
            {
                if (hut != null && s == hutSlot) continue;
                float angle = facing + frontGap * 0.5f + (s + 0.5f) * span / slots;
                Place(active[next++], Slot(angle, ringRadius), ringClearing, ringStone);
            }

            for (int i = 0; i < plaza.Length; i++)
            {
                if (plaza[i] == null) continue;
                RecordUndo(plaza[i]);
                float t = plaza.Length == 1 ? 0.5f : i / (float)(plaza.Length - 1);
                plaza[i].position = Slot(facing + (t - 0.5f) * plazaArc, plazaRadius);
            }

            var centreObject = centre != null ? centre : transform;
            var centreFootprint = Footprint(centreObject);
            centreFootprint.Configure(centreClearing.x, centreClearing.y, 0f, 1f);
            centreFootprint.ConfigureStar(ringRadius + starOverhang, star.x, star.y, star.z);
        }

        private void Place(Transform building, Vector3 position, Vector2 clearing, Vector2 stone)
        {
            RecordUndo(building);
            building.position = position;
            Vector3 toCentre = Centre - position;
            if (toCentre.sqrMagnitude > 0.01f) building.rotation = Quaternion.LookRotation(toCentre, Vector3.up);
            Footprint(building).Configure(clearing.x, clearing.y, stone.x, stone.y);
        }

        private static GroundFootprint Footprint(Transform t)
        {
            var f = t.GetComponent<GroundFootprint>();
            if (f == null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) return UnityEditor.Undo.AddComponent<GroundFootprint>(t.gameObject);
#endif
                f = t.gameObject.AddComponent<GroundFootprint>();
            }
            RecordUndo(f);
            return f;
        }

        private static void RecordUndo(Object target)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.Undo.RecordObject(target, "Arrange Village");
#endif
        }
    }
}
