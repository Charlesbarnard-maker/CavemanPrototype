using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A CRANE ARM — the game's stylised, heavier take on item transfer (its "inserter", but slow and
    /// chunky): a counterweighted lattice boom that swings a small STACK between the cell(s) BEHIND it
    /// and the cell(s) in FRONT (R aims it while placing).
    ///
    /// It runs a real swing cycle, so the motion always reads:
    ///   swing to the pickup side → WAIT there (claw open, gentle bob) until something arrives →
    ///   grab a stack → swing across carrying it visibly → drop → swing back.
    /// If the drop side is FULL it waits at the front, claw pulsing red-amber with the cargo held up —
    /// unmistakably "blocked", not broken.
    ///
    /// Tiers are data-driven off the def: interval = seconds per full swing cycle · outputPerCycle =
    /// items per grab · searchRadius = reach in cells. A powered tier (powerDraw &gt; 0) is a wired grid
    /// consumer that runs at HALF speed unwired — same rule as drills.
    /// </summary>
    public class CraneArm : MonoBehaviour, IPowerConsumer
    {
        public BuildingDefinition def;
        public Belt.Dir dir = Belt.Dir.E;   // deposit side; it grabs from the opposite side
        public int reach = 1;                // cells scanned on each side (nearest first)
        public int grabSize = 2;             // items per swing
        public float cycleTime = 2.2f;       // seconds per full grab→carry→drop→return cycle
        public readonly List<ItemDefinition> filterItems = new(); // Gantry whitelist (empty = anything)
        public bool HasFilterUI => def != null && def.filter;

        public ItemDefinition held;          // cargo in the claw (single type per swing)
        public int heldCount;

        public static readonly List<CraneArm> All = new();
        public static readonly Dictionary<Vector2Int, CraneArm> Grid = new();
        public static CraneArm At(Vector2Int c) => Grid.TryGetValue(c, out var a) ? a : null;

        // ---- swing state machine ----
        private enum Phase { ToSource, AtSource, ToSink, AtSink }
        private Phase _phase = Phase.ToSource;
        private float _swing;        // 0..1 progress of the current half-swing
        private float _poll;         // throttles grab/deposit attempts while waiting
        private float _waitT;        // how long we've been stuck in a wait phase (drives the blocked cue)
        private Vector2Int _cell;

        // ---- art ----
        private Transform _beam;             // the rotating boom root
        private Transform _clawL, _clawR;    // claw fingers (open/close)
        private SpriteRenderer _clawLSr, _clawRSr;
        private SpriteRenderer _cargoSr;     // the held item shown under the claw
        private LineRenderer _cable;         // mast-top → boom-tip tie cable
        private Vector3 _mastTop;            // world offset of the mast top (cable anchor)
        private GameObject _inPort, _outPort; // grab/drop side markers (rebuilt on SetDir)
        private const float BoomLen = 0.72f; // boom tip distance from the pivot (local +Y)
        // dust puffs on grab/drop
        private const int PuffCount = 4;
        private SpriteRenderer[] _puffs;
        private Vector3[] _puffVel;
        private float[] _puffAge;
        private int _puffNext;

        // ---- IPowerConsumer (Gantry tier): unwired = half speed, wired = full. ----
        public int PowerDraw => def != null ? def.powerDraw : 0;
        public float CurrentDraw
        {
            get
            {
                if (PowerDraw <= 0 || !PowerNet.Active) return 0f;
                return (heldCount > 0 || _phase == Phase.ToSource) ? PowerDraw : PowerDraw * 0.1f;
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
            arm.RebuildPortMarkers();

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

        public void SetDir(Belt.Dir d)
        {
            dir = d;
            RebuildPortMarkers();
            _phase = Phase.ToSource; _swing = 0f; // re-orient the swing to the new axis
        }

        // The grab/drop side markers on the placed crane — the same cyan-notch / green-arrow language
        // every other building uses, so which way it works is readable at a glance.
        private void RebuildPortMarkers()
        {
            if (_inPort != null) Destroy(_inPort);
            if (_outPort != null) Destroy(_outPort);
            _inPort = Ports.MakeInputNotch(transform, Belt.Opposite(dir)).gameObject;
            _outPort = Ports.MakeOutputArrow(transform, dir).gameObject;
        }

        // =====================================================================================
        //  ART — a small industrial crane built from procedural sprites: shadow, base plate,
        //  mast, counterweighted LATTICE boom with a tie cable, and a claw that opens/closes.
        // =====================================================================================
        private void BuildArt()
        {
            var tint = def.color;
            var dark = Color.Lerp(tint, Color.black, 0.45f);
            var steel = Color.Lerp(tint, Color.black, 0.25f);

            // Soft ground shadow under the base.
            MakeSprite("Shadow", transform, new Vector3(0.05f, -0.07f, 0f), new Vector3(0.74f, 0.5f, 1f),
                new Color(0f, 0f, 0f, 0.30f), 4);
            // Base plate + a slightly smaller cap (reads as a rotating turntable).
            MakeSprite("Base", transform, Vector3.zero, new Vector3(0.6f, 0.6f, 1f), dark, 6);
            MakeSprite("BaseCap", transform, Vector3.zero, new Vector3(0.42f, 0.42f, 1f), steel, 6);
            // Mast — the short tower the cable hangs from.
            MakeSprite("Mast", transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.10f, 0.26f, 1f),
                Color.Lerp(tint, Color.white, 0.15f), 8);
            _mastTop = new Vector3(0f, 0.17f, 0f);

            // The rotating boom root.
            var beamGo = new GameObject("Boom");
            beamGo.transform.SetParent(transform, false);
            _beam = beamGo.transform;

            // Counterweight behind the pivot.
            MakeSprite("Counterweight", _beam, new Vector3(0f, -0.20f, 0f), new Vector3(0.22f, 0.15f, 1f), dark, 7);
            // Lattice boom: three chord segments with gaps + a thin top rail, so it reads as girder work.
            for (int i = 0; i < 3; i++)
                MakeSprite($"BoomSeg{i}", _beam, new Vector3(0f, 0.14f + i * 0.22f, 0f), new Vector3(0.13f, 0.17f, 1f), tint, 7);
            MakeSprite("BoomRail", _beam, new Vector3(0f, 0.36f, 0f), new Vector3(0.045f, 0.62f, 1f),
                Color.Lerp(tint, Color.white, 0.25f), 7);

            // Claw: two fingers that OPEN while waiting to grab and CLOSE around cargo.
            _clawL = MakeSprite("ClawL", _beam, new Vector3(-0.055f, BoomLen, 0f), new Vector3(0.05f, 0.15f, 1f), steel, 8, out _clawLSr).transform;
            _clawR = MakeSprite("ClawR", _beam, new Vector3(0.055f, BoomLen, 0f), new Vector3(0.05f, 0.15f, 1f), steel, 8, out _clawRSr).transform;

            // Cargo — drawn just under the claw while carrying.
            var cargo = new GameObject("Cargo");
            cargo.transform.SetParent(_beam, false);
            cargo.transform.localPosition = new Vector3(0f, BoomLen - 0.12f, 0f);
            _cargoSr = cargo.AddComponent<SpriteRenderer>();
            _cargoSr.sortingOrder = 9;
            _cargoSr.enabled = false;

            // Tie cable from the mast top to the boom tip.
            var cableGo = new GameObject("Cable");
            cableGo.transform.SetParent(transform, false);
            _cable = cableGo.AddComponent<LineRenderer>();
            _cable.material = PlaceholderArt.LineMaterial();
            _cable.widthMultiplier = 0.03f;
            _cable.positionCount = 2;
            _cable.useWorldSpace = true;
            _cable.sortingOrder = 7;
            _cable.startColor = _cable.endColor = new Color(0.16f, 0.16f, 0.18f, 0.9f);

            // Dust puff pool for grab/drop moments.
            _puffs = new SpriteRenderer[PuffCount];
            _puffVel = new Vector3[PuffCount];
            _puffAge = new float[PuffCount];
            for (int i = 0; i < PuffCount; i++)
            {
                var p = new GameObject("puff");
                p.transform.SetParent(transform, false);
                var sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = PlaceholderArt.Square();
                sr.sortingOrder = 10;
                sr.enabled = false;
                _puffs[i] = sr;
                _puffAge[i] = 99f;
            }
        }

        private GameObject MakeSprite(string n, Transform parent, Vector3 lp, Vector3 ls, Color c, int order)
            => MakeSprite(n, parent, lp, ls, c, order, out _);
        private GameObject MakeSprite(string n, Transform parent, Vector3 lp, Vector3 ls, Color c, int order, out SpriteRenderer sr)
        {
            var go = new GameObject(n);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = lp;
            go.transform.localScale = ls;
            sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = c;
            sr.sortingOrder = order;
            return go;
        }

        private void Puff(Vector3 world)
        {
            for (int k = 0; k < 2; k++)
            {
                int i = _puffNext; _puffNext = (_puffNext + 1) % PuffCount;
                _puffs[i].transform.position = world;
                _puffs[i].transform.localScale = Vector3.one * 0.08f;
                _puffs[i].color = new Color(0.75f, 0.72f, 0.65f, 0.55f);
                _puffs[i].enabled = true;
                _puffVel[i] = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.15f, 0.4f), 0f);
                _puffAge[i] = 0f;
            }
        }

        // =====================================================================================
        //  SIM — the swing cycle. Each HALF-swing takes cycleTime/2, so a full unobstructed
        //  round trip moves grabSize items per cycleTime (the tier's rated throughput).
        // =====================================================================================
        void Update()
        {
            // Powered tier: unwired/dead grid = HALF speed (never a hard stop) — the drill rule.
            float pw = (PowerDraw > 0 && PowerNet.Active) ? 0.5f + 0.5f * PowerNet.FactorOf(this) : 1f;
            float dt = Time.deltaTime * pw;
            float half = Mathf.Max(0.15f, cycleTime * 0.5f);

            switch (_phase)
            {
                case Phase.ToSource:
                    _swing += dt / half;
                    if (_swing >= 1f) { _swing = 1f; _phase = Phase.AtSource; _waitT = 0f; _poll = 99f; }
                    break;

                case Phase.AtSource: // parked over the pickup side, claw open — WAITING for goods
                    _waitT += dt; _poll += dt;
                    if (_poll >= 0.2f)
                    {
                        _poll = 0f;
                        TryGrab();
                        if (heldCount > 0) // got a stack (partial is fine — keep the line moving)
                        {
                            Puff(BoomTipWorld());
                            _phase = Phase.ToSink; _swing = 0f; _waitT = 0f;
                        }
                    }
                    break;

                case Phase.ToSink: // carrying — the cargo rides the claw across
                    _swing += dt / half;
                    if (_swing >= 1f) { _swing = 1f; _phase = Phase.AtSink; _waitT = 0f; _poll = 99f; }
                    break;

                case Phase.AtSink: // over the drop side; if it's full we WAIT here, visibly blocked
                    _waitT += dt; _poll += dt;
                    if (_poll >= 0.25f)
                    {
                        _poll = 0f;
                        int before = heldCount;
                        TryDeposit();
                        if (heldCount < before) Puff(BoomTipWorld());
                        if (heldCount <= 0) { held = null; heldCount = 0; _phase = Phase.ToSource; _swing = 0f; _waitT = 0f; }
                    }
                    break;
            }

            AnimateBeam();
            AnimatePuffs(Time.deltaTime);
        }

        private Vector3 BoomTipWorld() => _beam != null ? _beam.TransformPoint(new Vector3(0f, BoomLen, 0f)) : transform.position;

        // Smooth, eased swing between the two parked angles; claw opens/closes; the cargo icon rides
        // the claw; waiting-blocked pulses the claw red-amber so a full sink reads instantly.
        private void AnimateBeam()
        {
            if (_beam == null) return;
            float frontZ = Belt.Angle(dir);
            float backZ = frontZ + 180f;

            float k; // 0 = parked at source, 1 = parked at sink
            switch (_phase)
            {
                case Phase.ToSource: k = 1f - Mathf.SmoothStep(0f, 1f, _swing); break;
                case Phase.AtSource: k = 0f; break;
                case Phase.ToSink: k = Mathf.SmoothStep(0f, 1f, _swing); break;
                default: k = 1f; break;
            }
            float z = Mathf.LerpAngle(backZ, frontZ, k);
            // A gentle idle bob while parked, so a waiting crane reads alive (and "waiting"), not frozen.
            if (_phase == Phase.AtSource || _phase == Phase.AtSink)
                z += Mathf.Sin(Time.time * 2.2f) * 1.6f;
            _beam.localRotation = Quaternion.Euler(0f, 0f, z);

            // Claw fingers: open wide while waiting to grab, closed around cargo, half-open otherwise.
            float open = _phase == Phase.AtSource ? 24f : heldCount > 0 ? 4f : 14f;
            if (_clawL != null) _clawL.localRotation = Quaternion.Euler(0f, 0f, +open);
            if (_clawR != null) _clawR.localRotation = Quaternion.Euler(0f, 0f, -open);

            // Blocked cue: stuck at the sink with cargo → claw + cargo pulse red-amber.
            bool blocked = _phase == Phase.AtSink && heldCount > 0 && _waitT > 0.6f;
            var clawCol = blocked
                ? Color.Lerp(new Color(0.95f, 0.35f, 0.2f), new Color(0.95f, 0.7f, 0.2f), 0.5f + 0.5f * Mathf.Sin(Time.time * 7f))
                : Color.Lerp(def != null ? def.color : Color.gray, Color.black, 0.25f);
            if (_clawLSr != null) _clawLSr.color = clawCol;
            if (_clawRSr != null) _clawRSr.color = clawCol;

            // Cargo in the claw.
            if (_cargoSr != null)
            {
                _cargoSr.enabled = held != null && heldCount > 0;
                if (_cargoSr.enabled)
                {
                    _cargoSr.sprite = held.icon != null ? held.icon : PlaceholderArt.Square();
                    _cargoSr.color = held.icon != null ? Color.white : held.color;
                    float s = 0.22f + 0.028f * Mathf.Min(5, heldCount);
                    _cargoSr.transform.localScale = new Vector3(s, s, 1f);
                }
            }

            // Tie cable follows the boom tip.
            if (_cable != null)
            {
                _cable.SetPosition(0, transform.position + _mastTop);
                _cable.SetPosition(1, BoomTipWorld());
            }
        }

        private void AnimatePuffs(float dt)
        {
            if (_puffs == null) return;
            for (int i = 0; i < PuffCount; i++)
            {
                if (!_puffs[i].enabled) continue;
                _puffAge[i] += dt;
                if (_puffAge[i] >= 0.5f) { _puffs[i].enabled = false; continue; }
                float t = _puffAge[i] / 0.5f;
                _puffs[i].transform.position += _puffVel[i] * dt;
                _puffs[i].transform.localScale = Vector3.one * Mathf.Lerp(0.08f, 0.2f, t);
                var c = _puffs[i].color; c.a = 0.55f * (1f - t); _puffs[i].color = c;
            }
        }

        // =====================================================================================
        //  TRANSFER — grab/deposit reuse the same seam rules as belts, so nothing new can leak.
        // =====================================================================================
        private bool Accepts(ItemDefinition it)
        {
            if (it == null || it.isLiquid) return false;
            if (held != null && heldCount > 0 && it != held) return false;
            if (HasFilterUI && filterItems.Count > 0 && !filterItems.Contains(it)) return false;
            return true;
        }

        private void Hold(ItemDefinition it, int n) { held = it; heldCount += n; }

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

        private void TryDeposit()
        {
            for (int d = 1; d <= reach && heldCount > 0; d++)
            {
                var c = _cell + Belt.Step(dir) * d;
                var b = Belt.At(c);
                if (b != null)
                {
                    var entry = Belt.FromTo(c, _cell); // the item enters the belt from the edge facing the crane
                    while (heldCount > 0 && b.CanAccept(held) && b.ReceiveItem(held, entry, 0f)) heldCount--;
                    continue;
                }
                DepositToBuildings(c);
            }
            if (heldCount <= 0) { held = null; heldCount = 0; }
        }

        private void DepositToBuildings(Vector2Int c)
        {
            if (heldCount <= 0) return;
            if (WorldGrid.Storages.TryGetValue(c, out var s) && s != null && s.def != null
                && (s.accepts == held || (s.accepts == null && s.configurable && s.CanAdopt(held))))
            {
                if (s.accepts == null) s.accepts = held;
                while (heldCount > 0 && s.Store.Total() < s.def.capacity && s.Store.Add(held, 1) > 0) heldCount--;
            }
            if (heldCount > 0 && WorldGrid.Workshops.TryGetValue(c, out var w) && w != null && w.WantsInput(held))
                while (heldCount > 0 && w.CanAcceptBeltInput(held) && w.InBuffer.Add(held, 1) > 0) heldCount--;
            if (heldCount > 0 && WorldGrid.Research.TryGetValue(c, out var rb) && rb != null && rb.Accepts(held))
                while (heldCount > 0 && rb.InBuffer.Add(held, 1) > 0) heldCount--;
            if (heldCount > 0 && WorldGrid.Generators.TryGetValue(c, out var g) && g != null && g.fuel == held)
                while (heldCount > 0 && g.Buffer.Add(held, 1) > 0) heldCount--;
            if (heldCount > 0 && WorldGrid.Depots.TryGetValue(c, out var dp) && dp != null && dp.def != null
                && !dp.def.isLiquidHarbour && (dp.item == held || dp.item == null)) // solid cargo only
            {
                if (dp.item == null) dp.item = held;
                while (heldCount > 0 && dp.store.Total() < dp.def.capacity && dp.store.Add(held, 1) > 0) heldCount--;
            }
        }
    }
}
