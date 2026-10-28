using SporeTribal.Economy;
using UnityEngine;

namespace SporeTribal.Definitions
{
    [CreateAssetMenu(menuName = "Tribal/Recital", fileName = "Recital")]
    public class RecitalDefinition : ScriptableObject
    {
        private const float ExtraMusicianBonus = 0.5f;

        [Header("Gathering")]
        [Tooltip("If the band is still on its way after this many seconds, the crowd meets it wherever the chieftain is.")]
        [SerializeField] private float approachTimeout = 45f;
        [SerializeField] private int maxAudience = 6;
        [Tooltip("The recital starts once everyone is in place, or after this many seconds.")]
        [SerializeField] private float musterTimeout = 20f;

        [Header("Rounds")]
        [SerializeField] private float callSeconds = 5f;
        [SerializeField] private float pauseSeconds = 1.5f;
        [Tooltip("How long the band plays its answer before the crowd reacts to it.")]
        [SerializeField] private float playSeconds = 4f;
        [Tooltip("Where the friendship meter starts, so a few mistakes end the recital.")]
        [SerializeField, Range(0f, 0.9f)] private float startProgress = 0.35f;
        [Tooltip("Meter gained by one musician answering correctly. Each extra matching musician adds 50%.")]
        [SerializeField] private float baseIncrement = 0.25f;
        [SerializeField] private float failPenalty = 0.15f;
        [Tooltip("Relationship the rival gains when the meter fills. 70 is an alliance.")]
        [SerializeField] private int rewardAtFull = 70;
        [Tooltip("Relationship lost when the crowd leaves disappointed.")]
        [SerializeField] private int disappointment = 5;

        [Header("Tier tolerance")]
        [SerializeField] private TierTuning harsh = new TierTuning { time = 0.7f, gain = 1f, penalty = 1.5f };
        [SerializeField] private TierTuning neutral = new TierTuning { time = 1f, gain = 1f, penalty = 1f };
        [SerializeField] private TierTuning warm = new TierTuning { time = 1.25f, gain = 1.25f, penalty = 0.6f };

        [Header("Gifts")]
        [SerializeField] private int smallGift = 50;
        [SerializeField] private int largeGift = 100;

        public float ApproachTimeout => approachTimeout;
        public int MaxAudience => maxAudience;
        public float MusterTimeout => musterTimeout;
        public float CallSeconds => callSeconds;
        public float PauseSeconds => pauseSeconds;
        public float PlaySeconds => playSeconds;
        public float StartProgress => startProgress;
        public float FailPenalty => failPenalty;
        public int RewardAtFull => rewardAtFull;
        public int Disappointment => disappointment;
        public int SmallGift => smallGift;
        public int LargeGift => largeGift;

        public TierTuning TuningFor(RelationshipTier tier) => tier switch
        {
            RelationshipTier.AtWar => harsh,
            RelationshipTier.Unfriendly => harsh,
            RelationshipTier.Friendly => warm,
            _ => neutral
        };

        public float GainFor(int matchingMusicians, TierTuning tuning) =>
            baseIncrement * (1f + ExtraMusicianBonus * (matchingMusicians - 1)) * tuning.gain;

        public float EarnedFraction(float progress) => Mathf.Clamp01((progress - startProgress) / (1f - startProgress));
    }
}
