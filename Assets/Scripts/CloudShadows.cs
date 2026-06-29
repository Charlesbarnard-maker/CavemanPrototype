using UnityEngine;

namespace Caveman
{
    /// <summary>A handful of big, soft shadow blobs drifting on the wind over the ground — the cheapest, biggest
    /// atmosphere win. They live in a window that follows the camera/player and wrap around it, so the cost is a
    /// fixed dozen transforms no matter how large the world is. Sorted under gameplay so only the ground darkens.</summary>
    public class CloudShadows : MonoBehaviour
    {
        public Transform target;
        private const int Count = 14;
        private const float Field = 52f;                       // half-window the clouds wrap within
        private static readonly Vector2 Drift = new Vector2(0.55f, 0.22f);
        private Transform[] _c;
        private Vector2[] _pos;
        private float[] _scl;

        static float Frac(float v) => v - Mathf.Floor(v);

        void Start()
        {
            var sprite = PlaceholderArt.CloudShadow();
            _c = new Transform[Count]; _pos = new Vector2[Count]; _scl = new float[Count];
            for (int i = 0; i < Count; i++)
            {
                var go = new GameObject("Cloud");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = new Color(0.10f, 0.12f, 0.17f, 0.16f); // cool + faint
                sr.sortingOrder = -12;
                _c[i] = go.transform;
                _pos[i] = new Vector2((Frac(i * 0.37f) * 2f - 1f) * Field, (Frac(i * 0.713f) * 2f - 1f) * Field);
                _scl[i] = 14f + 18f * Frac(i * 0.531f);
                go.transform.localScale = new Vector3(_scl[i], _scl[i] * 0.78f, 1f);
            }
        }

        void Update()
        {
            if (_c == null) return;
            Vector3 cam = target != null ? target.position : Vector3.zero;
            Vector2 d = Drift * Time.deltaTime;
            for (int i = 0; i < Count; i++)
            {
                _pos[i] += d;
                float w = Field + _scl[i];
                if (_pos[i].x - cam.x > w) _pos[i].x -= 2f * w;
                else if (_pos[i].x - cam.x < -w) _pos[i].x += 2f * w;
                if (_pos[i].y - cam.y > w) _pos[i].y -= 2f * w;
                else if (_pos[i].y - cam.y < -w) _pos[i].y += 2f * w;
                _c[i].position = new Vector3(_pos[i].x, _pos[i].y, 0f);
            }
        }
    }
}
