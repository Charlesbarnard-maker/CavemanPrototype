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

    public enum BuildingKind
    {
        Collector, // auto-harvests a nearby resource patch (fully automatic — no workers)
        Storage,   // holds a large amount of one resource type
        Workshop,  // converts input resources into an output (a recipe)
        Belt,      // a directional conveyor segment placed on the grid
        Depot,     // a long-distance transfer station (route endpoint)
        Route,     // a tool: link two depots with a caravan vehicle
        Power,     // a generator: burns fuel to supply electrical power (Industrial age)
        Hearth,    // burns fuel to project a HEAT RADIUS (Stone-age proximity power — no grid/wires)
        Bridge,    // a plank tile placed on water to make it passable (feet + belts)
        Pipe,      // a liquid-network segment (continuous flow, not items)
        Pump,      // draws water from adjacent water terrain into the pipe network
        Research,  // a Research Lodge: consumes crafted research items into research points
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

        [Header("Progression")]
        [Tooltip("Age at which this becomes buildable (0 = Stone Age, available from the start).")]
        public int unlockAge = 0;
        [Tooltip("Optional Research Tech id that must be PURCHASED before this can be built (empty = none).")]
        public string requiredTech = "";

        [Header("Power (Industrial age)")]
        [Tooltip("Workshop: electrical power drawn while running (0 = none).")]
        public int powerDraw = 0;
        [Tooltip("Power plant: electrical power supplied while fuelled.")]
        public int powerOutput = 0;

        [Header("Energy — Stone-age heat radius")]
        [Tooltip("Hearth: heat coverage radius in world units (machines inside a lit hearth can run).")]
        public float heatRadius = 0f;
        [Tooltip("Workshop: needs energy to run (a lit Hearth's heat radius now, electricity later) — HARD-STOPS without it.")]
        public bool requiresPower = false;

        [Header("Logistics")]
        [Tooltip("Route vehicles only: travel speed (cart < caravan < train < drone).")]
        public float vehicleSpeed = 3.5f;

        [Header("Visuals / cost")]
        public Color color = Color.white;
        public List<ItemAmount> cost = new();

        [Header("Help")]
        [TextArea]
        [Tooltip("Build-menu tooltip. If blank, a description is auto-generated from the data.")]
        public string description = "";
    }
}
