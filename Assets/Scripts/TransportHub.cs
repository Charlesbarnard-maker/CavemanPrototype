using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// The basic logistics building (e.g. a Mammoth Shack). Each assigned worker
    /// becomes a Transporter NPC that physically carries goods from collector /
    /// workshop output buffers to the matching storage. This is the manual tier of
    /// transport; later ages automate it (wooden rollers → conveyors).
    /// </summary>
    public class TransportHub : MonoBehaviour
    {
        public BuildingDefinition def;
        public bool mechanical; // runs a transporter with no worker (a conveyor)
        public ItemDefinition priorityItem; // null = haul anything (nearest); else prefer this
        public float range = 14f; // transporters only serve sources within this of the hub (belts go further)

        /// <summary>Cycle the priority through the resources currently being produced (+ Any).</summary>
        public void CyclePriority()
        {
            var items = new List<ItemDefinition> { null }; // null = Any
            void Add(ItemDefinition i) { if (i != null && !items.Contains(i)) items.Add(i); }
            foreach (var p in ProductionBuilding.All) Add(p.produces);
            foreach (var w in WorkshopBuilding.All) Add(w.output);
            int idx = items.IndexOf(priorityItem);
            priorityItem = items[(idx + 1) % items.Count];
        }

        public static readonly List<TransportHub> All = new();
        void OnEnable() => All.Add(this);
        void OnDisable() { All.Remove(this); ClearTransporters(); }

        private readonly List<Transporter> _transporters = new();
        private SpriteRenderer _sr;
        private Color _baseColor;

        public static TransportHub Spawn(BuildingDefinition def, Vector3 pos)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one * 1.0f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = def.color;
            sr.sortingOrder = 5;

            go.AddComponent<BoxCollider2D>();

            var h = go.AddComponent<TransportHub>();
            h.def = def;
            h.mechanical = def.mechanical;
            h.range = def.logisticsRange > 0f ? def.logisticsRange : 14f;
            if (h.mechanical)
            {
                var t = Transporter.Spawn(h); // a conveyor runs itself, no worker needed
                t.moveSpeed = 4.5f;            // faster than a hand-hauler
                h._transporters.Add(t);
            }
            return h;
        }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
        }

        private void ClearTransporters()
        {
            foreach (var t in _transporters) if (t != null) Destroy(t.gameObject);
            _transporters.Clear();
        }

        void Update()
        {
            if (_sr == null) return;
            _sr.color = _baseColor; // legacy hub (not buildable) — always reads as active
        }
    }
}
