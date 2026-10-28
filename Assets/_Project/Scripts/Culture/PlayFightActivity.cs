using System.Collections.Generic;
using PrimeTween;
using SporeTribal.Commands;
using SporeTribal.Feedback;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.Culture
{
    [CreateAssetMenu(menuName = "Tribal/Culture/Play Fight", fileName = "PlayFight")]
    public class PlayFightActivity : CultureActivity
    {
        [Header("Play fight")]
        [Tooltip("Distance between the two partners at the start of each bout.")]
        [SerializeField] private float partnerGap = 1.3f;
        [Tooltip("Spacing between separate pairs so they do not bump into each other.")]
        [SerializeField] private float pairSpacing = 4f;
        [Tooltip("Seconds per push. A round is both partners pushing once.")]
        [SerializeField] private float beatSeconds = 0.9f;
        [Tooltip("The pusher stepping in.")]
        [SerializeField] private ShoveMotion lunge = new ShoveMotion { distance = 0.35f, tilt = 8f };
        [Tooltip("The pushed partner reeling back.")]
        [SerializeField] private ShoveMotion stagger = new ShoveMotion { distance = 0.8f, tilt = -20f };
        [Tooltip("Delay between the pusher lunging and the pushed one reeling.")]
        [SerializeField] private float impactDelay = 0.12f;

        [Header("Effects")]
        [Tooltip("Looping effect played between each pair while they play fight.")]
        [SerializeField] private VfxId fightEffect = VfxId.PlayFight;
        [SerializeField] private float effectHeight = 1f;

        public PlayFightActivity()
        {
            SetAppeal(new AnimationCurve(new Keyframe(-1f, 1f), new Keyframe(0f, 0.25f), new Keyframe(1f, 0f)));
        }

        public override int Fit(int available) => base.Fit(available) / 2 * 2;

        public override void Layout(Vector3 centre, int count, List<ActivitySlot> slots)
        {
            int pairs = count / 2;
            Vector3[] centres = CircleFormation.GetSlots(centre, pairs, pairSpacing);
            for (int i = 0; i < pairs; i++)
            {
                Vector3 outward = centres[i] - centre;
                outward.y = 0f;
                Vector3 axis = outward.sqrMagnitude < 0.01f ? Vector3.right : Vector3.Cross(Vector3.up, outward.normalized);
                Vector3 first = centres[i] - axis * (partnerGap * 0.5f);
                Vector3 second = centres[i] + axis * (partnerGap * 0.5f);
                slots.Add(new ActivitySlot(first, second));
                slots.Add(new ActivitySlot(second, first));
            }
        }

        public override Sequence Perform(IReadOnlyList<VillagerUnit> group, Vector3 centre, float seconds)
        {
            int rounds = Mathf.Max(1, Mathf.FloorToInt(seconds / (beatSeconds * 2f)));
            Sequence all = Sequence.Create();
            for (int i = 0; i + 1 < group.Count; i += 2)
            {
                var bout = new Bout(this, group[i], group[i + 1]);
                Vector3 middle = (group[i].transform.position + group[i + 1].transform.position) * 0.5f + Vector3.up * effectHeight;
                ActivityEffect.Spawn(fightEffect, middle, seconds, group[i], group[i + 1]);
                all.Group(Sequence.Create(cycles: rounds)
                    .ChainDelay(Random.Range(0f, beatSeconds))
                    .ChainCallback(bout, static b => b.Push(true))
                    .ChainDelay(impactDelay)
                    .ChainCallback(bout, static b => b.Reel(true))
                    .ChainDelay(beatSeconds)
                    .ChainCallback(bout, static b => b.Push(false))
                    .ChainDelay(impactDelay)
                    .ChainCallback(bout, static b => b.Reel(false))
                    .ChainDelay(beatSeconds));
            }
            return all;
        }

        private sealed class Bout
        {
            private readonly PlayFightActivity owner;
            private readonly VillagerUnit first;
            private readonly VillagerUnit second;

            public Bout(PlayFightActivity owner, VillagerUnit first, VillagerUnit second)
            {
                this.owner = owner;
                this.first = first;
                this.second = second;
            }

            public void Push(bool firstPushes)
            {
                if (!Ready(out Vector3 toward, firstPushes)) return;
                (firstPushes ? first : second).Shove(toward, owner.lunge.distance, owner.impactDelay * 2f, owner.lunge.tilt);
            }

            public void Reel(bool firstPushes)
            {
                if (!Ready(out Vector3 toward, firstPushes)) return;
                (firstPushes ? second : first).Shove(toward, owner.stagger.distance, owner.beatSeconds * 0.6f, owner.stagger.tilt);
            }

            private bool Ready(out Vector3 toward, bool firstPushes)
            {
                toward = default;
                if (first == null || second == null) return false;
                VillagerUnit pusher = firstPushes ? first : second;
                VillagerUnit pushed = firstPushes ? second : first;
                toward = pushed.transform.position - pusher.transform.position;
                return true;
            }
        }
    }
}
