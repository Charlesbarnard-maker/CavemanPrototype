using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A collector. It binds to a nearby ResourceNode. Each ASSIGNED worker spawns
    /// a Worker NPC that walks out, harvests the patch, and carries loads back into
    /// this building's buffer. With no workers assigned it sits idle. The building
    /// pushes its buffer into an adjacent matching StorageBuilding.
    /// </summary>
    public class ProductionBuilding : MonoBehaviour
    {
        public BuildingDefinition def;
        public ItemDefinition produces;
        public int outputPerCycle = 1;
        public float interval = 2f;
        public int maxWorkers = 2;
        public float sourceRange = 6f;   // worker can commute this far
        public float transferRange = 2.0f;

        public Inventory Buffer { get; private set; }
        public ResourceNode Source => _source;
        public int AssignedWorkers { get; private set; }

        private readonly List<Worker> _workers = new();
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

            go.AddComponent<BoxCollider2D>(); // clickable for demolish / staffing

            var pb = go.AddComponent<ProductionBuilding>();
            pb.def = def;
            pb.produces = def.item;
            pb.outputPerCycle = def.outputPerCycle;
            pb.interval = def.interval;
            pb.maxWorkers = Mathf.Max(1, def.maxWorkers);
            pb.Buffer = new Inventory { capacity = Mathf.Max(1, def.capacity) };
            pb.Bind();
            return pb;
        }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
        }

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

        /// <summary>Assign one worker if there's a free person and a free slot.</summary>
        public bool TryAssign()
        {
            if (AssignedWorkers >= maxWorkers) return false;
            if (Colony.Instance == null || Colony.Instance.FreeWorkers <= 0) return false;
            AssignedWorkers++;
            RefreshWorkers();
            return true;
        }

        public void Unassign()
        {
            if (AssignedWorkers <= 0) return;
            AssignedWorkers--;
            RefreshWorkers();
        }

        private void RefreshWorkers()
        {
            _workers.RemoveAll(w => w == null);
            while (_workers.Count < AssignedWorkers) _workers.Add(Worker.Spawn(this));
            while (_workers.Count > AssignedWorkers)
            {
                var w = _workers[_workers.Count - 1];
                _workers.RemoveAt(_workers.Count - 1);
                if (w != null) Destroy(w.gameObject);
            }
        }

        public void Pulse() => _flash = 0.25f;

        void Update()
        {
            PushToStorage();

            bool working = AssignedWorkers > 0 && ((_source != null && _source.HasResource) || Buffer.Total() > 0);
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
