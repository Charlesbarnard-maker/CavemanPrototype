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
        Collector, // auto-harvests a nearby resource patch into its buffer
        Storage,   // holds a large amount of one resource type
    }

    /// <summary>
    /// Data for a placeable structure. Collectors harvest a patch into a small
    /// buffer and push to adjacent storage; Storage holds a large amount.
    /// </summary>
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

        [Header("Capacity")]
        [Tooltip("Collector buffer size, or storage size.")]
        public int capacity = 10;

        [Header("Visuals / cost")]
        public Color color = Color.white;
        public List<ItemAmount> cost = new();
    }
}
