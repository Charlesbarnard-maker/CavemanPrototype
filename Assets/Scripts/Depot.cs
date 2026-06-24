using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A transfer station for long-distance logistics. Holds one resource type
    /// (set like a warehouse). Belts deliver into it and pull from it locally; a
    /// caravan Route carries goods between two depots across the map. Connect a
    /// belt → depot → (route) → depot → belt to move resources home from afar.
    /// </summary>
    public class Depot : MonoBehaviour
    {
        public BuildingDefinition def;
        public ItemDefinition item;       // what this depot handles (configurable)
        public Inventory store;

        public static readonly List<Depot> All = new();
        private List<Vector2Int> _cells; // every grid cell this building occupies
        private SpriteRenderer _sr;
        private Color _baseColor;

        public static Depot Spawn(BuildingDefinition def, Vector3 pos)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(def.FootW * 1.1f, def.FootH * 1.1f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = def.color;
            sr.sortingOrder = 4;

            go.AddComponent<BoxCollider2D>();

            var d = go.AddComponent<Depot>();
            d.def = def;
            d.item = def.item; // usually null (configurable)
            d.store = new Inventory { capacity = Mathf.Max(1, def.capacity) };
            d._sr = sr;
            d._baseColor = def.color;
            d._cells = Footprint.Cells(go.transform.position, def.FootW, def.FootH);
            foreach (var c in d._cells) WorldGrid.Depots[c] = d;
            return d;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable()
        {
            All.Remove(this);
            if (_cells != null) foreach (var c in _cells) WorldGrid.Remove(WorldGrid.Depots, c, this);
        }

        /// <summary>Choose which resource this depot handles (only while empty).</summary>
        public void CycleItem()
        {
            if (store.Total() > 0) return;
            var items = new List<ItemDefinition>();
            void Add(ItemDefinition i) { if (i != null && !items.Contains(i)) items.Add(i); }
            foreach (var p in ProductionBuilding.All) Add(p.produces);
            foreach (var w in WorkshopBuilding.All) Add(w.output);
            if (items.Count == 0) return;
            int idx = items.IndexOf(item);
            item = items[(idx + 1) % items.Count];
        }

        void Update()
        {
            if (_sr == null || def == null) return;
            float f = def.capacity > 0 ? (float)store.Total() / def.capacity : 0f;
            Color empty = Color.Lerp(_baseColor, Color.black, 0.5f);
            _sr.color = Color.Lerp(empty, _baseColor, Mathf.Clamp01(0.2f + 0.8f * f));
        }
    }
}
