using System.Collections.Generic;
using SporeTribal.Definitions;
using SporeTribal.Rendering;
using SporeTribal.Utilities;
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
        [Tooltip("Compass direction the village opens towards, in degrees. 180 faces -Z, towards the default camera.")]
        [SerializeField] private float facing = 180f;
        [Tooltip("Ring sizes and ground footprints, shared between villages.")]
        [SerializeField] private VillageLayoutDefinition layout;

        private Transform CentreTransform => centre != null ? centre : transform;

        public Vector3 Centre => CentreTransform.position;

        public IEnumerable<Transform> Pieces
        {
            get
            {
                if (hut != null) yield return hut;
                foreach (Transform piece in ring) if (piece != null) yield return piece;
                foreach (Transform piece in plaza) if (piece != null) yield return piece;
            }
        }

        public Vector3 Slot(float bearing, float radius) => Centre + VectorExtensions.FromBearing(bearing) * radius;

        [ContextMenu("Arrange")]
        public void Arrange()
        {
            PlaceHut();
            PlaceRing();
            PlacePlaza();
            ConfigureCentreFootprint();
        }

        private void PlaceHut()
        {
            if (hut == null) return;
            float back = facing + 180f;
            Place(hut, Slot(back, layout.HutDistance), layout.HutClearing, Vector2.zero);
        }

        private void PlaceRing()
        {
            Transform[] active = System.Array.FindAll(ring, b => b != null && b.gameObject.activeInHierarchy);
            int slots = active.Length + (hut != null ? 1 : 0);
            float span = 360f - layout.FrontGap;
            int hutSlot = slots / 2;
            int next = 0;
            for (int s = 0; s < slots && next < active.Length; s++)
            {
                if (hut != null && s == hutSlot) continue;
                float bearing = facing + layout.FrontGap * 0.5f + (s + 0.5f) * span / slots;
                Place(active[next++], Slot(bearing, layout.RingRadius), layout.RingClearing, layout.RingStone);
            }
        }

        private void PlacePlaza()
        {
            for (int i = 0; i < plaza.Length; i++)
            {
                if (plaza[i] == null) continue;
                RecordUndo(plaza[i]);
                float t = plaza.Length == 1 ? 0.5f : i / (float)(plaza.Length - 1);
                plaza[i].position = Slot(facing + (t - 0.5f) * layout.PlazaArc, layout.PlazaRadius);
            }
        }

        private void ConfigureCentreFootprint()
        {
            GroundFootprint footprint = Footprint(CentreTransform);
            Vector2 clearing = layout.CentreClearing;
            Vector3 star = layout.Star;
            footprint.Configure(clearing.x, clearing.y, 0f, 1f);
            footprint.ConfigureStar(layout.RingRadius + layout.StarOverhang, star.x, star.y, star.z);
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
            if (!t.TryGetComponent(out GroundFootprint footprint))
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) return UnityEditor.Undo.AddComponent<GroundFootprint>(t.gameObject);
#endif
                footprint = t.gameObject.AddComponent<GroundFootprint>();
            }
            RecordUndo(footprint);
            return footprint;
        }

        private static void RecordUndo(Object target)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.Undo.RecordObject(target, "Arrange Village");
#endif
        }
    }
}
