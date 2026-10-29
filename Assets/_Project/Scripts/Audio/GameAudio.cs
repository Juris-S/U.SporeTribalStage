using SporeTribal.AI;
using SporeTribal.CameraControl;
using System.Collections.Generic;
using SporeTribal.Culture;
using SporeTribal.Economy;
using SporeTribal.Villagers;
using UnityEngine;

namespace SporeTribal.Audio
{
    public class GameAudio : MonoBehaviour
    {
        [Tooltip("The player's tribe: raids on it and performances it takes part in are what we hear.")]
        [SerializeField] private Village village;
        [Tooltip("Seconds without anyone being attacked before the battle music fades out.")]
        [SerializeField, Min(1f)] private float battleCalmSeconds = 10f;
        [Tooltip("Camera zoom (0 in, 1 out) where the wind starts to rise, and where it is at full strength.")]
        [SerializeField] private Vector2 windZoomRange = new Vector2(0.5f, 1f);

        [Header("Scatting")]
        [Tooltip("Camera zoom (0 in, 1 out) below which a close-up villager may scat.")]
        [SerializeField, Range(0f, 1f)] private float scatZoomMax = 0.25f;
        [Tooltip("A villager this close to the camera focus counts as zoomed in on.")]
        [SerializeField, Min(0f)] private float scatRadius = 8f;
        [SerializeField, Range(0f, 1f)] private float scatChance = 0.05f;
        [Tooltip("Seconds between rolls while zoomed in.")]
        [SerializeField, Min(0.5f)] private float scatRollSeconds = 3f;

        private readonly Dictionary<IdleActivityDirector, AudioSource> held = new Dictionary<IdleActivityDirector, AudioSource>();
        private IdleActivityDirector[] activityDirectors;
        private TribeRaidDirector[] raidDirectors;
        private SocialPerformanceDirector performance;
        private Village[] villages;
        private RTSCameraController cameraRig;
        private float nextScatRoll;
        private float battleUntil;
        private bool inBattle;

        private void OnEnable()
        {
            raidDirectors = FindObjectsByType<TribeRaidDirector>();
            foreach (TribeRaidDirector d in raidDirectors) d.Alerted += OnRaidAlert;
            performance = village.GetComponent<SocialPerformanceDirector>();
            if (performance != null)
            {
                performance.Started += OnPerformanceStarted;
                performance.Ended += OnPerformanceEnded;
            }
            activityDirectors = FindObjectsByType<IdleActivityDirector>();
            foreach (IdleActivityDirector d in activityDirectors)
            {
                d.Performing += OnActivityChanged;
                d.Ended += OnActivityChanged;
            }
            village.Attacked += OnAttacked;
            VillagerUnit.Died += OnVillagerDied;
            cameraRig = FindAnyObjectByType<RTSCameraController>();
            villages = FindObjectsByType<Village>();
            foreach (Village v in villages) v.Attacked += OnBattle;
        }

        private void Start()
        {
            AudioManager.StartLoop(SfxId.AmbienceBirds);
            AudioManager.StartLoop(SfxId.AmbienceInsects);
            AudioManager.StartLoop(SfxId.AmbienceWind);
            UpdateWind();
        }

        private void Update()
        {
            UpdateWind();
            RollScat();
            if (inBattle && Time.time >= battleUntil)
            {
                inBattle = false;
                AudioManager.StopLoop(SfxId.BattleLoop);
            }
        }

        private void RollScat()
        {
            if (cameraRig == null || cameraRig.Zoom01 > scatZoomMax || Time.time < nextScatRoll) return;
            nextScatRoll = Time.time + scatRollSeconds;
            if (Random.value >= scatChance) return;

            VillagerUnit nearest = null;
            float best = scatRadius * scatRadius;
            foreach (VillagerUnit unit in VillagerUnit.All)
            {
                if (!unit.IsAlive || unit.IsFighting || unit.IsAttackTarget) continue;
                float sqr = (unit.transform.position - cameraRig.Focus).sqrMagnitude;
                if (sqr >= best) continue;
                best = sqr;
                nearest = unit;
            }
            if (nearest != null) AudioManager.Play(SfxId.VillagerScat, nearest.transform.position);
        }

        private void UpdateWind()
        {
            if (cameraRig != null)
            {
                float wind = Mathf.InverseLerp(windZoomRange.x, windZoomRange.y, cameraRig.Zoom01);
                AudioManager.SetLoopGain(SfxId.AmbienceWind, wind);
                AudioManager.SetLoopGain(SfxId.AmbienceBirds, 1f - wind);
                AudioManager.SetLoopGain(SfxId.AmbienceInsects, 1f - wind);
            }
        }

        private void OnActivityChanged(CultureActivity _)
        {
            foreach (IdleActivityDirector d in activityDirectors)
            {
                bool performing = d.Current != null && d.CurrentPhase == IdleActivityDirector.Phase.Performing;
                bool holding = held.TryGetValue(d, out AudioSource source);
                if (performing && !holding)
                {
                    AudioSource started = AudioManager.PlayHeld(d.Current.Sound, d.Centre);
                    if (started != null) held[d] = started;
                }
                else if (!performing && holding)
                {
                    AudioManager.StopHeld(source);
                    held.Remove(d);
                }
            }
        }

        private void OnDisable()
        {
            foreach (IdleActivityDirector d in activityDirectors)
            {
                d.Performing -= OnActivityChanged;
                d.Ended -= OnActivityChanged;
            }
            foreach (AudioSource source in held.Values) AudioManager.StopHeld(source);
            held.Clear();
            foreach (TribeRaidDirector d in raidDirectors) if (d != null) d.Alerted -= OnRaidAlert;
            if (performance != null)
            {
                performance.Started -= OnPerformanceStarted;
                performance.Ended -= OnPerformanceEnded;
            }
            village.Attacked -= OnAttacked;
            VillagerUnit.Died -= OnVillagerDied;
            AudioManager.StopLoop(SfxId.AmbienceBirds);
            AudioManager.StopLoop(SfxId.AmbienceInsects);
            AudioManager.StopLoop(SfxId.AmbienceWind);
            foreach (Village v in villages) if (v != null) v.Attacked -= OnBattle;
            if (inBattle) AudioManager.StopLoop(SfxId.BattleLoop);
            inBattle = false;
        }

        private void OnRaidAlert(Village target, RaidAlert alert, Vector3 from)
        {
            if (target == village && alert != RaidAlert.ThiefIntercepted) AudioManager.PlayUi(SfxId.DiplomacyWarCry);
        }

        private void OnBattle(Vector3 where)
        {
            battleUntil = Time.time + battleCalmSeconds;
            if (inBattle) return;
            inBattle = true;
            AudioManager.StartLoop(SfxId.BattleLoop);
        }

        private void OnVillagerDied(VillagerUnit unit) =>
            AudioManager.Play(unit.Home == village ? SfxId.VillagerDeath : SfxId.EnemyDeath, unit.transform.position);

        private void OnAttacked(Vector3 where) => AudioManager.Play(SfxId.VoiceWarCry, where);

        private void OnPerformanceStarted(Village target)
        {
            AudioManager.PlayUi(SfxId.DiplomacyCurious);
            AudioManager.StartLoop(SfxId.PerformanceLoop);
        }

        private void OnPerformanceEnded(SocialPerformanceDirector.Outcome outcome, Village target)
        {
            AudioManager.StopLoop(SfxId.PerformanceLoop);
            if (outcome == SocialPerformanceDirector.Outcome.Allied || outcome == SocialPerformanceDirector.Outcome.Pleased)
                AudioManager.PlayUi(SfxId.DiplomacyHappy);
        }
    }
}
