using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A long-distance carrier (elephant → cart → train) that shuttles goods between
    /// two depots: loads the source depot's resource, travels to the destination, and
    /// unloads. Travel time scales with distance, so where you place depots matters.
    /// </summary>
    public class RouteVehicle : MonoBehaviour
    {
        public Depot a, b;
        public int capacity = 10;
        public float speed = 3.5f;

        public static readonly List<RouteVehicle> All = new();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        // A trip is a real cycle: travel to source → LOAD (takes time) → travel to dest → UNLOAD
        // (takes time). So throughput is gated by distance + handling, not instant teleport — which
        // is what makes multiple vehicles / multiple routes worthwhile.
        private enum State { ToSource, Loading, ToDest, Unloading }
        private State _state = State.ToSource;
        public float loadTime = 1.5f, unloadTime = 1.5f;
        private float _phaseTimer;
        private int _carry;
        private ItemDefinition _carryItem;
        private SpriteRenderer _sr;
        private Color _baseColor;
        private LineRenderer _line;
        private Transform _arrow; // a static marker at the route midpoint showing FROM → TO direction

        public static RouteVehicle Spawn(Depot a, Depot b, int capacity, float speed, Color color)
        {
            var go = new GameObject("Caravan");
            go.transform.position = a.transform.position;
            go.transform.localScale = Vector3.one * 0.6f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Circle();
            sr.sortingOrder = 14;

            var lr = go.AddComponent<LineRenderer>();
            var sh = Shader.Find("Sprites/Default");
            if (sh != null) lr.material = new Material(sh);
            lr.widthMultiplier = 0.14f;
            lr.numCapVertices = 2;
            lr.startColor = lr.endColor = new Color(0.8f, 0.72f, 0.5f, 0.3f);
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.sortingOrder = 0;

            var v = go.AddComponent<RouteVehicle>();
            v.a = a; v.b = b; v.capacity = Mathf.Max(1, capacity); v.speed = speed;
            v._sr = sr; v._baseColor = color; v._line = lr;
            sr.color = color;

            // Direction arrow at the route midpoint, so FROM → TO is readable at a glance.
            var arrowGo = new GameObject("RouteArrow");
            var asr = arrowGo.AddComponent<SpriteRenderer>();
            asr.sprite = PlaceholderArt.Triangle();
            asr.color = new Color(0.96f, 0.86f, 0.45f, 0.85f);
            asr.sortingOrder = 1;
            v._arrow = arrowGo.transform;

            // Make the destination handle the same resource the source provides.
            if (b.item == null) b.item = a.item;
            return v;
        }

        void OnDestroy() { if (_arrow != null) Destroy(_arrow.gameObject); }

        /// <summary>Upgrade this route's vehicle in place (donkey cart → … → train) without
        /// rebuilding the route — the depots/endpoints and current cargo are kept.</summary>
        public void SetTier(int newCapacity, float newSpeed, Color newColor)
        {
            capacity = Mathf.Max(1, newCapacity);
            speed = newSpeed;
            _baseColor = newColor;
            if (_sr != null && _carry <= 0) _sr.color = newColor;
        }

        void Update()
        {
            if (a == null || b == null) { Destroy(gameObject); return; }
            _line.SetPosition(0, a.transform.position);
            _line.SetPosition(1, b.transform.position);
            if (_arrow != null)
            {
                Vector3 pa = a.transform.position, pb = b.transform.position;
                Vector3 d = pb - pa;
                _arrow.position = new Vector3((pa.x + pb.x) * 0.5f, (pa.y + pb.y) * 0.5f, 0f);
                _arrow.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
                _arrow.localScale = Vector3.one * 0.5f;
            }

            switch (_state)
            {
                case State.ToSource:
                    if (MoveTo(a.transform.position)) { _phaseTimer = 0f; _state = State.Loading; }
                    break;

                case State.Loading:
                    if (_carry > 0) { _state = State.ToDest; break; } // carrying leftovers → just deliver
                    _phaseTimer += Time.deltaTime;
                    if (_phaseTimer >= loadTime)
                    {
                        _phaseTimer = 0f;
                        if (a.item != null)
                        {
                            int got = a.store.RemoveUpTo(a.item, capacity);
                            if (got > 0) { _carry = got; _carryItem = a.item; _state = State.ToDest; }
                            // else: nothing to load — idle at the source and retry next load cycle
                        }
                    }
                    break;

                case State.ToDest:
                    if (MoveTo(b.transform.position)) { _phaseTimer = 0f; _state = State.Unloading; }
                    break;

                case State.Unloading:
                    _phaseTimer += Time.deltaTime;
                    if (_phaseTimer >= unloadTime)
                    {
                        _phaseTimer = 0f;
                        if (_carry > 0 && _carryItem != null)
                        {
                            if (b.item == null) b.item = _carryItem;
                            _carry -= b.store.Add(_carryItem, _carry); // keep remainder if dest is full
                        }
                        if (_carry <= 0) _carryItem = null;
                        _state = State.ToSource; // head back (deliver any remainder on the next loop)
                    }
                    break;
            }

            if (_sr != null)
                _sr.color = (_carry > 0 && _carryItem != null)
                    ? Color.Lerp(_baseColor, _carryItem.color, 0.6f) : _baseColor;
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
