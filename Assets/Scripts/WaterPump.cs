using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A mechanical water pump: must sit next to WATER TERRAIN (river/lake/coast) and connect
    /// to a pipe network. Each tick it floods its connected pipes and fills any water storage
    /// they reach — continuous flow from the map's water features into your base, without
    /// workers carrying it. This is the Bronze-age evolution of water logistics (Stone age
    /// still hand-carries via the Water Hole). Out of water/disconnected → no flow (starvation).
    /// </summary>
    public class WaterPump : MonoBehaviour
    {
        public BuildingDefinition def;
        public ItemDefinition water;
        public float interval = 0.5f;
        public int flowPerTick = 4;

        public static readonly List<WaterPump> All = new();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        private float _t;
        private bool _flowing;
        private SpriteRenderer _sr;
        private Color _base;

        private static readonly Queue<Vector2Int> _q = new();
        private static readonly HashSet<Vector2Int> _seen = new();
        private static readonly Belt.Dir[] _dirs = { Belt.Dir.N, Belt.Dir.E, Belt.Dir.S, Belt.Dir.W };

        public static WaterPump Spawn(BuildingDefinition def, Vector3 pos)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(def.FootW, def.FootH, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = def.color;
            sr.sortingOrder = 5;

            go.AddComponent<BoxCollider2D>();

            var p = go.AddComponent<WaterPump>();
            p.def = def;
            p.water = def.item;
            p._sr = sr;
            p._base = def.color;
            return p;
        }

        void Update()
        {
            _t += Time.deltaTime;
            if (_t < interval) return;
            _t = 0f;
            _flowing = Pump();
            if (_sr != null) _sr.color = _flowing ? _base : Color.Lerp(_base, Color.black, 0.5f);
        }

        // Source water only if adjacent to water terrain; then BFS the connected pipe network
        // and top up every water-accepting storage the pipes touch (split a per-tick budget).
        private bool Pump()
        {
            if (water == null) return false;
            var me = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));

            bool atWater = false;
            foreach (var d in _dirs) if (TerrainGrid.IsWater(me + Belt.Step(d))) { atWater = true; break; }
            if (!atWater) return false;

            _q.Clear(); _seen.Clear();
            foreach (var d in _dirs)
            {
                var s = me + Belt.Step(d);
                if (PipeNet.At(s) != null && _seen.Add(s)) _q.Enqueue(s);
            }

            int budget = flowPerTick;
            bool delivered = false;
            int guard = 0;
            while (_q.Count > 0 && guard++ < 1024)
            {
                var c = _q.Dequeue();
                foreach (var d in _dirs)
                {
                    var nb = c + Belt.Step(d);
                    if (WorldGrid.Storages.TryGetValue(nb, out var st) && st != null && st.accepts == water
                        && st.def != null && budget > 0)
                    {
                        int room = st.def.capacity - st.Store.Total();
                        if (room > 0)
                        {
                            int add = Mathf.Min(budget, room);
                            st.Store.Add(water, add);
                            budget -= add;
                            delivered = true;
                        }
                    }
                    if (PipeNet.At(nb) != null && _seen.Add(nb)) _q.Enqueue(nb);
                }
                if (budget <= 0) break;
            }
            return delivered;
        }
    }
}
