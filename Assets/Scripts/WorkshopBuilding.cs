using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A processor: consumes input resources from the pool and produces an output
    /// over time (a recipe). Needs assigned workers to run — more workers process
    /// faster. Output goes to its buffer (and on to a matching storage if present),
    /// and counts toward the shared pool. This is the engine for all production
    /// chains; each chain is just a different recipe (Sawmill, Campfire, ...).
    /// </summary>
    public class WorkshopBuilding : MonoBehaviour, IStaffable
    {
        public BuildingDefinition def;
        public ItemDefinition output;
        public int outputPerCycle = 1;
        public float processTime = 2.5f;
        public int maxWorkers = 2;
        public List<ItemAmount> inputs = new();

        public Inventory Buffer { get; private set; }
        public int AssignedWorkers { get; private set; }
        public int MaxWorkers => maxWorkers;
        public string StaffLabel => def != null ? def.displayName : "Workshop";

        public static readonly List<WorkshopBuilding> All = new();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        private float _timer, _flash;
        private SpriteRenderer _sr;
        private Color _baseColor;
        private LineRenderer _link;

        public static WorkshopBuilding Spawn(BuildingDefinition def, Vector3 pos)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one * 1.0f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = def.color;
            sr.sortingOrder = 5;

            go.AddComponent<BoxCollider2D>();

            var w = go.AddComponent<WorkshopBuilding>();
            w.def = def;
            w.output = def.item;
            w.outputPerCycle = def.outputPerCycle;
            w.processTime = Mathf.Max(0.2f, def.interval);
            w.maxWorkers = Mathf.Max(1, def.maxWorkers);
            w.inputs = def.inputs;
            w.Buffer = new Inventory { capacity = Mathf.Max(1, def.capacity) };
            return w;
        }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
        }

        public bool TryAssign()
        {
            if (AssignedWorkers >= maxWorkers) return false;
            if (Colony.Instance == null || Colony.Instance.FreeWorkers <= 0) return false;
            AssignedWorkers++;
            return true;
        }

        public void Unassign()
        {
            if (AssignedWorkers > 0) AssignedWorkers--;
        }

        void Update()
        {
            var carried = Colony.Instance != null ? Colony.Instance.carried : null;
            bool produced = false;

            if (AssignedWorkers > 0 && output != null && Buffer.Total() < Buffer.capacity)
            {
                _timer += Time.deltaTime * AssignedWorkers; // more workers = faster
                if (_timer >= processTime)
                {
                    if (Economy.CanAfford(inputs, carried))
                    {
                        _timer -= processTime;
                        Economy.Spend(inputs, carried);
                        Buffer.Add(output, outputPerCycle);
                        _flash = 0.25f;
                        produced = true;
                    }
                    else
                    {
                        _timer = processTime; // inputs missing — wait
                    }
                }
            }
            else
            {
                _timer = 0f;
            }

            PushToStorage();
            bool working = AssignedWorkers > 0 && (produced || Buffer.Total() > 0);
            UpdateVisual(working);
        }

        private void PushToStorage()
        {
            int have = Buffer.Count(output);
            if (have <= 0) { if (_link != null) _link.enabled = false; return; }

            StorageBuilding store = null;
            float bestSq = float.MaxValue;
            foreach (var s in StorageBuilding.All)
            {
                if (s == null || s.accepts != output) continue;
                float sq = ((Vector2)(s.transform.position - transform.position)).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; store = s; }
            }
            if (store == null) { if (_link != null) _link.enabled = false; return; }

            int accepted = store.Store.Add(output, have);
            if (accepted > 0) Buffer.RemoveUpTo(output, accepted);
            DrawLink(store.transform.position);
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
