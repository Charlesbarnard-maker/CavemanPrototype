// Bespoke item-sprite dispatch: map an item id to its hand-authored procedural sprite.
// Partial of PlaceholderArt. Mirrors BespokeBuilding — SpriteDatabase.ForItem tries this first.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        /// <summary>The hand-authored belt/icon sprite for item <paramref name="id"/>, or null to fall back to
        /// the generic shape. Every material item has one (so items read clearly on conveyors).</summary>
        public static Sprite BespokeItem(string id) => id switch
        {
            // Raw minerals
            "stone" => ItemStone(),
            "ore" => ItemIronOre(),
            "copper_ore" => ItemCopperOre(),
            "clay" => ItemClay(),
            "gems" => ItemGems(),
            "charcoal" => ItemCharcoal(),
            // Refined metals
            "metal" => ItemIron(),
            "copper" => ItemCopper(),
            "bronze_plate" => ItemBronzePlate(),
            "steel" => ItemSteel(),
            // Wood & fibre
            "wood" => ItemWood(),
            "planks" => ItemPlanks(),
            "fiber" => ItemFiber(),
            // Manufactured parts
            "tools" => ItemTools(),
            "bronze_gear" => ItemBronzeGear(),
            "steel_beam" => ItemSteelBeam(),
            "machine_part" => ItemMachinePart(),
            "engine" => ItemEngine(),
            "bricks" => ItemBricks(),
            "stoneblock" => ItemStoneBlock(),
            // Food chain
            "food" => ItemFood(),
            "cooked" => ItemCookedFood(),
            "meat" => ItemMeat(),
            "grain" => ItemGrain(),
            "flour" => ItemFlour(),
            "bread" => ItemBread(),
            // Luxury & textiles
            "jewelry" => ItemJewelry(),
            "cloth" => ItemCloth(),
            "clothes" => ItemClothes(),
            "pot" => ItemPottery(),
            // Research goods
            "idea" => ItemIdeaTablet(),
            "scroll" => ItemStudyScroll(),
            "schematic" => ItemSchematic(),
            "blueprint" => ItemBlueprint(),
            // Liquids & endgame
            "water" => ItemWater(),
            "oil" => ItemOil(),
            "fuel" => ItemFuel(),
            "monument" => ItemMonumentBlock(),
            _ => null,
        };
    }
}
