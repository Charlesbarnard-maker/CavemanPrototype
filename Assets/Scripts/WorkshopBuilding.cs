using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A processor: consumes input resources from the pool and produces an output
    /// over time (a recipe). Needs assigned workers to run — more workers process
    /// faster. Output accumulates in its buffer (counts toward the pool) and is
    /// moved to storage by Transporters. The engine for all production chains.
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
                float prod = Colony.Instance != null ? Colony.Instance.Productivity : 1f;
                _timer += Time.deltaTime * AssignedWorkers * prod; // more workers / well-fed = faster
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

            bool working = AssignedWorkers > 0 && (produced || Buffer.Total() > 0);
            UpdateVisual(working);
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
    }
}
