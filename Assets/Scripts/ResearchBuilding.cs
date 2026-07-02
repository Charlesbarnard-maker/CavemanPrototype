using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// The Research Lodge: a pure SINK that accepts the current age's research item (belt-fed into
    /// its InBuffer, or pulled from an adjacent storage/workshop) and converts each one into
    /// research points (see <see cref="Research"/>). No worker needed — it just consumes whatever
    /// your factory delivers, so the bottleneck is how fast you can PRODUCE research items. One
    /// Lodge serves every age; the item it wants changes as you advance.
    /// </summary>
    public class ResearchBuilding : MonoBehaviour
    {
        public BuildingDefinition def;
        public Inventory InBuffer { get; private set; }   // belt-delivered research items
        public float interval = 1.0f;                      // seconds between consuming one item
        // The Lodge has an INPUT side only (no output): belts deliver on the side opposite OutputSide,
        // exactly like a storage's input. R rotates it during placement (cyan input notch shows where).
        public Belt.Dir OutputSide = Belt.Dir.E;

        public static readonly List<ResearchBuilding> All = new();
        private List<Vector2Int> _cells;
        private List<Vector2Int> _neighbours;

        private float _timer;
        private SpriteRenderer _sr, _statusDot;
        private Color _baseColor;
        private float _flash;

        // Rolling points/min (for the panel) so the player can see research throughput.
        private int _pointsWindow;
        private float _rateTimer;
        public float PointsPerMin { get; private set; }

        void OnEnable() { All.Add(this); }
        void OnDisable()
        {
            All.Remove(this);
            if (_cells != null) foreach (var c in _cells) WorldGrid.Remove(WorldGrid.Research, c, this);
        }

        /// <summary>True for ANY research item (so belts can keep feeding the Lodge for building-unlock
        /// nodes and at the final age — not just the next age's item).</summary>
        public bool Accepts(ItemDefinition i) => Research.IsResearchItem(i);

        public static ResearchBuilding Spawn(BuildingDefinition def, Vector3 pos, Belt.Dir outputSide = Belt.Dir.E)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(def.FootW, def.FootH, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteDatabase.ForBuilding(def);
            sr.color = def.color;
            sr.sortingOrder = 5;

            go.AddComponent<BoxCollider2D>();

            var rb = go.AddComponent<ResearchBuilding>();
            rb.def = def;
            rb.OutputSide = outputSide;
            rb.InBuffer = new Inventory { capacity = 24 };
            rb._cells = Footprint.Cells(go.transform.position, def.FootW, def.FootH);
            foreach (var c in rb._cells) WorldGrid.Research[c] = rb;
            Ports.PlacePorts(go.transform, def.FootW, def.FootH, outputSide, true, false, singlePort: true); // single input notch
            return rb;
        }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
        }

        // Cells immediately around the footprint (so you can also feed the Lodge by placing a
        // warehouse / maker workshop right next to it, not only via belt).
        private List<Vector2Int> Neighbours()
        {
            if (_neighbours != null) return _neighbours;
            var set = new HashSet<Vector2Int>();
            if (_cells != null)
                foreach (var cell in _cells)
                    foreach (var d in new[] { Belt.Dir.N, Belt.Dir.E, Belt.Dir.S, Belt.Dir.W })
                    {
                        var nc = cell + Belt.Step(d);
                        if (!_cells.Contains(nc)) set.Add(nc);
                    }
            _neighbours = new List<Vector2Int>(set);
            return _neighbours;
        }

        // Take ONE of any research item from this Lodge's InBuffer (belt-fed), else an adjacent
        // storage / workshop / collector. Returns the item consumed, or null if none available.
        private ItemDefinition ConsumeOneResearchItem()
        {
            ItemDefinition found = null;
            foreach (var kv in InBuffer.Items)
                if (kv.Value > 0 && Research.IsResearchItem(kv.Key)) { found = kv.Key; break; }
            if (found != null) { InBuffer.RemoveUpTo(found, 1); return found; }

            foreach (var nc in Neighbours())
            {
                if (WorldGrid.Storages.TryGetValue(nc, out var s) && s != null && Research.IsResearchItem(s.accepts) && s.Store.RemoveUpTo(s.accepts, 1) > 0) return s.accepts;
                if (WorldGrid.Workshops.TryGetValue(nc, out var w) && w != null && Research.IsResearchItem(w.output) && w.Buffer.RemoveUpTo(w.output, 1) > 0) return w.output;
                if (WorldGrid.Collectors.TryGetValue(nc, out var p) && p != null && Research.IsResearchItem(p.produces) && p.Buffer.RemoveUpTo(p.produces, 1) > 0) return p.produces;
            }
            return null;
        }

        void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= interval)
            {
                _timer = 0f;
                var consumed = ConsumeOneResearchItem();
                if (consumed != null)
                {
                    int ppi = Research.PointsFor(consumed);
                    Research.Deliver(consumed, 1);
                    _pointsWindow += ppi; // accumulate POINTS for the throughput readout
                    _flash = 0.3f;
                }
            }

            // Throughput readout (points/min) over a ~4s window.
            _rateTimer += Time.deltaTime;
            if (_rateTimer >= 4f)
            {
                PointsPerMin = _pointsWindow * (60f / _rateTimer);
                _pointsWindow = 0;
                _rateTimer = 0f;
            }

            UpdateVisual();
            UpdateStatus();
        }

        /// <summary>Plain-language status: working (consuming), waiting (no items), or done.</summary>
        public Color StatusColor
        {
            get
            {
                if (Research.AllResearched) return Status.Idle;     // every tech bought — truly done
                if (InBuffer.Total() > 0) return Status.Working;    // has items to convert
                return Status.Waiting;                              // amber — waiting for research items to arrive
            }
        }

        private float _statusWarnT; // debounce: a caution shows only after the problem persists (no blink)
        private void UpdateStatus()
        {
            if (_statusDot == null) _statusDot = Status.MakeDot(transform);
            Status.Apply(_statusDot, Status.Debounce(StatusColor, ref _statusWarnT));
        }

        private void UpdateVisual()
        {
            if (_sr == null) return;
            Color shown = _baseColor;
            if (_flash > 0f)
            {
                _flash -= Time.deltaTime;
                shown = Color.Lerp(_baseColor, Color.white, Mathf.Clamp01(_flash / 0.3f));
            }
            _sr.color = shown;
        }
    }
}
