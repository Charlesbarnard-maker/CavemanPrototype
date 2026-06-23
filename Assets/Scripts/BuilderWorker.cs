using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A builder assigned to a ConstructionSite. Spawns at (and fetches from) the
    /// Town Hall — later, dedicated constructor stations. Carries a small load per
    /// trip, so a building needs several trips, then constructs it. Drawn as a
    /// square to tell it apart from the round gatherer workers.
    /// </summary>
    public class BuilderWorker : MonoBehaviour
    {
        public ConstructionSite site;
        public float moveSpeed = 3.2f;
        public int carryCapacity = 2;

        private enum State { ToDepot, ToSite, Building }
        private State _state = State.ToDepot;
        private ItemDefinition _carryItem;
        private int _carryQty;
        private SpriteRenderer _sr;
        private readonly Color _baseColor = new Color(0.85f, 0.86f, 0.95f);

        public static BuilderWorker Spawn(ConstructionSite site)
        {
            var go = new GameObject("Builder");
            // Builders originate from the Town Hall (nearest housing).
            go.transform.position = NearestHousing(site.transform.position);
            go.transform.localScale = Vector3.one * 0.34f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.sortingOrder = 13;

            var b = go.AddComponent<BuilderWorker>();
            b.site = site;
            b._sr = sr;
            sr.color = b._baseColor;
            return b;
        }

        void Update()
        {
            if (site == null) { Destroy(gameObject); return; }

            if (!site.MaterialsDone)
            {
                if (_state == State.ToDepot)
                {
                    var need = site.NextNeeded();
                    if (need == null) { _state = State.Building; }
                    else if (MoveTo(DepotFor(need.item)))
                    {
                        int want = Mathf.Min(carryCapacity, need.amount);
                        int got = Economy.SpendUpTo(need.item, want, Carried());
                        if (got > 0) { _carryItem = need.item; _carryQty = got; _state = State.ToSite; }
                        // else: not enough in the pool yet — wait and retry.
                    }
                }
                else // ToSite
                {
                    if (MoveTo(site.transform.position))
                    {
                        site.DeliverUnits(_carryQty);
                        _carryQty = 0;
                        _carryItem = null;
                        _state = State.ToDepot;
                    }
                }
            }
            else
            {
                if (MoveTo(site.transform.position))
                    site.AddBuildProgress(Time.deltaTime);
            }

            UpdateColor();
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
            // Prefer the nearest storage that actually holds the item.
            StorageBuilding bestStore = null;
            float bestSq = float.MaxValue;
            foreach (var s in StorageBuilding.All)
            {
                if (s == null || s.accepts != item || s.Store.Count(item) <= 0) continue;
                float sq = ((Vector2)(s.transform.position - transform.position)).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; bestStore = s; }
            }
            if (bestStore != null) return bestStore.transform.position;

            // Fallback: the Town Hall (nearest housing) acts as the depot.
            return NearestHousing(transform.position);
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
