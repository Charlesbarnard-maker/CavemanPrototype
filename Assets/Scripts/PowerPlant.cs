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
        public Belt.Dir InputSide = Belt.Dir.S; // the edge belts deliver FUEL on (R aims it); cyan notch sits here

        // Belt-fed FUEL buffer: conveyors deposit fuel here (charcoal/wood), and the generator burns from it
        // first — so you can automate a generator instead of hand-feeding from your carried pile.
        public Inventory Buffer { get; private set; }

        public static readonly List<PowerPlant> All = new();
        private List<Vector2Int> _cells;
        void OnEnable() => All.Add(this);
        void OnDisable()
        {
            All.Remove(this);
            if (_cells != null) foreach (var c in _cells) WorldGrid.Remove(WorldGrid.Generators, c, this);
        }

        private float _timer;
        private bool _fueled;
        private SpriteRenderer _sr;
        private Color _baseColor;
        public bool renewable; // fuel-free, VARIABLE output (windmill/solar)
        public bool solar;     // a solar panel — output follows daylight
        private float _windSeed; // per-windmill phase so they don't all gust in unison

        /// <summary>Power supplied right now. Fuel burners: full when fuelled, else 0. Renewables: no fuel but
        /// VARIABLE — a Windmill GUSTS (Perlin 0.35→1.0), a Solar panel follows DAYLIGHT (0 at night → 1 midday).</summary>
        public float CurrentOutput => renewable
            ? output * (solar ? Colony.Daylight : WindFactor())
            : (_fueled ? output : 0f);
        private float WindFactor() => 0.35f + 0.65f * Mathf.PerlinNoise(Time.time * 0.16f + _windSeed, _windSeed);
        public bool Fueled => _fueled;
        /// <summary>Fuel currently buffered (belt-fed), for the panel readout.</summary>
        public int FuelStored => fuel != null && Buffer != null ? Buffer.Count(fuel) : 0;

        public static PowerPlant Spawn(BuildingDefinition def, Vector3 pos, Belt.Dir inputSide = Belt.Dir.S)
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
            p.InputSide = inputSide;
            p.Buffer = new Inventory { capacity = 20 };
            p._sr = sr;
            p._baseColor = def.color;
            p.renewable = def.renewable || def.solar;
            p.solar = def.solar;
            p._windSeed = Random.value * 100f; // each windmill gusts on its own phase

            // Register the footprint so belts can deposit fuel on the input edge, and show the cyan notch.
            p._cells = Footprint.Cells(go.transform.position, def.FootW, def.FootH);
            foreach (var c in p._cells) WorldGrid.Generators[c] = p;
            if (p.fuel != null) Ports.MakeInputNotch(go.transform, inputSide); // belts feed fuel here

            // Wired-grid node: a generator links to up to 4 poles/batteries/machines (no radius — the
            // player draws the wires). See PowerNode / PowerNet.
            var node = go.AddComponent<PowerNode>();
            node.role = PowerNode.Role.Generator;
            node.maxConnections = 4;
            node.generator = p;
            return p;
        }

        void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= interval)
            {
                _timer -= interval;
                // A generator burns FUEL it's actually been fed — belt fuel into its cyan intake. No more
                // silently draining your carried pile: out of belt-fed fuel → no power (the grid browns out).
                if (fuel == null) _fueled = true; // (no current generator is fuel-free)
                else _fueled = Buffer != null && Buffer.Count(fuel) >= fuelPerCycle && Buffer.RemoveUpTo(fuel, fuelPerCycle) >= fuelPerCycle;
            }
            if (_sr != null)
                _sr.color = _fueled ? _baseColor : Color.Lerp(_baseColor, Color.black, 0.55f);
        }
    }
}
