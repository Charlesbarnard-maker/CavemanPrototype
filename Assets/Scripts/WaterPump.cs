using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A mechanical water pump: must sit next to WATER TERRAIN (river/lake/coast) and connect
    /// to a pipe network. Each tick it floods its connected pipes and fills any water storage
    /// (barrels, as buffers) AND any water-using workshop (Campfire/Farm/Bakery, fed directly
    /// into its input buffer) the pipes reach — continuous flow from the map's water features
    /// into your base, without workers carrying it. The Bronze-age evolution of water logistics
    /// (Stone age still hand-carries via the Water Hole). Out of water/disconnected → no flow.
    /// </summary>
    public class WaterPump : MonoBehaviour
    {
        public BuildingDefinition def;
        public ItemDefinition water;
        public float interval = 0.5f;
        public int flowPerTick = 4;
        public int range = 16;       // how many pipe-steps pressure carries before it dies
        public bool isBooster;       // a relay (no water source) that re-pressurises the network

        public static readonly List<WaterPump> All = new();
        private readonly List<Vector2Int> _myBoost = new(); // boost cells I registered
        void OnEnable() => All.Add(this);
        void OnDisable()
        {
            All.Remove(this);
            foreach (var c in _myBoost) PipeNet.BoostCells.Remove(c);
            _myBoost.Clear();
        }

        private float _t;
        private bool _flowing;
        private SpriteRenderer _sr;
        private Color _base;

        private static readonly Queue<Vector2Int> _q = new();
        private static readonly Dictionary<Vector2Int, int> _dist = new(); // pipe cell → pressure distance
        private static readonly Belt.Dir[] _dirs = { Belt.Dir.N, Belt.Dir.E, Belt.Dir.S, Belt.Dir.W };

        public static WaterPump Spawn(BuildingDefinition def, Vector3 pos)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(def.FootW, def.FootH, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteDatabase.ForBuilding(def);
            sr.color = def.color;
            sr.sortingOrder = 5;

            go.AddComponent<BoxCollider2D>();

            var p = go.AddComponent<WaterPump>();
            p.def = def;
            p.water = def.item;
            p.isBooster = def.booster;
            p._sr = sr;
            p._base = def.color;
            // A booster registers its neighbouring cells as pressure-reset points.
            if (p.isBooster)
            {
                var me = new Vector2Int(cellX(pos), cellY(pos));
                foreach (var d in _dirs)
                {
                    var c = me + Belt.Step(d);
                    PipeNet.BoostCells.Add(c);
                    p._myBoost.Add(c);
                }
            }
            return p;
        }

        private static int cellX(Vector3 p) => Mathf.RoundToInt(p.x);
        private static int cellY(Vector3 p) => Mathf.RoundToInt(p.y);

        // An adjacent resource node yielding our liquid (the Oil Well's Oil deposit), or null.
        private ResourceNode FindSourceNode(Vector2Int me)
        {
            foreach (var n in ResourceNode.All)
            {
                if (n == null || n.yields != water || !n.HasResource) continue;
                int nx = Mathf.RoundToInt(n.transform.position.x), ny = Mathf.RoundToInt(n.transform.position.y);
                if (Mathf.Abs(nx - me.x) <= 2 && Mathf.Abs(ny - me.y) <= 2) return n;
            }
            return null;
        }

        void Update()
        {
            if (isBooster) return; // passive relay — only marks BoostCells
            _t += Time.deltaTime;
            if (_t < interval) return;
            _t = 0f;
            _flowing = Pump();
            if (_sr != null) _sr.color = _flowing ? _base : Color.Lerp(_base, Color.black, 0.5f);
        }

        // Source water only if adjacent to water terrain; then flood the connected pipe network
        // tracking PRESSURE = pipe-steps from the pump. Pressure dies past `range` (distant
        // sinks starve) UNLESS a Booster cell resets it to full — the layout/scaling problem.
        // Sinks within pressure get water from a shared per-tick budget (nearest first).
        private bool Pump()
        {
            if (water == null) return false;
            var me = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));

            // Source the liquid: WATER from adjacent water terrain (infinite), or — for an Oil Well — from an
            // adjacent OIL deposit (finite, extracted). The rest of the flood (pressure → sinks) is identical.
            bool atWater = false;
            foreach (var d in _dirs) if (TerrainGrid.IsWater(me + Belt.Step(d))) { atWater = true; break; }
            ResourceNode node = atWater ? null : FindSourceNode(me);
            if (!atWater && node == null) return false;

            _q.Clear(); _dist.Clear();
            foreach (var d in _dirs)
            {
                var s = me + Belt.Step(d);
                if (PipeNet.At(s) != null && !_dist.ContainsKey(s)) { _dist[s] = 1; _q.Enqueue(s); }
            }
            if (_q.Count == 0) return false; // no connected pipe → nothing to pump (and don't waste finite oil)

            int budget = flowPerTick;
            if (node != null) { budget = node.Extract(flowPerTick); if (budget <= 0) return false; } // finite deposit
            bool delivered = false;
            int guard = 0;
            while (_q.Count > 0 && guard++ < 4096)
            {
                var c = _q.Dequeue();
                int dc = _dist[c];
                if (dc > range) continue; // out of pressure — no delivery, no further reach here

                if (budget > 0)
                {
                    foreach (var d in _dirs)
                    {
                        var nb = c + Belt.Step(d);
                        if (budget <= 0) break;
                        // Sink 1: water storage (barrel) — a buffer.
                        if (WorldGrid.Storages.TryGetValue(nb, out var st) && st != null && st.accepts == water && st.def != null)
                        {
                            int room = st.def.capacity - st.Store.Total();
                            if (room > 0) { int add = Mathf.Min(budget, room); st.Store.Add(water, add); budget -= add; delivered = true; }
                        }
                        // Sink 2: a liquid-using workshop (Refinery, Campfire…) — fed into its input buffer,
                        // capped at a fair share per liquid so one liquid can't fill the buffer and starve another.
                        if (budget > 0 && WorldGrid.Workshops.TryGetValue(nb, out var wk) && wk != null && wk.WantsInput(water))
                        {
                            int inputs = wk.inputs != null ? Mathf.Max(1, wk.inputs.Count) : 1;
                            int perCap = wk.InBuffer.capacity / inputs;
                            int room = Mathf.Min(wk.InBuffer.capacity - wk.InBuffer.Total(), perCap - wk.InBuffer.Count(water));
                            if (room > 0) { int add = Mathf.Min(budget, room); wk.InBuffer.Add(water, add); budget -= add; delivered = true; }
                        }
                    }
                }

                // Propagate pressure: a Booster cell resets distance to full (extends range).
                int baseD = PipeNet.BoostCells.Contains(c) ? 0 : dc;
                foreach (var d in _dirs)
                {
                    var nb = c + Belt.Step(d);
                    if (PipeNet.At(nb) == null) continue;
                    int nd = baseD + 1;
                    if (!_dist.TryGetValue(nb, out var old) || nd < old) { _dist[nb] = nd; _q.Enqueue(nb); }
                }
            }
            return delivered;
        }
    }
}
