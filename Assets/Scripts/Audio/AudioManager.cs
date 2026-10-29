using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BustAKlud
{
    public enum Sfx
    {
        Fire,
        Bounce,
        Stick,
        Pop,
        Bang,
        Fall,
        CeilingDrop,
        Swap,
        Click,
        Hover,
        LevelClear,
        GameOver
    }

    /// <summary>
    /// Plays the supplied recordings (cannon, bounce, thud, bang) alongside synthesised effects and music,
    /// through a small pool of voices so overlapping pops never cut each other off.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const int VoiceCount = 16;
        const float MusicVolume = 0.42f;

        // A minor pentatonic, so a chain of pops climbs like a little tune.
        static readonly int[] PopScale = { 0, 3, 5, 7, 10, 12, 15, 17, 19, 22, 24 };

        public static AudioManager Instance { get; private set; }

        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        readonly Dictionary<Sfx, float> lastPlayed = new Dictionary<Sfx, float>();
        AudioSource[] voices;
        AudioSource music;
        int nextVoice;

        void Awake()
        {
            Instance = this;

            voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0f;
            }

            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.volume = MusicVolume;
            music.spatialBlend = 0f;

            LoadClips();
            ApplySettings();
            GameSettings.Changed += ApplySettings;
            StartCoroutine(ProceduralAudio.BuildMusic(OnMusicReady));
        }

        void OnDestroy()
        {
            GameSettings.Changed -= ApplySettings;
        }

        void LoadClips()
        {
            // The recordings play as-is straight away; they're swapped for trimmed, normalised copies as soon as
            // their audio data has loaded (on WebGL that isn't until a moment after startup).
            StartCoroutine(UpgradeRecording(Sfx.Fire, "cannon5", 1.1f, 0.85f));
            StartCoroutine(UpgradeRecording(Sfx.Bounce, "bounce", 0.4f, 0.9f));
            StartCoroutine(UpgradeRecording(Sfx.Stick, "thud", 0.4f, 0.8f));
            StartCoroutine(UpgradeRecording(Sfx.Bang, "bang", 0.75f, 0.9f));

            clips[Sfx.Pop] = ProceduralAudio.Pop();
            clips[Sfx.Fall] = ProceduralAudio.Fall();
            clips[Sfx.CeilingDrop] = ProceduralAudio.Rumble();
            clips[Sfx.Swap] = ProceduralAudio.Whoosh();
            clips[Sfx.Click] = ProceduralAudio.Click();
            clips[Sfx.Hover] = ProceduralAudio.Hover();
            clips[Sfx.LevelClear] = ProceduralAudio.LevelClear();
            clips[Sfx.GameOver] = ProceduralAudio.GameOver();
        }

        IEnumerator UpgradeRecording(Sfx sfx, string clipName, float maxSeconds, float targetPeak)
        {
            AudioClip source = GameAssets.Clip(clipName);
            clips[sfx] = source;
            if (source == null)
                yield break;

            source.LoadAudioData();
            float giveUp = Time.realtimeSinceStartup + 10f;
            while (source.loadState != AudioDataLoadState.Loaded && Time.realtimeSinceStartup < giveUp)
                yield return null;

            bool loaded = source.loadState == AudioDataLoadState.Loaded;
            if (loaded)
                clips[sfx] = Prepare(source, maxSeconds, targetPeak);
            Debug.Log($"[audio] {clipName}: {(loaded && clips[sfx] != source ? "prepared" : "using the raw recording")}");
        }

        /// <summary>Copies a clip, trimming it, fading the tail and scaling it to a target peak.</summary>
        static AudioClip Prepare(AudioClip source, float maxSeconds, float targetPeak)
        {
            if (source == null)
                return null;

            int channels = source.channels;
            int frames = Mathf.Min(source.samples, (int)(maxSeconds * source.frequency));
            var data = new float[frames * channels];
            if (!source.GetData(data, 0))
                return source;

            float peak = 0.0001f;
            for (int i = 0; i < data.Length; i++)
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));

            float gain = targetPeak / peak;
            int fadeFrames = Mathf.Min(frames, (int)(0.12f * source.frequency));
            for (int f = 0; f < frames; f++)
            {
                float fade = f >= frames - fadeFrames ? (frames - f) / (float)fadeFrames : 1f;
                for (int c = 0; c < channels; c++)
                    data[f * channels + c] *= gain * fade;
            }

            var clip = AudioClip.Create(source.name + "_prepared", frames, channels, source.frequency, false);
            clip.SetData(data, 0);
            return clip;
        }

        void OnMusicReady(AudioClip clip)
        {
            music.clip = clip;
            music.Play();
        }

        void ApplySettings()
        {
            music.mute = !GameSettings.MusicOn;
        }

        public void Play(Sfx sfx, float volume = 1f, float pitch = 1f)
        {
            if (!GameSettings.SfxOn || !clips.TryGetValue(sfx, out AudioClip clip) || clip == null)
                return;

            // Guard against the same sound stacking into mush within a single frame burst.
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(sfx, out float last) && now - last < 0.02f)
                return;
            lastPlayed[sfx] = now;

            AudioSource voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.clip = clip;
            voice.volume = Mathf.Clamp01(volume);
            voice.pitch = pitch;
            voice.Play();
        }

        /// <summary>The nth pop in a chain, climbing the scale.</summary>
        public void PlayPop(int index, float volume = 0.8f)
        {
            int step = PopScale[Mathf.Clamp(index, 0, PopScale.Length - 1)];
            Play(Sfx.Pop, volume, Mathf.Pow(2f, step / 12f));
        }
    }
}
