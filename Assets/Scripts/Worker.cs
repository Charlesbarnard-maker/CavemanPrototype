using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A little gatherer spawned by a collector. It walks to the building's bound
    /// resource patch, chops it (the patch recoils), carries a load back to the
    /// building's buffer, and repeats. If the buffer is full it waits at home
    /// (backpressure travels all the way to the worker).
    /// </summary>
    public class Worker : MonoBehaviour
    {
        public ProductionBuilding home;
        public float moveSpeed = 3f;
        public float chopTime = 1.5f;
        public int carryAmount = 3;

        private enum State { ToNode, Chopping, ToHome, ToStorage }
        private State _state = State.ToNode;
        private float _chopTimer;
        private float _nudgeTimer;
        private int _carrying;
        private ItemDefinition _carryItem;

        private SpriteRenderer _sr;
        private Color _baseColor;

        public static Worker Spawn(ProductionBuilding home)
        {
            var go = new GameObject(home.name + " Worker");
            go.transform.position = home.transform.position;
            go.transform.localScale = Vector3.one * 0.32f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Circle();
            sr.sortingOrder = 12;

            var w = go.AddComponent<Worker>();
            w.home = home;
            w.chopTime = Mathf.Max(0.3f, home.interval);
            w._sr = sr;
            w._baseColor = new Color(0.96f, 0.91f, 0.62f);
            sr.color = w._baseColor;
            return w;
        }

        void Update()
        {
            if (home == null) { Destroy(gameObject); return; }

            ResourceNode node = home.Source;

            switch (_state)
            {
                case State.ToNode:
                    if (node == null || !node.HasResource)
                    {
                        MoveTo(home.transform.position); // nothing to do — idle near home
                        break;
                    }
                    if (MoveTo(node.transform.position))
                    {
                        _state = State.Chopping;
                        _chopTimer = 0f;
                        _nudgeTimer = 0f;
                    }
                    break;

                case State.Chopping:
                    if (node == null || !node.HasResource) { _state = State.ToHome; break; }
                    _chopTimer += Time.deltaTime * Prod;
                    _nudgeTimer -= Time.deltaTime;
                    if (_nudgeTimer <= 0f) { node.Nudge(); _nudgeTimer = 0.3f; }
                    if (_chopTimer >= chopTime)
                    {
                        _carrying = node.Extract(carryAmount);
                        _carryItem = node.yields;
                        if (_carrying > 0 && home != null) home.RecordProduced(_carrying); // throughput metric
                        node.Nudge();
                        _state = State.ToHome;
                    }
                    break;

                case State.ToHome:
                    if (MoveTo(home.transform.position))
                    {
                        if (_carrying > 0 && _carryItem != null)
                        {
                            int accepted = home.Buffer.Add(_carryItem, _carrying);
                            if (accepted > 0) { _carrying -= accepted; home.Pulse(); }
                            if (_carrying > 0) { _state = State.ToStorage; break; } // buffer full — haul it myself
                        }
                        _carryItem = null;
                        _state = State.ToNode;
                    }
                    break;

                case State.ToStorage:
                    // Buffer was full, so deliver the load straight to a storage instead of stalling.
                    if (_carrying <= 0 || _carryItem == null) { _state = State.ToNode; break; }
                    var store = NearestStorage(_carryItem);
                    if (store == null)
                    {
                        if (MoveTo(home.transform.position)) // nowhere to deliver — try the home buffer again
                        {
                            int a = home.Buffer.Add(_carryItem, _carrying);
                            if (a > 0) { _carrying -= a; home.Pulse(); }
                            if (_carrying <= 0) { _carryItem = null; _state = State.ToNode; }
                        }
                        break;
                    }
                    if (MoveTo(store.transform.position))
                    {
                        int a = store.Store.Add(_carryItem, _carrying);
                        if (a > 0) _carrying -= a;
                        if (_carrying <= 0) { _carryItem = null; _state = State.ToNode; }
                        else _state = State.ToHome; // this store filled up — re-evaluate
                    }
                    break;
            }

            UpdateColor();
        }

        private StorageBuilding NearestStorage(ItemDefinition item)
        {
            StorageBuilding best = null;
            float bestSq = float.MaxValue;
            foreach (var s in StorageBuilding.All)
            {
                if (s == null || s.accepts != item) continue;
                if (s.def != null && s.Store.Total() >= s.def.capacity) continue;
                float sq = ((Vector2)(s.transform.position - transform.position)).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = s; }
            }
            return best;
        }

        private static float Prod => Colony.Instance != null ? Colony.Instance.Productivity : 1f;

        private bool MoveTo(Vector3 target)
        {
            Vector3 p = transform.position;
            target.z = 0f; p.z = 0f;
            transform.position = Vector3.MoveTowards(p, target, moveSpeed * Prod * Time.deltaTime);
            return (transform.position - target).sqrMagnitude < 0.04f;
        }

        private void UpdateColor()
        {
            if (_sr == null) return;
            _sr.color = (_carrying > 0 && _carryItem != null)
                ? Color.Lerp(_baseColor, _carryItem.color, 0.7f)
                : _baseColor;
        }
    }
}
