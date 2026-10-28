using System.Collections.Generic;
using PrimeTween;
using SporeTribal.Economy;
using SporeTribal.Villagers;
using UnityEngine;
using UnityEngine.AI;

namespace SporeTribal.Culture
{
    [RequireComponent(typeof(Village), typeof(TribeCulture))]
    public class IdleActivityDirector : MonoBehaviour
    {
        public enum Phase { Idle, Gathering, Performing }

        [Tooltip("Activities this tribe may choose from. Their appeal curves decide how often each one happens for the tribe's alignment.")]
        [SerializeField] private CultureActivity[] activities;
        [Tooltip("Seconds between activities at full appeal (min, max). Weaker appeal stretches the gap.")]
        [SerializeField] private Vector2 gapSeconds = new Vector2(20f, 40f);
        [Tooltip("Floor on the appeal used to stretch the gap, so a balanced tribe still acts now and then.")]
        [SerializeField, Range(0.05f, 1f)] private float minimumAppeal = 0.2f;
        [Tooltip("Optional: activities form here instead of where the idle villagers happen to be (e.g. the visual middle of the buildings).")]
        [SerializeField] private Transform activitySpot;
        [Tooltip("Gives up waiting for stragglers after this many seconds and starts with whoever is there.")]
        [SerializeField] private float gatherTimeout = 12f;

        private const float ArrivalGrace = 0.5f;

        private readonly List<VillagerUnit> candidates = new List<VillagerUnit>();
        private readonly List<VillagerUnit> group = new List<VillagerUnit>();
        private readonly List<int> serials = new List<int>();
        private readonly List<ActivitySlot> slots = new List<ActivitySlot>();
        private Village village;
        private TribeCulture culture;
        private Sequence running;
        private Vector3 centre;
        private float phaseStart;
        private float phaseEnd;
        private float nextAt;

        public IReadOnlyList<CultureActivity> Activities => activities;
        public CultureActivity Current { get; private set; }
        public Phase CurrentPhase { get; private set; }
        public TribeCulture Culture => culture;
        public string LastRefusal { get; private set; }

        public Vector3 Centre => centre;

        public event System.Action<CultureActivity> Started;
        public event System.Action<CultureActivity> Performing;
        public event System.Action<CultureActivity> Ended;

        private void Awake()
        {
            village = GetComponent<Village>();
            culture = GetComponent<TribeCulture>();
        }

        private void Start() => ScheduleNext();

        private void OnDisable() => End(false);

        private void Update()
        {
            if (Current == null)
            {
                if (Time.time >= nextAt) AutoStart();
                return;
            }
            if (Disturbed())
            {
                End(false);
                return;
            }
            if (CurrentPhase == Phase.Gathering)
            {
                bool settled = Time.time - phaseStart > ArrivalGrace && AllInPlace();
                if (settled || Time.time >= phaseStart + gatherTimeout) BeginPerforming();
            }
            else if (Time.time >= phaseEnd) End(true);
        }

        public bool TryStart(CultureActivity activity)
        {
            LastRefusal = null;
            if (Current != null || activity == null || village.HasSurrendered)
            {
                LastRefusal = Current != null ? $"already running {Current.Label}" : "no activity or tribe surrendered";
                return false;
            }

            candidates.Clear();
            foreach (VillagerUnit unit in VillagerUnit.All)
                if (unit.Home == village && (activity.IncludeChieftain ? unit.IsFree : unit.IsIdle)) candidates.Add(unit);
            if (candidates.Count == 0)
            {
                LastRefusal = "no idle villagers";
                return false;
            }

            Vector3 seed = activitySpot != null ? activitySpot.position : candidates[Random.Range(0, candidates.Count)].transform.position;
            candidates.Sort((a, b) => (a.transform.position - seed).sqrMagnitude.CompareTo((b.transform.position - seed).sqrMagnitude));
            int count = activity.Fit(candidates.Count);
            if (count == 0)
            {
                LastRefusal = $"{candidates.Count} idle villager(s) is not enough";
                return false;
            }

            group.Clear();
            serials.Clear();
            centre = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                group.Add(candidates[i]);
                centre += candidates[i].transform.position;
            }
            centre = Snap(activitySpot != null ? activitySpot.position : centre / count);

            slots.Clear();
            activity.Layout(centre, count, slots);
            for (int i = 0; i < count; i++)
            {
                group[i].Accept(TribalOrder.Watch(Snap(slots[i].Position), slots[i].Look));
                serials.Add(group[i].OrderSerial);
            }

            Current = activity;
            CurrentPhase = Phase.Gathering;
            phaseStart = Time.time;
            village.Attacked += OnAttacked;
            village.Surrendered += OnSurrendered;
            Started?.Invoke(activity);
            return true;
        }

        public void Cancel() => End(false);

        private void AutoStart()
        {
            CultureActivity pick = PickByAlignment();
            ScheduleNext();
            if (pick != null) TryStart(pick);
        }

        private CultureActivity PickByAlignment()
        {
            float total = TotalAppeal();
            if (total <= 0f) return null;
            float roll = Random.value * total;
            foreach (CultureActivity activity in activities)
            {
                roll -= activity.AppealAt(culture.Alignment);
                if (roll <= 0f) return activity;
            }
            return null;
        }

        private float TotalAppeal()
        {
            float total = 0f;
            if (activities == null) return total;
            foreach (CultureActivity activity in activities)
                if (activity != null) total += activity.AppealAt(culture.Alignment);
            return total;
        }

        private void ScheduleNext()
        {
            float appeal = Mathf.Clamp(TotalAppeal(), minimumAppeal, 1f);
            nextAt = Time.time + Random.Range(gapSeconds.x, gapSeconds.y) / appeal;
        }

        private void BeginPerforming()
        {
            float seconds = Random.Range(Current.Duration.x, Current.Duration.y);
            running = Current.Perform(group, centre, seconds);
            CurrentPhase = Phase.Performing;
            phaseEnd = Time.time + seconds;
            Performing?.Invoke(Current);
        }

        private bool AllInPlace()
        {
            foreach (VillagerUnit unit in group) if (unit.IsMoving) return false;
            return true;
        }

        private bool Disturbed()
        {
            for (int i = 0; i < group.Count; i++)
            {
                VillagerUnit unit = group[i];
                if (unit == null || !unit.IsAlive || unit.OrderSerial != serials[i]) return true;
            }
            return false;
        }

        private void OnAttacked(Vector3 _) => End(false);
        private void OnSurrendered(Village _) => End(false);

        private void End(bool completed)
        {
            if (Current == null) return;
            CultureActivity ended = Current;
            Current = null;
            CurrentPhase = Phase.Idle;
            village.Attacked -= OnAttacked;
            village.Surrendered -= OnSurrendered;
            if (running.isAlive) running.Stop();

            if (completed) ended.Conclude(group);
            for (int i = 0; i < group.Count; i++)
            {
                VillagerUnit unit = group[i];
                if (unit != null && unit.IsAlive && unit.OrderSerial == serials[i]) unit.Release(true);
            }
            group.Clear();
            serials.Clear();
            ScheduleNext();
            Ended?.Invoke(ended);
        }

        private static Vector3 Snap(Vector3 point) =>
            NavMesh.SamplePosition(point, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : point;
    }
}
