using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A long-distance carrier (donkey cart → wagon → train → drone) that serves a LINE: an ordered list
    /// of Stations it visits in a loop. The loco pulls a CONSIST of WAGONS — one commodity per wagon — so a
    /// single line can haul MIXED cargo: each stop unloads the wagons carrying ITS resource then loads its own
    /// surplus into a free wagon, picking goods up where they're made and dropping them where they're wanted.
    /// Wagon count is AGE-GATED (donkey pulls 1, diesel pulls 5). Between stops it follows the laid TRACK
    /// (claiming each cell so trains never cross, obeying SIGNALS for one-way + block control), passing any
    /// stations not on its line — or flies straight if no track connects them. A ship is a single-hold variant.
    /// </summary>
    public class RouteVehicle : MonoBehaviour
    {
        public List<Depot> stops = new(); // the line, in visit order (loops back to stops[0])
        public int capacity = 10;          // per-WAGON capacity (total haul = capacity × wagon count)
        public float speed = 3.5f;

        // First stop + membership test.
        public Depot a => (stops != null && stops.Count > 0) ? stops[0] : null;
        public bool Serves(Depot d) => d != null && stops != null && stops.Contains(d);
        public int StopCount => stops != null ? stops.Count : 0;

        // ---- Read-only summary for the global line overview (InventoryHud) ----
        public bool IsShip => _isShip;
        public int WagonCount => _isShip ? 1 : _wagons.Count;
        public int CurrentLoad => TotalCarry();
        public int LoadCapacity => capacity * Mathf.Max(1, _wagons.Count);
        public string VehicleName()
        {
            if (_isShip) return "Cargo ship";
            string[] n = { "Donkey cart", "Ox wagon", "Horse wagon", "Steam train", "Diesel train" };
            return n[Mathf.Clamp(AgeLocoTier(), 0, 4)];
        }
        /// <summary>"Wood 8 · Oil 5" of what's currently loaded across the consist, or "empty".</summary>
        public string CargoSummary()
        {
            var agg = new List<KeyValuePair<ItemDefinition, int>>();
            foreach (var w in _wagons)
            {
                if (w.item == null || w.amount <= 0) continue;
                int i = agg.FindIndex(kv => kv.Key == w.item);
                if (i >= 0) agg[i] = new KeyValuePair<ItemDefinition, int>(w.item, agg[i].Value + w.amount);
                else agg.Add(new KeyValuePair<ItemDefinition, int>(w.item, w.amount));
            }
            if (agg.Count == 0) return "empty";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < agg.Count; i++)
            {
                if (i > 0) sb.Append(" · ");
                string hex = ColorUtility.ToHtmlStringRGB(agg[i].Key.color);
                sb.Append($"<color=#{hex}>{agg[i].Key.displayName}</color> {agg[i].Value}");
            }
            return sb.ToString();
        }

        public static readonly List<RouteVehicle> All = new();
        void OnEnable() => All.Add(this);
        void OnDisable() { All.Remove(this); ReleaseHeld(); DestroyWagons(); DestroyPuffs(); if (_couplerGo != null) Destroy(_couplerGo); }

        private enum Phase { Travel, Service }
        private Phase _phase = Phase.Travel;
        public float serviceTime = 1.5f;
        private float _phaseTimer;
        private int _toIdx;        // index of the stop we're heading to
        private int _legBuilt = -1; // the _toIdx the current rail route was built for
        private SpriteRenderer _sr;
        private Color _baseColor;
        private LineRenderer _line;
        private bool _isShip;                  // ships keep the boat sprite + cargo tint; land vehicles show a per-age loco
        private int _frame; private float _animT; private float _prevX; // loco walk/wheel animation + facing flip
        private static int AgeLocoTier() => Mathf.Clamp(Colony.Instance != null ? Colony.Instance.Age : 0, 0, 4);

        // ---- Consist: the loco pulls a list of WAGONS, each holding ONE commodity. A line therefore hauls
        // MIXED cargo (a wagon per commodity), unlike the old single-hold caravan. Count is age-gated
        // (WagonsForAge); each wagon holds up to `capacity`. A ship is one hold (one wagon, no trailing art).
        private sealed class Wagon
        {
            public ItemDefinition item; // commodity carried (null = empty)
            public int amount;
            public Transform tf;        // trailing sprite (world-space, unparented so the loco's flip doesn't distort it)
            public SpriteRenderer sr;
            public int frame; public float animT;
        }
        private readonly List<Wagon> _wagons = new();
        private int _wagonTier = -1;                    // loco/age tier the consist was last sized for (rebuild on age-up)
        private readonly List<Vector3> _trail = new();  // recent loco positions (index 0 = newest) for wagon following

        // Cosmetic only: a thin dark spine drawn through the cars (visible in the GAPS → reads as couplings),
        // and a small pool of exhaust puffs kicked out behind a MOVING loco (white steam at tier 3, dark diesel
        // exhaust at 4, light dust for the draft animals). No gameplay effect.
        private LineRenderer _coupler; private GameObject _couplerGo;
        private const int PuffCount = 8;
        private SpriteRenderer[] _puffs;
        private Vector3[] _puffVel; private Color[] _puffCol; private float[] _puffLife, _puffAge;
        private int _puffNext; private float _puffTimer;

        // Track following (see CanEnter/BlockAheadClear). Each leg is a fresh forward path from the
        // vehicle's current position to the next stop, traversed cell-by-cell with claims + signals.
        private List<Vector2Int> _railCells;
        private int _cellIdx;
        private const int Step = 1; // legs are always traversed forward (the path runs from→to)
        private bool _railDone;
        private bool _waiting;
        private float _noRouteToastT = -99f; // rate-limit the "no track route" warning
        private static readonly Vector2Int NoCell = new Vector2Int(int.MinValue, int.MinValue);
        private Vector2Int _curCell = NoCell, _claimNext = NoCell;

        public static RouteVehicle Spawn(Depot a, Depot b, int capacity, float speed, Color color)
            => Spawn(new List<Depot> { a, b }, capacity, speed, color);

        public static RouteVehicle Spawn(List<Depot> stops, int capacity, float speed, Color color)
        {
            if (stops == null || stops.Count < 2) return null;
            bool ship = stops[0] != null && stops[0].def != null && stops[0].def.isHarbour;
            var go = new GameObject(ship ? "Cargo Ship" : "Caravan");
            go.transform.position = stops[0].transform.position;
            go.transform.localScale = Vector3.one * (ship ? 0.85f : 0.6f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ship ? PlaceholderArt.Boat() : PlaceholderArt.TrainLoco(AgeLocoTier(), 0);
            sr.sortingOrder = 14;

            var lr = go.AddComponent<LineRenderer>();
            lr.material = PlaceholderArt.LineMaterial(); // shared (no per-vehicle Material leak)
            lr.widthMultiplier = 0.14f;
            lr.numCapVertices = 2;
            lr.startColor = lr.endColor = new Color(0.8f, 0.72f, 0.5f, 0.3f);
            lr.useWorldSpace = true;
            lr.sortingOrder = 0;
            lr.enabled = false; // the straight station-to-station line was ugly + redundant (the train follows the track)

            var v = go.AddComponent<RouteVehicle>();
            v.stops = new List<Depot>(stops);
            v.capacity = Mathf.Max(1, capacity); v.speed = speed;
            v._sr = sr; v._baseColor = color; v._line = lr; v._isShip = ship;
            sr.color = ship ? color : Color.white;

            // Build the consist now that ship/capacity are known. Stops KEEP their own items — a line carries
            // mixed cargo, so we no longer force every stop onto one shared commodity (each is set by its belts
            // or its panel; an unconfigured drop-off adopts the first cargo delivered to it, see ServiceStop).
            v.RebuildConsist();
            if (!ship) v.CreateCoupler();
            return v;
        }

        /// <summary>Upgrade this route's vehicle in place (donkey cart → … → train) — stops + cargo kept.</summary>
        public void SetTier(int newCapacity, float newSpeed, Color newColor)
        {
            capacity = Mathf.Max(1, newCapacity);
            speed = newSpeed;
            _baseColor = newColor;
            if (_sr != null && TotalCarry() <= 0) _sr.color = newColor;
        }

        void Update()
        {
            // Drop destroyed stops; a line needs at least two to run.
            if (stops != null) stops.RemoveAll(s => s == null);
            if (stops == null || stops.Count < 2) { Destroy(gameObject); return; }
            if (_toIdx >= stops.Count) _toIdx = 0;

            EnsureConsistForAge(); // grow the consist as the colony ages up (donkey → … → diesel)
            DrawLine();
            var to = stops[_toIdx];

            switch (_phase)
            {
                case Phase.Travel:
                    if (TravelToStop(to)) { _phase = Phase.Service; _phaseTimer = 0f; _legBuilt = -1; }
                    break;
                case Phase.Service:
                    _phaseTimer += Time.deltaTime;
                    if (_phaseTimer >= serviceTime)
                    {
                        ServiceStop(to); // unload this stop's cargo, then load its surplus into a free wagon
                        _toIdx = (_toIdx + 1) % stops.Count;
                        _phase = Phase.Travel;
                    }
                    break;
            }

            if (_sr != null)
            {
                if (!_isShip)
                {
                    // animate the loco (legs / wheels) + face the way it's travelling (flip x)
                    float dx = transform.position.x - _prevX; _prevX = transform.position.x;
                    if (Mathf.Abs(dx) > 0.0006f) { var sc = transform.localScale; sc.x = Mathf.Abs(sc.x) * (dx < 0f ? -1f : 1f); transform.localScale = sc; }
                    _animT += Time.deltaTime;
                    if (_animT >= 0.12f) { _animT = 0f; _frame = (_frame + 1) % 3; }
                    _sr.sprite = PlaceholderArt.TrainLoco(AgeLocoTier(), _frame);
                }
                var dom = DominantItem();
                _sr.color = _waiting ? new Color(0.95f, 0.75f, 0.25f) // amber — held at a signal / occupied cell
                    : _isShip ? ((TotalCarry() > 0 && dom != null) ? Color.Lerp(_baseColor, dom.color, 0.6f) : _baseColor)
                    : Color.white; // land loco shows its own colours
            }

            UpdateWagons(); // trail the wagons behind the loco + tint each to its commodity
        }

        // Per-stop load/unload mode: 0 = load + unload (default), 1 = load only, 2 = unload only. Keyed by station,
        // so it survives reordering; missing = default. Lets a stop be a pure SOURCE or a pure SINK.
        public readonly Dictionary<Depot, int> stopMode = new();
        public int StopModeOf(Depot d) => d != null && stopMode.TryGetValue(d, out var m) ? m : 0;
        public void CycleStopMode(Depot d) { if (d != null) stopMode[d] = (StopModeOf(d) + 1) % 3; }

        /// <summary>A plain-language problem with this line's stop MODES ("" = fine), shown in the Lines panel —
        /// catches the silent "my line does nothing" setups: every stop unload-only (nothing is ever loaded, so
        /// the consist runs empty) or every stop load-only (nothing is ever delivered).</summary>
        public string LineWarning()
        {
            if (stops == null || stops.Count < 2) return "";
            bool anyLoad = false, anyUnload = false;
            foreach (var s in stops)
            {
                if (s == null) continue;
                int m = StopModeOf(s);
                if (m != 1) anyUnload = true; // mode 0 or 2 can unload
                if (m != 2) anyLoad = true;   // mode 0 or 1 can load
            }
            if (!anyLoad) return "no stop LOADS — every stop is unload-only, so the vehicle runs empty. Set a source stop to Load.";
            if (!anyUnload) return "no stop UNLOADS — every stop is load-only, so cargo is never delivered. Set a destination to Unload.";
            return "";
        }

        // Service a stop. The consist UNLOADS every wagon carrying this stop's resource (delivering into its
        // store), then LOADS this stop's surplus into a free/matching wagon — unless we just delivered that
        // resource here (so a destination doesn't immediately re-export what it received). Each stop thus acts
        // as a source for what it makes and a sink for what it wants; mixed commodities ride together between.
        private void ServiceStop(Depot stop)
        {
            if (stop == null) return;
            int mode = StopModeOf(stop);                       // 0 = load+unload, 1 = load only, 2 = unload only
            bool doUnload = mode != 1, doLoad = mode != 2;

            // 1) UNLOAD — deliver wagons into the stop. An unconfigured stop adopts the first cargo delivered.
            bool unloadedHere = false; ItemDefinition delivered = null; int deliveredTotal = 0;
            if (doUnload)
            foreach (var w in _wagons)
            {
                if (w.item == null || w.amount <= 0) continue;
                if (stop.item == null) stop.item = w.item;          // a fresh drop-off adopts what's delivered
                if (w.item != stop.item) continue;
                int moved = stop.store.Add(w.item, w.amount);
                if (moved > 0) { w.amount -= moved; unloadedHere = true; delivered = w.item; deliveredTotal += moved; if (w.item != null && w.item.isLiquid) stop.trainFedAt = Time.time; if (w.amount <= 0) w.item = null; }
            }
            // A floating "+N" at the stop so a working line reads as productive (reuses the hand-gather popup).
            if (deliveredTotal > 0 && delivered != null)
                GatherPopup.Show(stop.transform.position, $"+{deliveredTotal} {delivered.displayName}", delivered.color);

            // 2) LOAD — pull this stop's surplus into empty (or same-commodity) wagons, up to per-wagon capacity.
            var I = stop.item;
            if (doLoad && I != null && I != delivered && stop.store.Count(I) > 0)
            {
                foreach (var w in _wagons)
                {
                    if (stop.store.Count(I) <= 0) break;
                    if (w.item != null && w.item != I) continue;          // wagon dedicated to another commodity
                    if (w.item == I && w.amount >= capacity) continue;    // this wagon's full
                    if (w.item == null) w.item = I;
                    int room = capacity - w.amount;
                    if (room > 0) w.amount += stop.store.RemoveUpTo(I, room);
                }
            }
        }

        // The straight station-to-station route line was ugly and redundant (the train follows the track),
        // so it's kept disabled. Left as a no-op hook in case we add a selected-only route overlay later.
        private void DrawLine()
        {
            if (_line != null && _line.enabled) _line.enabled = false;
        }

        // ---- Consist sizing + cargo helpers ------------------------------------------------------
        // Age-gated wagon count: Donkey(0):1 · Ox(1):2 · Horse(2):3 · Steam(3):4 · Diesel(4):5. The consist
        // grows as the colony advances, so the same line hauls more (and more varied) cargo in later ages.
        private static int WagonsForAge(int age) => Mathf.Clamp(age, 0, 4) + 1;

        private void RebuildConsist()
        {
            int want = _isShip ? 1 : WagonsForAge(Colony.Instance != null ? Colony.Instance.Age : 0);
            EnsureWagonCount(want);
            _wagonTier = AgeLocoTier();
        }

        private void EnsureConsistForAge()
        {
            if (_isShip) return;
            int t = AgeLocoTier();
            if (t == _wagonTier) return;
            _wagonTier = t;
            EnsureWagonCount(WagonsForAge(t));
        }

        // Grow the consist to `want` wagons (creating trailing sprites for land vehicles); on the rare shrink,
        // drop only EMPTY wagons from the tail so cargo is never silently destroyed.
        private void EnsureWagonCount(int want)
        {
            want = Mathf.Max(1, want);
            while (_wagons.Count < want)
            {
                var w = new Wagon();
                if (!_isShip)
                {
                    var go = new GameObject("Wagon");
                    go.transform.position = transform.position;
                    go.transform.localScale = Vector3.one * 0.55f;
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = PlaceholderArt.CargoWagon(0);
                    sr.sortingOrder = 13; // just under the loco (14)
                    sr.color = new Color(0.78f, 0.78f, 0.78f, 1f);
                    w.tf = go.transform; w.sr = sr;
                }
                _wagons.Add(w);
            }
            for (int i = _wagons.Count - 1; i >= want; i--)
            {
                if (_wagons[i].amount > 0) break; // keep extra wagons until they've emptied (don't lose cargo)
                if (_wagons[i].tf != null) Destroy(_wagons[i].tf.gameObject);
                _wagons.RemoveAt(i);
            }
        }

        private void DestroyWagons()
        {
            foreach (var w in _wagons) if (w.tf != null) Destroy(w.tf.gameObject);
            _wagons.Clear();
        }

        private int TotalCarry()
        {
            int t = 0; foreach (var w in _wagons) t += w.amount; return t;
        }

        // The commodity the consist is carrying most of (for the loco/ship tint), or null if empty.
        private ItemDefinition DominantItem()
        {
            ItemDefinition best = null; int bestAmt = 0;
            foreach (var w in _wagons) if (w.item != null && w.amount > bestAmt) { best = w.item; bestAmt = w.amount; }
            return best;
        }

        // Trail the wagons a fixed arc-distance behind the loco along its recent path, animate wheels, face the
        // travel direction, and tint each to its commodity (a liquid wagon shows the tanker art).
        private void UpdateWagons()
        {
            if (_trail.Count == 0 || (_trail[0] - transform.position).sqrMagnitude > 0.0036f) // ~0.06 world units
                _trail.Insert(0, transform.position);
            const int maxTrail = 96;
            if (_trail.Count > maxTrail) _trail.RemoveRange(maxTrail, _trail.Count - maxTrail);

            if (_isShip) return;
            for (int i = 0; i < _wagons.Count; i++)
            {
                var w = _wagons[i];
                if (w.tf == null) continue;
                SampleTrail(0.5f + i * 0.5f, out var pos, out float dirX);
                w.tf.position = pos;
                w.animT += Time.deltaTime;
                if (w.animT >= 0.12f) { w.animT = 0f; w.frame = (w.frame + 1) % 3; }
                bool liquid = w.item != null && w.item.isLiquid;
                w.sr.sprite = liquid ? PlaceholderArt.LiquidWagon(w.frame) : PlaceholderArt.CargoWagon(w.frame);
                var sc = w.tf.localScale; sc.x = Mathf.Abs(sc.x) * (dirX < 0f ? -1f : 1f); w.tf.localScale = sc;
                w.sr.color = w.item != null ? Color.Lerp(Color.white, w.item.color, 0.55f) : new Color(0.78f, 0.78f, 0.78f, 1f);
            }

            // Draw the coupling spine through loco + every wagon (sits UNDER the cars, so it only shows in the gaps).
            if (_coupler != null)
            {
                _coupler.positionCount = 1 + _wagons.Count;
                _coupler.SetPosition(0, transform.position);
                for (int i = 0; i < _wagons.Count; i++)
                    _coupler.SetPosition(i + 1, _wagons[i].tf != null ? _wagons[i].tf.position : transform.position);
            }
            // Exhaust while actually travelling (not stopped to load / held at a signal).
            UpdatePuffs(_phase == Phase.Travel && !_waiting, transform.localScale.x < 0f ? -1f : 1f);
        }

        private void CreateCoupler()
        {
            _couplerGo = new GameObject("Coupler");
            _coupler = _couplerGo.AddComponent<LineRenderer>();
            _coupler.material = PlaceholderArt.LineMaterial(); // shared
            _coupler.useWorldSpace = true;
            _coupler.widthMultiplier = 0.09f;
            _coupler.numCapVertices = 1;
            _coupler.sortingOrder = 12; // below wagons (13) + loco (14) → only the gaps show, like couplings
            _coupler.startColor = _coupler.endColor = new Color(0.16f, 0.16f, 0.18f, 1f);
            _coupler.positionCount = 0;
        }

        private void EnsurePuffs()
        {
            if (_puffs != null) return;
            _puffs = new SpriteRenderer[PuffCount];
            _puffVel = new Vector3[PuffCount]; _puffCol = new Color[PuffCount];
            _puffLife = new float[PuffCount]; _puffAge = new float[PuffCount];
            for (int i = 0; i < PuffCount; i++)
            {
                var go = new GameObject("Exhaust");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = PlaceholderArt.Circle();
                sr.sortingOrder = 15; // above the loco (14) so smoke reads on top
                sr.enabled = false;
                _puffs[i] = sr;
            }
        }

        private void EmitPuff(float travelDirX)
        {
            int tier = AgeLocoTier();
            int i = _puffNext; _puffNext = (_puffNext + 1) % PuffCount;
            var sr = _puffs[i];
            sr.enabled = true;
            float backX = travelDirX < 0f ? 0.18f : -0.18f; // start just behind the loco
            sr.transform.position = transform.position + new Vector3(backX, 0.22f, 0f);
            sr.transform.localScale = Vector3.one * Random.Range(0.10f, 0.15f);
            _puffVel[i] = new Vector3(backX * 1.3f + Random.Range(-0.08f, 0.08f), Random.Range(0.40f, 0.80f), 0f);
            _puffLife[i] = Random.Range(0.5f, 0.9f); _puffAge[i] = 0f;
            _puffCol[i] = tier >= 4 ? new Color(0.36f, 0.36f, 0.40f) // diesel exhaust
                        : tier == 3 ? new Color(0.85f, 0.85f, 0.88f) // steam
                        : new Color(0.80f, 0.74f, 0.60f);            // draft-animal dust
            sr.color = new Color(_puffCol[i].r, _puffCol[i].g, _puffCol[i].b, 0f);
        }

        private void UpdatePuffs(bool moving, float travelDirX)
        {
            if (_isShip) return;
            EnsurePuffs();
            if (moving)
            {
                _puffTimer += Time.deltaTime;
                float interval = AgeLocoTier() >= 3 ? 0.16f : 0.30f; // machines chuff faster than animals kick dust
                if (_puffTimer >= interval) { _puffTimer = 0f; EmitPuff(travelDirX); }
            }
            for (int i = 0; i < PuffCount; i++)
            {
                var sr = _puffs[i];
                if (sr == null || !sr.enabled) continue;
                _puffAge[i] += Time.deltaTime;
                float u = _puffLife[i] > 0f ? _puffAge[i] / _puffLife[i] : 1f;
                if (u >= 1f) { sr.enabled = false; continue; }
                sr.transform.position += _puffVel[i] * Time.deltaTime;
                _puffVel[i] *= Mathf.Max(0f, 1f - Time.deltaTime * 1.2f);
                var c = _puffCol[i]; c.a = (1f - u) * 0.7f; sr.color = c;
                sr.transform.localScale = Vector3.one * Mathf.Lerp(0.10f, 0.26f, u);
            }
        }

        private void DestroyPuffs()
        {
            if (_puffs == null) return;
            foreach (var s in _puffs) if (s != null) Destroy(s.gameObject);
            _puffs = null;
        }

        // Walk back along the trail to a target arc-distance; return that world position + x travel-direction.
        private void SampleTrail(float target, out Vector3 pos, out float dirX)
        {
            pos = transform.position; dirX = 1f;
            if (_trail.Count < 2) return;
            float acc = 0f;
            for (int k = 1; k < _trail.Count; k++)
            {
                Vector3 newer = _trail[k - 1], older = _trail[k];
                float seg = Vector3.Distance(newer, older);
                if (seg <= 1e-5f) continue;
                if (acc + seg >= target)
                {
                    pos = Vector3.Lerp(newer, older, (target - acc) / seg);
                    dirX = newer.x - older.x; // travel direction (older → newer)
                    return;
                }
                acc += seg;
            }
            pos = _trail[_trail.Count - 1];
            dirX = _trail.Count >= 2 ? _trail[_trail.Count - 2].x - _trail[_trail.Count - 1].x : 1f;
        }

        // ---- Track-gated travel to the next stop -------------------------------------------------
        private bool TravelToStop(Depot to)
        {
            if (_legBuilt != _toIdx)
            {
                _legBuilt = _toIdx;
                BuildLegRoute(to);
                _railDone = _railCells == null || _railCells.Count == 0;
                _cellIdx = 0;
                _waiting = false;
            }

            // SHIPS follow the water route cell-by-cell — no rail claims, no signals. With no water route they
            // HOLD (don't fly over land); line-creation prevents that, but be safe if terrain ever changes.
            if (_isShip)
            {
                if (_railCells == null || _railCells.Count == 0) { _waiting = true; return false; }
                _waiting = false;
                var wc = _railCells[_cellIdx];
                if (MoveTo(new Vector3(wc.x, wc.y, 0f)))
                {
                    if (_cellIdx >= _railCells.Count - 1) return true; // reached the dock cell → arrived
                    _cellIdx += Step;
                }
                return false;
            }

            if (_railCells == null || _railCells.Count == 0)
            {
                // No track route to this stop → HOLD (amber), don't teleport over land. The player must lay
                // rail that actually connects the stops. A rate-limited toast tells them why it's parked.
                ReleaseHeld();
                _waiting = true;
                if (Time.unscaledTime - _noRouteToastT > 6f && to != null)
                {
                    _noRouteToastT = Time.unscaledTime;
                    Toast.Show($"<color=#fc8>🚂 No track route to {(to.def != null ? to.def.displayName : "the station")} — lay rail to connect the stops.</color>");
                }
                return false;
            }

            if (_railDone) // all track cells passed → pull into the station, KEEPING its platform cell
            {
                // Hold _curCell through loading so another line's train can't pass through the occupied
                // platform; it's released when this vehicle departs onto the next cell of the next leg.
                return MoveTo(to.transform.position);
            }

            var nc = _railCells[_cellIdx];
            if (nc == _curCell) { AdvanceCell(); return false; }

            // The leg route is built ONCE; if track on it was demolished or unlinked since, the next cell may no
            // longer be rail (or no longer connect from here). Re-path from the current position (BuildLegRoute
            // uses live rail state) instead of gliding over the gap — upholding "trains follow track". If no route
            // remains, the empty-route branch above parks the train amber next frame and prompts for track.
            if (!RailNet.IsRail(nc) || (_curCell != NoCell && !RailNet.Linked(_curCell, nc)))
            {
                _legBuilt = -1; // force a rebuild next frame
                _waiting = true;
                return false;
            }

            if (!CanEnter(_cellIdx)) { _waiting = true; return false; }
            _waiting = false;

            RailGraph.Claim(nc, this); _claimNext = nc;
            if (MoveTo(new Vector3(nc.x, nc.y, 0f)))
            {
                if (_curCell != NoCell) RailGraph.Release(_curCell, this);
                _curCell = nc; _claimNext = NoCell;
                AdvanceCell();
            }
            return false;
        }

        private void AdvanceCell()
        {
            if (_cellIdx >= _railCells.Count - 1) _railDone = true;
            else _cellIdx += Step;
        }

        // May this vehicle enter path cell `idx`? No if another train holds it; at a signal, only when
        // travelling its way (one-way) AND the block ahead (to the next signal) is clear of other trains.
        private bool CanEnter(int idx)
        {
            var nc = _railCells[idx];
            if (RailGraph.OccupiedByOther(nc, this)) return false;
            var sig = Signal.At(nc);
            if (sig != null)
            {
                if (!sig.Allows(TravelDirAt(idx))) return false; // one-way (two-way allows its axis); the path already respects this
                if (!BlockAheadClear(idx)) return false;        // block ahead occupied → wait
            }
            return true;
        }

        private Belt.Dir TravelDirAt(int idx)
        {
            int j = idx + Step;
            if (j >= 0 && j < _railCells.Count) return Belt.FromTo(_railCells[idx], _railCells[j]);
            int k = idx - Step;
            if (k >= 0 && k < _railCells.Count) return Belt.FromTo(_railCells[k], _railCells[idx]);
            return Belt.Dir.E;
        }

        private bool BlockAheadClear(int sigIdx)
        {
            for (int i = sigIdx + Step; i >= 0 && i < _railCells.Count; i += Step)
            {
                if (RailGraph.OccupiedByOther(_railCells[i], this)) return false;
                if (Signal.At(_railCells[i]) != null) return true; // next signal → end of this block
            }
            return true;
        }

        private void ReleaseHeld()
        {
            if (_curCell != NoCell) RailGraph.Release(_curCell, this);
            if (_claimNext != NoCell) RailGraph.Release(_claimNext, this);
            _curCell = NoCell; _claimNext = NoCell;
        }

        // Build the forward route from the vehicle's CURRENT position to `to`. Ships path over WATER (A* on the
        // sea, never the rail graph); land vehicles follow laid TRACK. Null/empty = no route (ship holds; a land
        // vehicle flies straight as before).
        private void BuildLegRoute(Depot to)
        {
            _railCells = null;
            if (to == null) return;
            if (_isShip)
            {
                _railCells = WaterNet.WaterPath(transform.position, to.transform.position); // water-only — never over land
                return;
            }
            var ra = RailNet.RailNear(transform.position);
            var rb = RailNet.RailNear(to.transform.position);
            if (ra == null || rb == null) return;
            var cells = RailNet.FindPath(ra.Value, rb.Value);
            if (cells == null || cells.Count == 0) return;
            _railCells = cells;
        }

        private bool MoveTo(Vector3 t)
        {
            t.z = 0f;
            Vector3 p = transform.position; p.z = 0f;
            transform.position = Vector3.MoveTowards(p, t, speed * Time.deltaTime);
            return (transform.position - t).sqrMagnitude < 0.04f;
        }
    }
}
