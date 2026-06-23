using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Builds the entire MVP scene in code so there's no Inspector wiring: camera,
    /// player, Colony (population), a pre-placed Town Hall, resource/food patches,
    /// the build system, and the HUD. Add this component to one empty GameObject
    /// and press Play.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        void Start()
        {
            // --- Items ---
            var stone = MakeItem("stone", "Stone", new Color(0.55f, 0.55f, 0.62f));
            var wood = MakeItem("wood", "Wood", new Color(0.52f, 0.34f, 0.16f));
            var food = MakeItem("food", "Food", new Color(0.85f, 0.35f, 0.35f));
            food.foodValue = 1;
            var planks = MakeItem("planks", "Planks", new Color(0.74f, 0.58f, 0.34f));
            var cookedFood = MakeItem("cooked", "Cooked Food", new Color(0.92f, 0.56f, 0.30f));
            cookedFood.foodValue = 3; // cooking turns 2 raw food into 1 cooked worth 3 — a real gain

            // --- Buildings ---
            var woodHut = MakeCollector("Wood Hut", wood, 1, 2.0f, 2, 12, new Color(0.80f, 0.52f, 0.25f),
                new ItemAmount(wood, 5), new ItemAmount(stone, 3));
            var stonePit = MakeCollector("Stone Pit", stone, 1, 2.5f, 2, 12, new Color(0.45f, 0.52f, 0.62f),
                new ItemAmount(wood, 5), new ItemAmount(stone, 5));
            var foragerHut = MakeCollector("Forager Hut", food, 1, 2.0f, 2, 12, new Color(0.78f, 0.40f, 0.40f),
                new ItemAmount(wood, 4));
            var sawmill = MakeWorkshop("Sawmill", planks, 1, 2.5f, 2, 12, new Color(0.66f, 0.50f, 0.30f),
                new List<ItemAmount> { new ItemAmount(wood, 2) },
                new ItemAmount(wood, 6), new ItemAmount(stone, 4));
            var campfire = MakeWorkshop("Campfire", cookedFood, 1, 3.0f, 2, 12, new Color(0.85f, 0.45f, 0.25f),
                new List<ItemAmount> { new ItemAmount(food, 2) },
                new ItemAmount(wood, 4), new ItemAmount(stone, 2));
            var woodStore = MakeStorage("Wood Warehouse", wood, 100, new Color(0.62f, 0.40f, 0.20f),
                new ItemAmount(wood, 8));
            var stoneStore = MakeStorage("Stone Storage", stone, 100, new Color(0.40f, 0.43f, 0.50f),
                new ItemAmount(wood, 8));
            var foodStore = MakeStorage("Granary", food, 100, new Color(0.70f, 0.45f, 0.35f),
                new ItemAmount(wood, 8));
            var house = MakeHousing("House", 2, new Color(0.72f, 0.62f, 0.45f),
                new ItemAmount(wood, 8), new ItemAmount(stone, 4), new ItemAmount(planks, 3));
            var mammothShack = MakeLogistics("Mammoth Shack", 2, new Color(0.46f, 0.40f, 0.55f),
                new ItemAmount(wood, 6), new ItemAmount(stone, 2));

            // --- Camera (follows the player) ---
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.16f, 0.19f, 0.16f);

            // --- Player ---
            var player = MakeSprite("Player", new Color(0.92f, 0.82f, 0.25f), Vector2.zero, 0.7f, 10, PlaceholderArt.Circle());
            player.AddComponent<PlayerController>();
            var gatherer = player.AddComponent<PlayerGatherer>();
            var builder = player.AddComponent<BuildController>();
            builder.gatherer = gatherer;
            builder.placeNodeRange = 6f;
            builder.buildables = new List<BuildingDefinition>
            { woodHut, stonePit, foragerHut, sawmill, campfire, woodStore, stoneStore, foodStore, house, mammothShack };

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;

            // --- Colony (population) ---
            var colony = new GameObject("Colony").AddComponent<Colony>();
            colony.foodItem = food;
            colony.carried = gatherer.Inventory;
            colony.SetStartingPopulation(3);
            gatherer.Inventory.Add(food, 50); // generous starting larder while you learn the ropes

            // --- Town Hall (pre-placed, houses 3) ---
            var townHallDef = MakeHousing("Town Hall", 3, new Color(0.60f, 0.50f, 0.70f));
            var townHall = HousingBuilding.Spawn(townHallDef, new Vector2(0f, -1.6f));
            townHall.isHQ = true; // builders are managed from here

            // --- HUD ---
            var hud = new GameObject("HUD").AddComponent<InventoryHud>();
            hud.gatherer = gatherer;
            hud.builder = builder;
            hud.woodItem = wood;
            hud.stoneItem = stone;
            hud.foodItem = food;

            // --- Resource patches ---
            var treeColor = new Color(0.27f, 0.55f, 0.22f);
            SpawnNode("Tree", wood, treeColor, new Vector2(5f, 2.5f), 1.3f, PlaceholderArt.Triangle());
            SpawnNode("Tree", wood, treeColor, new Vector2(7f, 1f), 1.2f, PlaceholderArt.Triangle());
            SpawnNode("Tree", wood, treeColor, new Vector2(6f, -1.5f), 1.4f, PlaceholderArt.Triangle());
            SpawnNode("Tree", wood, treeColor, new Vector2(8.5f, 2.2f), 1.1f, PlaceholderArt.Triangle());

            var rockColor = new Color(0.55f, 0.55f, 0.6f);
            SpawnNode("Rock", stone, rockColor, new Vector2(-5f, 2.5f), 1.2f, PlaceholderArt.Hexagon());
            SpawnNode("Rock", stone, rockColor, new Vector2(-7f, 1f), 1.1f, PlaceholderArt.Hexagon());
            SpawnNode("Rock", stone, rockColor, new Vector2(-6f, -1.5f), 1.3f, PlaceholderArt.Hexagon());
            SpawnNode("Rock", stone, rockColor, new Vector2(-8.5f, 2.2f), 1.0f, PlaceholderArt.Hexagon());

            // Bushes (food) — bottom of the map
            var bushColor = new Color(0.45f, 0.55f, 0.25f);
            SpawnNode("Bush", food, bushColor, new Vector2(-1.5f, -5f), 0.8f, PlaceholderArt.Circle());
            SpawnNode("Bush", food, bushColor, new Vector2(0.5f, -5.5f), 0.8f, PlaceholderArt.Circle());
            SpawnNode("Bush", food, bushColor, new Vector2(2.5f, -4.5f), 0.8f, PlaceholderArt.Circle());
        }

        private static ItemDefinition MakeItem(string id, string name, Color color)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.id = id;
            item.displayName = name;
            item.color = color;
            return item;
        }

        private static BuildingDefinition MakeCollector(string name, ItemDefinition item, int output,
            float interval, int maxWorkers, int capacity, Color color, params ItemAmount[] cost)
        {
            var def = ScriptableObject.CreateInstance<BuildingDefinition>();
            def.displayName = name;
            def.kind = BuildingKind.Collector;
            def.item = item;
            def.outputPerCycle = output;
            def.interval = interval;
            def.maxWorkers = maxWorkers;
            def.capacity = capacity;
            def.color = color;
            def.cost = new List<ItemAmount>(cost);
            return def;
        }

        private static BuildingDefinition MakeStorage(string name, ItemDefinition item, int capacity,
            Color color, params ItemAmount[] cost)
        {
            var def = ScriptableObject.CreateInstance<BuildingDefinition>();
            def.displayName = name;
            def.kind = BuildingKind.Storage;
            def.item = item;
            def.capacity = capacity;
            def.color = color;
            def.cost = new List<ItemAmount>(cost);
            return def;
        }

        private static BuildingDefinition MakeWorkshop(string name, ItemDefinition output, int outPer,
            float processTime, int maxWorkers, int capacity, Color color, List<ItemAmount> inputs, params ItemAmount[] cost)
        {
            var def = ScriptableObject.CreateInstance<BuildingDefinition>();
            def.displayName = name;
            def.kind = BuildingKind.Workshop;
            def.item = output;
            def.outputPerCycle = outPer;
            def.interval = processTime;
            def.maxWorkers = maxWorkers;
            def.capacity = capacity;
            def.color = color;
            def.inputs = inputs;
            def.cost = new List<ItemAmount>(cost);
            return def;
        }

        private static BuildingDefinition MakeLogistics(string name, int maxWorkers, Color color, params ItemAmount[] cost)
        {
            var def = ScriptableObject.CreateInstance<BuildingDefinition>();
            def.displayName = name;
            def.kind = BuildingKind.Logistics;
            def.maxWorkers = maxWorkers;
            def.color = color;
            def.cost = new List<ItemAmount>(cost);
            return def;
        }

        private static BuildingDefinition MakeHousing(string name, int houseCapacity, Color color, params ItemAmount[] cost)
        {
            var def = ScriptableObject.CreateInstance<BuildingDefinition>();
            def.displayName = name;
            def.kind = BuildingKind.Housing;
            def.houseCapacity = houseCapacity;
            def.color = color;
            def.cost = new List<ItemAmount>(cost);
            return def;
        }

        private static void SpawnNode(string name, ItemDefinition item, Color color, Vector2 pos, float size, Sprite sprite)
        {
            var go = MakeSprite(name, color, pos, size, 0, sprite);
            go.AddComponent<BoxCollider2D>();
            var node = go.AddComponent<ResourceNode>();
            node.yields = item;
            node.capacity = 30;
            node.regenAmount = 1;
            node.regenInterval = 1.5f;
        }

        private static GameObject MakeSprite(string name, Color color, Vector2 pos, float size, int sortingOrder, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * size;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return go;
        }
    }
}
