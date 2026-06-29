using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>Animates soft props (grass / bushes / ferns / reeds) as WIND through a field: every blade leans
    /// slightly INTO a shared wind direction, and GUSTS ripple across the world as a travelling wave (the spatial
    /// phase comes from each prop's position projected on the wind) — so it reads as a breeze sweeping the meadow,
    /// not random wobble. One central Update; per-item cost is a couple of Sin calls. Hard-capped for huge worlds.</summary>
    public class SwayAnimator : MonoBehaviour
    {
        public static SwayAnimator Instance { get; private set; }
        private const int Cap = 3500;
        private static readonly Vector2 Wind = new Vector2(0.88f, 0.47f); // the breeze blows this way
        private readonly List<Transform> _t = new();
        private readonly List<float> _wave = new();   // position projected on the wind → travelling-gust phase
        private readonly List<float> _amp = new();

        void Awake() { Instance = this; }

        public void Register(Transform t, float amp = 5f)
        {
            if (t == null || _t.Count >= Cap) return;
            _t.Add(t);
            _wave.Add(t.position.x * Wind.x + t.position.y * Wind.y);
            _amp.Add(amp);
        }

        void Update()
        {
            float ripple = Time.time * 1.5f;  // fast leaf ripple
            float gustT = Time.time * 0.55f;   // slow gust front
            for (int i = 0; i < _t.Count; i++)
            {
                var t = _t[i];
                if (t == null) continue;
                float w = _wave[i] * 0.16f;
                float gust = 0.55f + 0.45f * Mathf.Sin(gustT - w * 0.35f);          // 0.1..1.0 envelope sweeping across
                float ang = 2f + _amp[i] * gust * Mathf.Sin(ripple - w);            // small lean into the wind + gusting ripple
                t.localRotation = Quaternion.Euler(0f, 0f, ang);
            }
        }
    }
}
