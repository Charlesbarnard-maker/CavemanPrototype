using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A collector. Binds to a nearby ResourceNode; each ASSIGNED worker spawns a
    /// Worker NPC that walks out, harvests, and carries loads back into this
    /// building's buffer. The buffer is emptied by Transporters (TransportHub) —
    /// there's no instant teleport to storage. A full buffer stalls the collector
    /// (backpressure), which is the signal that you need transport.
    /// </summary>
    public class ProductionBuilding : MonoBehaviour, IStaffable
    {
        public BuildingDefinition def;
        public ItemDefinition produces;
        public int outputPerCycle = 1;
        public float interval = 2f;
        public int maxWorkers = 2;
        public float sourceRange = 6f;   // worker can commute this far

        public Inventory Buffer { get; private set; }
        public ResourceNode Source => _source;
        public int AssignedWorkers { get; private set; }
        public int MaxWorkers => maxWorkers;
        public string StaffLabel => def != null ? def.displayName : "Collector";

        public static readonly List<ProductionBuilding> All = new();
        private Vector2Int _gridCell;
        void OnEnable() { All.Add(this); _gridCell = Belt.CellOf(transform.position); WorldGrid.Collectors[_gridCell] = this; }
        void OnDisable() { All.Remove(this); WorldGrid.Remove(WorldGrid.Collectors, _gridCell, this); }

        private readonly List<Worker> _workers = new();
        private ResourceNode _source;
        private float _flash;
        private SpriteRenderer _sr;
        private Color _baseColor;
        private SpriteRenderer _statusDot;

        public static ProductionBuilding Spawn(BuildingDefinition def, Vector3 pos)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one * 0.9f;

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
            pb.maxWorkers = Mathf.Max(1, def.maxWorkers);
            pb.Buffer = new Inventory { capacity = Mathf.Max(1, def.capacity) };
            pb.Bind();
            return pb;
        }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
        }

        public void Bind()
        {
            ResourceNode best = null;
            float bestSq = sourceRange * sourceRange;
            foreach (var n in ResourceNode.All)
            {
                // Prefer the nearest node in range that still HAS resource, so we re-bind
                // to a live patch when the old one is exhausted.
                if (n == null || n.yields != produces || !n.HasResource) continue;
                float sq = ((Vector2)(n.transform.position - transform.position)).sqrMagnitude;
                if (sq <= bestSq) { bestSq = sq; best = n; }
            }
            if (best != null) _source = best;
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
            if (_source == null || !_source.HasResource) Bind();
        }

        public bool TryAssign()
        {
            if (AssignedWorkers >= maxWorkers) return false;
            if (Colony.Instance == null || Colony.Instance.FreeWorkers <= 0) return false;
            AssignedWorkers++;
            RefreshWorkers();
            return true;
        }

        public void Unassign()
        {
            if (AssignedWorkers <= 0) return;
            AssignedWorkers--;
            RefreshWorkers();
        }

        private void RefreshWorkers()
        {
            _workers.RemoveAll(w => w == null);
            while (_workers.Count < AssignedWorkers) _workers.Add(Worker.Spawn(this));
            while (_workers.Count > AssignedWorkers)
            {
                var w = _workers[_workers.Count - 1];
                _workers.RemoveAt(_workers.Count - 1);
                if (w != null) Destroy(w.gameObject);
            }
        }

        public void Pulse() => _flash = 0.25f;

        void Update()
        {
            MaybeRebind(); // chase fresh patches as nearby ones deplete

            bool working = AssignedWorkers > 0 && _source != null && _source.HasResource
                           && Buffer.Total() < Buffer.capacity;
            UpdateVisual(working);
            UpdateStatus();
        }

        /// <summary>Live status colour (green working / yellow backed-up / red starved /
        /// grey idle) — also used by the minimap to surface bottlenecks across the base.</summary>
        public Color StatusColor
        {
            get
            {
                if (AssignedWorkers == 0) return Status.Idle;
                if (Buffer.Total() >= Buffer.capacity) return Status.BackedUp;
                if (_source == null || !_source.HasResource) return Status.Starved;
                return Status.Working;
            }
        }

        private void UpdateStatus()
        {
            if (_statusDot == null) _statusDot = Status.MakeDot(transform);
            _statusDot.color = StatusColor;
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
