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
    }

    /// <summary>Data for a placeable structure.</summary>
    [CreateAssetMenu(fileName = "Building", menuName = "Caveman/Building Definition")]
    public class BuildingDefinition : ScriptableObject
    {
        public string displayName = "Wood Hut";
        public BuildingKind kind = BuildingKind.Collector;

        [Tooltip("Collector: the resource it produces. Storage: the resource it holds.")]
        public ItemDefinition item;

        [Header("Collector")]
        public int outputPerCycle = 1;
        [Tooltip("Seconds between each automatic harvest.")]
        public float interval = 2f;
        [Tooltip("Max workers that can be assigned (each is one harvesting NPC).")]
        public int maxWorkers = 2;

        [Header("Housing")]
        [Tooltip("How many people this houses (adds to population cap).")]
        public int houseCapacity = 0;

        [Header("Capacity")]
        [Tooltip("Collector buffer size, or storage size.")]
        public int capacity = 10;

        [Header("Visuals / cost")]
        public Color color = Color.white;
        public List<ItemAmount> cost = new();
    }
}
