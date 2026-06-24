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
            var water = MakeItem("water", "Water", new Color(0.30f, 0.55f, 0.85f)); // survival + crafting
            var meat = MakeItem("meat", "Meat", new Color(0.72f, 0.30f, 0.30f)); meat.foodValue = 2;
            var clay = MakeItem("clay", "Clay", new Color(0.70f, 0.45f, 0.35f));
            var charcoal = MakeItem("charcoal", "Charcoal", new Color(0.24f, 0.24f, 0.27f));
            var bricks = MakeItem("bricks", "Bricks", new Color(0.74f, 0.40f, 0.32f));
            var grain = MakeItem("grain", "Grain", new Color(0.86f, 0.76f, 0.36f));
            var flour = MakeItem("flour", "Flour", new Color(0.90f, 0.86f, 0.72f));
            var bread = MakeItem("bread", "Bread", new Color(0.80f, 0.58f, 0.30f)); bread.foodValue = 4;
            var ore = MakeItem("ore", "Ore", new Color(0.62f, 0.58f, 0.42f)); // rare — found far from base
            var metal = MakeItem("metal", "Metal", new Color(0.66f, 0.68f, 0.74f));
            var tools = MakeItem("tools", "Tools", new Color(0.55f, 0.60f, 0.68f));
            var monument = MakeItem("monument", "Monument Block", new Color(0.90f, 0.86f, 0.62f)); // endgame

            // --- Buildings ---
            var woodHut = MakeCollector("Wood Hut", wood, 1, 2.0f, 2, 12, new Color(0.80f, 0.52f, 0.25f),
                new ItemAmount(wood, 5), new ItemAmount(stone, 3));
            var stonePit = MakeCollector("Stone Pit", stone, 1, 2.5f, 2, 12, new Color(0.45f, 0.52f, 0.62f),
                new ItemAmount(wood, 5), new ItemAmount(stone, 5));
            var foragerHut = MakeCollector("Forager Hut", food, 1, 2.0f, 2, 12, new Color(0.78f, 0.40f, 0.40f),
                new ItemAmount(wood, 4));
            var waterHole = MakeCollector("Water Hole", water, 1, 2.0f, 2, 12, new Color(0.40f, 0.62f, 0.85f),
                new ItemAmount(wood, 4));
            var sawmill = MakeWorkshop("Sawmill", planks, 1, 2.5f, 2, 12, new Color(0.66f, 0.50f, 0.30f),
                new List<ItemAmount> { new ItemAmount(wood, 2) },
                new ItemAmount(wood, 6), new ItemAmount(stone, 4));
            // Campfire: needs Wood as fuel and Water to cook, plus the raw Food.
            var campfire = MakeWorkshop("Campfire", cookedFood, 1, 3.0f, 2, 12, new Color(0.85f, 0.45f, 0.25f),
                new List<ItemAmount> { new ItemAmount(food, 2), new ItemAmount(wood, 1), new ItemAmount(water, 1) },
                new ItemAmount(wood, 4), new ItemAmount(stone, 2));
            var woodStore = MakeStorage("Wood Warehouse", wood, 100, new Color(0.62f, 0.40f, 0.20f),
                new ItemAmount(wood, 8));
            var stoneStore = MakeStorage("Stone Storage", stone, 100, new Color(0.40f, 0.43f, 0.50f),
                new ItemAmount(wood, 8));
            var foodStore = MakeStorage("Granary", food, 100, new Color(0.70f, 0.45f, 0.35f),
                new ItemAmount(wood, 8));
            var waterStore = MakeStorage("Water Barrel", water, 100, new Color(0.35f, 0.50f, 0.72f),
                new ItemAmount(wood, 8));
            // Generic warehouse: the player picks what it stores (e.g. Planks, Cooked Food).
            var warehouse = MakeStorage("Warehouse", null, 120, new Color(0.55f, 0.52f, 0.45f),
                new ItemAmount(wood, 10)); warehouse.configurable = true;
            var house = MakeHousing("House", 2, new Color(0.72f, 0.62f, 0.45f),
                new ItemAmount(wood, 8), new ItemAmount(stone, 4), new ItemAmount(planks, 3));
            // Long-distance logistics: depots + caravan routes (replaces the old haulers).
            var depot = ScriptableObject.CreateInstance<BuildingDefinition>();
            depot.displayName = "Depot"; depot.kind = BuildingKind.Depot; depot.unlockAge = 0;
            depot.item = null; depot.capacity = 80; depot.color = new Color(0.50f, 0.45f, 0.55f);
            depot.cost = new List<ItemAmount> { new ItemAmount(wood, 10), new ItemAmount(stone, 6) };
            var caravan = ScriptableObject.CreateInstance<BuildingDefinition>();
            caravan.displayName = "Caravan (Elephant)"; caravan.kind = BuildingKind.Route; caravan.unlockAge = 0;
            caravan.capacity = 12; caravan.color = new Color(0.55f, 0.50f, 0.50f);
            caravan.cost = new List<ItemAmount> { new ItemAmount(wood, 8) };

            // --- Age 1: Tribal ---
            var hunter = MakeCollector("Hunter's Hut", meat, 1, 2.5f, 2, 12, new Color(0.66f, 0.34f, 0.34f),
                new ItemAmount(wood, 6)); hunter.unlockAge = 1;
            var clayPit = MakeCollector("Clay Pit", clay, 1, 2.5f, 2, 12, new Color(0.68f, 0.46f, 0.36f),
                new ItemAmount(wood, 5)); clayPit.unlockAge = 1;
            var charcoalBurner = MakeWorkshop("Charcoal Burner", charcoal, 1, 3.0f, 2, 12, new Color(0.30f, 0.30f, 0.33f),
                new List<ItemAmount> { new ItemAmount(wood, 2) },
                new ItemAmount(wood, 6), new ItemAmount(stone, 4)); charcoalBurner.unlockAge = 1;
            var clayStore = MakeStorage("Clay Pile", clay, 100, new Color(0.60f, 0.42f, 0.34f),
                new ItemAmount(wood, 8)); clayStore.unlockAge = 1;
            var smokehouse = MakeStorage("Smokehouse", meat, 100, new Color(0.55f, 0.32f, 0.30f),
                new ItemAmount(wood, 8), new ItemAmount(stone, 2)); smokehouse.unlockAge = 1;
            var longhouse = MakeHousing("Longhouse", 5, new Color(0.66f, 0.56f, 0.42f),
                new ItemAmount(wood, 14), new ItemAmount(planks, 6)); longhouse.unlockAge = 1;

            // --- Age 2: Bronze ---
            var kiln = MakeWorkshop("Kiln", bricks, 1, 3.5f, 2, 12, new Color(0.70f, 0.42f, 0.34f),
                new List<ItemAmount> { new ItemAmount(clay, 2), new ItemAmount(charcoal, 1) },
                new ItemAmount(wood, 8), new ItemAmount(stone, 6)); kiln.unlockAge = 2;
            var farm = MakeWorkshop("Farm", grain, 2, 3.0f, 3, 16, new Color(0.80f, 0.72f, 0.38f),
                new List<ItemAmount> { new ItemAmount(water, 1) },
                new ItemAmount(wood, 8)); farm.unlockAge = 2;
            var mill = MakeWorkshop("Mill", flour, 1, 2.5f, 2, 12, new Color(0.85f, 0.80f, 0.65f),
                new List<ItemAmount> { new ItemAmount(grain, 2) },
                new ItemAmount(wood, 8), new ItemAmount(planks, 4)); mill.unlockAge = 2;
            var bakery = MakeWorkshop("Bakery", bread, 1, 3.5f, 2, 12, new Color(0.82f, 0.60f, 0.34f),
                new List<ItemAmount> { new ItemAmount(flour, 1), new ItemAmount(water, 1) },
                new ItemAmount(planks, 5), new ItemAmount(bricks, 4)); bakery.unlockAge = 2;
            var brickStore = MakeStorage("Brick Yard", bricks, 100, new Color(0.66f, 0.40f, 0.34f),
                new ItemAmount(wood, 8)); brickStore.unlockAge = 2;
            var woodBelt = ScriptableObject.CreateInstance<BuildingDefinition>();
            woodBelt.displayName = "Wooden Belt"; woodBelt.kind = BuildingKind.Belt; woodBelt.unlockAge = 0;
            woodBelt.interval = 1.1f; // slow — the early tier
            woodBelt.color = new Color(0.60f, 0.50f, 0.35f);
            woodBelt.cost = new List<ItemAmount> { new ItemAmount(wood, 1) };
            var fastBelt = ScriptableObject.CreateInstance<BuildingDefinition>();
            fastBelt.displayName = "Conveyor Belt"; fastBelt.kind = BuildingKind.Belt; fastBelt.unlockAge = 2;
            fastBelt.interval = 0.45f; // fast — the upgrade
            fastBelt.color = new Color(0.72f, 0.63f, 0.40f);
            fastBelt.cost = new List<ItemAmount> { new ItemAmount(planks, 1) };

            // Exploration payoff: Ore is mined from distant veins, hauled home, and is
            // required to reach the Iron Age.
            var mine = MakeCollector("Mine", ore, 1, 3.0f, 2, 12, new Color(0.50f, 0.48f, 0.40f),
                new ItemAmount(wood, 6), new ItemAmount(stone, 4)); mine.unlockAge = 1;
            var oreStore = MakeStorage("Ore Stockpile", ore, 100, new Color(0.52f, 0.50f, 0.42f),
                new ItemAmount(wood, 8)); oreStore.unlockAge = 1;
            // Smelting: Ore + Charcoal -> Metal (charcoal is shared with the Kiln -> cascade).
            var smelter = MakeWorkshop("Smelter", metal, 1, 3.5f, 2, 12, new Color(0.55f, 0.50f, 0.50f),
                new List<ItemAmount> { new ItemAmount(ore, 1), new ItemAmount(charcoal, 1) },
                new ItemAmount(stone, 10), new ItemAmount(clay, 6)); smelter.unlockAge = 2;
            // Toolmaker: Metal + Planks -> Tools (an Iron-age comfort good).
            var toolmaker = MakeWorkshop("Toolmaker", tools, 1, 4.0f, 2, 12, new Color(0.50f, 0.55f, 0.60f),
                new List<ItemAmount> { new ItemAmount(metal, 1), new ItemAmount(planks, 1) },
                new ItemAmount(planks, 8), new ItemAmount(bricks, 6)); toolmaker.unlockAge = 3;
            // Endgame: the Monument (Industrial age). A long resource sink you pour the
            // top of every production chain into — completing it (10 blocks) is the win.
            var monumentBldg = MakeWorkshop("Monument", monument, 1, 6.0f, 3, 12, new Color(0.88f, 0.84f, 0.62f),
                new List<ItemAmount> { new ItemAmount(metal, 2), new ItemAmount(tools, 1), new ItemAmount(bricks, 2), new ItemAmount(planks, 2) },
                new ItemAmount(bricks, 20), new ItemAmount(metal, 15), new ItemAmount(tools, 8)); monumentBldg.unlockAge = 4;

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

            // --- Ground backdrop (behind everything; revealed as the fog clears) ---
            MakeSprite("Ground", new Color(0.22f, 0.31f, 0.19f), Vector2.zero, 220f, -100, PlaceholderArt.Square());

            // --- Player ---
            var player = MakeSprite("Player", new Color(0.92f, 0.82f, 0.25f), Vector2.zero, 0.7f, 10, PlaceholderArt.Circle());
            player.AddComponent<PlayerController>();
            var gatherer = player.AddComponent<PlayerGatherer>();
            var builder = player.AddComponent<BuildController>();
            builder.gatherer = gatherer;
            builder.placeNodeRange = 6f;
            builder.buildables = new List<BuildingDefinition>
            { woodHut, stonePit, foragerHut, waterHole, sawmill, campfire,
              woodStore, stoneStore, foodStore, waterStore, warehouse, house,
              hunter, clayPit, charcoalBurner, clayStore, smokehouse, longhouse,
              kiln, farm, mill, bakery, brickStore, woodBelt, fastBelt,
              mine, oreStore, smelter, toolmaker, monumentBldg, depot, caravan };

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;

            // --- Fog of war (explore to reveal the map) ---
            var fog = new GameObject("FogOfWar").AddComponent<FogOfWar>();
            fog.target = player.transform;

            // --- Colony (population) ---
            var colony = new GameObject("Colony").AddComponent<Colony>();
            colony.foodItem = food;
            colony.waterItem = water;
            colony.carried = gatherer.Inventory;
            colony.SetStartingPopulation(5);
            gatherer.Inventory.Add(food, 90);   // generous starting larder while you learn the ropes
            gatherer.Inventory.Add(water, 90);  // generous starting water
            colony.ageReqs = new List<Colony.AgeReq>
            {
                new Colony.AgeReq { pop = 8,  cost = new List<ItemAmount> { new ItemAmount(wood, 40), new ItemAmount(stone, 25) } },
                new Colony.AgeReq { pop = 12, cost = new List<ItemAmount> { new ItemAmount(planks, 25), new ItemAmount(clay, 20), new ItemAmount(stone, 30) } },
                new Colony.AgeReq { pop = 18, cost = new List<ItemAmount> { new ItemAmount(bricks, 30), new ItemAmount(planks, 30), new ItemAmount(ore, 20) } },
                new Colony.AgeReq { pop = 28, cost = new List<ItemAmount> { new ItemAmount(metal, 30), new ItemAmount(tools, 15), new ItemAmount(planks, 40) } },
            };
            // Comfort goods (the demand sink): people want better food as the colony evolves.
            colony.comforts = new List<Colony.Comfort>
            {
                new Colony.Comfort { item = cookedFood, unlockAge = 1 }, // Tribal: want cooked food
                new Colony.Comfort { item = bread,      unlockAge = 2 }, // Bronze: want bread too
                new Colony.Comfort { item = tools,      unlockAge = 3 }, // Iron: want tools
            };

            // --- Town Hall (pre-placed, houses 3) ---
            var townHallDef = MakeHousing("Town Hall", 5, new Color(0.60f, 0.50f, 0.70f));
            var townHall = HousingBuilding.Spawn(townHallDef, new Vector2(0f, -1.6f));
            townHall.isHQ = true; // builders are managed from here

            // --- HUD ---
            var hud = new GameObject("HUD").AddComponent<InventoryHud>();
            hud.gatherer = gatherer;
            hud.builder = builder;
            hud.woodItem = wood;
            hud.stoneItem = stone;
            hud.foodItem = food;
            hud.waterItem = water;
            hud.monumentItem = monument;
            hud.debugItems = new List<ItemDefinition>
            { wood, stone, food, water, planks, cookedFood, meat, clay, charcoal, bricks, grain, flour, bread, ore, metal, tools, monument };

            // --- Guided objectives ladder (the "what next / why advance" hook) ---
            var carriedInv = gatherer.Inventory;
            int Have(ItemDefinition i) => Economy.Available(i, carriedInv);
            bool HasCollectorOf(ItemDefinition i) { foreach (var p in ProductionBuilding.All) if (p != null && p.produces == i) return true; return false; }
            bool HasWorkshopOf(ItemDefinition i) { foreach (var w in WorkshopBuilding.All) if (w != null && w.output == i) return true; return false; }
            int Pop() => Colony.Instance != null ? Colony.Instance.Population : 0;
            int AgeNow() => Colony.Instance != null ? Colony.Instance.Age : 0;
            var objectives = new GameObject("Objectives").AddComponent<Objectives>();
            objectives.quests = new List<Quest>
            {
                new Quest { title = "Gather 12 Wood by hand",            done = () => Have(wood) >= 12,      reward = () => carriedInv.Add(stone, 8),  rewardText = "+8 Stone" },
                new Quest { title = "Build a Forager Hut for food",      done = () => HasCollectorOf(food),  reward = () => carriedInv.Add(food, 20),  rewardText = "+20 Food" },
                new Quest { title = "Build a Water Hole",                done = () => HasCollectorOf(water), reward = () => carriedInv.Add(water, 20), rewardText = "+20 Water" },
                new Quest { title = "Grow your settlement to 8 people",  done = () => Pop() >= 8,            reward = () => carriedInv.Add(wood, 25),  rewardText = "+25 Wood" },
                new Quest { title = "Advance to the Tribal Age",         done = () => AgeNow() >= 1,         reward = () => carriedInv.Add(stone, 25), rewardText = "+25 Stone" },
                new Quest { title = "Build a Sawmill & make 25 Planks",  done = () => Have(planks) >= 25,    reward = () => carriedInv.Add(planks, 10),rewardText = "+10 Planks" },
                new Quest { title = "Lay 5 conveyor belts",              done = () => Belt.Count >= 5,       reward = () => carriedInv.Add(wood, 15),  rewardText = "+15 Wood" },
                new Quest { title = "Run a Caravan route (2 depots)",    done = () => RouteVehicle.All.Count >= 1, reward = () => carriedInv.Add(wood, 30), rewardText = "+30 Wood" },
                new Quest { title = "Cook 15 Cooked Food",               done = () => Have(cookedFood) >= 15,reward = () => carriedInv.Add(stone, 15), rewardText = "+15 Stone" },
                new Quest { title = "Grow to 12 people",                 done = () => Pop() >= 12,           reward = () => carriedInv.Add(planks, 15),rewardText = "+15 Planks" },
                new Quest { title = "Advance to the Bronze Age",         done = () => AgeNow() >= 2,         reward = () => carriedInv.Add(stone, 30), rewardText = "+30 Stone" },
                new Quest { title = "Bake 15 Bread",                     done = () => Have(bread) >= 15,     reward = () => carriedInv.Add(planks, 20),rewardText = "+20 Planks" },
                new Quest { title = "Explore far & mine 25 Ore",         done = () => Have(ore) >= 25,       reward = () => carriedInv.Add(stone, 40), rewardText = "+40 Stone" },
                new Quest { title = "Smelt 20 Metal",                    done = () => Have(metal) >= 20,     reward = () => carriedInv.Add(stone, 40), rewardText = "+40 Stone" },
                new Quest { title = "Advance to the Iron Age",           done = () => AgeNow() >= 3,         reward = () => carriedInv.Add(planks, 40),rewardText = "+40 Planks" },
                new Quest { title = "Craft 10 Tools",                    done = () => Have(tools) >= 10,     reward = () => carriedInv.Add(planks, 30),rewardText = "+30 Planks" },
                new Quest { title = "Keep your people happy (90%+)",     done = () => Colony.Instance != null && Colony.Instance.Happiness >= 0.9f, reward = () => carriedInv.Add(food, 30), rewardText = "+30 Food" },
                new Quest { title = "Advance to the Industrial Age",     done = () => AgeNow() >= 4,         reward = () => carriedInv.Add(planks, 50),rewardText = "+50 Planks" },
                new Quest { title = "Build a thriving settlement: 30 people", done = () => Pop() >= 30, reward = () => carriedInv.Add(metal, 15), rewardText = "+15 Metal" },
                new Quest { title = "Build a prosperous colony: 600 Prosperity", done = () => Colony.Instance != null && Colony.Instance.PeakProsperity >= 600, reward = () => carriedInv.Add(tools, 10), rewardText = "+10 Tools" },
                new Quest { title = "Begin your legacy: build the Monument",      done = () => HasWorkshopOf(monument), reward = () => carriedInv.Add(planks, 40), rewardText = "+40 Planks" },
                new Quest { title = "🏆 Complete the Monument — 10 Blocks. YOU WIN!", done = () => Have(monument) >= 10, isWin = true },
            };

            // --- Resource patches: spread WIDE across the map so you must explore.
            //     A clear central area stays open as the player's base/processing yard. ---
            const float baseClear = 11f;
            SpawnPatches("Tree", wood, new Color(0.27f, 0.55f, 0.22f), 12,
                new Vector2(24f, 4f), new Vector2(18f, 18f), new Vector2(1.0f, 1.5f), PlaceholderArt.Triangle(), 30, baseClear);
            SpawnPatches("Rock", stone, new Color(0.55f, 0.55f, 0.6f), 12,
                new Vector2(-24f, 4f), new Vector2(18f, 18f), new Vector2(1.0f, 1.5f), PlaceholderArt.Hexagon(), 30, baseClear);
            SpawnPatches("Bush", food, new Color(0.45f, 0.55f, 0.25f), 9,
                new Vector2(0f, -24f), new Vector2(20f, 10f), new Vector2(0.7f, 1.0f), PlaceholderArt.Circle(), 30, baseClear);
            // Water bodies (lakes) scattered around the map.
            SpawnPatches("Lake", water, new Color(0.30f, 0.55f, 0.85f), 4,
                new Vector2(6f, 26f), new Vector2(22f, 8f), new Vector2(3.0f, 4.2f), PlaceholderArt.Circle(), 240, baseClear);
            // Animal herds (meat — Tribal age) and clay deposits (Bronze age), spread out.
            SpawnPatches("Herd", meat, new Color(0.66f, 0.34f, 0.34f), 8,
                new Vector2(26f, -18f), new Vector2(14f, 12f), new Vector2(0.8f, 1.2f), PlaceholderArt.Circle(), 30, baseClear);
            SpawnPatches("Clay", clay, new Color(0.68f, 0.46f, 0.36f), 9,
                new Vector2(-26f, -18f), new Vector2(14f, 12f), new Vector2(1.0f, 1.4f), PlaceholderArt.Hexagon(), 40, baseClear);
            // Ore veins — rare, far from base (must explore to find them), high yield.
            SpawnPatches("Ore Vein", ore, new Color(0.62f, 0.58f, 0.42f), 4,
                new Vector2(46f, 34f), new Vector2(12f, 12f), new Vector2(1.1f, 1.5f), PlaceholderArt.Hexagon(), 60, 26f, 0);
            SpawnPatches("Ore Vein", ore, new Color(0.62f, 0.58f, 0.42f), 4,
                new Vector2(-46f, -34f), new Vector2(12f, 12f), new Vector2(1.1f, 1.5f), PlaceholderArt.Hexagon(), 60, 26f, 0);
        }

        // Scatters `count` patches around `center`, jittered by ±spread, random size,
        // and kept at least `minClear` from the origin so the base area stays open.
        private static void SpawnPatches(string name, ItemDefinition item, Color color, int count,
            Vector2 center, Vector2 spread, Vector2 sizeRange, Sprite sprite, int capacity, float minClear, int regen = 1)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = center + new Vector2(Random.Range(-spread.x, spread.x), Random.Range(-spread.y, spread.y));
                if (pos.magnitude < minClear) pos = pos.normalized * minClear; // keep the base clear
                float size = Random.Range(sizeRange.x, sizeRange.y);
                float b = Random.Range(0.9f, 1.1f); // slight colour variation
                var c = new Color(Mathf.Clamp01(color.r * b), Mathf.Clamp01(color.g * b), Mathf.Clamp01(color.b * b));
                SpawnNode(name, item, c, pos, size, sprite, capacity, regen);
            }
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

        private static void SpawnNode(string name, ItemDefinition item, Color color, Vector2 pos, float size, Sprite sprite, int capacity = 30, int regen = 1)
        {
            var go = MakeSprite(name, color, pos, size, 0, sprite);
            go.AddComponent<BoxCollider2D>();
            var node = go.AddComponent<ResourceNode>();
            node.yields = item;
            node.capacity = capacity;
            node.regenAmount = regen; // 0 = finite (depletes and vanishes)
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
