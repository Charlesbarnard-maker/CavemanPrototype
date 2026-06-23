using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A builder assigned to a ConstructionSite. Walks to a depot (a storage that
    /// holds the needed material, else the Town Hall), picks up a load (consuming
    /// it from the pool), carries it to the site, and repeats until all materials
    /// are delivered — then constructs the building. Drawn as a square to tell it
    /// apart from the round gatherer workers.
    /// </summary>
    public class BuilderWorker : MonoBehaviour
    {
        public ConstructionSite site;
        public float moveSpeed = 3.2f;

        private enum State { ToDepot, ToSite, Building }
        private State _state = State.ToDepot;
        private ItemAmount _hauling;
        private SpriteRenderer _sr;
        private readonly Color _baseColor = new Color(0.85f, 0.86f, 0.95f);

        public static BuilderWorker Spawn(ConstructionSite site)
        {
            var go = new GameObject("Builder");
            go.transform.position = site.transform.position;
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
                        int got = Economy.SpendUpTo(need.item, need.amount, Carried());
                        if (got >= need.amount) { _hauling = need; _state = State.ToSite; }
                        // else: not enough in the pool yet — wait and retry.
                    }
                }
                else // ToSite
                {
                    if (MoveTo(site.transform.position))
                    {
                        site.DeliverFirst();
                        _hauling = null;
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
            foreach (var s in FindObjectsByType<StorageBuilding>())
            {
                if (s == null || s.accepts != item || s.Store.Count(item) <= 0) continue;
                float sq = ((Vector2)(s.transform.position - transform.position)).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; bestStore = s; }
            }
            if (bestStore != null) return bestStore.transform.position;

            // Fallback: the nearest housing (e.g. Town Hall) acts as a depot.
            HousingBuilding bestHouse = null; bestSq = float.MaxValue;
            foreach (var h in FindObjectsByType<HousingBuilding>())
            {
                if (h == null) continue;
                float sq = ((Vector2)(h.transform.position - transform.position)).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; bestHouse = h; }
            }
            return bestHouse != null ? bestHouse.transform.position : site.transform.position;
        }

        private static Inventory Carried() => Colony.Instance != null ? Colony.Instance.carried : null;

        private void UpdateColor()
        {
            if (_sr == null) return;
            _sr.color = (_hauling != null && _hauling.item != null)
                ? Color.Lerp(_baseColor, _hauling.item.color, 0.7f)
                : _baseColor;
        }
    }
}
