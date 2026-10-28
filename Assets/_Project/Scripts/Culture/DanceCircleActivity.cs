using System.Collections.Generic;
using PrimeTween;
using SporeTribal.Commands;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.Culture
{
    [CreateAssetMenu(menuName = "Tribal/Culture/Dance Circle", fileName = "DanceCircle")]
    public class DanceCircleActivity : CultureActivity
    {
        [Header("Dance circle")]
        [Tooltip("Metres between neighbours on the ring.")]
        [SerializeField] private float spacing = 1.8f;

        [Tooltip("How far the ring swells and shrinks as a fraction of its radius (0 = rigid circle).")]
        [SerializeField, Range(0f, 0.6f)] private float pulseAmount = 0.25f;
        [Tooltip("Swell-and-shrink cycles over the whole dance.")]
        [SerializeField] private float pulseBeats = 3f;

        public DanceCircleActivity()
        {
            SetAppeal(new AnimationCurve(new Keyframe(-1f, 0f), new Keyframe(0f, 0.25f), new Keyframe(1f, 1f)));
            SetIncludeChieftain(true);
        }

        public override int Fit(int available) => available < 3 ? 0 : base.Fit(available);

        public override void Layout(Vector3 centre, int count, List<ActivitySlot> slots)
        {
            foreach (Vector3 spot in CircleFormation.GetSlots(centre, count, spacing))
                slots.Add(new ActivitySlot(spot, centre));
        }

        public override Sequence Perform(IReadOnlyList<VillagerUnit> group, Vector3 centre, float seconds)
        {
            foreach (VillagerUnit unit in group)
            {
                unit.Dance(seconds, true);
                unit.Circle(centre, seconds, pulseAmount, pulseBeats);
            }
            return default;
        }
    }
}
