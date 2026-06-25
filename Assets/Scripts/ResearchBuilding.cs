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

        /// <summary>True if this item is the one currently being researched (so a belt may deliver it).</summary>
        public bool Accepts(ItemDefinition i) => i != null && i == Research.CurrentItem;

        public static ResearchBuilding Spawn(BuildingDefinition def, Vector3 pos, Belt.Dir outputSide = Belt.Dir.E)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(def.FootW, def.FootH, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = def.color;
            sr.sortingOrder = 5;

            go.AddComponent<BoxCollider2D>();

            var rb = go.AddComponent<ResearchBuilding>();
            rb.def = def;
            rb.InBuffer = new Inventory { capacity = 24 };
            rb._cells = Footprint.Cells(go.transform.position, def.FootW, def.FootH);
            foreach (var c in rb._cells) WorldGrid.Research[c] = rb;
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

        // Pull one of the current research item from an adjacent storage / workshop / collector.
        private bool AdjacentConsume(ItemDefinition item)
        {
            foreach (var nc in Neighbours())
            {
                if (WorldGrid.Storages.TryGetValue(nc, out var s) && s != null && s.accepts == item && s.Store.RemoveUpTo(item, 1) > 0) return true;
                if (WorldGrid.Workshops.TryGetValue(nc, out var w) && w != null && w.output == item && w.Buffer.RemoveUpTo(item, 1) > 0) return true;
                if (WorldGrid.Collectors.TryGetValue(nc, out var p) && p != null && p.produces == item && p.Buffer.RemoveUpTo(item, 1) > 0) return true;
            }
            return false;
        }

        void Update()
        {
            var item = Research.CurrentItem;

            _timer += Time.deltaTime;
            if (_timer >= interval)
            {
                _timer = 0f;
                if (item != null)
                {
                    int ppi = Mathf.Max(1, Research.PointsPerItem); // value BEFORE delivering
                    // Belt-delivered first, then an adjacent storage/machine.
                    bool got = InBuffer.RemoveUpTo(item, 1) > 0 || AdjacentConsume(item);
                    if (got)
                    {
                        Research.Deliver(item, 1);
                        _pointsWindow += ppi; // accumulate POINTS for the throughput readout
                        _flash = 0.3f;
                    }
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
                if (Research.Complete) return Status.Idle;          // nothing left to research
                if (InBuffer.Total() > 0) return Status.Working;    // has items to convert
                return Status.Starved;                              // waiting for research items
            }
        }

        private void UpdateStatus()
        {
            if (_statusDot == null) _statusDot = Status.MakeDot(transform);
            Status.Apply(_statusDot, StatusColor);
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
