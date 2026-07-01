using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// All game audio, generated procedurally at runtime (no asset files to ship or break): a handful of soft
    /// UI/feedback SFX plus a gentle looping ambient pad. Volumes are driven live by <see cref="GameSettings"/>.
    /// Created once by GameBootstrap.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        private AudioSource _sfx, _music;
        private AudioClip _click, _place, _deny, _chime;
        private const int Rate = 44100;

        public static AudioManager Ensure()
        {
            if (Instance != null) return Instance;
            return new GameObject("AudioManager").AddComponent<AudioManager>();
        }

        void Awake()
        {
            Instance = this;
            GameSettings.Load();

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false; _sfx.spatialBlend = 0f;
            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false; _music.loop = true; _music.spatialBlend = 0f;

            _click = Tone("ui_click", new[] { 680f, 1020f }, 0.055f, 0.004f, 0.05f, 0.55f, false);
            _place = Tone("place", new[] { 150f, 90f }, 0.14f, 0.002f, 0.11f, 0.60f, false);
            _deny = Tone("deny", new[] { 240f, 175f }, 0.17f, 0.004f, 0.15f, 0.50f, false);
            _chime = Tone("chime", new[] { 523.25f, 783.99f, 1046.5f }, 0.65f, 0.003f, 0.6f, 0.42f, true);

            _music.clip = Ambient();
            _music.volume = GameSettings.MusicVolume;
            _music.Play();
        }

        void Update()
        {
            if (_sfx != null) _sfx.volume = GameSettings.SfxVolume;
            if (_music != null) _music.volume = GameSettings.MusicVolume;
        }

        public static void Click() => PlayOne(Instance != null ? Instance._click : null, 0.7f);
        public static void Place() => PlayOne(Instance != null ? Instance._place : null, 0.9f);
        public static void Deny() => PlayOne(Instance != null ? Instance._deny : null, 0.8f);
        public static void Chime() => PlayOne(Instance != null ? Instance._chime : null, 0.8f);

        private static void PlayOne(AudioClip c, float vol)
        {
            if (Instance == null || Instance._sfx == null || c == null) return;
            Instance._sfx.PlayOneShot(c, vol);
        }

        // Sum of sine partials with an attack then a linear (or exponential/bell) decay envelope.
        private static AudioClip Tone(string name, float[] freqs, float dur, float attack, float decay, float gain, bool bell)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(dur * Rate));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env;
                if (t < attack) env = t / Mathf.Max(0.0001f, attack);
                else
                {
                    float td = t - attack;
                    env = bell ? Mathf.Exp(-td / Mathf.Max(0.001f, decay))
                               : Mathf.Clamp01(1f - td / Mathf.Max(0.001f, decay));
                }
                float s = 0f;
                for (int k = 0; k < freqs.Length; k++) s += Mathf.Sin(2f * Mathf.PI * freqs[k] * t) / (k + 1f);
                data[i] = Mathf.Clamp(s * env * gain, -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // A soft, slow ambient pad. Frequencies (and the swell LFO) complete a WHOLE number of cycles over the
        // 8-second buffer, so the loop is seamless with no click at the seam. Kept quiet — a bed, not a tune.
        private static AudioClip Ambient()
        {
            const float dur = 8f;
            int n = Mathf.RoundToInt(dur * Rate);
            var data = new float[n];
            float[] partials = { 110f, 165f, 220f }; // A2 + fifth + octave — all integer cycles over 8s
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float lfo = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 0.125f * t); // one swell per loop → seamless
                float s = 0f;
                for (int k = 0; k < partials.Length; k++) s += Mathf.Sin(2f * Mathf.PI * partials[k] * t) / (k + 2f);
                data[i] = Mathf.Clamp(s * 0.16f * lfo, -1f, 1f);
            }
            var clip = AudioClip.Create("ambient", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
