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

    /// <summary>
    /// A placeable structure that produces a resource automatically over time.
    /// This is the heart of the game: spend manually-gathered resources to build
    /// something that gathers FOR you — removing manual work, not speeding it up.
    /// </summary>
    [CreateAssetMenu(fileName = "Building", menuName = "Caveman/Building Definition")]
    public class BuildingDefinition : ScriptableObject
    {
        public string displayName = "Wood Hut";
        public ItemDefinition produces;
        public int outputPerCycle = 1;
        [Tooltip("Seconds between each automatic output.")]
        public float interval = 2f;
        [Tooltip("Placeholder tint for the building.")]
        public Color color = Color.white;
        public List<ItemAmount> cost = new();
    }
}
