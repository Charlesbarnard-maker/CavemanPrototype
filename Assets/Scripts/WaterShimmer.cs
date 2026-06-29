using UnityEngine;

namespace Caveman
{
    /// <summary>A few glints that twinkle on water near the camera, so lakes and the sea feel alive. Camera-
    /// windowed (a couple dozen movers), each fades in/out and hops to a fresh nearby WATER cell when it dims.</summary>
    public class WaterShimmer : MonoBehaviour
    {
        public Transform target;
        private const int Count = 22;
        private const float Field = 28f;
        private Transform[] _g;
        private SpriteRenderer[] _sr;
        private float[] _ph;

        static float Frac(float v) => v - Mathf.Floor(v);

        void Start()
        {
            var sprite = PlaceholderArt.WaterGlint();
            _g = new Transform[Count]; _sr = new SpriteRenderer[Count]; _ph = new float[Count];
            for (int i = 0; i < Count; i++)
            {
                var go = new GameObject("Glint");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = new Color(0.85f, 0.96f, 1f, 0f);
                sr.sortingOrder = -10;
                go.transform.localScale = new Vector3(0.55f, 0.32f, 1f);
                _g[i] = go.transform; _sr[i] = sr; _ph[i] = i * 0.73f;
                Hop(go.transform, target != null ? target.position : Vector3.zero, i);
            }
        }

        private void Hop(Transform t, Vector3 cam, int seed)
        {
            for (int k = 0; k < 6; k++)
            {
                float a = Frac((seed + k) * 2.399f) * 6.2831853f;
                float r = 4f + Frac((seed + k) * 1.618f) * Field;
                int x = Mathf.RoundToInt(cam.x + Mathf.Cos(a) * r), y = Mathf.RoundToInt(cam.y + Mathf.Sin(a) * r);
                if (TerrainGrid.At(x, y) == Terrain.Water) { t.position = new Vector3(x, y, 0f); return; }
            }
            t.position = new Vector3(99999f, 99999f, 0f); // no water near → park it off-world (invisible)
        }

        void Update()
        {
            if (_g == null) return;
            Vector3 cam = target != null ? target.position : Vector3.zero;
            float time = Time.time;
            for (int i = 0; i < Count; i++)
            {
                float tw = Mathf.Sin(time * 2.1f + _ph[i]);
                _sr[i].color = new Color(0.85f, 0.96f, 1f, Mathf.Max(0f, tw) * 0.55f);
                if (tw < -0.95f) Hop(_g[i], cam, i * 7 + Mathf.RoundToInt(time));
            }
        }
    }
}
