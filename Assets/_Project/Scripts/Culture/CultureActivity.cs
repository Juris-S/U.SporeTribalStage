using System.Collections.Generic;
using PrimeTween;
using SporeTribal.Audio;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.Culture
{
    public abstract class CultureActivity : ScriptableObject
    {
        [SerializeField] private string label;
        [Tooltip("Sound held at the activity's centre while it performs. None for silence.")]
        [SerializeField] private SfxId sound;
        [Tooltip("How much the tribe wants this, by alignment: -1 (aggressive) on the left, +1 (friendly) on the right. 0 never happens, 1 is the most frequent.")]
        [SerializeField] private AnimationCurve appeal = AnimationCurve.Linear(-1f, 0f, 1f, 1f);
        [Tooltip("Fewest and most villagers taking part (min, max).")]
        [SerializeField] private Vector2Int groupSize = new Vector2Int(2, 6);
        [Tooltip("How long the activity runs once everyone is in place (min, max seconds).")]
        [SerializeField] private Vector2 duration = new Vector2(8f, 14f);

        [Tooltip("Whether the chieftain may be borrowed for this activity.")]
        [SerializeField] private bool includeChieftain;

        public bool IncludeChieftain => includeChieftain;
        public SfxId Sound => sound;
        public string Label => string.IsNullOrEmpty(label) ? name : label;
        public Vector2 Duration => duration;

        public float AppealAt(float alignment) => Mathf.Max(0f, appeal.Evaluate(alignment));

        protected void SetIncludeChieftain(bool include) => includeChieftain = include;

        protected void SetAppeal(AnimationCurve curve) => appeal = curve;

        public virtual int Fit(int available)
        {
            int count = Mathf.Min(available, groupSize.y);
            return count >= groupSize.x ? count : 0;
        }

        public abstract void Layout(Vector3 centre, int count, List<ActivitySlot> slots);

        public abstract Sequence Perform(IReadOnlyList<VillagerUnit> group, Vector3 centre, float seconds);

        public virtual void Conclude(IReadOnlyList<VillagerUnit> group)
        {
            foreach (VillagerUnit unit in group) unit.Happy();
        }
    }
}
