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

        // A procedural FACTORY-AMBIENT bed (no audio assets): a low industrial drone + a slow chord pad that
        // swells, a soft MACHINE PULSE on the beat, and metallic TICKS / steam-hiss on the off-beat — so it reads
        // as "the factory is working" without a driving tune. Every frequency (and the swell) is snapped to the
        // loop length, and the beat divides it evenly, so the 16s buffer repeats seamlessly (no click at the seam).
        private static AudioClip Ambient()
        {
            const float loop = 16f;
            int n = Mathf.RoundToInt(loop * Rate);
            var data = new float[n];
            float Snap(float f) => Mathf.Round(f * loop) / loop; // whole cycles per loop → seamless

            // Deterministic low-level noise for the metallic tick (fixed seed → identical every run + every loop).
            var rng = new System.Random(4242);
            var noise = new float[n];
            for (int i = 0; i < n; i++) noise[i] = (float)(rng.NextDouble() * 2.0 - 1.0);

            float d1 = Snap(55f), d2 = Snap(110f);                          // sub drone (A1 + A2)
            float p1 = Snap(220f), p2 = Snap(261.63f), p3 = Snap(329.63f);  // A3 / C4 / E4 pad (a-minor-ish)
            float swellF = Snap(0.0625f);                                   // one slow swell per loop
            float m1 = Snap(1760f), m2 = Snap(2637f);                       // metallic partials
            const float beat = 1.0f;   // a calm ~60 BPM industrial heartbeat (divides 16s exactly)
            float thumpF = Snap(58f);

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float s = 0f;

                // low industrial drone
                s += 0.11f * Mathf.Sin(2f * Mathf.PI * d1 * t) + 0.07f * Mathf.Sin(2f * Mathf.PI * d2 * t);

                // slow chord pad that swells across the loop
                float swell = 0.35f + 0.35f * Mathf.Sin(2f * Mathf.PI * swellF * t);
                s += swell * (0.055f * Mathf.Sin(2f * Mathf.PI * p1 * t)
                            + 0.045f * Mathf.Sin(2f * Mathf.PI * p2 * t)
                            + 0.040f * Mathf.Sin(2f * Mathf.PI * p3 * t));

                // machine pulse — a soft low thump on every beat
                float tb = t - beat * Mathf.Floor(t / beat);
                s += 0.14f * Mathf.Exp(-tb / 0.10f) * Mathf.Sin(2f * Mathf.PI * thumpF * t);

                // metallic tick / steam hiss on the off-beat
                float to = tb - beat * 0.5f; if (to < 0f) to += beat;
                float tickEnv = Mathf.Exp(-to / 0.04f);
                s += 0.045f * tickEnv * (0.6f * Mathf.Sin(2f * Mathf.PI * m1 * t)
                                       + 0.4f * Mathf.Sin(2f * Mathf.PI * m2 * t)
                                       + 0.5f * noise[i]);

                data[i] = Mathf.Clamp(s * 0.9f, -1f, 1f);
            }
            var clip = AudioClip.Create("factory_ambient", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
