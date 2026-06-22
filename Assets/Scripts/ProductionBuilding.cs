using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// An automatic producer bound to a nearby ResourceNode. Every `interval`
    /// seconds it extracts from that patch and deposits into the target inventory
    /// with no player input. If its patch is empty (regenerating) or out of range,
    /// it idles (dims) until the patch has resource again.
    /// </summary>
    public class ProductionBuilding : MonoBehaviour
    {
        public BuildingDefinition def;
        public ItemDefinition produces;
        public int outputPerCycle = 1;
        public float interval = 2f;
        public float sourceRange = 2.5f;

        private Inventory _target;
        private ResourceNode _source;
        private float _timer;
        private float _flash;
        private SpriteRenderer _sr;
        private Color _baseColor;
        private LineRenderer _link;

        public static ProductionBuilding Spawn(BuildingDefinition def, Vector3 pos, Inventory target)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one * 0.9f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = def.color;
            sr.sortingOrder = 5;

            go.AddComponent<BoxCollider2D>(); // so it can be clicked for removal

            var pb = go.AddComponent<ProductionBuilding>();
            pb.def = def;
            pb.produces = def.produces;
            pb.outputPerCycle = def.outputPerCycle;
            pb.interval = def.interval;
            pb._target = target;
            pb.Bind();
            return pb;
        }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
        }

        /// <summary>(Re)find the nearest matching resource patch in range.</summary>
        public void Bind()
        {
            ResourceNode best = null;
            float bestSq = sourceRange * sourceRange;
            foreach (var n in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            {
                if (n == null || n.yields != produces) continue;
                float sq = ((Vector2)(n.transform.position - transform.position)).sqrMagnitude;
                if (sq <= bestSq) { bestSq = sq; best = n; }
            }
            _source = best;
        }

        void Update()
        {
            bool working = _source != null && _source.HasResource && _target != null && produces != null;

            if (working)
            {
                _timer += Time.deltaTime;
                if (_timer >= interval)
                {
                    _timer -= interval;
                    int got = _source.Extract(outputPerCycle);
                    if (got > 0)
                    {
                        _target.Add(produces, got);
                        _flash = 0.25f;
                    }
                }
            }
            else
            {
                _timer = 0f;
            }

            UpdateVisual(working);
            UpdateLink(working);
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

        private void UpdateLink(bool working)
        {
            if (!working || _source == null)
            {
                if (_link != null) _link.enabled = false;
                return;
            }

            if (_link == null)
            {
                _link = gameObject.AddComponent<LineRenderer>();
                var shader = Shader.Find("Sprites/Default");
                if (shader != null) _link.material = new Material(shader);
                _link.widthMultiplier = 0.08f;
                _link.numCapVertices = 2;
                _link.sortingOrder = 4;
                _link.startColor = _link.endColor = new Color(1f, 1f, 1f, 0.4f);
                _link.positionCount = 2;
                _link.useWorldSpace = true;
            }

            _link.enabled = true;
            _link.SetPosition(0, transform.position);
            _link.SetPosition(1, _source.transform.position);
        }
    }
}
