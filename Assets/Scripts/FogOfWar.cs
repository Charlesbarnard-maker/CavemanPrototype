using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A discoverable fog of war: the whole map starts dark and is permanently
    /// revealed in a radius around the player as they move. Encourages exploration;
    /// the more you walk, the more of the map you uncover (and can see on the M
    /// world map + the minimap). Implemented as one big world-space sprite whose texture is painted
    /// transparent where explored.
    /// </summary>
    public class FogOfWar : MonoBehaviour
    {
        public Transform target;
        public float worldSize = 420f;     // covers roughly -210..210 (a big world to explore)
        public int res = 420;              // fog texture resolution (~1 px per cell)
        public float revealRadius = 9f;    // world units revealed around the player
        public float moveThreshold = 1.2f; // re-reveal after the player moves this far

        public static FogOfWar Instance { get; private set; }
        public Texture2D Tex => _tex;
        public float WorldSize => worldSize;

        private Texture2D _tex;
        private Color32[] _px;
        private Vector3 _lastReveal = new Vector3(99999f, 99999f, 0f);
        private static readonly Color32 Fog = new Color32(10, 10, 14, 255);
        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        void Awake()
        {
            Instance = this;
            _tex = new Texture2D(res, res, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            _px = new Color32[res * res];
            for (int i = 0; i < _px.Length; i++) _px[i] = Fog;
            _tex.SetPixels32(_px);
            _tex.Apply();

            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(_tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res / worldSize);
            sr.sortingOrder = 50; // above everything in the world
            transform.position = Vector3.zero;
        }

        /// <summary>Reveal the entire map at once (sandbox/testing).</summary>
        public void RevealAll()
        {
            if (_px == null) return;
            for (int i = 0; i < _px.Length; i++) _px[i] = Clear;
            _tex.SetPixels32(_px);
            _tex.Apply();
        }

        /// <summary>Has this world position been revealed? (cheap array lookup)</summary>
        public bool IsExplored(Vector3 worldPos)
        {
            if (_px == null) return false;
            float ppu = res / worldSize;
            int x = Mathf.RoundToInt(worldPos.x * ppu + res * 0.5f);
            int y = Mathf.RoundToInt(worldPos.y * ppu + res * 0.5f);
            if (x < 0 || x >= res || y < 0 || y >= res) return false;
            return _px[y * res + x].a == 0; // alpha 0 == revealed
        }

        void Update()
        {
            if (target == null) return;
            if ((target.position - _lastReveal).sqrMagnitude < moveThreshold * moveThreshold) return;
            _lastReveal = target.position;
            Reveal(target.position, revealRadius);
        }

        private void Reveal(Vector3 worldPos, float radius)
        {
            float ppu = res / worldSize;
            float cx = worldPos.x * ppu + res * 0.5f;
            float cy = worldPos.y * ppu + res * 0.5f;
            int rpx = Mathf.CeilToInt(radius * ppu);
            int minX = Mathf.Clamp((int)(cx - rpx), 0, res - 1);
            int maxX = Mathf.Clamp((int)(cx + rpx), 0, res - 1);
            int minY = Mathf.Clamp((int)(cy - rpx), 0, res - 1);
            int maxY = Mathf.Clamp((int)(cy + rpx), 0, res - 1);
            int r2 = rpx * rpx;

            bool changed = false;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy > r2) continue;
                    int idx = y * res + x;
                    if (_px[idx].a != 0) { _px[idx] = Clear; changed = true; }
                }
            }
            if (changed) { _tex.SetPixels32(_px); _tex.Apply(); }
        }
    }
}
