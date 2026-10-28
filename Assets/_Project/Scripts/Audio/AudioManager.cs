using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.Audio;

namespace SporeTribal.Audio
{
    public class AudioManager : MonoBehaviour
    {
        [Header("Sfx")]
        [SerializeField] private SfxLibrary library;
        [Tooltip("Mixer group for in-world one-shots (MainMixer > SFX > World).")]
        [SerializeField] private AudioMixerGroup worldOutput;
        [Tooltip("Mixer group for non-positional one-shots (MainMixer > SFX > UI).")]
        [SerializeField] private AudioMixerGroup uiOutput;
        [SerializeField, Min(1)] private int voices = 12;
        [SerializeField] private float maxDistance = 40f;

        [Header("Music")]
        [Tooltip("Played in shuffled order, one after another.")]
        [SerializeField] private AudioClip[] playlist;
        [SerializeField] private AudioMixerGroup musicOutput;
        [Tooltip("Keep this low so the music sits under the sfx.")]
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;
        [SerializeField, Min(0f)] private float musicFadeSeconds = 3f;
        [Tooltip("Silence between tracks.")]
        [SerializeField, Min(0f)] private float musicGapSeconds = 4f;

        [SerializeField, Min(0f)] private float loopFadeSeconds = 1.5f;

        private class LoopVoice
        {
            public AudioSource Source;
            public Tween Fade;
            public float Target;
            public float Volume;
            public float Level;
            public float Gain = 1f;

            public void Apply() => Source.volume = Volume * Level * Gain;
        }

        private static AudioManager instance;

        private AudioSource[] pool;
        private int nextVoice;
        private AudioSource music;
        private readonly Dictionary<SfxId, LoopVoice> loops = new Dictionary<SfxId, LoopVoice>();
        private int lastTrack = -1;
        private float nextTrackAt;
        private Sequence fade;

        private void Awake()
        {
            instance = this;
            pool = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                pool[i] = gameObject.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
                pool[i].rolloffMode = AudioRolloffMode.Linear;
                pool[i].maxDistance = maxDistance;
            }
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.outputAudioMixerGroup = musicOutput;
        }

        private void OnDestroy()
        {
            fade.Stop();
            foreach (LoopVoice v in loops.Values) v.Fade.Stop();
            if (instance == this) instance = null;
        }

        private void Update()
        {
            if (playlist == null || playlist.Length == 0 || music.isPlaying || Time.unscaledTime < nextTrackAt) return;
            PlayNextTrack();
        }

        public static void Play(SfxId id, Vector3 position)
        {
            if (instance != null) instance.PlayInternal(id, position, true);
        }

        public static void PlayUi(SfxId id)
        {
            if (instance != null) instance.PlayInternal(id, Vector3.zero, false);
        }

        public static void StartLoop(SfxId id)
        {
            if (instance != null) instance.StartLoopInternal(id);
        }

        public static void StopLoop(SfxId id)
        {
            if (instance != null) instance.StopLoopInternal(id);
        }

        public static void SetLoopGain(SfxId id, float gain)
        {
            if (instance == null || !instance.loops.TryGetValue(id, out LoopVoice voice)) return;
            voice.Gain = gain;
            voice.Apply();
        }

        public static AudioSource PlayHeld(SfxId id, Vector3 position) =>
            instance != null ? instance.PlayHeldInternal(id, position) : null;

        public static void StopHeld(AudioSource source)
        {
            if (instance != null && source != null) instance.StopHeldInternal(source);
        }

        private AudioSource PlayHeldInternal(SfxId id, Vector3 position)
        {
            SfxLibrary.Entry entry = library != null ? library.Get(id) : null;
            if (entry == null || entry.clips.Length == 0) return null;
            var held = new GameObject("Held " + id);
            held.transform.SetParent(transform);
            held.transform.position = position;
            AudioSource source = held.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = worldOutput;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.maxDistance = maxDistance;
            source.loop = true;
            source.volume = entry.volume;
            source.clip = entry.clips[Random.Range(0, entry.clips.Length)];
            source.Play();
            return source;
        }

        private void StopHeldInternal(AudioSource source) =>
            Tween.Custom(source, source.volume, 0f, loopFadeSeconds, static (s, v) => s.volume = v, useUnscaledTime: true)
                .OnComplete(source, static s => Destroy(s.gameObject));

        private void StartLoopInternal(SfxId id)
        {
            SfxLibrary.Entry entry = library != null ? library.Get(id) : null;
            if (entry == null || entry.clips.Length == 0) return;
            if (!loops.TryGetValue(id, out LoopVoice voice))
            {
                voice = new LoopVoice { Source = gameObject.AddComponent<AudioSource>() };
                voice.Source.playOnAwake = false;
                voice.Source.loop = true;
                voice.Source.outputAudioMixerGroup = musicOutput;
                loops[id] = voice;
            }
            if (voice.Source.isPlaying && voice.Target > 0f) return;
            voice.Fade.Stop();
            if (!voice.Source.isPlaying)
            {
                voice.Source.clip = entry.clips[Random.Range(0, entry.clips.Length)];
                voice.Level = 0f;
                voice.Volume = entry.volume;
                voice.Apply();
                voice.Source.Play();
            }
            voice.Volume = entry.volume;
            voice.Target = 1f;
            FadeLevel(voice, 1f);
        }

        private void StopLoopInternal(SfxId id)
        {
            if (!loops.TryGetValue(id, out LoopVoice voice) || !voice.Source.isPlaying) return;
            voice.Fade.Stop();
            voice.Target = 0f;
            FadeLevel(voice, 0f).OnComplete(voice, static v => v.Source.Stop());
        }

        private Tween FadeLevel(LoopVoice voice, float to)
        {
            voice.Fade = Tween.Custom(voice, voice.Level, to, loopFadeSeconds, static (v, level) => { v.Level = level; v.Apply(); }, useUnscaledTime: true);
            return voice.Fade;
        }

        private void PlayInternal(SfxId id, Vector3 position, bool positional)
        {
            SfxLibrary.Entry entry = library != null ? library.Get(id) : null;
            if (entry == null || entry.clips.Length == 0 || Time.unscaledTime < entry.nextAllowed) return;
            entry.nextAllowed = Time.unscaledTime + entry.cooldown;

            int index = Random.Range(0, entry.clips.Length);
            if (entry.clips.Length > 1 && index == entry.lastIndex) index = (index + 1) % entry.clips.Length;
            entry.lastIndex = index;

            AudioSource voice = pool[nextVoice];
            nextVoice = (nextVoice + 1) % pool.Length;
            voice.Stop();
            voice.transform.position = position;
            voice.outputAudioMixerGroup = positional ? worldOutput : uiOutput;
            voice.spatialBlend = positional ? 1f : 0f;
            voice.pitch = 1f + Random.Range(-entry.pitchVariance, entry.pitchVariance);
            voice.volume = entry.volume;
            voice.clip = entry.clips[index];
            voice.Play();
        }

        private void PlayNextTrack()
        {
            int index = Random.Range(0, playlist.Length);
            if (playlist.Length > 1 && index == lastTrack) index = (index + 1) % playlist.Length;
            lastTrack = index;

            AudioClip clip = playlist[index];
            music.clip = clip;
            music.volume = 0f;
            music.Play();
            nextTrackAt = Time.unscaledTime + clip.length + musicGapSeconds;

            float fadeSeconds = Mathf.Min(musicFadeSeconds, clip.length * 0.5f);
            fade.Stop();
            fade = Sequence.Create(useUnscaledTime: true)
                .Chain(Tween.Custom(music, 0f, musicVolume, fadeSeconds, static (s, v) => s.volume = v))
                .ChainDelay(clip.length - fadeSeconds * 2f)
                .Chain(Tween.Custom(music, musicVolume, 0f, fadeSeconds, static (s, v) => s.volume = v));
        }
    }
}
