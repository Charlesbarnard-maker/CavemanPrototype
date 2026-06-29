using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A long-distance carrier (donkey cart → wagon → train → drone) that serves a LINE: an ordered list
    /// of Stations it visits in a loop. At each stop it unloads matching cargo then loads that stop's goods;
    /// between stops it follows the laid TRACK (claiming each cell so trains never cross, and obeying
    /// SIGNALS for one-way + block control), passing any stations that aren't on its line — or flies
    /// straight if no track connects them. A 2-station line is just the classic A↔B shuttle.
    /// </summary>
    public class RouteVehicle : MonoBehaviour
    {
        public List<Depot> stops = new(); // the line, in visit order (loops back to stops[0])
        public int capacity = 10;
        public float speed = 3.5f;

        // First stop + membership test.
        public Depot a => (stops != null && stops.Count > 0) ? stops[0] : null;
        public bool Serves(Depot d) => d != null && stops != null && stops.Contains(d);
        public int StopCount => stops != null ? stops.Count : 0;

        public static readonly List<RouteVehicle> All = new();
        void OnEnable() => All.Add(this);
        void OnDisable() { All.Remove(this); ReleaseHeld(); }

        private enum Phase { Travel, Service }
        private Phase _phase = Phase.Travel;
        public float serviceTime = 1.5f;
        private float _phaseTimer;
        private int _toIdx;        // index of the stop we're heading to
        private int _legBuilt = -1; // the _toIdx the current rail route was built for
        private int _carry;
        private ItemDefinition _carryItem;
        private SpriteRenderer _sr;
        private Color _baseColor;
        private LineRenderer _line;
        private bool _isShip;                  // ships keep the boat sprite + cargo tint; land vehicles show a per-age loco
        private int _frame; private float _animT; private float _prevX; // loco walk/wheel animation + facing flip
        private static int AgeLocoTier() => Mathf.Clamp(Colony.Instance != null ? Colony.Instance.Age : 0, 0, 4);

        // Track following (see CanEnter/BlockAheadClear). Each leg is a fresh forward path from the
        // vehicle's current position to the next stop, traversed cell-by-cell with claims + signals.
        private List<Vector2Int> _railCells;
        private int _cellIdx;
        private const int Step = 1; // legs are always traversed forward (the path runs from→to)
        private bool _railDone;
        private bool _waiting;
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

            var v = go.AddComponent<RouteVehicle>();
            v.stops = new List<Depot>(stops);
            v.capacity = Mathf.Max(1, capacity); v.speed = speed;
            v._sr = sr; v._baseColor = color; v._line = lr; v._isShip = ship;
            sr.color = ship ? color : Color.white;

            // One commodity per line: give every un-set stop the first stop's item, so loads/unloads match.
            ItemDefinition commodity = null;
            foreach (var s in v.stops) if (s != null && s.item != null) { commodity = s.item; break; }
            if (commodity != null) foreach (var s in v.stops) if (s != null && s.item == null) s.item = commodity;
            return v;
        }

        /// <summary>Upgrade this route's vehicle in place (donkey cart → … → train) — stops + cargo kept.</summary>
        public void SetTier(int newCapacity, float newSpeed, Color newColor)
        {
            capacity = Mathf.Max(1, newCapacity);
            speed = newSpeed;
            _baseColor = newColor;
            if (_sr != null && _carry <= 0) _sr.color = newColor;
        }

        void Update()
        {
            // Drop destroyed stops; a line needs at least two to run.
            if (stops != null) stops.RemoveAll(s => s == null);
            if (stops == null || stops.Count < 2) { Destroy(gameObject); return; }
            if (_toIdx >= stops.Count) _toIdx = 0;

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
                        ServiceStop(to, _toIdx == 0); // stop 0 = the pickup (load); the rest are drop-offs
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
                _sr.color = _waiting ? new Color(0.95f, 0.75f, 0.25f) // amber — held at a signal / occupied cell
                    : _isShip ? ((_carry > 0 && _carryItem != null) ? Color.Lerp(_baseColor, _carryItem.color, 0.6f) : _baseColor)
                    : Color.white; // land loco shows its own colours
            }
        }

        // Service a stop. The PICKUP stop (stop 0) loads its commodity up to capacity; every other stop
        // is a DROP-OFF that receives the cargo (taking what fits; the remainder rides on to the next).
        // This is predictable (no ping-pong) and reads as "start from the source, then list the deliveries".
        private void ServiceStop(Depot stop, bool isSource)
        {
            if (stop == null) return;
            if (isSource)
            {
                if (stop.item != null && (_carryItem == null || _carryItem == stop.item))
                {
                    if (_carryItem == null) _carryItem = stop.item;
                    int room = capacity - _carry;
                    if (room > 0) _carry += stop.store.RemoveUpTo(stop.item, room);
                }
            }
            else if (_carry > 0 && _carryItem != null)
            {
                if (stop.item == null) stop.item = _carryItem;     // an un-set drop-off adopts what's delivered
                if (stop.item == _carryItem)
                {
                    _carry -= stop.store.Add(_carryItem, _carry);  // deliver what fits; keep any overflow for the next stop
                    if (_carry <= 0) _carryItem = null;
                }
            }
        }

        // The persistent route indicator: a faint loop through the stops (the train itself follows track).
        private void DrawLine()
        {
            if (_line == null) return;
            int n = stops.Count;
            _line.positionCount = n + 1;
            for (int i = 0; i < n; i++)
                _line.SetPosition(i, stops[i] != null ? stops[i].transform.position : transform.position);
            _line.SetPosition(n, stops[0] != null ? stops[0].transform.position : transform.position);
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

            if (_railCells == null || _railCells.Count == 0)
            {
                ReleaseHeld(); // flying straight (no track) → don't keep a stale cell claimed
                return MoveTo(to.transform.position);
            }

            if (_railDone) // all track cells passed → pull into the station, KEEPING its platform cell
            {
                // Hold _curCell through loading so another line's train can't pass through the occupied
                // platform; it's released when this vehicle departs onto the next cell of the next leg.
                return MoveTo(to.transform.position);
            }

            var nc = _railCells[_cellIdx];
            if (nc == _curCell) { AdvanceCell(); return false; }

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

        // Build the forward rail route from the vehicle's CURRENT position to `to`, or null if no track.
        private void BuildLegRoute(Depot to)
        {
            _railCells = null;
            if (to == null) return;
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
