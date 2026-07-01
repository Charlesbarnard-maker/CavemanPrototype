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
        /// <summary>True while this collector is actively gathering — drives the visible worker units.</summary>
        public bool Working => !Paused && _source != null && _source.HasResource && Buffer.Total() < Buffer.capacity;

        // ---- Manual paid age-upgrade (stone tools → metal → machines): faster gather rate + a look change. ----
        public int Tier { get; private set; }
        private float _speedMult = 1f;
        public UpgradeTier PendingUpgrade =>
            (def != null && def.upgrades != null && Tier < def.upgrades.Count) ? def.upgrades[Tier] : null;
        public bool UpgradeUnlocked
        { get { var u = PendingUpgrade; return u != null && (Colony.Instance == null || u.unlockAge <= Colony.Instance.Age); } }
        /// <summary>True when an upgrade is available AND affordable right now — drives the world ⬆ badge.</summary>
        public bool CanUpgradeNow
        { get { var u = PendingUpgrade; return UpgradeUnlocked && u != null && Economy.CanAfford(u.cost, Colony.Instance != null ? Colony.Instance.carried : null); } }
        /// <summary>How many upgrade tiers this building has in total (for the "tier X/Y" panel readout).</summary>
        public int UpgradeTierCount => def != null && def.upgrades != null ? def.upgrades.Count : 0;

        /// <summary>Buy the next upgrade tier (paid from the carried pile) — speeds the building up and
        /// recolours it. Returns false if there's no tier available, it's age-locked, or unaffordable.</summary>
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
            Pulse();
            return true;
        }

        public static readonly List<ProductionBuilding> All = new();
        private List<Vector2Int> _cells; // every grid cell this building occupies
        void OnEnable() { All.Add(this); }
        void OnDisable()
        {
            All.Remove(this);
            if (_source != null) { _source.Claims = Mathf.Max(0, _source.Claims - 1); _source = null; } // release our node claim
            if (_cells != null) foreach (var c in _cells) WorldGrid.Remove(WorldGrid.Collectors, c, this);
            if (_fx != null) Destroy(_fx.gameObject); // FX is a standalone object → clean it up on demolish
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
        private SpriteRenderer _upgradeBadge;
        private MachineWorkFX _fx; // cosmetic "machine working" arm + dust (no workers, no logic)

        public static ProductionBuilding Spawn(BuildingDefinition def, Vector3 pos, Belt.Dir outputSide = Belt.Dir.E)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(def.FootW * 0.9f, def.FootH * 0.9f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteDatabase.ForBuilding(def);
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
            Ports.PlacePorts(go.transform, def.FootW, def.FootH, outputSide, false, true, singlePort: true);
            pb.Bind();
            // Visible GATHERERS: little caveman workers walk out to the node and back (replaces the old
            // machine-arm FX). Cosmetic — the gather timer above does the real work.
            WorkerUnit.SpawnForCollector(pb, Mathf.Clamp(def.maxWorkers, 1, 3));
            // Upgrade machinery overlay — grows with the building's tier (gear → +piston → +smokestack/glow).
            MachineUpgradeFX.Attach(go.transform, () => pb != null ? pb.Tier : 0);
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
            if (best != null) SetSource(best);
        }

        // Re-point our source while keeping each node's Claims count accurate, so collectors fan out across a
        // cluster instead of all binding the same node. Releasing the old claim frees that node for others.
        private void SetSource(ResourceNode n)
        {
            if (_source == n) return;
            if (_source != null) _source.Claims = Mathf.Max(0, _source.Claims - 1);
            _source = n;
            if (_source != null) _source.Claims++;
        }

        // Nearest live node of our type within `range`. BOUNDED so it never becomes a map-wide
        // scan: out-of-range nodes are rejected by a cheap squared-distance test, and at most
        // MaxSearchCandidates in-range nodes are examined (the window shrinks to the nearest).
        // No pathfinding — just binds to the nearest patch. None found → returns null and the
        // collector idles in place (surfaced by its status dot).
        private const int LowNode = 5; // a node below this is "nearly tapped" — prefer a fuller one in reach
        // Pick the best node of our type within `range`, preferring (in order): a HEALTHY amount (>= LowNode) so
        // we don't lock onto a near-empty patch while a full one sits in reach; then FEWER OTHER collectors
        // already on it, so a cluster's collectors fan OUT across its nodes instead of all hammering one (and
        // workers stop trekking to a node that's about to be cleared); then NEARER. Bounded (squared-distance
        // reject + a hard candidate cap) — never a map-wide scan.
        private ResourceNode FindNearestNode(float range)
        {
            ResourceNode best = null;
            bool bestHealthy = false;
            int bestOther = int.MaxValue;
            float bestSq = 0f;
            float rangeSq = range * range;
            int examined = 0;
            foreach (var n in ResourceNode.All)
            {
                if (n == null || n.yields != produces || n.Amount < 1) continue;
                float sq = ((Vector2)(n.transform.position - transform.position)).sqrMagnitude;
                if (sq > rangeSq) continue;                       // outside search radius — local, not global
                if (++examined > MaxSearchCandidates) break;      // hard cap on candidates checked
                bool healthy = n.Amount >= LowNode;
                int other = n.Claims - (n == _source ? 1 : 0);    // claims by OTHER collectors (ignore our own)
                bool better = best == null
                    || (healthy && !bestHealthy)                                       // healthy beats tapped-out
                    || (healthy == bestHealthy && other < bestOther)                   // then fewer rivals on it
                    || (healthy == bestHealthy && other == bestOther && sq < bestSq);  // then nearer
                if (better) { best = n; bestHealthy = healthy; bestOther = other; bestSq = sq; }
            }
            return best;
        }

        // Re-bind to a fresh nearby patch when the current source is gone (finite vein
        // exhausted/destroyed) — so local depletion pushes you outward instead of just
        // killing the collector. Throttled so we don't scan every frame when none remain.
        private float _rebindT;
        private void MaybeRebind()
        {
            // Re-scan not only when the source is GONE, but also when it's nearly tapped (Amount < LowNode) — so a
            // depleting node RELEASES the collector to a fuller reachable one instead of looping on near-empty.
            bool needsBetter = _source == null || !_source.HasResource || _source.Amount < LowNode;
            if (!needsBetter) return;
            _rebindT += Time.deltaTime;
            if (_rebindT < 1f) return;
            _rebindT = 0f;
            Bind(searchRadius); // FindNearestNode prefers a healthy node, falling back to any with resource
        }

        public void Pulse() { _flash = 0.25f; if (_fx != null) _fx.Strike(); } // flash + a dust/arm work pulse

        /// <summary>Save/load: restore the upgrade tier (rate + tint) and paused flag. The bound source node
        /// is NOT restored — the collector re-binds to the nearest live node of its type after load.</summary>
        internal void LoadRestore(int tier, bool paused)
        {
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

        void Update()
        {
            MaybeRebind(); // chase fresh patches as nearby ones deplete

            // Fully automated gather: pull GatherPerCycle from the bound node into the Buffer each
            // interval (no worker NPC). Backs up if the Buffer is full → the BackedUp signal.
            bool working = !Paused && _source != null && _source.HasResource && Buffer.Total() < Buffer.capacity;
            if (working)
            {
                float iv = interval / Mathf.Max(0.1f, _speedMult); // upgrades shorten the gather cadence
                _timer += Time.deltaTime * Economy.ProductionScale; // global production-rate dial (feeds the denser belts)
                if (_timer >= iv)
                {
                    _timer -= iv;
                    int got = _source.Extract(GatherPerCycle * Mathf.Max(1, outputPerCycle)); // honour the def's per-cycle yield (was ignored)
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

            // Drive the cosmetic "machine working" animation (arm aimed at the tapped node + dust).
            if (_fx != null)
            {
                _fx.SetWorking(working);
                if (_source != null) _fx.SetTarget(_source.transform.position);
            }
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
            if (_upgradeBadge == null) _upgradeBadge = Status.MakeUpgradeBadge(transform);
            Status.ApplyUpgradeBadge(_upgradeBadge, CanUpgradeNow);
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
