using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A collector — a fully automated machine. It binds to the nearest matching ResourceNode and,
    /// once built, gathers on a fixed timer straight into its Buffer (NO workers — buildings run by
    /// themselves). Belts/adjacency drain the Buffer; a full Buffer backs the collector up (the
    /// BackedUp signal that you need to haul output out). When its node runs dry it auto-rebinds to
    /// the nearest live node within searchRadius (never a map-wide scan).
    /// </summary>
    public class ProductionBuilding : MonoBehaviour
    {
        public BuildingDefinition def;
        public ItemDefinition produces;
        public int outputPerCycle = 1;
        public float interval = 2f;
        public float sourceRange = 6f;   // how far the initial placement bind looks for a node
        public float searchRadius = 16f; // when the node runs dry, look this far for a fresh one
        private const int MaxSearchCandidates = 48; // hard cap on in-radius nodes examined (no map-wide scan)
        private const int GatherPerCycle = 2; // units per `interval` → 60/min at the standard 2.0s (one belt lane)
        public Belt.Dir OutputSide = Belt.Dir.E; // belts pull output only from this side

        public Inventory Buffer { get; private set; }
        public bool Paused { get; private set; }            // player can halt it (priorities)
        public void TogglePause() => Paused = !Paused;
        public ResourceNode Source => _source;

        public static readonly List<ProductionBuilding> All = new();
        private List<Vector2Int> _cells; // every grid cell this building occupies
        void OnEnable() { All.Add(this); }
        void OnDisable()
        {
            All.Remove(this);
            if (_cells != null) foreach (var c in _cells) WorldGrid.Remove(WorldGrid.Collectors, c, this);
        }

        // Rolling gather-rate estimate (units/min).
        private int _producedWindow;
        private float _rateTimer;
        public float RatePerMin { get; private set; }
        public void RecordProduced(int n) { if (n > 0) _producedWindow += n; }

        private float _timer;     // gather cadence
        private ResourceNode _source;
        private float _flash;
        private SpriteRenderer _sr;
        private Color _baseColor;
        private SpriteRenderer _statusDot;

        public static ProductionBuilding Spawn(BuildingDefinition def, Vector3 pos, Belt.Dir outputSide = Belt.Dir.E)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(def.FootW * 0.9f, def.FootH * 0.9f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = def.color;
            sr.sortingOrder = 5;

            go.AddComponent<BoxCollider2D>(); // clickable for select / demolish

            var pb = go.AddComponent<ProductionBuilding>();
            pb.def = def;
            pb.produces = def.item;
            pb.outputPerCycle = def.outputPerCycle;
            pb.interval = def.interval;
            pb.searchRadius = def.searchRadius > 0f ? def.searchRadius : 16f;
            pb.Buffer = new Inventory { capacity = Mathf.Max(1, def.capacity) };
            pb.OutputSide = outputSide;
            pb._cells = Footprint.Cells(go.transform.position, def.FootW, def.FootH);
            foreach (var c in pb._cells) WorldGrid.Collectors[c] = pb;
            Ports.PlacePorts(go.transform, def.FootW, def.FootH, outputSide, false, true);
            pb.Bind();
            return pb;
        }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
        }

        public void Bind() => Bind(sourceRange);

        /// <summary>Bind to the nearest live node of our type within `range`. Used at placement
        /// (sourceRange) and when chasing a fresh patch after depletion (searchRadius).</summary>
        public void Bind(float range)
        {
            ResourceNode best = FindNearestNode(range);
            // Water collectors draw from adjacent WATER TERRAIN, not a pre-placed node: create
            // an infinite, invisible source at the nearest water cell (location-dependent
            // extraction from a real map feature). Made once; parented so it's cleaned up.
            if (best == null && def != null && def.fromWaterTerrain
                && TerrainGrid.NearestWaterCell(transform.position, sourceRange, out var wc))
            {
                var go = new GameObject("WaterSource");
                go.transform.SetParent(transform);
                go.transform.position = new Vector3(wc.x, wc.y, 0f);
                go.AddComponent<SpriteRenderer>();   // no sprite — the water terrain is the visual
                go.AddComponent<BoxCollider2D>();
                var n = go.AddComponent<ResourceNode>();
                n.yields = produces;
                n.capacity = 9999;
                n.regenAmount = 9999;
                n.regenInterval = 0.5f;
                best = n;
            }
            if (best != null) _source = best;
        }

        // Nearest live node of our type within `range`. BOUNDED so it never becomes a map-wide
        // scan: out-of-range nodes are rejected by a cheap squared-distance test, and at most
        // MaxSearchCandidates in-range nodes are examined (the window shrinks to the nearest).
        // No pathfinding — just binds to the nearest patch. None found → returns null and the
        // collector idles in place (surfaced by its status dot).
        private ResourceNode FindNearestNode(float range)
        {
            ResourceNode best = null;
            float bestSq = range * range;
            int examined = 0;
            foreach (var n in ResourceNode.All)
            {
                if (n == null || n.yields != produces || !n.HasResource) continue;
                float sq = ((Vector2)(n.transform.position - transform.position)).sqrMagnitude;
                if (sq > bestSq) continue;                    // outside search radius — local, not global
                if (++examined > MaxSearchCandidates) break;  // hard cap on candidates checked
                bestSq = sq; best = n;                        // nearest-so-far
            }
            return best;
        }

        // Re-bind to a fresh nearby patch when the current source is gone (finite vein
        // exhausted/destroyed) — so local depletion pushes you outward instead of just
        // killing the collector. Throttled so we don't scan every frame when none remain.
        private float _rebindT;
        private void MaybeRebind()
        {
            if (_source != null && _source.HasResource) return;
            _rebindT += Time.deltaTime;
            if (_rebindT < 1f) return;
            _rebindT = 0f;
            // Wider search than the placement bind: chase a fresh node within searchRadius.
            if (_source == null || !_source.HasResource) Bind(searchRadius);
        }

        public void Pulse() => _flash = 0.25f;

        void Update()
        {
            MaybeRebind(); // chase fresh patches as nearby ones deplete

            // Fully automated gather: pull GatherPerCycle from the bound node into the Buffer each
            // interval (no worker NPC). Backs up if the Buffer is full → the BackedUp signal.
            bool working = !Paused && _source != null && _source.HasResource && Buffer.Total() < Buffer.capacity;
            if (working)
            {
                float prod = Colony.Instance != null ? Colony.Instance.Productivity : 1f; // inert 1f now
                _timer += Time.deltaTime * prod;
                if (_timer >= interval)
                {
                    _timer -= interval;
                    int got = _source.Extract(GatherPerCycle);
                    if (got > 0)
                    {
                        int accepted = Buffer.Add(produces, got);
                        if (accepted > 0) { RecordProduced(accepted); Pulse(); }
                    }
                    _source.Nudge();
                }
            }
            else _timer = 0f;

            _rateTimer += Time.deltaTime;
            if (_rateTimer >= 4f)
            {
                RatePerMin = _producedWindow * (60f / _rateTimer);
                _producedWindow = 0;
                _rateTimer = 0f;
            }

            UpdateVisual(working);
            UpdateStatus();
        }

        /// <summary>Live status colour (green working / yellow backed-up / red starved /
        /// grey paused) — also used by the minimap to surface bottlenecks across the base.</summary>
        public Color StatusColor
        {
            get
            {
                if (Paused) return Status.Idle;
                if (Buffer.Total() >= Buffer.capacity) return Status.BackedUp;
                if (_source == null || !_source.HasResource) return Status.Starved;
                return Status.Working;
            }
        }

        private void UpdateStatus()
        {
            if (_statusDot == null) _statusDot = Status.MakeDot(transform);
            Status.Apply(_statusDot, StatusColor);
        }

        private void UpdateVisual(bool working)
        {
            if (_sr == null) return;
            Color shown = working ? _baseColor : Color.Lerp(_baseColor, Color.black, 0.5f);
            if (_flash > 0f)
            {
                _flash -= Time.deltaTime;
                shown = Color.Lerp(shown, Color.white, Mathf.Clamp01(_flash / 0.25f));
            }
            _sr.color = shown;
        }
    }
}
