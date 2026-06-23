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

        private enum State { ToA, ToB }
        private State _state = State.ToA;
        private int _carry;
        private ItemDefinition _carryItem;
        private SpriteRenderer _sr;
        private Color _baseColor;
        private LineRenderer _line;

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
            // Make the destination handle the same resource the source provides.
            if (b.item == null) b.item = a.item;
            return v;
        }

        void Update()
        {
            if (a == null || b == null) { Destroy(gameObject); return; }
            _line.SetPosition(0, a.transform.position);
            _line.SetPosition(1, b.transform.position);

            if (_state == State.ToA)
            {
                if (MoveTo(a.transform.position))
                {
                    if (a.item != null)
                    {
                        int got = a.store.RemoveUpTo(a.item, capacity);
                        if (got > 0) { _carry = got; _carryItem = a.item; }
                    }
                    _state = State.ToB;
                }
            }
            else
            {
                if (MoveTo(b.transform.position))
                {
                    if (_carry > 0 && _carryItem != null)
                    {
                        if (b.item == null) b.item = _carryItem;
                        int accepted = b.store.Add(_carryItem, _carry);
                        _carry -= accepted; // if full, keep the remainder and try again next trip
                    }
                    if (_carry <= 0) { _carryItem = null; _state = State.ToA; }
                    else _state = State.ToA; // partially delivered; head back to pick up more anyway
                }
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
