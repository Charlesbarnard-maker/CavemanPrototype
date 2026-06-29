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
        [Tooltip("Liquid (e.g. water): moves only via pipes / carrying, NEVER on belts.")]
        public bool isLiquid = false;
        [Tooltip("Bulk raw with its own dedicated stockpile (Stone/Ore): the configurable Warehouse won't hold it.")]
        public bool noWarehouse = false;
        [Tooltip("Icon shown on belts (and later in UI). Resolved from `sprite` via SpriteDatabase — do not set directly.")]
        public Sprite icon = null;
        [Tooltip("Visual reference resolved via SpriteDatabase — external sprite if one exists, else the procedural fallback shape.")]
        public SpriteDefinition sprite = new SpriteDefinition("", PlaceholderShape.Circle);
        [TextArea]
        [Tooltip("Short blurb for the in-game Guide: what it is, where it comes from, what it's for.")]
        public string description = "";
    }
}
