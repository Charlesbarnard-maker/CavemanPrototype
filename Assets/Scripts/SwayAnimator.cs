using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>Gently sways registered SOFT props (grass / bushes / ferns / reeds) with a per-item phase so a
    /// field RIPPLES on the breeze rather than marching in unison. One central Update drives them all — a few
    /// thousand Sin calls, comfortably cheap. Hard-capped so a huge world can't blow the budget.</summary>
    public class SwayAnimator : MonoBehaviour
    {
        public static SwayAnimator Instance { get; private set; }
        private const int Cap = 3500;
        private readonly List<Transform> _t = new();
        private readonly List<float> _phase = new();
        private readonly List<float> _amp = new();

        void Awake() { Instance = this; }

        public void Register(Transform t, float amp = 5f)
        {
            if (t == null || _t.Count >= Cap) return;
            _t.Add(t);
            _phase.Add(t.position.x * 0.7f + t.position.y * 1.3f); // de-sync neighbours
            _amp.Add(amp);
        }

        void Update()
        {
            float time = Time.time * 1.5f;
            for (int i = 0; i < _t.Count; i++)
            {
                var t = _t[i];
                if (t != null) t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time + _phase[i]) * _amp[i]);
            }
        }
    }
}
