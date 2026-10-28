using SporeTribal.Definitions;
using UnityEngine;

namespace SporeTribal.AI
{
    public class RaidPlanSchedule
    {
        private const float ForcedRetrySeconds = 2f;

        private float nextCheck = float.MaxValue;
        private bool armed;

        public RaidPlanSchedule(RaidDefinition plan) => Plan = plan;

        public RaidDefinition Plan { get; }
        public bool IsForced { get; private set; }
        public bool IsDue => Time.time >= nextCheck;

        public void Arm()
        {
            if (armed) return;
            armed = true;
            nextCheck = Mathf.Min(nextCheck, Time.time + Plan.InitialDelay);
        }

        public void Disarm()
        {
            armed = false;
            nextCheck = float.MaxValue;
        }

        public void Force(float delay)
        {
            nextCheck = Mathf.Min(nextCheck, Time.time + delay);
            IsForced = true;
        }

        public void MarkLaunched()
        {
            nextCheck = Time.time + Plan.TimeBetweenExecution;
            IsForced = false;
        }

        public void MarkRefused() => nextCheck = Time.time + (IsForced ? ForcedRetrySeconds : Plan.CheckTime);
    }
}
