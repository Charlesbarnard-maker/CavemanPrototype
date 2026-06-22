using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A collector. It binds to a nearby ResourceNode and spawns a Worker that
    /// physically walks out, harvests the patch, and carries loads back into this
    /// building's buffer. The building then pushes its buffer into an adjacent
    /// matching StorageBuilding ("drop pile" transfer — the Stone-Age tier).
    /// </summary>
    public class ProductionBuilding : MonoBehaviour
    {
        public BuildingDefinition def;
        public ItemDefinition produces;
        public int outputPerCycle = 1;
        public float interval = 2f;
        public float sourceRange = 6f;   // worker can commute this far
        public float transferRange = 2.0f;

        public Inventory Buffer { get; private set; }
        public ResourceNode Source => _source;

        private ResourceNode _source;
        private float _flash;
        private SpriteRenderer _sr;
        private Color _baseColor;
        private LineRenderer _link;

        public static ProductionBuilding Spawn(BuildingDefinition def, Vector3 pos)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one * 0.9f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = def.color;
            sr.sortingOrder = 5;

            go.AddComponent<BoxCollider2D>(); // clickable for demolish

            var pb = go.AddComponent<ProductionBuilding>();
            pb.def = def;
            pb.produces = def.item;
            pb.outputPerCycle = def.outputPerCycle;
            pb.interval = def.interval;
            pb.Buffer = new Inventory { capacity = Mathf.Max(1, def.capacity) };
            pb.Bind();

            Worker.Spawn(pb);
            return pb;
        }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
        }

        /// <summary>Find the nearest matching resource patch in commute range.</summary>
        public void Bind()
        {
            ResourceNode best = null;
            float bestSq = sourceRange * sourceRange;
            foreach (var n in FindObjectsByType<ResourceNode>())
            {
                if (n == null || n.yields != produces) continue;
                float sq = ((Vector2)(n.transform.position - transform.position)).sqrMagnitude;
                if (sq <= bestSq) { bestSq = sq; best = n; }
            }
            _source = best;
        }

        /// <summary>Called by the worker when it deposits a load.</summary>
        public void Pulse() => _flash = 0.25f;

        void Update()
        {
            PushToStorage();

            bool working = (_source != null && _source.HasResource) || Buffer.Total() > 0;
            UpdateVisual(working);
        }

        private void PushToStorage()
        {
            int have = Buffer.Count(produces);
            if (have <= 0)
            {
                if (_link != null) _link.enabled = false;
                return;
            }

            var store = FindNearestStorage();
            if (store == null)
            {
                if (_link != null) _link.enabled = false;
                return;
            }

            int accepted = store.Store.Add(produces, have);
            if (accepted > 0) Buffer.RemoveUpTo(produces, accepted);

            DrawLink(store.transform.position);
        }

        private StorageBuilding FindNearestStorage()
        {
            StorageBuilding best = null;
            float bestSq = transferRange * transferRange;
            foreach (var s in FindObjectsByType<StorageBuilding>())
            {
                if (s == null || s.accepts != produces) continue;
                float sq = ((Vector2)(s.transform.position - transform.position)).sqrMagnitude;
                if (sq <= bestSq) { bestSq = sq; best = s; }
            }
            return best;
        }

        private void UpdateVisual(bool working)
        {
            if (_sr == null) return;
            Color shown = working ? _baseColor : Color.Lerp(_baseColor, Color.black, 0.5f);
            if (_flash > 0f)
            {
                _flash -= Time.deltaTime;
                shown = Color.Lerp(shown, Color.white, Mathf.Clamp01(_flash / 0.25f));
            }
            _sr.color = shown;
        }

        private void DrawLink(Vector3 to)
        {
            if (_link == null)
            {
                _link = gameObject.AddComponent<LineRenderer>();
                var shader = Shader.Find("Sprites/Default");
                if (shader != null) _link.material = new Material(shader);
                _link.widthMultiplier = 0.08f;
                _link.numCapVertices = 2;
                _link.sortingOrder = 4;
                _link.startColor = _link.endColor = new Color(1f, 1f, 1f, 0.35f);
                _link.positionCount = 2;
                _link.useWorldSpace = true;
            }
            _link.enabled = true;
            _link.SetPosition(0, transform.position);
            _link.SetPosition(1, to);
        }
    }
}
