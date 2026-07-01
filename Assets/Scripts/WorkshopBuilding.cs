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

        // Configurable multi-recipe machines (Basic/Advanced Smelter): when set, the SELECTED recipe
        // drives output/outputPerCycle/processTime/inputs above. Null/empty = a fixed single recipe.
        public List<Recipe> recipes;
        private int _recipeIndex = -1;
        public bool HasRecipeChoice => recipes != null && recipes.Count > 1;
        public Recipe ActiveRecipe => (recipes != null && _recipeIndex >= 0 && _recipeIndex < recipes.Count) ? recipes[_recipeIndex] : null;
        /// <summary>Save: the selected recipe index (-1 = single fixed recipe).</summary>
        internal int RecipeIndexForSave => _recipeIndex;

        /// <summary>Save/load: restore the selected recipe, upgrade tier (rate + tint) and paused flag.</summary>
        internal void LoadRestore(int tier, bool paused, int recipeIndex)
        {
            if (recipes != null && recipeIndex >= 0 && recipeIndex < recipes.Count) ApplyRecipe(recipeIndex);
            Paused = paused;
            if (def != null && def.upgrades != null && tier > 0 && tier <= def.upgrades.Count)
            {
                Tier = tier;
                var u = def.upgrades[tier - 1];
                _speedMult = Mathf.Max(0.1f, u.speedMult);
                _baseColor = u.tint;
                if (_sr != null) _sr.color = _baseColor;
            }
        }

        public Inventory Buffer { get; private set; }      // finished output
        public Inventory InBuffer { get; private set; }     // inputs delivered by belts
        public bool Paused { get; private set; }            // player can halt it to free shared inputs
        public void TogglePause() => Paused = !Paused;
        /// <summary>True while this workshop is actively producing — drives the visible worker units.</summary>
        public bool Working => !Paused && output != null && Buffer.Total() < Buffer.capacity && !_starved;

        // ---- Manual paid age-upgrade (geared → reinforced → automated): faster processing + a look change. ----
        public int Tier { get; private set; }
        private float _speedMult = 1f;
        public UpgradeTier PendingUpgrade =>
            (def != null && def.upgrades != null && Tier < def.upgrades.Count) ? def.upgrades[Tier] : null;
        public bool UpgradeUnlocked
        { get { var u = PendingUpgrade; return u != null && (Colony.Instance == null || u.unlockAge <= Colony.Instance.Age); } }
        /// <summary>True when an upgrade is available AND you can afford it right now — drives the world ⬆ badge.</summary>
        public bool CanUpgradeNow
        { get { var u = PendingUpgrade; return UpgradeUnlocked && u != null && Economy.CanAfford(u.cost, Colony.Instance != null ? Colony.Instance.carried : null); } }
        /// <summary>How many upgrade tiers this building has in total (for the "tier X/Y" panel readout).</summary>
        public int UpgradeTierCount => def != null && def.upgrades != null ? def.upgrades.Count : 0;

        /// <summary>Buy the next upgrade tier (paid from the carried pile) — speeds processing + recolours.
        /// Returns false if there's no tier available, it's age-locked, or unaffordable.</summary>
        public bool TryUpgrade()
        {
            var u = PendingUpgrade;
            if (u == null || !UpgradeUnlocked) return false;
            var carried = Colony.Instance != null ? Colony.Instance.carried : null;
            if (!Economy.CanAfford(u.cost, carried)) return false;
            Economy.Spend(u.cost, carried);
            Tier++;
            _speedMult = Mathf.Max(0.1f, u.speedMult);
            _baseColor = u.tint;
            if (_sr != null) _sr.color = _baseColor;
            _flash = 0.25f;
            return true;
        }

        // Power network: a requiresPower machine is wired into a powered network (see PowerNet) and runs
        // at its supply/demand factor. PowerDraw is its draw at full tilt; CurrentDraw is what it pulls
        // RIGHT NOW — full while actively processing, a trickle while stalled, ZERO when switched off.
        public int PowerDraw => def != null ? def.powerDraw : 0;
        public const float IdleDrawFraction = 0.1f; // a stalled (but on) machine still sips power
        /// <summary>Live power draw, fed to PowerNet's demand: 0 when switched off (Paused), full while
        /// actively processing, a small idle trickle while stalled (starved / output-full).</summary>
        public float CurrentDraw
        {
            get
            {
                if (!RequiresPower || Paused) return 0f;
                bool working = output != null && Buffer.Total() < Buffer.capacity && !_starved;
                return working ? PowerDraw : PowerDraw * IdleDrawFraction;
            }
        }

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
        private SpriteRenderer _upgradeBadge;

        public static WorkshopBuilding Spawn(BuildingDefinition def, Vector3 pos, Belt.Dir outputSide = Belt.Dir.E)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(def.FootW, def.FootH, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteDatabase.ForBuilding(def);
            sr.color = def.color;
            sr.sortingOrder = 5;

            go.AddComponent<BoxCollider2D>();

            var w = go.AddComponent<WorkshopBuilding>();
            w.def = def;
            w.output = def.item;
            w.outputPerCycle = def.outputPerCycle;
            w.processTime = Mathf.Max(0.2f, def.interval);
            w.inputs = def.inputs;
            // Configurable smelter etc.: adopt the recipe list + select the first age-unlocked recipe,
            // which overrides output/inputs/processTime above. Plain workshops leave recipes null.
            if (def.recipes != null && def.recipes.Count > 0)
            {
                w.recipes = def.recipes;
                w.ApplyRecipe(w.FirstUnlockedRecipe());
            }
            w.Buffer = new Inventory { capacity = Mathf.Max(1, def.capacity) };
            w.InBuffer = new Inventory { capacity = 32 }; // shared input buffer (fair-shared across inputs)
            w.OutputSide = outputSide;
            w._cells = Footprint.Cells(go.transform.position, def.FootW, def.FootH);
            foreach (var c in w._cells) WorldGrid.Workshops[c] = w;
            Ports.PlacePorts(go.transform, def.FootW, def.FootH, outputSide,
                def.inputs != null && def.inputs.Count > 0, true,
                def.inputs != null && def.inputs.Count > 1, singlePort: true); // one in/out slot per side (multi-input → one per non-output side)

            // Powered machines are wired CONSUMER nodes (1 wire — they're fed, not relays). See PowerNet.
            if (def.requiresPower)
            {
                var node = go.AddComponent<PowerNode>();
                node.role = PowerNode.Role.Consumer;
                node.maxConnections = 1;
                node.consumer = w;
            }
            WorkerUnit.SpawnForWorkshop(w, 1); // a visible worker tending the machine while it runs
            // Upgrade machinery overlay — grows with the workshop's tier (gear → +piston → +smokestack/glow).
            MachineUpgradeFX.Attach(go.transform, () => w != null ? w.Tier : 0);
            return w;
        }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
        }

        // ---- Configurable recipe selection (Basic/Advanced Smelter) ----------------------------
        // Point this machine at recipe i: that recipe now drives output/inputs/processTime so all the
        // rest of the workshop logic (CanMake, ConsumeInputs, WantsInput, ports) works unchanged.
        public void ApplyRecipe(int i)
        {
            if (recipes == null || i < 0 || i >= recipes.Count) return;
            _recipeIndex = i;
            var r = recipes[i];
            output = r.output;
            outputPerCycle = r.outputPerCycle;
            processTime = Mathf.Max(0.2f, r.processTime);
            inputs = r.inputs;
            _timer = 0f; _starved = false;
        }

        private int FirstUnlockedRecipe()
        {
            // Default to the LOWEST-age unlocked recipe (the simplest one), independent of list order, so a
            // freshly placed multi-recipe machine never auto-selects a higher-tier recipe by authoring accident.
            int age = Colony.Instance != null ? Colony.Instance.Age : 0;
            int best = -1, bestAge = int.MaxValue;
            for (int i = 0; i < recipes.Count; i++)
                if (recipes[i].unlockAge <= age && recipes[i].unlockAge < bestAge) { bestAge = recipes[i].unlockAge; best = i; }
            return best >= 0 ? best : 0; // none unlocked yet → the first
        }

        /// <summary>Switch to the next age-unlocked recipe (player clicks "change recipe" in the panel).
        /// Dumps any belt-fed inputs back to hand first so the old recipe's items don't strand here.</summary>
        public void CycleRecipe()
        {
            if (recipes == null || recipes.Count <= 1) return;
            int age = Colony.Instance != null ? Colony.Instance.Age : 0;
            for (int step = 1; step <= recipes.Count; step++)
            {
                int i = (_recipeIndex + step) % recipes.Count;
                if (recipes[i].unlockAge <= age) { DumpInBuffer(); ApplyRecipe(i); return; }
            }
        }

        // Move everything currently in the input buffer back to the player's hand (so a recipe switch
        // never leaves the now-wrong inputs clogging InBuffer). Mirrors the demolish dump.
        private void DumpInBuffer()
        {
            if (InBuffer == null) return;
            var carried = Colony.Instance != null ? Colony.Instance.carried : null;
            var snapshot = new List<KeyValuePair<ItemDefinition, int>>(InBuffer.Items);
            foreach (var kv in snapshot)
            {
                if (kv.Key == null || kv.Value <= 0) continue;
                if (carried != null) carried.Add(kv.Key, kv.Value);
                InBuffer.RemoveUpTo(kv.Key, kv.Value);
            }
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
            return InBuffer.Total() + 1 + OwedReserve(i) <= InBuffer.capacity;
        }

        // Slots the OTHER inputs are still owed up to their guaranteed floor — so a fast feed of input `i`
        // can't fill the buffer and deadlock the recipe. Shared by the belt gate AND the liquid-pipe path.
        private const int ReservePerOther = 4;
        private int OwedReserve(ItemDefinition i)
        {
            if (inputs == null) return 0;
            int owed = 0;
            foreach (var c in inputs)
            {
                if (c == null || c.item == null || c.item == i) continue;
                int have = InBuffer.Count(c.item);
                if (have < ReservePerOther) owed += ReservePerOther - have;
            }
            return owed;
        }

        /// <summary>How many units of liquid input `i` a PIPE may deposit into the shared InBuffer right now,
        /// under the SAME reserve-floor fair-share the belt gate uses — so a fast pipe can't starve the other
        /// inputs (fixes the old WaterPump `capacity/N` hard-share that under-filled multi-input liquid recipes).
        /// Returns 0 if `i` isn't wanted or there's no fair room. The caller still bounds the add by its flow.</summary>
        public int LiquidInputRoom(ItemDefinition i)
        {
            if (!WantsInput(i)) return 0;
            int free = InBuffer.capacity - InBuffer.Total();
            if (free <= 0) return 0;
            int distinct = inputs != null ? inputs.Count : 1;
            if (distinct <= 1) return free; // single input may use the whole buffer
            return Mathf.Max(0, free - OwedReserve(i));
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

        // THE single energy seam — every era (Stone-age hearth radius, mid steam, late electricity)
        // returns its speed factor through here, so machine code never changes as energy evolves.
        // P0: behaviour-identical to the old inline logic (Industrial brownout, else full speed). A
        // later phase makes the pre-Industrial branch consult the hearth/steam heat field.
        public bool RequiresPower => def != null && def.requiresPower;
        // The single energy seam: a powered machine runs at its NETWORK's supply factor — 1 when
        // connected with enough generation, &lt;1 if the network is oversubscribed (brownout), 0 if it
        // isn't connected to any powered network. Unpowered machines always run. Better generators /
        // poles in later ages flow through this same accessor (no machine-code change).
        private float EffectivePowerFactor()
        {
            // Power is a Bronze-age mechanic: before then, machines run free even if requiresPower.
            return (RequiresPower && PowerNet.Active) ? PowerNet.FactorOf(this) : 1f;
        }

        void Update()
        {
            var carried = Colony.Instance != null ? Colony.Instance.carried : null;
            bool produced = false;

            if (!Paused && output != null && Buffer.Total() < Buffer.capacity)
            {
                float pt = processTime / Mathf.Max(0.1f, _speedMult); // upgrades shorten the process time
                float pw = EffectivePowerFactor();
                _timer += Time.deltaTime * pw * Economy.ProductionScale; // fixed rate (no workers); energy seam + global production dial
                if (_timer >= pt)
                {
                    if (CanMake(carried))
                    {
                        _timer -= pt;
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
                        _timer = Mathf.Max(0f, pt - 0.5f);
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
        // Why a powered machine isn't (fully) running — so the player can SEE the cause instead of a vague stall:
        //   Unwired  = not connected to any grid (no wire at all);
        //   GridDead = wired, but its network generates nothing;
        //   Brownout = wired + some supply, but demand > generation so it runs SLOW (0 < factor < 1);
        //   Ok       = full speed (or it doesn't need power yet).
        public enum PowerState { Ok, Unwired, GridDead, Brownout }
        private PowerNode _pnode;
        private PowerNode Pnode => _pnode != null ? _pnode : (_pnode = GetComponent<PowerNode>());
        public PowerState PowerStatus
        {
            get
            {
                if (!RequiresPower || !PowerNet.Active) return PowerState.Ok; // pre-Tribal / not a powered machine
                float f = PowerNet.FactorOf(this);
                bool wired = Pnode != null && Pnode.links.Count > 0;
                if (!wired) return PowerState.Unwired;       // no wire → not on any grid
                if (f <= 0f) return PowerState.GridDead;     // wired, but the network has zero generation
                if (f < 0.999f) return PowerState.Brownout;  // running below full speed
                return PowerState.Ok;
            }
        }
        // A powered machine fully STALLED for lack of power (unwired or a dead grid). Brownout still runs (slow).
        public bool NoPower => PowerStatus == PowerState.Unwired || PowerStatus == PowerState.GridDead;
        public Color StatusColor
        {
            get
            {
                if (Paused) return Status.Idle;
                var ps = PowerStatus;
                if (ps == PowerState.Unwired) return Status.NoPower;  // RED — not wired to a grid
                if (ps == PowerState.GridDead) return Status.NoPower; // RED — grid generates nothing
                if (Buffer.Total() >= Buffer.capacity) return Status.BackedUp;
                if (_starved) return Status.Waiting; // amber — just waiting on belt-delivered inputs, not broken
                if (ps == PowerState.Brownout) return new Color(1f, 0.6f, 0.2f);   // orange — running slow (brownout)
                return Status.Working;
            }
        }

        private static bool _powerHintShown; // one-time onboarding when a machine first needs power
        public static void ResetPowerHint() => _powerHintShown = false;
        private void UpdateStatus()
        {
            if (_statusDot == null) _statusDot = Status.MakeDot(transform);
            Status.Apply(_statusDot, StatusColor);
            if (_upgradeBadge == null) _upgradeBadge = Status.MakeUpgradeBadge(transform);
            Status.ApplyUpgradeBadge(_upgradeBadge, CanUpgradeNow);
            if (!_powerHintShown && NoPower)
            {
                _powerHintShown = true;
                string nm = def != null ? def.displayName : "This machine";
                if (PowerStatus == PowerState.Unwired)
                    Toast.Show($"<color=#6cf>⚡ {nm} needs POWER and isn't wired to the grid.</color> Build a Wood Generator (belt Wood into its fuel edge), then select the Generator or a Power Pole and draw a WIRE to this machine. Unwired powered machines stop (blue dot).");
                else
                    Toast.Show($"<color=#6cf>⚡ {nm} is wired, but the grid has NO POWER.</color> Its network's demand exceeds its generation — add or fuel a Generator, or add a Battery (purple dot).");
            }
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
            // No-power "electric cut" blink — the whole body pulses toward red so you can spot a powerless
            // machine across the map without selecting it.
            if (NoPower)
            {
                float blink = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 7f);
                shown = Color.Lerp(Color.Lerp(_baseColor, Color.black, 0.5f), new Color(0.95f, 0.12f, 0.12f), blink);
            }
            _sr.color = shown;
        }
    }
}
