using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A processor — a fully automated machine. Once built it runs by itself at a FIXED rate
    /// (no workers): each processTime it consumes its recipe inputs (belt-fed InBuffer first, then
    /// adjacent storage/machines) and adds the output to its Buffer. A full output Buffer or missing
    /// inputs stall it (the BackedUp / Starved signals). The engine for all production chains.
    /// </summary>
    public class WorkshopBuilding : MonoBehaviour
    {
        public BuildingDefinition def;
        public ItemDefinition output;
        public int outputPerCycle = 1;
        public float processTime = 2.5f;
        public List<ItemAmount> inputs = new();
        public Belt.Dir OutputSide = Belt.Dir.E; // belts pull output only from this side

        public Inventory Buffer { get; private set; }      // finished output
        public Inventory InBuffer { get; private set; }     // inputs delivered by belts
        public bool Paused { get; private set; }            // player can halt it to free shared inputs
        public void TogglePause() => Paused = !Paused;

        // Power (Industrial age): a running machine draws power; the grid's supply/demand
        // ratio scales its speed (brownouts) via Power.Factor.
        public int PowerDraw => def != null ? def.powerDraw : 0;
        public bool DrawsPower => Power.Active && PowerDraw > 0 && !Paused; // runs automatically once built

        public static readonly List<WorkshopBuilding> All = new();
        private List<Vector2Int> _cells; // every grid cell this building occupies
        void OnEnable() { All.Add(this); }
        void OnDisable()
        {
            All.Remove(this);
            if (_cells != null) foreach (var c in _cells) WorldGrid.Remove(WorldGrid.Workshops, c, this);
        }

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

        public static WorkshopBuilding Spawn(BuildingDefinition def, Vector3 pos, Belt.Dir outputSide = Belt.Dir.E)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(def.FootW, def.FootH, 1f);

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
            w.inputs = def.inputs;
            w.Buffer = new Inventory { capacity = Mathf.Max(1, def.capacity) };
            w.InBuffer = new Inventory { capacity = 32 }; // shared input buffer (fair-shared across inputs)
            w.OutputSide = outputSide;
            w._cells = Footprint.Cells(go.transform.position, def.FootW, def.FootH);
            foreach (var c in w._cells) WorldGrid.Workshops[c] = w;
            Ports.PlacePorts(go.transform, def.FootW, def.FootH, outputSide,
                def.inputs != null && def.inputs.Count > 0, true,
                def.inputs != null && def.inputs.Count > 1); // multi-input → input ports on all non-output sides
            return w;
        }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
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

        /// <summary>Per-input consumption at the machine's fixed rate, in items/min, so the player can
        /// size belts/collectors against the 60/min lane. Ignores brownouts. e.g. a Sawmill
        /// (wood 2, 2.0s) → "60 Wood/min".</summary>
        public string InputDemandText()
        {
            if (inputs == null || inputs.Count == 0 || processTime <= 0f) return "";
            var parts = new List<string>();
            foreach (var c in inputs)
            {
                if (c == null || c.item == null) continue;
                float perMin = c.amount * (60f / processTime);
                parts.Add($"{perMin:0} {c.item.displayName}/min");
            }
            return parts.Count > 0 ? string.Join(", ", parts) : "";
        }

        /// <summary>Is this item one of the recipe inputs (so a belt may deliver it here)?</summary>
        public bool WantsInput(ItemDefinition i)
        {
            foreach (var c in inputs) if (c != null && c.item == i) return true;
            return false;
        }

        /// <summary>Belt-delivery gate. Beyond "is it an input and is there room", a multi-input
        /// recipe gives EACH input a fair share of the shared InBuffer — so a fast belt of one item
        /// can't fill the whole buffer and starve the other input (the mixed-buffer DEADLOCK that
        /// made e.g. the Idea Bench sit "waiting for stone" while stone backed up on its belt).</summary>
        public bool CanAcceptBeltInput(ItemDefinition i)
        {
            if (!WantsInput(i) || InBuffer.Total() >= InBuffer.capacity) return false;
            int distinct = inputs != null ? inputs.Count : 1;
            if (distinct <= 1) return true; // single input may use the whole buffer
            // Each OTHER input keeps a guaranteed FLOOR of slots, so one (or several) fast belts can't
            // fill the buffer and deadlock the recipe — but this input may use everything NOT still
            // owed to under-floor others. (The old capacity/N hard share stalled a single belted input
            // at ≈12 and backed it up while the machine sat half-empty waiting for the other input —
            // the "first few go through then pile up even though there's space" bug.) Gating on the
            // free space minus the others' OWED reserve fixes that AND prevents two fast belts from
            // jointly starving a third input on a 3-/4-input recipe (Campfire, Monument).
            const int reservePerOther = 4;
            int owedToOthers = 0;
            foreach (var c in inputs)
            {
                if (c == null || c.item == null || c.item == i) continue;
                int have = InBuffer.Count(c.item);
                if (have < reservePerOther) owedToOthers += reservePerOther - have;
            }
            return InBuffer.Total() + 1 + owedToOthers <= InBuffer.capacity;
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

        // The grid cells immediately surrounding this building's footprint (cached).
        private static readonly Belt.Dir[] _dirs = { Belt.Dir.N, Belt.Dir.E, Belt.Dir.S, Belt.Dir.W };
        private List<Vector2Int> _neighbours;
        private List<Vector2Int> Neighbours()
        {
            if (_neighbours != null) return _neighbours;
            var set = new HashSet<Vector2Int>();
            if (_cells != null)
                foreach (var cell in _cells)
                    foreach (var d in _dirs)
                    {
                        var nc = cell + Belt.Step(d);
                        if (!_cells.Contains(nc)) set.Add(nc); // skip our own interior cells
                    }
            _neighbours = new List<Vector2Int>(set);
            return _neighbours;
        }

        /// <summary>Units of `item` reachable from storages / machines adjacent to the footprint.</summary>
        private static readonly HashSet<Component> _counted = new(); // reused (no per-call alloc)
        private int AdjacentAvailable(ItemDefinition item)
        {
            int n = 0;
            _counted.Clear(); // a multi-cell neighbour must count only once
            foreach (var nc in Neighbours())
            {
                if (WorldGrid.Storages.TryGetValue(nc, out var s) && s != null && s.accepts == item && _counted.Add(s)) n += s.Store.Count(item);
                if (WorldGrid.Collectors.TryGetValue(nc, out var p) && p != null && p.produces == item && _counted.Add(p)) n += p.Buffer.Count(item);
                if (WorldGrid.Workshops.TryGetValue(nc, out var w) && w != null && w != this && w.output == item && _counted.Add(w)) n += w.Buffer.Count(item);
            }
            return n;
        }

        /// <summary>Pull `amount` of `item` from storages / machines adjacent to the footprint.</summary>
        private void AdjacentConsume(ItemDefinition item, int amount)
        {
            int rem = amount;
            foreach (var nc in Neighbours())
            {
                if (rem <= 0) return;
                if (WorldGrid.Storages.TryGetValue(nc, out var s) && s != null && s.accepts == item) rem -= s.Store.RemoveUpTo(item, rem);
                if (rem <= 0) return;
                if (WorldGrid.Collectors.TryGetValue(nc, out var p) && p != null && p.produces == item) rem -= p.Buffer.RemoveUpTo(item, rem);
                if (rem <= 0) return;
                if (WorldGrid.Workshops.TryGetValue(nc, out var w) && w != null && w != this && w.output == item) rem -= w.Buffer.RemoveUpTo(item, rem);
            }
        }

        void Update()
        {
            Power.EnsureFresh();
            var carried = Colony.Instance != null ? Colony.Instance.carried : null;
            bool produced = false;

            if (!Paused && output != null && Buffer.Total() < Buffer.capacity)
            {
                float prod = Colony.Instance != null ? Colony.Instance.Productivity : 1f; // inert 1f now
                float pw = Power.Active && PowerDraw > 0 ? Power.Factor : 1f; // brownouts slow machines
                _timer += Time.deltaTime * prod * pw; // fixed rate (no workers); powered machines slow under brownout
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

            bool working = produced || Buffer.Total() > 0;
            UpdateVisual(working);
            UpdateStatus();
        }

        /// <summary>Live status colour (green working / yellow output-full / red missing-input /
        /// grey paused) — also drives minimap dots.</summary>
        public Color StatusColor
        {
            get
            {
                if (Paused) return Status.Idle;
                if (Buffer.Total() >= Buffer.capacity) return Status.BackedUp;
                if (_starved) return Status.Starved;
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
