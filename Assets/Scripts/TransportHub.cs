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
    public class TransportHub : MonoBehaviour, IStaffable
    {
        public BuildingDefinition def;
        public int maxWorkers = 2;

        public int AssignedWorkers { get; private set; }
        public int MaxWorkers => maxWorkers;
        public string StaffLabel => def != null ? def.displayName : "Transport";

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
            h.maxWorkers = Mathf.Max(1, def.maxWorkers);
            return h;
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
            RefreshTransporters();
            return true;
        }

        public void Unassign()
        {
            if (AssignedWorkers <= 0) return;
            AssignedWorkers--;
            RefreshTransporters();
        }

        private void RefreshTransporters()
        {
            _transporters.RemoveAll(t => t == null);
            while (_transporters.Count < AssignedWorkers) _transporters.Add(Transporter.Spawn(this));
            while (_transporters.Count > AssignedWorkers)
            {
                var t = _transporters[_transporters.Count - 1];
                _transporters.RemoveAt(_transporters.Count - 1);
                if (t != null) Destroy(t.gameObject);
            }
        }

        private void ClearTransporters()
        {
            foreach (var t in _transporters) if (t != null) Destroy(t.gameObject);
            _transporters.Clear();
        }

        void Update()
        {
            if (_sr == null) return;
            bool working = AssignedWorkers > 0;
            _sr.color = working ? _baseColor : Color.Lerp(_baseColor, Color.black, 0.5f);
        }
    }
}
