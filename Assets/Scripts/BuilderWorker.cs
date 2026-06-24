using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A builder from the Colony's HQ builder squad. It picks the nearest active
    /// construction site, hauls its materials (a small load per trip, consumed from
    /// the pool on pickup and reserved so builders don't over-fetch), then helps
    /// build. Idle builders wait at the HQ. Square sprite to distinguish from the
    /// round gatherer workers.
    /// </summary>
    public class BuilderWorker : MonoBehaviour
    {
        public float moveSpeed = 3.2f;
        public int carryCapacity = 2;

        private ConstructionSite _site;
        private int _slot = -1; // my reserved position around the current site
        private ItemDefinition _targetItem; // what this fetch trip is going for
        private ItemDefinition _carryItem;
        private int _carryQty;

        private SpriteRenderer _sr;
        private readonly Color _baseColor = new Color(0.85f, 0.86f, 0.95f);
        private const float BaseScale = 0.34f;

        public static BuilderWorker Spawn()
        {
            var go = new GameObject("Builder");
            go.transform.position = NearestHousing(Vector3.zero);
            go.transform.localScale = Vector3.one * 0.34f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.sortingOrder = 13;

            var b = go.AddComponent<BuilderWorker>();
            b._sr = sr;
            sr.color = b._baseColor;
            return b;
        }

        void Update()
        {
            // Acquire / re-acquire a site when not committed to one.
            if (_site == null || _site.IsComplete)
            {
                ReleaseSlot();
                DropClaim();
                _site = NearestActiveSite();
                _targetItem = null;
                if (_site != null) _slot = _site.ClaimSlot();
            }

            ResetPulse();
            if (_site == null) { MoveTo(NearestHousing(transform.position)); UpdateColor(); return; } // idle at HQ

            if (_site.MaterialsDone)
            {
                if (MoveTo(_site.SlotPosition(_slot))) { _site.AddBuildProgress(Time.deltaTime); Pulse(); }
                UpdateColor();
                return;
            }

            if (_carryQty > 0) // carrying — deliver
            {
                if (MoveTo(_site.SlotPosition(_slot)))
                {
                    _site.Deliver(_carryItem, _carryQty);
                    _carryItem = null;
                    _carryQty = 0;
                }
            }
            else // empty — fetch
            {
                if (_targetItem == null)
                {
                    var m = _site.NextFetchable();
                    if (m == null) { MoveTo(_site.SlotPosition(_slot)); UpdateColor(); return; } // wait to build
                    _targetItem = m.item;
                }

                if (MoveTo(DepotFor(_targetItem)))
                {
                    var m = _site.MatFor(_targetItem);
                    int avail = m != null ? m.needed - m.claimed : 0;
                    if (avail <= 0) { _targetItem = null; }
                    else
                    {
                        int want = Mathf.Min(carryCapacity, avail);
                        int got = Economy.SpendUpTo(_targetItem, want, Carried());
                        if (got > 0)
                        {
                            _site.Claim(_targetItem, got);
                            _carryItem = _targetItem;
                            _carryQty = got;
                            _targetItem = null;
                        }
                    }
                }
            }

            UpdateColor();
        }

        private void DropClaim()
        {
            if (_site != null && _carryQty > 0 && _carryItem != null) _site.Unclaim(_carryItem, _carryQty);
            _carryItem = null;
            _carryQty = 0;
        }

        private void ReleaseSlot()
        {
            if (_site != null) _site.ReleaseSlot(_slot);
            _slot = -1;
        }

        // A gentle scale pulse while actively building — visible "work" feedback.
        private void Pulse()
        {
            if (_sr == null) return;
            transform.localScale = Vector3.one * (BaseScale * (1f + 0.18f * Mathf.Abs(Mathf.Sin(Time.time * 9f))));
        }
        private void ResetPulse()
        {
            if (transform.localScale.x != BaseScale) transform.localScale = Vector3.one * BaseScale;
        }

        void OnDestroy() { ReleaseSlot(); DropClaim(); }

        private ConstructionSite NearestActiveSite()
        {
            ConstructionSite best = null;
            float bestSq = float.MaxValue;
            foreach (var s in ConstructionSite.All)
            {
                if (s == null || s.IsComplete) continue;
                float sq = ((Vector2)(s.transform.position - transform.position)).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = s; }
            }
            return best;
        }

        private bool MoveTo(Vector3 target)
        {
            Vector3 p = transform.position;
            target.z = 0f; p.z = 0f;
            transform.position = Vector3.MoveTowards(p, target, moveSpeed * Time.deltaTime);
            return (transform.position - target).sqrMagnitude < 0.05f;
        }

        private Vector3 DepotFor(ItemDefinition item)
        {
            StorageBuilding bestStore = null;
            float bestSq = float.MaxValue;
            foreach (var s in StorageBuilding.All)
            {
                if (s == null || s.accepts != item || s.Store.Count(item) <= 0) continue;
                float sq = ((Vector2)(s.transform.position - transform.position)).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; bestStore = s; }
            }
            if (bestStore != null) return bestStore.transform.position;
            return NearestHousing(transform.position); // fallback: the HQ acts as the depot
        }

        private static Vector3 NearestHousing(Vector3 from)
        {
            HousingBuilding best = null;
            float bestSq = float.MaxValue;
            foreach (var h in HousingBuilding.All)
            {
                if (h == null) continue;
                float sq = ((Vector2)(h.transform.position - from)).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = h; }
            }
            return best != null ? best.transform.position : from;
        }

        private static Inventory Carried() => Colony.Instance != null ? Colony.Instance.carried : null;

        private void UpdateColor()
        {
            if (_sr == null) return;
            _sr.color = (_carryQty > 0 && _carryItem != null)
                ? Color.Lerp(_baseColor, _carryItem.color, 0.7f)
                : _baseColor;
        }
    }
}
