using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>A quantity of a particular item — used for recipe costs.</summary>
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
    /// A craftable tool. For the MVP a tool simply multiplies how much you gather
    /// per hit while owned. This is the seed of the whole upgrade/tech tree:
    /// spend simple resources to improve how you collect more.
    /// </summary>
    [CreateAssetMenu(fileName = "Tool", menuName = "Caveman/Tool Definition")]
    public class ToolDefinition : ScriptableObject
    {
        public string displayName = "Stone Axe";

        [Tooltip("Multiplies resources gained per hit while this tool is owned.")]
        public int gatherPower = 2;

        public List<ItemAmount> cost = new();
    }
}
