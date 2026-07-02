using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A CRANE ARM — the game's stylised, heavier take on item transfer (its "inserter", but slow and
    /// chunky): a counterweighted beam that GRABS a small stack from the cell(s) behind it and swings
    /// it to the cell(s) in front. Deliberately batch-y — few, weighty swings with the cargo visibly
    /// held in the claw — rather than a fast per-item flicker.
    ///
    /// What it does that belts can't: reaches over a 1–2 cell GAP, lifts over port sides (a crane
    /// ignores which edge a building's notch is on), filtered pulls off a mixed line (Gantry tier),
    /// and loading/unloading without belt spaghetti. Belts remain the base I/O — arms are the
    /// OPTIONAL optimisation layer (never required).
    ///
    /// Tiers (data-driven off the def): interval = seconds per full swing · outputPerCycle = items
    /// per grab · searchRadius = reach in cells. A powered tier (powerDraw &gt; 0) is a wired grid
    /// consumer that runs at HALF speed unwired — same rule as drills, slowed never walled.
    /// </summary>
    public class CraneArm : MonoBehaviour, IPowerConsumer
    {
        public BuildingDefinition def;
        public Belt.Dir dir = Belt.Dir.E;   // deposit side; it grabs from the opposite side
        public int reach = 1;                // cells scanned on each side (nearest first)
        public int grabSize = 2;             // items per swing
        public float cycleTime = 2.2f;       // seconds per full grab-swing-drop cycle
        public readonly List<ItemDefinition> filterItems = new(); // Gantry whitelist (empty = anything)
        public bool HasFilterUI => def != null && def.filter;

        public ItemDefinition held;          // cargo in the claw (single type per swing)
        public int heldCount;

        public static readonly List<CraneArm> All = new();
        public static readonly Dictionary<Vector2Int, CraneArm> Grid = new();
        public static CraneArm At(Vector2Int c) => Grid.TryGetValue(c, out var a) ? a : null;

        private Vector2Int _cell;
        private float _t;                    // cycle clock
        private Transform _beam;             // rotating arm
        private SpriteRenderer _cargoSr;     // the held item shown in the claw
        private bool _workedThisCycle;       // moved something last swing (drives the idle-draw trickle)

        // ---- IPowerConsumer (Gantry tier): unwired = half speed, wired = full. ----
        public int PowerDraw => def != null ? def.powerDraw : 0;
        public float CurrentDraw
        {
            get
            {
                if (PowerDraw <= 0 || !PowerNet.Active) return 0f;
                return (heldCount > 0 || _workedThisCycle) ? PowerDraw : PowerDraw * 0.1f;
            }
        }

        void OnEnable() { All.Add(this); _cell = Belt.CellOf(transform.position); Grid[_cell] = this; }
        void OnDisable()
        {
            All.Remove(this);
            if (Grid.TryGetValue(_cell, out var a) && a == this) Grid.Remove(_cell);
            // Cargo in the claw goes back to the player's hand — demolishing never destroys items.
            if (held != null && heldCount > 0 && Colony.Instance != null && Colony.Instance.carried != null)
                Colony.Instance.carried.Add(held, heldCount);
            held = null; heldCount = 0;
        }

        public static CraneArm Spawn(BuildingDefinition def, Vector3 pos, Belt.Dir dir = Belt.Dir.E)
        {
            var cell = Belt.CellOf(pos);
            var existing = At(cell);
            if (existing != null) { existing.SetDir(dir); return existing; } // re-aim in place

            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(cell.x, cell.y, 0f);
            go.transform.localScale = Vector3.one;
            go.AddComponent<BoxCollider2D>(); // clickable to select / demolish; counts as solid

            var arm = go.AddComponent<CraneArm>();
            arm.def = def;
            arm.dir = dir;
            arm.reach = Mathf.Max(1, Mathf.RoundToInt(def.searchRadius));
            arm.grabSize = Mathf.Max(1, def.outputPerCycle);
            arm.cycleTime = Mathf.Max(0.3f, def.interval);
            arm.BuildArt();

            // A powered tier is a wired grid CONSUMER (1 wire), like a drill or powered machine.
            if (def.powerDraw > 0)
            {
                var node = go.AddComponent<PowerNode>();
                node.role = PowerNode.Role.Consumer;
                node.maxConnections = 1;
                node.consumer = arm;
            }
            return arm;
        }

        public void SetDir(Belt.Dir d) { dir = d; }

        // ---- procedural art: a stone/wood pivot base + a rotating beam with a claw + the cargo ----
        private void BuildArt()
        {
            var baseGo = new GameObject("Base");
            baseGo.transform.SetParent(transform, false);
            baseGo.transform.localScale = new Vector3(0.62f, 0.62f, 1f);
            var bs = baseGo.AddComponent<SpriteRenderer>();
            bs.sprite = PlaceholderArt.Square();
            bs.color = Color.Lerp(def.color, Color.black, 0.35f);
            bs.sortingOrder = 6; // above belts + items, below the beam

            var beamGo = new GameObject("Beam");
            beamGo.transform.SetParent(transform, false);
            _beam = beamGo.transform;
            var beamSpr = new GameObject("BeamSprite");
            beamSpr.transform.SetParent(_beam, false);
            beamSpr.transform.localPosition = new Vector3(0f, 0.34f, 0f); // beam extends along local +Y from the pivot
            beamSpr.transform.localScale = new Vector3(0.16f, 0.78f, 1f);
            var sr = beamSpr.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = def.color;
            sr.sortingOrder = 7;

            var claw = new GameObject("Claw");
            claw.transform.SetParent(_beam, false);
            claw.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            claw.transform.localScale = new Vector3(0.26f, 0.14f, 1f);
            var cs = claw.AddComponent<SpriteRenderer>();
            cs.sprite = PlaceholderArt.Square();
            cs.color = Color.Lerp(def.color, Color.black, 0.5f);
            cs.sortingOrder = 8;

            var cargo = new GameObject("Cargo");
            cargo.transform.SetParent(_beam, false);
            cargo.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            cargo.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
            _cargoSr = cargo.AddComponent<SpriteRenderer>();
            _cargoSr.sortingOrder = 9;
            _cargoSr.enabled = false;
        }

        void Update()
        {
            // Powered tier: unwired/dead grid = HALF speed (never a hard stop) — the drill rule.
            float pw = (PowerDraw > 0 && PowerNet.Active) ? 0.5f + 0.5f * PowerNet.FactorOf(this) : 1f;
            _t += Time.deltaTime * pw;
            AnimateBeam();

            if (_t < cycleTime) return;
            _t = 0f;
            _workedThisCycle = false;
            if (heldCount == 0) TryGrab();
            if (heldCount > 0) TryDeposit();
        }

        // The beam pendulums between the grab side (empty) and the drop side (loaded), easing so the
        // swing reads heavy. Cargo icon shows what's in the claw, scaled slightly by the stack size.
        private void AnimateBeam()
        {
            if (_beam == null) return;
            float frontZ = Belt.Angle(dir);            // beam's +Y toward the deposit side
            float backZ = frontZ + 180f;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_t / cycleTime));
            float z = heldCount > 0 ? Mathf.LerpAngle(backZ, frontZ, k) : Mathf.LerpAngle(frontZ, backZ, k);
            _beam.localRotation = Quaternion.Euler(0f, 0f, z);
            if (_cargoSr != null)
            {
                _cargoSr.enabled = held != null && heldCount > 0;
                if (_cargoSr.enabled)
                {
                    _cargoSr.sprite = held.icon != null ? held.icon : PlaceholderArt.Square();
                    _cargoSr.color = held.icon != null ? Color.white : held.color;
                    float s = 0.24f + 0.03f * Mathf.Min(5, heldCount);
                    _cargoSr.transform.localScale = new Vector3(s, s, 1f);
                }
            }
        }

        // May the claw take this item? Never liquids; must match the current part-stack; Gantry whitelist.
        private bool Accepts(ItemDefinition it)
        {
            if (it == null || it.isLiquid) return false;
            if (held != null && heldCount > 0 && it != held) return false;
            if (HasFilterUI && filterItems.Count > 0 && !filterItems.Contains(it)) return false;
            return true;
        }

        private void Hold(ItemDefinition it, int n) { held = it; heldCount += n; if (n > 0) _workedThisCycle = true; }

        // ---- GRAB: scan the cells behind (nearest first) — belt leads, then any building's output
        //      buffer. Side-agnostic on buildings: a crane lifts over the wall, ports don't gate it. ----
        private void TryGrab()
        {
            var back = Belt.Opposite(dir);
            for (int d = 1; d <= reach && heldCount < grabSize; d++)
            {
                var c = _cell + Belt.Step(back) * d;
                var b = Belt.At(c);
                if (b != null)
                {
                    while (heldCount < grabSize && b.TryTakeLead(Accepts, out var it)) Hold(it, 1);
                    continue; // a belt cell can't also hold a building
                }
                GrabFromBuildings(c);
            }
        }

        private void GrabFromBuildings(Vector2Int c)
        {
            int need() => grabSize - heldCount;
            if (need() <= 0) return;
            if (WorldGrid.Collectors.TryGetValue(c, out var p) && p != null && p.produces != null && Accepts(p.produces))
            { int n = p.Buffer.RemoveUpTo(p.produces, need()); if (n > 0) Hold(p.produces, n); }
            if (need() > 0 && WorldGrid.Workshops.TryGetValue(c, out var w) && w != null && w.output != null && Accepts(w.output))
            { int n = w.Buffer.RemoveUpTo(w.output, need()); if (n > 0) Hold(w.output, n); }
            if (need() > 0 && WorldGrid.Storages.TryGetValue(c, out var s) && s != null && s.accepts != null && Accepts(s.accepts))
            { int n = s.Store.RemoveUpTo(s.accepts, need()); if (n > 0) Hold(s.accepts, n); }
            if (need() > 0 && WorldGrid.Depots.TryGetValue(c, out var dp) && dp != null && dp.item != null && Accepts(dp.item))
            { int n = dp.store.RemoveUpTo(dp.item, need()); if (n > 0) Hold(dp.item, n); }
        }

        // ---- DEPOSIT: scan the cells ahead (nearest first) — belts, storage, workshop inputs, the
        //      research lodge, generator fuel intakes and depots. Drops as much of the stack as fits. ----
        private void TryDeposit()
        {
            for (int d = 1; d <= reach && heldCount > 0; d++)
            {
                var c = _cell + Belt.Step(dir) * d;
                var b = Belt.At(c);
                if (b != null)
                {
                    // The item enters the belt from the edge facing the crane.
                    var entry = Belt.FromTo(c, _cell);
                    while (heldCount > 0 && b.CanAccept(held) && b.ReceiveItem(held, entry, 0f)) TookOne();
                    continue;
                }
                DepositToBuildings(c);
            }
            if (heldCount <= 0) { held = null; heldCount = 0; }
        }

        private void TookOne() { heldCount--; _workedThisCycle = true; }

        private void DepositToBuildings(Vector2Int c)
        {
            if (heldCount <= 0) return;
            if (WorldGrid.Storages.TryGetValue(c, out var s) && s != null && s.def != null
                && (s.accepts == held || (s.accepts == null && s.configurable && s.CanAdopt(held))))
            {
                if (s.accepts == null) s.accepts = held;
                while (heldCount > 0 && s.Store.Total() < s.def.capacity && s.Store.Add(held, 1) > 0) TookOne();
            }
            if (heldCount > 0 && WorldGrid.Workshops.TryGetValue(c, out var w) && w != null && w.WantsInput(held))
                while (heldCount > 0 && w.CanAcceptBeltInput(held) && w.InBuffer.Add(held, 1) > 0) TookOne();
            if (heldCount > 0 && WorldGrid.Research.TryGetValue(c, out var rb) && rb != null && rb.Accepts(held))
                while (heldCount > 0 && rb.InBuffer.Add(held, 1) > 0) TookOne();
            if (heldCount > 0 && WorldGrid.Generators.TryGetValue(c, out var g) && g != null && g.fuel == held)
                while (heldCount > 0 && g.Buffer.Add(held, 1) > 0) TookOne();
            if (heldCount > 0 && WorldGrid.Depots.TryGetValue(c, out var dp) && dp != null && dp.def != null
                && !dp.def.isLiquidHarbour && (dp.item == held || dp.item == null)) // solid cargo only
            {
                if (dp.item == null) dp.item = held;
                while (heldCount > 0 && dp.store.Total() < dp.def.capacity && dp.store.Add(held, 1) > 0) TookOne();
            }
        }
    }
}
