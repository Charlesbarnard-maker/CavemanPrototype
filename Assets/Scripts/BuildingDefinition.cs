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

    public enum BuildingKind
    {
        Collector, // auto-harvests a nearby resource patch (needs workers assigned)
        Storage,   // holds a large amount of one resource type
        Housing,   // raises the population cap
        Workshop,  // converts input resources into an output (a recipe)
        Logistics, // (legacy) houses transporters that carry goods between buildings
        Belt,      // a directional conveyor segment placed on the grid
        Depot,     // a long-distance transfer station (route endpoint)
        Route,     // a tool: link two depots with a caravan vehicle
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

        [Header("Housing")]
        [Tooltip("How many people this houses (adds to population cap).")]
        public int houseCapacity = 0;

        [Header("Capacity")]
        [Tooltip("Collector buffer size, or storage size.")]
        public int capacity = 10;
        [Tooltip("Storage: player chooses which resource it holds (a generic warehouse).")]
        public bool configurable = false;

        [Header("Progression")]
        [Tooltip("Age at which this becomes buildable (0 = Stone Age, available from the start).")]
        public int unlockAge = 0;

        [Header("Logistics")]
        [Tooltip("Transport that runs WITHOUT a worker (a conveyor / wooden roller).")]
        public bool mechanical = false;
        [Tooltip("How far a transport hub's carriers will travel to serve a source.")]
        public float logisticsRange = 14f;
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
