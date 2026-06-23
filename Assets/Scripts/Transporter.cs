using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A transporter from a TransportHub. It physically carries goods from a
    /// collector/workshop output buffer to the nearest matching storage (a small
    /// load per trip, deliberately slow — the basic manual tier of logistics).
    /// Idle transporters wait at their hub.
    /// </summary>
    public class Transporter : MonoBehaviour
    {
        public TransportHub hub;
        public float moveSpeed = 2.8f;   // deliberately slow — the most basic transport
        public int carryCapacity = 4;

        private Inventory _srcBuf;
        private Transform _srcT;
        private StorageBuilding _store;
        private ItemDefinition _item;
        private int _carryQty;
        private bool _hasJob;

        private SpriteRenderer _sr;
        private readonly Color _baseColor = new Color(0.78f, 0.70f, 0.58f);

        public static Transporter Spawn(TransportHub hub)
        {
            var go = new GameObject("Transporter");
            go.transform.position = hub.transform.position;
            go.transform.localScale = Vector3.one * 0.36f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.sortingOrder = 12;

            var t = go.AddComponent<Transporter>();
            t.hub = hub;
            t._sr = sr;
            sr.color = t._baseColor;
            return t;
        }

        void Update()
        {
            if (hub == null) { Destroy(gameObject); return; }

            if (_carryQty > 0)
            {
                // Deliver to the storage we picked.
                if (_store == null) { GiveBack(); ClearJob(); UpdateColor(); return; }
                if (MoveTo(_store.transform.position))
                {
                    int deposited = _store.Store.Add(_item, _carryQty);
                    _carryQty -= deposited;
                    if (_carryQty > 0) GiveBack(); // storage full — return remainder to source
                    _carryQty = 0;
                    ClearJob();
                }
            }
            else
            {
                if (!_hasJob && !FindJob()) { MoveTo(hub.transform.position); UpdateColor(); return; }
                if (_srcT == null) { ClearJob(); UpdateColor(); return; }
                if (MoveTo(_srcT.position))
                {
                    int qty = _srcBuf != null ? _srcBuf.RemoveUpTo(_item, carryCapacity) : 0;
                    if (qty > 0) _carryQty = qty;
                    else ClearJob(); // nothing left to grab
                }
            }

            UpdateColor();
        }

        private bool FindJob()
        {
            var pref = hub != null ? hub.priorityItem : null;
            if (pref != null && ScanFor(pref)) { _hasJob = true; return true; }
            if (ScanFor(null)) { _hasJob = true; return true; }
            _hasJob = false;
            return false;
        }

        // Finds the nearest source buffer (optionally restricted to `only`) that has a
        // valid destination storage; records it as the current job.
        private bool ScanFor(ItemDefinition only)
        {
            float bestSq = float.MaxValue;
            bool found = false;

            void Consider(Inventory buf, ItemDefinition item, Transform t)
            {
                if (buf == null || item == null || buf.Count(item) <= 0) return;
                if (only != null && item != only) return;
                if (hub != null && ((Vector2)(t.position - hub.transform.position)).sqrMagnitude > hub.range * hub.range)
                    return; // out of this hub's service range — that's what belts are for
                var store = StorageFor(item);
                if (store == null) return;
                float sq = ((Vector2)(t.position - transform.position)).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    _srcBuf = buf; _srcT = t; _item = item; _store = store;
                    found = true;
                }
            }

            foreach (var p in ProductionBuilding.All) if (p != null) Consider(p.Buffer, p.produces, p.transform);
            foreach (var w in WorkshopBuilding.All) if (w != null) Consider(w.Buffer, w.output, w.transform);
            return found;
        }

        private StorageBuilding StorageFor(ItemDefinition item)
        {
            StorageBuilding best = null;
            float bestSq = float.MaxValue;
            foreach (var s in StorageBuilding.All)
            {
                if (s == null || s.accepts != item) continue;
                if (s.def != null && s.Store.Total() >= s.def.capacity) continue; // full
                float sq = ((Vector2)(s.transform.position - transform.position)).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = s; }
            }
            return best;
        }

        private void GiveBack()
        {
            if (_srcBuf != null && _item != null && _carryQty > 0) _srcBuf.Add(_item, _carryQty);
        }

        private void ClearJob()
        {
            _hasJob = false;
            _srcBuf = null; _srcT = null; _store = null; _item = null; _carryQty = 0;
        }

        void OnDestroy() { GiveBack(); }

        private bool MoveTo(Vector3 target)
        {
            Vector3 p = transform.position;
            target.z = 0f; p.z = 0f;
            transform.position = Vector3.MoveTowards(p, target, moveSpeed * Time.deltaTime);
            return (transform.position - target).sqrMagnitude < 0.05f;
        }

        private void UpdateColor()
        {
            if (_sr == null) return;
            _sr.color = (_carryQty > 0 && _item != null)
                ? Color.Lerp(_baseColor, _item.color, 0.7f)
                : _baseColor;
        }
    }
}
