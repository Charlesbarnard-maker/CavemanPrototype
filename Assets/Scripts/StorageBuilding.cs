using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A storage structure that holds a large amount of one resource type
    /// (Woodpile, Stone Storage). Collectors placed next to it drop their
    /// buffer into it. Brightens as it fills.
    /// </summary>
    public class StorageBuilding : MonoBehaviour
    {
        public BuildingDefinition def;
        public ItemDefinition accepts;
        public bool configurable;
        public Inventory Store { get; private set; }
        // Orientation: belts DELIVER on the input side (opposite OutputSide) and PULL from the
        // OutputSide — so a warehouse is a pass-through node (e.g. warehouse → belt → sawmill).
        public Belt.Dir OutputSide = Belt.Dir.E;

        /// <summary>Choose which resource this (configurable) warehouse holds — only while empty.</summary>
        public void CycleAccepts()
        {
            if (!configurable || Store.Total() > 0) return;
            var items = new List<ItemDefinition>();
            void Add(ItemDefinition i) { if (i != null && !i.noWarehouse && !items.Contains(i)) items.Add(i); } // Stone/Ore keep their own stockpiles
            foreach (var p in ProductionBuilding.All) Add(p.produces);
            foreach (var w in WorkshopBuilding.All) Add(w.output);
            if (items.Count == 0) return;
            int idx = items.IndexOf(accepts);
            accepts = items[(idx + 1) % items.Count];
        }

        public static readonly List<StorageBuilding> All = new();
        private List<Vector2Int> _cells; // every grid cell this building occupies
        void OnEnable() { All.Add(this); }
        void OnDisable()
        {
            All.Remove(this);
            if (_cells != null) foreach (var c in _cells) WorldGrid.Remove(WorldGrid.Storages, c, this);
        }

        private SpriteRenderer _sr;
        private Color _baseColor;

        public static StorageBuilding Spawn(BuildingDefinition def, Vector3 pos, Belt.Dir outputSide = Belt.Dir.E)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(def.FootW, def.FootH, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteDatabase.ForBuilding(def);
            sr.color = def.color;
            sr.sortingOrder = 3;

            go.AddComponent<BoxCollider2D>(); // clickable for demolish

            var sb = go.AddComponent<StorageBuilding>();
            sb.def = def;
            sb.accepts = def.item; // null for a configurable warehouse until the player sets it
            sb.configurable = def.configurable;
            sb.Store = new Inventory { capacity = Mathf.Max(1, def.capacity) };
            sb.OutputSide = outputSide;
            sb._cells = Footprint.Cells(go.transform.position, def.FootW, def.FootH);
            foreach (var c in sb._cells) WorldGrid.Storages[c] = sb;
            Ports.PlacePorts(go.transform, def.FootW, def.FootH, outputSide, true, true, singlePort: false); // a port PER edge cell → a 2×2 store has 2 in + 2 out, a 3×3 warehouse 3 + 3
            return sb;
        }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
        }

        void Update()
        {
            if (_sr == null || def == null) return;
            float f = def.capacity > 0 ? (float)Store.Total() / def.capacity : 0f;
            Color empty = Color.Lerp(_baseColor, Color.black, 0.55f);
            _sr.color = Color.Lerp(empty, _baseColor, Mathf.Clamp01(0.15f + 0.85f * f));
        }
    }
}
