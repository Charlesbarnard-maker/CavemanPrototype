using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Data definition for a gatherable/craftable item (Stone, Wood, ...).
    /// This is the data-driven core: new resources are new assets, not new code.
    /// For now we create instances at runtime in GameBootstrap; later you'll
    /// author these as .asset files via Assets > Create > Caveman > Item Definition.
    /// </summary>
    [CreateAssetMenu(fileName = "Item", menuName = "Caveman/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        public string id = "stone";
        public string displayName = "Stone";
        [Tooltip("Placeholder tint until we have real art.")]
        public Color color = Color.gray;
        public int maxStack = 999;
        [Tooltip("Nourishment per unit when eaten (0 = not food). Cooked food is worth more.")]
        public int foodValue = 0;
        [TextArea]
        [Tooltip("Short blurb for the in-game Guide: what it is, where it comes from, what it's for.")]
        public string description = "";
    }
}
