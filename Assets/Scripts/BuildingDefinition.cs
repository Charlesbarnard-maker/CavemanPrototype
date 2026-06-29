using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>A quantity of a particular item — used for build/recipe costs.</summary>
    [System.Serializable]
    public class ItemAmount
    {
        public ItemDefinition item;
        public int amount = 1;

        public ItemAmount() { }
        public ItemAmount(ItemDefinition item, int amount)
        {
            this.item = item;
            this.amount = amount;
        }
    }

    /// <summary>One selectable workshop recipe — for a CONFIGURABLE multi-recipe machine (e.g. the
    /// Basic Smelter: Copper or Iron; the Advanced Smelter: Bronze or Steel). When a BuildingDefinition
    /// lists recipes, the placed workshop picks one (cycled in its panel) and that recipe drives its
    /// output/inputs/processTime; a plain single-recipe workshop leaves `recipes` empty and uses the
    /// def's item/inputs/interval directly. A recipe is only selectable once the colony reaches its
    /// `unlockAge` (so e.g. the Steel recipe stays Iron-age-gated inside the Advanced Smelter).</summary>
    [System.Serializable]
    public class Recipe
    {
        public ItemDefinition output;
        public int outputPerCycle = 1;
        public float processTime = 3f;
        public List<ItemAmount> inputs = new();
        public int unlockAge = 0;

        public Recipe() { }
        public Recipe(ItemDefinition output, int outputPerCycle, float processTime, int unlockAge, params ItemAmount[] inputs)
        {
            this.output = output; this.outputPerCycle = outputPerCycle; this.processTime = processTime;
            this.unlockAge = unlockAge; this.inputs = new List<ItemAmount>(inputs);
        }
    }

    /// <summary>One manual, paid UPGRADE step for a production building — bought from its panel once the
    /// colony reaches <see cref="unlockAge"/>. It multiplies the building's base rate by <see cref="speedMult"/>
    /// and recolours it (a stand-in for the per-age art) — the "stone tools → metal tools → machines" ladder.</summary>
    [System.Serializable]
    public class UpgradeTier
    {
        public string name = "Upgrade";
        public int unlockAge = 1;        // colony age required before this tier can be bought
        public float speedMult = 1.5f;   // ABSOLUTE rate multiplier vs the building's base (tiers ascend)
        public Color tint = Color.white; // look change until real per-age art lands
        public List<ItemAmount> cost = new();

        public UpgradeTier() { }
        public UpgradeTier(string name, int unlockAge, float speedMult, Color tint, params ItemAmount[] cost)
        {
            this.name = name; this.unlockAge = unlockAge; this.speedMult = speedMult; this.tint = tint;
            this.cost = new List<ItemAmount>(cost);
        }
    }

    public enum BuildingKind
    {
        Collector, // auto-harvests a nearby resource patch (fully automatic — no workers)
        Storage,   // holds a large amount of one resource type
        Workshop,  // converts input resources into an output (a recipe)
        Belt,      // a directional conveyor segment placed on the grid
        Depot,     // a long-distance transfer station (route endpoint)
        Route,     // a tool: link two depots with a caravan vehicle
        Power,     // a generator: burns fuel to supply power to the wired network
        Pole,      // a power pole: a wired relay node, linking generators/batteries to consumers
        Battery,   // stores surplus power and releases it on a deficit (a wired node)
        Bridge,    // a plank tile placed on water to make it passable (feet + belts)
        Pipe,      // a liquid-network segment (continuous flow, not items)
        Pump,      // draws water from adjacent water terrain into the pipe network
        Rail,      // a track segment — route vehicles (trains) path along laid rail
        Signal,    // a rail signal — one-way + block control for train planning
        Research,  // a Research Lodge: consumes crafted research items into research points
        Garage,    // parks the player's bought MOUNTS — buy/switch your ride from its panel
    }

    /// <summary>Data for a placeable structure.</summary>
    [CreateAssetMenu(fileName = "Building", menuName = "Caveman/Building Definition")]
    public class BuildingDefinition : ScriptableObject
    {
        public string displayName = "Wood Hut";
        public BuildingKind kind = BuildingKind.Collector;

        [Tooltip("Collector: the resource it produces. Storage: the resource it holds.")]
        public ItemDefinition item;

        [Header("Collector / Workshop")]
        public int outputPerCycle = 1;
        [Tooltip("Seconds between each automatic harvest / process cycle.")]
        public float interval = 2f;
        [Tooltip("Max workers that can be assigned (each is one harvesting NPC / speeds processing).")]
        public int maxWorkers = 2;
        [Tooltip("Workshop only: input resources consumed per cycle to make `item`.")]
        public List<ItemAmount> inputs = new();
        [Tooltip("Workshop only: optional SELECTABLE recipes (a configurable multi-recipe machine, e.g. Basic/Advanced Smelter). Empty = a single fixed recipe from item/inputs/interval above.")]
        public List<Recipe> recipes = new();

        [Header("Footprint")]
        [Tooltip("Building width in grid cells (1 = a single cell).")]
        public int footprintW = 1;
        [Tooltip("Building height in grid cells (1 = a single cell).")]
        public int footprintH = 1;
        public int FootW => Mathf.Max(1, footprintW);
        public int FootH => Mathf.Max(1, footprintH);

        [Header("Capacity")]
        [Tooltip("Collector buffer size, or storage size.")]
        public int capacity = 10;
        [Tooltip("Storage: player chooses which resource it holds (a generic warehouse).")]
        public bool configurable = false;
        [Tooltip("Collector: draws its resource from adjacent WATER terrain (not a node) — e.g. Water Hole.")]
        public bool fromWaterTerrain = false;
        [Tooltip("Collector: its worker CARRIES output to the nearest matching storage (no belt needed) — the early SURVIVAL collectors (Forager Hut, Water Hole) so food/water reach storage before the player learns belts.")]
        public bool autoStore = false;
        [Tooltip("Collector: when its node runs dry, how far (world units) it will look for a fresh node of the same type. 0 = use the default. Bounded + candidate-capped, so no map-wide scan.")]
        public float searchRadius = 0f;
        [Tooltip("Pump: a BOOSTER (no water source) — re-pressurises a pipe network to extend its range.")]
        public bool booster = false;
        [Tooltip("Belt: this is a 1→3 SPLITTER (distributes items evenly to three outputs) rather than a plain belt.")]
        public bool splitter = false;
        [Tooltip("Belt: this is an N→1 MERGER (deliberately combines two belt lines into one) rather than a plain belt.")]
        public bool merger = false;
        [Tooltip("Belt: this is an UNDERGROUND belt (entrance/exit pair) — items travel hidden up to a few tiles so surface belts/track can cross over the gap. Placed one click at a time; auto-pairs with an aligned end.")]
        public bool underground = false;
        [Tooltip("Belt: this is a FILTER belt — conveys ONLY its chosen item type (set in its panel), so unwanted items are turned away (back up / take another route).")]
        public bool filter = false;
        [Tooltip("Belt/Splitter: PRIORITY routing — a splitter fills its forward output first and only sends OVERFLOW to the sides (instead of an even 1→3 split).")]
        public bool priority = false;
        [Tooltip("Belt: this is a CONDITIONAL GATE — only passes items while the line it feeds still has room (its nearest downstream storage is below a fill threshold).")]
        public bool gate = false;
        [Tooltip("Signal: this is a TWO-WAY block signal (trains may pass in EITHER direction; it still enforces one-train-per-block) rather than a one-way signal.")]
        public bool bothWaySignal = false;
        [Tooltip("Rail: this is ELEVATED track — it does NOT reserve its cell, so a belt may pass UNDER it, and it renders raised (with a shadow). Lets a train line cross over conveyor lines.")]
        public bool elevated = false;
        [Tooltip("Depot: this is a HARBOUR (a dock placed on the shore) — no rail lane; cargo SHIPS run between harbours over water.")]
        public bool isHarbour = false;
        [Tooltip("Depot: this is a LIQUID HARBOUR — a harbour that connects by PIPE (not belts) and ships a FLUID between liquid harbours. Implies isHarbour.")]
        public bool isLiquidHarbour = false;
        [Tooltip("Build-menu category override (e.g. \"Boats\"). Empty = categorised by kind. Lets a Harbour (a Depot) sit under Boats while a Station sits under Trains.")]
        public string menuCategory = "";

        [Header("Progression")]
        [Tooltip("Age at which this becomes buildable (0 = Stone Age, available from the start).")]
        public int unlockAge = 0;
        [Tooltip("Manual, paid age-upgrade ladder (stone tools → metal → machines). Empty = not upgradable.")]
        public List<UpgradeTier> upgrades = new();
        [Tooltip("Optional Research Tech id that must be PURCHASED before this can be built (empty = none).")]
        public string requiredTech = "";

        [Header("Power (Industrial age)")]
        [Tooltip("Workshop: electrical power drawn while running (0 = none).")]
        public int powerDraw = 0;
        [Tooltip("Power plant: electrical power supplied while fuelled.")]
        public int powerOutput = 0;

        [Header("Energy — power network")]
        [Tooltip("Legacy (unused): the wired grid has no radius — connections are player-drawn wires.")]
        public float connectRange = 0f;
        [Tooltip("Legacy (unused): the wired grid has no radius — connections are player-drawn wires.")]
        public float supplyRange = 0f;
        [Tooltip("Workshop: needs a power connection to run — STOPS if not wired to a powered network.")]
        public bool requiresPower = false;
        [Tooltip("Battery: stored-energy capacity (power × seconds).")]
        public float batteryCapacity = 0f;
        [Tooltip("Battery: max power it can charge or discharge at once.")]
        public float batteryRate = 0f;

        [Header("Logistics")]
        [Tooltip("Route vehicles only: travel speed (cart < caravan < train < drone).")]
        public float vehicleSpeed = 3.5f;

        [Header("Visuals / cost")]
        public Color color = Color.white;
        [Tooltip("Visual reference resolved via SpriteDatabase — external sprite if one exists, else the procedural fallback shape.")]
        public SpriteDefinition sprite = new SpriteDefinition("", PlaceholderShape.Square);
        public List<ItemAmount> cost = new();

        [Header("Help")]
        [TextArea]
        [Tooltip("Build-menu tooltip. If blank, a description is auto-generated from the data.")]
        public string description = "";
    }
}
