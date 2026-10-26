using UnityEngine;

namespace SporeTribal.Definitions
{
    [CreateAssetMenu(menuName = "Tribal/Raid", fileName = "Raid")]
    public class RaidDefinition : ScriptableObject
    {
        public enum Objective
        {
            Raid,
            StealFood
        }

        [System.Serializable]
        public struct ToolChance
        {
            [Tooltip("Empty means the member goes unarmed.")]
            public ToolDefinition tool;
            [Min(0f)] public float weight;
        }

        [SerializeField] private string displayName = "Raid";
        [SerializeField] private Objective objective = Objective.Raid;

        [Header("Timing (seconds)")]
        [Tooltip("mInitialDelay: wait this long after the relationship enters its range before the first check.")]
        [SerializeField, Min(0f)] private float initialDelay;
        [Tooltip("mCheckTime: when a check fails (too few villagers, wrong food level, failed roll), try again after this long.")]
        [SerializeField, Min(1f)] private float checkTime = 180f;
        [Tooltip("mTimeBetweenExecution: cooldown after the plan launches.")]
        [SerializeField, Min(0f)] private float timeBetweenExecution = 720f;
        [Tooltip("mFrequency: chance (0 to 100) that a check which passes actually launches the plan.")]
        [SerializeField, Range(0f, 100f)] private float frequency = 100f;

        [Header("Squad")]
        [Tooltip("mNumRequiredTribeMembers: the plan waits until this many villagers are free.")]
        [SerializeField, Min(1)] private int requiredMembers = 2;
        [Tooltip("mNumDesiredTribeMembers: takes up to this many when more are free.")]
        [SerializeField, Min(1)] private int desiredMembers = 2;
        [Tooltip("_membersToolsProb: each member rolls one entry, weighted. Leave empty for an unarmed squad.")]
        [SerializeField] private ToolChance[] memberTools;

        [Header("Target")]
        [Tooltip("mMinRelationshipToTarget / mMaxRelationshipToTarget: the plan only runs while our opinion of the target is inside this range (-100 hostile to 100 friendly; Spore raids at <= -30).")]
        [SerializeField, Range(-100, 100)] private int minRelationship = -100;
        [SerializeField, Range(-100, 100)] private int maxRelationship = -30;
        [Tooltip("mMinTargetPopulation / mMaxTargetPopulation: the target village must have a head count inside this range.")]
        [SerializeField, Min(0)] private int minTargetPopulation;
        [SerializeField, Min(0)] private int maxTargetPopulation = 9999;

        [Header("Food")]
        [Tooltip("Launch only while the tribe's store holds at least this much.")]
        [SerializeField, Min(0)] private int minOwnFood;
        [Tooltip("Launch only while the tribe's store holds at most this much (food thieves go out when stores are low).")]
        [SerializeField, Min(0)] private int maxOwnFood = 9999;
        [Tooltip("StealFood only: the most a thief can carry off.")]
        [SerializeField, Min(1)] private int stealAmount = 10;

        public string DisplayName => displayName;
        public Objective Goal => objective;
        public float InitialDelay => initialDelay;
        public float CheckTime => checkTime;
        public float TimeBetweenExecution => timeBetweenExecution;
        public float Frequency => frequency;
        public int RequiredMembers => requiredMembers;
        public int DesiredMembers => Mathf.Max(requiredMembers, desiredMembers);
        public int StealAmount => stealAmount;

        public bool RelationshipAllows(int relationship) => relationship >= minRelationship && relationship <= maxRelationship;
        public bool PopulationAllows(int population) => population >= minTargetPopulation && population <= maxTargetPopulation;

        public bool FoodAllows(int food) => food >= minOwnFood && food <= maxOwnFood;

        public ToolDefinition RollTool()
        {
            if (memberTools == null || memberTools.Length == 0) return null;
            float total = 0f;
            foreach (ToolChance chance in memberTools) total += chance.weight;
            float roll = Random.value * total;
            foreach (ToolChance chance in memberTools)
            {
                roll -= chance.weight;
                if (roll <= 0f) return chance.tool;
            }
            return memberTools[memberTools.Length - 1].tool;
        }
    }
}
