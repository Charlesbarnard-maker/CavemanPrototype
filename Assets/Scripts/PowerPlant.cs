using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A generator: burns fuel to supply electrical <see cref="Power"/> to the grid.
    /// No workers — it runs as long as it's fed fuel; out of fuel → no output → the
    /// grid browns out. Fuel is the ongoing pressure (it competes with the Kiln /
    /// Smelter for charcoal — another shared-intermediate chokepoint).
    /// </summary>
    public class PowerPlant : MonoBehaviour
    {
        public BuildingDefinition def;
        public ItemDefinition fuel;
        public int fuelPerCycle = 1;
        public float interval = 3f;
        public int output = 60;
        // Power-network node: how far it links to poles/generators, and how far it supplies consumers
        // directly (so a generator next to a machine powers it with no pole needed). See PowerNet.
        public float ConnectRange = 6f;
        public float SupplyRange = 5f;

        public static readonly List<PowerPlant> All = new();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        private float _timer;
        private bool _fueled;
        private SpriteRenderer _sr;
        private Color _baseColor;

        /// <summary>Power supplied right now (0 when out of fuel).</summary>
        public float CurrentOutput => _fueled ? output : 0f;
        public bool Fueled => _fueled;

        public static PowerPlant Spawn(BuildingDefinition def, Vector3 pos)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(def.FootW, def.FootH, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteDatabase.ForBuilding(def);
            sr.color = def.color;
            sr.sortingOrder = 5;

            go.AddComponent<BoxCollider2D>(); // clickable for select / demolish

            var p = go.AddComponent<PowerPlant>();
            p.def = def;
            p.output = Mathf.Max(0, def.powerOutput);
            p.interval = Mathf.Max(0.2f, def.interval);
            bool hasFuel = def.inputs != null && def.inputs.Count > 0 && def.inputs[0] != null;
            p.fuel = hasFuel ? def.inputs[0].item : null;
            p.fuelPerCycle = hasFuel ? Mathf.Max(1, def.inputs[0].amount) : 1;
            p.ConnectRange = def.connectRange > 0f ? def.connectRange : 6f;
            p.SupplyRange = def.supplyRange > 0f ? def.supplyRange : 5f;
            p._sr = sr;
            p._baseColor = def.color;
            return p;
        }

        void Update()
        {
            var carried = Colony.Instance != null ? Colony.Instance.carried : null;
            _timer += Time.deltaTime;
            if (_timer >= interval)
            {
                _timer -= interval;
                // Fuel-free generators (no inputs) always run; otherwise burn fuel.
                _fueled = fuel == null || Economy.SpendUpTo(fuel, fuelPerCycle, carried) >= fuelPerCycle;
            }
            if (_sr != null)
                _sr.color = _fueled ? _baseColor : Color.Lerp(_baseColor, Color.black, 0.55f);
        }
    }
}
