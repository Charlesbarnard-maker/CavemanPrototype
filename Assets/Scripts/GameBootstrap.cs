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
            // Textiles + pottery — parallel comfort-good chains that deepen the demand sink.
            var fiber = MakeItem("fiber", "Plant Fiber", new Color(0.62f, 0.74f, 0.45f));
            var cloth = MakeItem("cloth", "Cloth", new Color(0.86f, 0.84f, 0.78f));
            var clothes = MakeItem("clothes", "Clothes", new Color(0.45f, 0.55f, 0.78f));
            var pot = MakeItem("pot", "Pottery", new Color(0.74f, 0.48f, 0.36f));
            // Exploration payoff: gems (rare, far) -> jewelry, a required Monument ingredient.
            var gems = MakeItem("gems", "Gems", new Color(0.55f, 0.85f, 0.80f));
            var jewelry = MakeItem("jewelry", "Jewelry", new Color(0.90f, 0.80f, 0.40f));
            // Masonry: gives Stone its own processing chain (like Wood -> Planks).
            var stoneBlock = MakeItem("stoneblock", "Stone Block", new Color(0.58f, 0.60f, 0.66f));

            // --- Item descriptions (shown in the in-game Guide, key G) ---
            wood.description = "The starter resource — chop it from trees. Used by nearly every building, and refined into Planks and Charcoal.";
            stone.description = "Basic building material — mine it from rocks. Used in most early buildings; cut into Stone Blocks by a Mason.";
            food.description = "Berries gathered from bushes (Forager Hut). Keeps people alive; cook it for far more nourishment.";
            water.description = "Drawn from lakes (Water Hole). People drink it every day; also needed for cooking, farming and baking.";
            planks.description = "A Sawmill turns Wood into Planks — a stronger material for houses and advanced machines.";
            cookedFood.description = "A Campfire cooks Food (+Wood +Water). More nourishing than raw, and your people's first comfort good (Tribal).";
            meat.description = "Hunted from animal herds (Hunter's Hut). A nourishing food; keep it in a Smokehouse.";
            clay.description = "Dug from clay deposits (Clay Pit). Fired into Bricks, and shaped into Pottery.";
            charcoal.description = "A Charcoal Burner turns Wood into Charcoal. It fuels BOTH the Kiln and the Smelter — a key shared bottleneck.";
            bricks.description = "A Kiln fires Clay + Charcoal into Bricks — a sturdy material for advanced buildings.";
            grain.description = "A Farm grows Grain from Water. Milled into Flour.";
            flour.description = "A Mill grinds Grain into Flour. Baked into Bread.";
            bread.description = "A Bakery bakes Flour + Water into Bread — high nourishment, and a Bronze comfort good.";
            ore.description = "Mined from FINITE veins far from base. Smelted into Metal. Its scarcity drives exploration.";
            metal.description = "A Smelter melts Ore + Charcoal into Metal — the backbone of Tools and the Monument.";
            tools.description = "A Toolmaker crafts Metal + Planks into Tools. An Iron-age comfort good that also gates the Industrial Age.";
            monument.description = "Built at the Monument from Metal + Tools + Bricks + Planks. Collect 10 Blocks to WIN the game.";
            fiber.description = "Plant Fiber harvested from Cotton fields (Cotton Farm). Woven into Cloth.";
            cloth.description = "A Weaver turns Fiber into Cloth. Tailored into Clothes.";
            clothes.description = "A Tailor sews Cloth into Clothes — an Industrial luxury comfort good.";
            pot.description = "A Potter shapes Clay into Pottery — a Bronze comfort good your people want.";
            gems.description = "The rarest resource, in FINITE deposits far out in the map. Cut into Jewelry; reaching them rewards exploration.";
            jewelry.description = "A Jeweler crafts Gems into Jewelry — a high-value luxury good.";
            stoneBlock.description = "A Mason cuts Stone into Stone Blocks — used to build the sturdy Stone House.";

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
            warehouse.footprintW = 2; warehouse.footprintH = 2; // TEST: first multi-cell building
            var house = MakeHousing("House", 2, new Color(0.72f, 0.62f, 0.45f),
                new ItemAmount(wood, 8), new ItemAmount(stone, 4), new ItemAmount(planks, 3));
            // Construction scaling: each yard adds builder capacity (you still need spare
            // population + materials flowing). Build more to construct whole areas faster.
            var buildYard = MakeBuildYard("Construction Yard", 3, 1, new Color(0.86f, 0.72f, 0.34f),
                new ItemAmount(wood, 12), new ItemAmount(stone, 8));
            buildYard.description = "Raises your builder cap by 3. Build more yards (and keep spare people) to construct faster — scaling construction is infrastructure, not a slider. Builders still haul materials from storage, so supply rate caps build speed.";
            // Long-distance logistics: depots + caravan routes (replaces the old haulers).
            var depot = ScriptableObject.CreateInstance<BuildingDefinition>();
            depot.displayName = "Depot"; depot.kind = BuildingKind.Depot; depot.unlockAge = 0;
            depot.item = null; depot.capacity = 80; depot.color = new Color(0.50f, 0.45f, 0.55f);
            depot.cost = new List<ItemAmount> { new ItemAmount(wood, 10), new ItemAmount(stone, 6) };
            // Route vehicle tiers — each links two depots; bigger/faster as ages advance.
            var caravan = MakeRoute("Caravan (Elephant)", 12, 3.5f, 0, new Color(0.55f, 0.50f, 0.50f),
                new ItemAmount(wood, 8));
            var oxCart = MakeRoute("Ox Cart", 18, 4.5f, 1, new Color(0.60f, 0.45f, 0.30f),
                new ItemAmount(wood, 10), new ItemAmount(planks, 4));
            var wagonTrain = MakeRoute("Wagon Train", 36, 6.5f, 3, new Color(0.45f, 0.45f, 0.52f),
                new ItemAmount(planks, 10), new ItemAmount(metal, 8));
            var cargoDrone = MakeRoute("Cargo Drone", 24, 12f, 4, new Color(0.50f, 0.70f, 0.85f),
                new ItemAmount(metal, 10), new ItemAmount(tools, 4));

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
            // Masonry: Stone -> Stone Blocks, used for sturdy housing.
            var mason = MakeWorkshop("Mason", stoneBlock, 1, 3.0f, 2, 12, new Color(0.58f, 0.60f, 0.66f),
                new List<ItemAmount> { new ItemAmount(stone, 2) },
                new ItemAmount(wood, 6), new ItemAmount(stone, 4)); mason.unlockAge = 2;
            var stoneHouse = MakeHousing("Stone House", 6, new Color(0.62f, 0.64f, 0.70f),
                new ItemAmount(stoneBlock, 6), new ItemAmount(planks, 4)); stoneHouse.unlockAge = 2;
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
            smelter.footprintW = 2; smelter.footprintH = 2; // TEST: multi-cell workshop
            // Toolmaker: Metal + Planks -> Tools (an Iron-age comfort good).
            var toolmaker = MakeWorkshop("Toolmaker", tools, 1, 4.0f, 2, 12, new Color(0.50f, 0.55f, 0.60f),
                new List<ItemAmount> { new ItemAmount(metal, 1), new ItemAmount(planks, 1) },
                new ItemAmount(planks, 8), new ItemAmount(bricks, 6)); toolmaker.unlockAge = 3;
            // Power: the Industrial age's new constraint. The Coal Generator burns Charcoal
            // to supply electrical power; from the Industrial age machines need it (or they
            // brown out and slow down). Unlocks in Iron so you can prepare before it bites.
            var generator = MakePower("Coal Generator", 60, charcoal, 1, 3f, 3, new Color(0.30f, 0.30f, 0.34f),
                new ItemAmount(bricks, 12), new ItemAmount(metal, 6));
            generator.description = "Burns Charcoal to supply electrical Power. From the Industrial age, machines need power — too little and the whole grid browns out, slowing every machine. Keep charcoal flowing to it.";
            // Endgame: the Monument (Industrial age). A long resource sink you pour the
            // top of every production chain into — completing it (10 blocks) is the win.
            var monumentBldg = MakeWorkshop("Monument", monument, 1, 6.0f, 3, 12, new Color(0.88f, 0.84f, 0.62f),
                new List<ItemAmount> { new ItemAmount(metal, 2), new ItemAmount(tools, 1), new ItemAmount(bricks, 2), new ItemAmount(planks, 2) },
                new ItemAmount(bricks, 20), new ItemAmount(metal, 15), new ItemAmount(tools, 8)); monumentBldg.unlockAge = 4;

            // --- Textiles & pottery chains (comfort goods) ---
            // Pottery (Bronze): Clay -> Pottery. Reuses the clay you already mine.
            var potter = MakeWorkshop("Potter", pot, 1, 3.0f, 2, 12, new Color(0.72f, 0.50f, 0.40f),
                new List<ItemAmount> { new ItemAmount(clay, 2) },
                new ItemAmount(wood, 6), new ItemAmount(stone, 4)); potter.unlockAge = 2;
            // Textiles: Cotton -> Fiber -> Cloth -> Clothes (an Industrial luxury).
            var cottonFarm = MakeCollector("Cotton Farm", fiber, 1, 2.5f, 2, 12, new Color(0.70f, 0.78f, 0.55f),
                new ItemAmount(wood, 6)); cottonFarm.unlockAge = 2;
            var weaver = MakeWorkshop("Weaver", cloth, 1, 3.5f, 2, 12, new Color(0.80f, 0.78f, 0.70f),
                new List<ItemAmount> { new ItemAmount(fiber, 2) },
                new ItemAmount(wood, 8), new ItemAmount(planks, 4)); weaver.unlockAge = 3;
            var tailor = MakeWorkshop("Tailor", clothes, 1, 4.0f, 2, 12, new Color(0.50f, 0.58f, 0.80f),
                new List<ItemAmount> { new ItemAmount(cloth, 2) },
                new ItemAmount(planks, 6), new ItemAmount(bricks, 4)); tailor.unlockAge = 4;
            // Gems (Iron, mined from distant deposits) -> Jewelry (Industrial) for the Monument.
            var gemMine = MakeCollector("Gem Mine", gems, 1, 3.5f, 2, 10, new Color(0.45f, 0.70f, 0.66f),
                new ItemAmount(wood, 8), new ItemAmount(stone, 6)); gemMine.unlockAge = 3;
            var jeweler = MakeWorkshop("Jeweler", jewelry, 1, 4.5f, 2, 10, new Color(0.85f, 0.78f, 0.45f),
                new List<ItemAmount> { new ItemAmount(gems, 2) },
                new ItemAmount(planks, 8), new ItemAmount(metal, 4)); jeweler.unlockAge = 4;

            // --- Hand-written descriptions for buildings that need strategic context
            //     (everything else auto-generates a full tooltip from its data). ---
            campfire.description = "Food + Wood + Water → Cooked Food (worth more nourishment). Your people's first comfort good. Needs workers + inputs delivered.";
            charcoalBurner.description = "Wood → Charcoal. Charcoal feeds BOTH the Kiln and the Smelter — scaling one can starve the other. A key shared-bottleneck.";
            kiln.description = "Clay + Charcoal → Bricks. Charcoal is shared with the Smelter, so watch that bottleneck. Bricks build advanced structures.";
            smelter.description = "Ore + Charcoal → Metal. Ore comes from distant Mines; Charcoal is shared with the Kiln. The backbone of the late game.";
            toolmaker.description = "Metal + Planks → Tools. Tools are an Iron-age comfort good your people demand — and gate the Industrial Age.";
            mine.description = "Build ON a distant Ore Vein. Ore is FINITE — veins deplete and vanish, so you must keep exploring outward and hauling it home.";
            gemMine.description = "Build ON a far Gem Deposit (the rarest, finite resource). Gems → Jewelry. Reaching them rewards exploration + good transport.";
            jeweler.description = "Gems → Jewelry, a high-value luxury good. Pairs with long-haul routes to bring distant gems home.";
            monumentBldg.description = "ENDGAME: pour Metal + Tools + Bricks + Planks in to produce Monument Blocks. Make 10 to WIN. A massive, sustained resource sink.";
            mason.description = "Stone → Stone Blocks (Stone's own processing chain). Stone Blocks build the sturdy Stone House.";
            warehouse.description = "Configurable storage: pick ONE resource it holds (open its panel). Essential for routing belt goods and feeding workshops by adjacency.";

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
            MakeSprite("Ground", Color.white, Vector2.zero, 220f, -100, PlaceholderArt.Ground(new Color(0.22f, 0.31f, 0.19f)));

            // --- Player ---
            var player = MakeSprite("Player", new Color(0.92f, 0.82f, 0.25f), Vector2.zero, 0.7f, 10, PlaceholderArt.Circle());
            player.AddComponent<PlayerController>();
            var gatherer = player.AddComponent<PlayerGatherer>();
            var builder = player.AddComponent<BuildController>();
            builder.gatherer = gatherer;
            builder.placeNodeRange = 6f;
            builder.buildables = new List<BuildingDefinition>
            { woodHut, stonePit, foragerHut, waterHole, sawmill, campfire,
              woodStore, stoneStore, foodStore, waterStore, warehouse, house, buildYard,
              hunter, clayPit, charcoalBurner, clayStore, smokehouse, longhouse,
              kiln, farm, mill, bakery, brickStore, mason, stoneHouse, woodBelt, fastBelt,
              mine, oreStore, smelter, toolmaker, monumentBldg, generator,
              potter, cottonFarm, weaver, tailor, gemMine, jeweler,
              depot, caravan, oxCart, wagonTrain, cargoDrone };

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
                new Colony.Comfort { item = pot,        unlockAge = 2 }, // Bronze: want pottery
                new Colony.Comfort { item = tools,      unlockAge = 3 }, // Iron: want tools
                new Colony.Comfort { item = clothes,    unlockAge = 4 }, // Industrial: want clothes
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
            { wood, stone, food, water, planks, cookedFood, meat, clay, charcoal, bricks, grain, flour, bread, ore, metal, tools, monument, fiber, cloth, clothes, pot, gems, jewelry, stoneBlock };

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
                new Quest { title = "Make 10 Pottery (a Bronze comfort)", done = () => Have(pot) >= 10,       reward = () => carriedInv.Add(clay, 20), rewardText = "+20 Clay" },
                new Quest { title = "Cut 15 Stone Blocks (Masonry)",      done = () => Have(stoneBlock) >= 15, reward = () => carriedInv.Add(stone, 25), rewardText = "+25 Stone" },
                new Quest { title = "Explore far & mine 25 Ore",         done = () => Have(ore) >= 25,       reward = () => carriedInv.Add(stone, 40), rewardText = "+40 Stone" },
                new Quest { title = "Smelt 20 Metal",                    done = () => Have(metal) >= 20,     reward = () => carriedInv.Add(stone, 40), rewardText = "+40 Stone" },
                new Quest { title = "Advance to the Iron Age",           done = () => AgeNow() >= 3,         reward = () => carriedInv.Add(planks, 40),rewardText = "+40 Planks" },
                new Quest { title = "Craft 10 Tools",                    done = () => Have(tools) >= 10,     reward = () => carriedInv.Add(planks, 30),rewardText = "+30 Planks" },
                new Quest { title = "Weave 15 Cloth (Cotton → Fiber → Cloth)", done = () => Have(cloth) >= 15, reward = () => carriedInv.Add(planks, 15),rewardText = "+15 Planks" },
                new Quest { title = "Keep your people happy (90%+)",     done = () => Colony.Instance != null && Colony.Instance.Happiness >= 0.9f, reward = () => carriedInv.Add(food, 30), rewardText = "+30 Food" },
                new Quest { title = "Advance to the Industrial Age",     done = () => AgeNow() >= 4,         reward = () => carriedInv.Add(planks, 50),rewardText = "+50 Planks" },
                new Quest { title = "Tailor 12 Clothes (Industrial comfort)", done = () => Have(clothes) >= 12, reward = () => carriedInv.Add(tools, 6), rewardText = "+6 Tools" },
                new Quest { title = "Prospect the far reaches for 20 Gems", done = () => Have(gems) >= 20, reward = () => carriedInv.Add(metal, 10), rewardText = "+10 Metal" },
                new Quest { title = "Craft 10 Jewelry (luxury wealth)", done = () => Have(jewelry) >= 10, reward = () => carriedInv.Add(planks, 30), rewardText = "+30 Planks" },
                new Quest { title = "Build a thriving settlement: 30 people", done = () => Pop() >= 30, reward = () => carriedInv.Add(metal, 15), rewardText = "+15 Metal" },
                new Quest { title = "Build a prosperous colony: 600 Prosperity", done = () => Colony.Instance != null && Colony.Instance.PeakProsperity >= 600, reward = () => carriedInv.Add(tools, 10), rewardText = "+10 Tools" },
                new Quest { title = "Begin your legacy: build the Monument",      done = () => HasWorkshopOf(monument), reward = () => carriedInv.Add(planks, 40), rewardText = "+40 Planks" },
                new Quest { title = "🏆 Complete the Monument — 10 Blocks. YOU WIN!", done = () => Have(monument) >= 10, isWin = true },
            };

            // --- Random world events (variety / surprise hook) ---
            var events = new GameObject("WorldEvents").AddComponent<WorldEvents>();
            events.carried = gatherer.Inventory;
            events.food = food;
            events.wood = wood;
            events.stone = stone;

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
            // Cotton fields (fiber for the textile chain) — mid-distance, north-west.
            SpawnPatches("Cotton", fiber, new Color(0.80f, 0.84f, 0.66f), 8,
                new Vector2(-8f, 26f), new Vector2(16f, 8f), new Vector2(0.7f, 1.1f), PlaceholderArt.Circle(), 36, baseClear);
            // Ore veins — rare, far from base (must explore to find them), high yield.
            SpawnPatches("Ore Vein", ore, new Color(0.62f, 0.58f, 0.42f), 4,
                new Vector2(46f, 34f), new Vector2(12f, 12f), new Vector2(1.1f, 1.5f), PlaceholderArt.Hexagon(), 60, 26f, 0);
            SpawnPatches("Ore Vein", ore, new Color(0.62f, 0.58f, 0.42f), 4,
                new Vector2(-46f, -34f), new Vector2(12f, 12f), new Vector2(1.1f, 1.5f), PlaceholderArt.Hexagon(), 60, 26f, 0);

            // --- Welcome / starter guidance (fades after a few seconds) ---
            Toast.Show("<color=#ffd24d>Welcome, chief!</color>  Click trees & rocks to gather by hand.");
            Toast.Show("<size=15>Goal: grow from caveman to a self-running civilisation — build the Monument to win.</size>");
            Toast.Show("<size=14>Press <b>H</b> for help · <b>G</b> for the Guide · <b>B</b> to build · follow the Objectives (top-right).</size>");

            // Gem deposits — rarest, farthest (a third exploration direction), finite.
            SpawnPatches("Gem Deposit", gems, new Color(0.50f, 0.82f, 0.76f), 3,
                new Vector2(48f, -42f), new Vector2(10f, 10f), new Vector2(1.0f, 1.4f), PlaceholderArt.Hexagon(), 45, 32f, 0);
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
            def.powerDraw = 10; // machines draw power once the Industrial age arrives
            def.cost = new List<ItemAmount>(cost);
            return def;
        }

        // A power plant: burns `fuel` to supply `output` electrical power (Industrial age).
        private static BuildingDefinition MakePower(string name, int output, ItemDefinition fuel, int fuelPerCycle,
            float interval, int unlockAge, Color color, params ItemAmount[] cost)
        {
            var def = ScriptableObject.CreateInstance<BuildingDefinition>();
            def.displayName = name;
            def.kind = BuildingKind.Power;
            def.powerOutput = output;
            def.interval = interval;
            def.unlockAge = unlockAge;
            def.inputs = fuel != null ? new List<ItemAmount> { new ItemAmount(fuel, fuelPerCycle) } : new List<ItemAmount>();
            def.color = color;
            def.cost = new List<ItemAmount>(cost);
            return def;
        }

        // A construction yard: raises the builder cap by `slots` (scales construction).
        private static BuildingDefinition MakeBuildYard(string name, int slots, int unlockAge, Color color, params ItemAmount[] cost)
        {
            var def = ScriptableObject.CreateInstance<BuildingDefinition>();
            def.displayName = name;
            def.kind = BuildingKind.Build;
            def.builderSlots = slots;
            def.unlockAge = unlockAge;
            def.color = color;
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

        private static BuildingDefinition MakeRoute(string name, int capacity, float speed, int unlockAge, Color color, params ItemAmount[] cost)
        {
            var def = ScriptableObject.CreateInstance<BuildingDefinition>();
            def.displayName = name;
            def.kind = BuildingKind.Route;
            def.capacity = capacity;
            def.vehicleSpeed = speed;
            def.unlockAge = unlockAge;
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
