using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A processor: consumes input resources from the pool and produces an output
    /// over time (a recipe). Needs assigned workers to run — more workers process
    /// faster. Output accumulates in its buffer (counts toward the pool) and is
    /// moved to storage by Transporters. The engine for all production chains.
    /// </summary>
    public class WorkshopBuilding : MonoBehaviour, IStaffable
    {
        public BuildingDefinition def;
        public ItemDefinition output;
        public int outputPerCycle = 1;
        public float processTime = 2.5f;
        public int maxWorkers = 2;
        public List<ItemAmount> inputs = new();

        public Inventory Buffer { get; private set; }      // finished output
        public Inventory InBuffer { get; private set; }     // inputs delivered by belts
        public int AssignedWorkers { get; private set; }
        public int MaxWorkers => maxWorkers;
        public string StaffLabel => def != null ? def.displayName : "Workshop";

        public static readonly List<WorkshopBuilding> All = new();
        private Vector2Int _gridCell;
        void OnEnable() { All.Add(this); _gridCell = Belt.CellOf(transform.position); WorldGrid.Workshops[_gridCell] = this; }
        void OnDisable() { All.Remove(this); WorldGrid.Remove(WorldGrid.Workshops, _gridCell, this); }

        private float _timer, _flash;
        private bool _starved;

        // Rolling throughput estimate (units/min) — lets the player measure whether a
        // tweak actually improved output (the optimisation "almost working" loop).
        private int _producedWindow;
        private float _rateTimer;
        public float RatePerMin { get; private set; }
        private SpriteRenderer _sr;
        private Color _baseColor;
        private SpriteRenderer _statusDot;

        public static WorkshopBuilding Spawn(BuildingDefinition def, Vector3 pos)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one * 1.0f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = def.color;
            sr.sortingOrder = 5;

            go.AddComponent<BoxCollider2D>();

            var w = go.AddComponent<WorkshopBuilding>();
            w.def = def;
            w.output = def.item;
            w.outputPerCycle = def.outputPerCycle;
            w.processTime = Mathf.Max(0.2f, def.interval);
            w.maxWorkers = Mathf.Max(1, def.maxWorkers);
            w.inputs = def.inputs;
            w.Buffer = new Inventory { capacity = Mathf.Max(1, def.capacity) };
            w.InBuffer = new Inventory { capacity = 24 };
            return w;
        }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
        }

        public bool TryAssign()
        {
            if (AssignedWorkers >= maxWorkers) return false;
            if (Colony.Instance == null || Colony.Instance.FreeWorkers <= 0) return false;
            AssignedWorkers++;
            return true;
        }

        public void Unassign()
        {
            if (AssignedWorkers > 0) AssignedWorkers--;
        }

        /// <summary>Which recipe inputs are currently short (for the "waiting for X" panel hint).</summary>
        public string MissingText()
        {
            if (Economy.FreeBuild) return "";
            var carried = Colony.Instance != null ? Colony.Instance.carried : null;
            var missing = new System.Collections.Generic.List<string>();
            foreach (var c in inputs)
            {
                if (c.item == null) continue;
                int have = InBuffer.Count(c.item)
                    + (Economy.LocalProduction ? AdjacentAvailable(c.item) : Economy.Available(c.item, carried));
                if (have < c.amount) missing.Add(c.item.displayName);
            }
            return missing.Count > 0 ? string.Join(", ", missing) : "";
        }

        /// <summary>Is this item one of the recipe inputs (so a belt may deliver it here)?</summary>
        public bool WantsInput(ItemDefinition i)
        {
            foreach (var c in inputs) if (c != null && c.item == i) return true;
            return false;
        }

        // Inputs: belt-fed InBuffer first, then —
        //   LocalProduction ON  → only ADJACENT storage/collector/workshop (logistics matters).
        //   LocalProduction OFF → the global pool (old "everything teleports" behaviour).
        private bool CanMake(Inventory carried)
        {
            if (Economy.FreeBuild) return true;
            foreach (var c in inputs)
            {
                if (c.item == null) continue;
                int have = InBuffer.Count(c.item)
                    + (Economy.LocalProduction ? AdjacentAvailable(c.item) : Economy.Available(c.item, carried));
                if (have < c.amount) return false;
            }
            return true;
        }

        private void ConsumeInputs(Inventory carried)
        {
            if (Economy.FreeBuild) return;
            foreach (var c in inputs)
            {
                if (c.item == null) continue;
                int need = c.amount - InBuffer.RemoveUpTo(c.item, c.amount);
                if (need <= 0) continue;
                if (Economy.LocalProduction) AdjacentConsume(c.item, need);
                else Economy.SpendUpTo(c.item, need, carried);
            }
        }

        // The four grid cells touching this workshop.
        private static readonly Belt.Dir[] _dirs = { Belt.Dir.N, Belt.Dir.E, Belt.Dir.S, Belt.Dir.W };

        /// <summary>Units of `item` reachable from directly adjacent storages / machines.</summary>
        private int AdjacentAvailable(ItemDefinition item)
        {
            int n = 0;
            foreach (var d in _dirs)
            {
                var nc = _gridCell + Belt.Step(d);
                if (WorldGrid.Storages.TryGetValue(nc, out var s) && s != null && s.accepts == item) n += s.Store.Count(item);
                if (WorldGrid.Collectors.TryGetValue(nc, out var p) && p != null && p.produces == item) n += p.Buffer.Count(item);
                if (WorldGrid.Workshops.TryGetValue(nc, out var w) && w != null && w != this && w.output == item) n += w.Buffer.Count(item);
            }
            return n;
        }

        /// <summary>Pull `amount` of `item` from adjacent storages / machines.</summary>
        private void AdjacentConsume(ItemDefinition item, int amount)
        {
            int rem = amount;
            foreach (var d in _dirs)
            {
                if (rem <= 0) return;
                var nc = _gridCell + Belt.Step(d);
                if (WorldGrid.Storages.TryGetValue(nc, out var s) && s != null && s.accepts == item) rem -= s.Store.RemoveUpTo(item, rem);
                if (rem <= 0) return;
                if (WorldGrid.Collectors.TryGetValue(nc, out var p) && p != null && p.produces == item) rem -= p.Buffer.RemoveUpTo(item, rem);
                if (rem <= 0) return;
                if (WorldGrid.Workshops.TryGetValue(nc, out var w) && w != null && w != this && w.output == item) rem -= w.Buffer.RemoveUpTo(item, rem);
            }
        }

        void Update()
        {
            var carried = Colony.Instance != null ? Colony.Instance.carried : null;
            bool produced = false;

            if (AssignedWorkers > 0 && output != null && Buffer.Total() < Buffer.capacity)
            {
                float prod = Colony.Instance != null ? Colony.Instance.Productivity : 1f;
                _timer += Time.deltaTime * AssignedWorkers * prod; // more workers / well-fed = faster
                if (_timer >= processTime)
                {
                    if (CanMake(carried))
                    {
                        _timer -= processTime;
                        ConsumeInputs(carried);
                        Buffer.Add(output, outputPerCycle);
                        _producedWindow += outputPerCycle;
                        _flash = 0.25f;
                        produced = true;
                        _starved = false;
                    }
                    else
                    {
                        // Inputs missing — back off so we don't re-scan the whole pool every
                        // frame while starved (perf); recheck roughly twice a second.
                        _timer = Mathf.Max(0f, processTime - 0.5f);
                        _starved = true;
                    }
                }
            }
            else
            {
                _timer = 0f;
            }

            // Roll up throughput over a ~4s window.
            _rateTimer += Time.deltaTime;
            if (_rateTimer >= 4f)
            {
                RatePerMin = _producedWindow * (60f / _rateTimer);
                _producedWindow = 0;
                _rateTimer = 0f;
            }

            bool working = AssignedWorkers > 0 && (produced || Buffer.Total() > 0);
            UpdateVisual(working);
            UpdateStatus();
        }

        /// <summary>Live status colour (green/yellow/red/grey) — also drives minimap dots.</summary>
        public Color StatusColor
        {
            get
            {
                if (AssignedWorkers == 0) return Status.Idle;
                if (Buffer.Total() >= Buffer.capacity) return Status.BackedUp;
                if (_starved) return Status.Starved;
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
