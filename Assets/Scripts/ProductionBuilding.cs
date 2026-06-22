using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// An automatic producer placed in the world. Every `interval` seconds it
    /// drops `outputPerCycle` of its resource into the target inventory — with no
    /// player input. This is the core automation primitive; later versions will
    /// pull from nearby ResourceNodes and feed into storage/logistics.
    /// </summary>
    public class ProductionBuilding : MonoBehaviour
    {
        public ItemDefinition produces;
        public int outputPerCycle = 1;
        public float interval = 2f;

        private Inventory _target;
        private float _timer;
        private float _flash;
        private SpriteRenderer _sr;
        private Color _baseColor;

        public static ProductionBuilding Spawn(BuildingDefinition def, Vector3 pos, Inventory target)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.9f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = def.color;
            sr.sortingOrder = 5;

            var pb = go.AddComponent<ProductionBuilding>();
            pb.produces = def.produces;
            pb.outputPerCycle = def.outputPerCycle;
            pb.interval = def.interval;
            pb._target = target;
            return pb;
        }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
        }

        void Update()
        {
            if (_target == null || produces == null) return;

            _timer += Time.deltaTime;
            if (_timer >= interval)
            {
                _timer -= interval;
                _target.Add(produces, outputPerCycle);
                _flash = 0.25f; // brief pulse so you can see it "tick"
            }

            if (_flash > 0f && _sr != null)
            {
                _flash -= Time.deltaTime;
                _sr.color = Color.Lerp(_baseColor, Color.white, Mathf.Clamp01(_flash / 0.25f));
            }
        }
    }
}
