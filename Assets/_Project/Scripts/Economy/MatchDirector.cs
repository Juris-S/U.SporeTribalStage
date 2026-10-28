using PrimeTween;
using SporeTribal.Inputs;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace SporeTribal.Economy
{
    public enum MatchOutcome { Conquest, Alliance, Defeat }

    [RequireComponent(typeof(AudioSource))]
    public class MatchDirector : MonoBehaviour
    {
        [Tooltip("The player's tribe.")]
        [SerializeField] private Village player;
        [Tooltip("Real seconds the world takes to ease from full speed to a stop.")]
        [SerializeField, Min(0.1f)] private float slowDownSeconds = 2.5f;
        [SerializeField] private int fontSize = 28;
        [Tooltip("One is picked at random when the player wins (and from Defeat Clips when they lose).")]
        [SerializeField] private AudioClip[] victoryClips;
        [SerializeField] private AudioClip[] defeatClips;
        [Tooltip("Mixer group the victory and defeat stings play through (MainMixer > SFX > Stingers). Level is set in the mixer, in dB.")]
        [SerializeField] private AudioMixerGroup stingerOutput;

        [Tooltip("Seconds the sting takes to rise from silence.")]
        [SerializeField, Min(0f)] private float stingerFadeIn = 1.5f;
        [Tooltip("Seconds the sting takes to fade to silence at the end of the clip.")]
        [SerializeField, Min(0f)] private float stingerFadeOut = 2f;

        public Village Player => player;
        public bool IsOver { get; private set; }
        public MatchOutcome Outcome { get; private set; }
        public bool Halted { get; private set; }

        public event System.Action<MatchOutcome> Ended;

        private Village[] villages;
        private AudioSource sting;
        private GUIStyle titleStyle;
        private GUIStyle buttonStyle;

        private void Awake()
        {
            sting = GetComponent<AudioSource>();
            sting.playOnAwake = false;
            sting.loop = false;
            sting.spatialBlend = 0f;
            sting.outputAudioMixerGroup = stingerOutput;
        }

        private void OnEnable()
        {
            Time.timeScale = 1f;
            GameInput.Unlock();
            villages = FindObjectsByType<Village>();
            foreach (Village v in villages)
            {
                v.Surrendered += OnChanged;
                v.AllianceDeclared += OnAlliance;
            }
        }

        private void OnDisable()
        {
            foreach (Village v in villages)
            {
                if (v == null) continue;
                v.Surrendered -= OnChanged;
                v.AllianceDeclared -= OnAlliance;
            }
        }

        private void OnAlliance(Village a, Village b) => Evaluate();
        private void OnChanged(Village _) => Evaluate();

        private void Evaluate()
        {
            if (IsOver) return;
            if (player.HasSurrendered) { End(MatchOutcome.Defeat); return; }

            bool anyRival = false, anyAlly = false;
            foreach (Village v in villages)
            {
                if (v == player) continue;
                anyRival = true;
                if (v.HasSurrendered) continue;
                if (!player.IsAlliedWith(v)) return;
                anyAlly = true;
            }
            if (!anyRival) return;
            MatchOutcome outcome = anyAlly ? MatchOutcome.Alliance : MatchOutcome.Conquest;
            End(outcome);
        }

        private void End(MatchOutcome outcome)
        {
            IsOver = true;
            Outcome = outcome;
            GameInput.Lock();
            Ended?.Invoke(outcome);
            PlaySting(outcome == MatchOutcome.Defeat ? defeatClips : victoryClips);
            SlowToHalt();
        }

        private void SlowToHalt()
        {
            Tween.Custom(this, 1f, 0f, slowDownSeconds, static (_, scale) => Time.timeScale = scale, Ease.OutQuad, useUnscaledTime: true)
                .OnComplete(this, static director => director.Halted = true);
        }

        private void PlaySting(AudioClip[] clips)
        {
            if (clips.Length == 0) return;
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            sting.clip = clip;
            sting.volume = 0f;
            sting.Play();
            Tween.Custom(this, 0f, clip.length, clip.length, static (director, t) => director.sting.volume = director.StingerGain(t), Ease.Linear, useUnscaledTime: true);
        }

        private float StingerGain(float t)
        {
            float length = sting.clip.length;
            float fadeIn = stingerFadeIn > 0f ? t / stingerFadeIn : 1f;
            float fadeOut = stingerFadeOut > 0f ? (length - t) / stingerFadeOut : 1f;
            return Mathf.Clamp01(Mathf.Min(fadeIn, fadeOut));
        }

        private void OnGUI()
        {
            if (!Halted) return;
            titleStyle ??= new GUIStyle(GUI.skin.label) { fontSize = fontSize * 2, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            buttonStyle ??= new GUIStyle(GUI.skin.button) { fontSize = fontSize };

            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none);
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            GUI.Label(new Rect(cx - 400f, cy - 140f, 800f, 80f), Title(), titleStyle);
            GUI.Label(new Rect(cx - 400f, cy - 60f, 800f, 50f), Subtitle(), new GUIStyle(titleStyle) { fontSize = fontSize, fontStyle = FontStyle.Normal });
            if (GUI.Button(new Rect(cx - 120f, cy + 20f, 240f, 60f), "Play again", buttonStyle))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private string Title() => Outcome == MatchOutcome.Defeat ? "Defeat" : "Victory";

        private string Subtitle() => Outcome switch
        {
            MatchOutcome.Conquest => "The rival tribe has fallen.",
            MatchOutcome.Alliance => "The tribes stand together.",
            _ => "Your main hut has fallen.",
        };
    }
}
