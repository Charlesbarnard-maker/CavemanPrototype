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
        private SpriteRenderer _statusPip; // green = pumping, red = idle/dry (source pumps only)
        private float _pipBase;            // resting pip scale (kept world-constant despite footprint scale)

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
            // Source pumps get a status pip so you can tell at a glance whether they're actually pumping
            // (green) or idle/dry/disconnected (red). Boosters are passive relays — the flowing pipes show it.
            if (!p.isBooster)
            {
                float scale = Mathf.Max(0.01f, go.transform.localScale.x);
                var pip = new GameObject("status");
                pip.transform.SetParent(go.transform);
                pip.transform.localPosition = new Vector3(0.34f, 0.34f, 0f); // parent space: ~the top-right corner
                p._pipBase = 0.24f / scale;
                pip.transform.localScale = Vector3.one * p._pipBase;
                var psr = pip.AddComponent<SpriteRenderer>();
                psr.sprite = PlaceholderArt.PipeDroplet(); // a soft round dot
                psr.color = new Color(1f, 0.35f, 0.3f, 0.9f);
                psr.sortingOrder = 6;
                p._statusPip = psr;
            }
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
            if (_t >= interval)
            {
                _t = 0f;
                _flowing = Pump();
                if (_sr != null) _sr.color = _flowing ? _base : Color.Lerp(_base, new Color(0.2f, 0.12f, 0.12f), 0.55f);
            }
            // The status pip animates EVERY frame off the cached _flowing state (Pump itself only ticks each interval).
            if (_statusPip != null)
            {
                _statusPip.color = _flowing ? new Color(0.3f, 1f, 0.4f, 0.95f) : new Color(1f, 0.35f, 0.3f, 0.9f);
                float pulse = _flowing ? 0.9f + 0.18f * Mathf.Sin(Time.time * 6f) : 1f; // a heartbeat while pumping
                _statusPip.transform.localScale = Vector3.one * (_pipBase * pulse);
            }
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
                var sp = PipeNet.At(s);
                if (sp != null && !_dist.ContainsKey(s)) { _dist[s] = 1; _q.Enqueue(s); sp.MarkFlow(water, d); } // flows OUT from the pump
            }
            if (_q.Count == 0) return false; // no connected pipe → nothing to pump (and don't waste finite oil)

            int budget = flowPerTick;
            if (node != null)
            {
                // OIL PRESSURE: a deposit's flow scales with how full it still is (down to a 25% trickle)
                // and the well NEVER takes the last unit — so a tapped-out field decays to a slow but
                // permanent seep (its slow regen sets the long-run rate) instead of vanishing. This kills
                // the soft-lock where all oil could be destroyed before the Monument needs Fuel.
                float frac = node.capacity > 0 ? (float)node.Amount / node.capacity : 1f;
                int want = Mathf.Max(1, Mathf.RoundToInt(flowPerTick * Mathf.Max(0.25f, frac)));
                want = Mathf.Min(want, node.Amount - 1);
                budget = want > 0 ? node.Extract(want) : 0;
                if (budget <= 0) return false;
            }
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
                        // Sink 2: a liquid-using workshop (Refinery, Campfire…) — fed into its input buffer under
                        // the SAME reserve-floor fair-share the belt gate uses (a fast pipe can't starve the other
                        // inputs, but a liquid can buffer past the old capacity/N hard cap that under-filled it).
                        if (budget > 0 && WorldGrid.Workshops.TryGetValue(nb, out var wk) && wk != null)
                        {
                            int room = wk.LiquidInputRoom(water);
                            if (room > 0) { int add = Mathf.Min(budget, room); wk.InBuffer.Add(water, add); budget -= add; delivered = true; }
                        }
                        // Sink 3: a liquid transfer STATION (Depot) — pipe-FED like a barrel, so a train can haul
                        // the liquid onward in a tanker wagon. A fresh station ADOPTS the liquid the pipe brings.
                        if (budget > 0 && WorldGrid.Depots.TryGetValue(nb, out var dpo) && dpo != null && dpo.def != null
                            && (!dpo.def.isHarbour || dpo.def.isLiquidHarbour) && (dpo.item == water || dpo.item == null))
                        {
                            if (dpo.item == null) dpo.item = water;
                            int room = dpo.def.capacity - dpo.store.Total();
                            if (room > 0) { int add = Mathf.Min(budget, room); dpo.store.Add(water, add); budget -= add; delivered = true; dpo.pumpFedAt = Time.time; }
                        }
                    }
                }

                // Propagate pressure: a Booster cell resets distance to full (extends range).
                int baseD = PipeNet.BoostCells.Contains(c) ? 0 : dc;
                foreach (var d in _dirs)
                {
                    var nb = c + Belt.Step(d);
                    var np = PipeNet.At(nb);
                    if (np == null) continue;
                    int nd = baseD + 1;
                    if (!_dist.TryGetValue(nb, out var old) || nd < old) { _dist[nb] = nd; _q.Enqueue(nb); np.MarkFlow(water, d); } // flows c→nb
                }
            }
            return delivered;
        }
    }
}
