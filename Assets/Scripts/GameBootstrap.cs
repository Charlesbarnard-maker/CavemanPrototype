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
            water.isLiquid = true; // liquid: moves via pipes/carrying, never on belts
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
            // RESEARCH ITEMS — crafted (never gathered) multi-input products delivered to a Research
            // Lodge to earn research points → the ONLY way to advance an age. Each tier needs a
            // deeper production chain than the last (see the Research system + GAME_DESIGN).
            var ideaTablet = MakeItem("idea", "Idea Tablet", new Color(0.88f, 0.82f, 0.52f));
            var studyScroll = MakeItem("scroll", "Study Scroll", new Color(0.92f, 0.86f, 0.60f));
            var schematic = MakeItem("schematic", "Schematic", new Color(0.55f, 0.72f, 0.88f));
            var blueprint = MakeItem("blueprint", "Blueprint", new Color(0.38f, 0.60f, 0.88f));
            ideaTablet.description = "RESEARCH item: Planks + Stone at an Idea Bench. Deliver to a Research Lodge to research the Tribal Age.";
            studyScroll.description = "RESEARCH item: Charcoal + Planks at a Scroll Maker. Deliver to a Research Lodge to research the Bronze Age.";
            schematic.description = "RESEARCH item: Bricks + Pottery at a Drafting Table. Deliver to a Research Lodge to research the Iron Age.";
            blueprint.description = "RESEARCH item: Metal + Tools at an Engineering Lab. Deliver to a Research Lodge to research the Industrial Age.";

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

            // --- Belt icons (placeholder shapes by material family, so items are distinguishable
            //     on conveyors; real per-item art drops into ItemDefinition.icon later). ---
            var triangle = PlaceholderArt.Triangle(); var hexagon = PlaceholderArt.Hexagon(); var square = PlaceholderArt.Square();
            wood.icon = planks.icon = triangle;                                              // woody
            stone.icon = ore.icon = clay.icon = bricks.icon = stoneBlock.icon = gems.icon = charcoal.icon = hexagon; // mineral
            metal.icon = tools.icon = monument.icon = cloth.icon = clothes.icon = pot.icon = jewelry.icon = square;   // manufactured
            ideaTablet.icon = studyScroll.icon = schematic.icon = blueprint.icon = square; // research items (manufactured look)
            // food / cooked / meat / grain / flour / bread / fiber keep the default round dot.

            // --- Buildings ---
            var woodHut = MakeCollector("Wood Hut", wood, 1, 2.0f, 2, 12, new Color(0.80f, 0.52f, 0.25f),
                new ItemAmount(wood, 5), new ItemAmount(stone, 3));
            var stonePit = MakeCollector("Stone Pit", stone, 1, 2.0f, 2, 12, new Color(0.45f, 0.52f, 0.62f),
                new ItemAmount(wood, 5), new ItemAmount(stone, 5));
            var foragerHut = MakeCollector("Forager Hut", food, 1, 2.0f, 2, 12, new Color(0.78f, 0.40f, 0.40f),
                new ItemAmount(wood, 4));
            var waterHole = MakeCollector("Water Hole", water, 1, 2.0f, 2, 12, new Color(0.40f, 0.62f, 0.85f),
                new ItemAmount(wood, 4));
            waterHole.fromWaterTerrain = true; // must sit next to real water terrain (river/lake)
            waterHole.description = "Stone-age water: a worker draws from adjacent water terrain and CARRIES it to the nearest Water Barrel (build one beside it so your colony has stored water to drink). Adjacent buildings can also use it directly. Water is a LIQUID — it can't ride belts; pipe it (Bronze) to move it further.";
            // Bridge: plank tile placed on water; makes it passable for feet + belts. Core
            // logistics infrastructure — strategic chokepoints across rivers.
            var bridge = ScriptableObject.CreateInstance<BuildingDefinition>();
            bridge.displayName = "Bridge"; bridge.kind = BuildingKind.Bridge;
            bridge.color = new Color(0.62f, 0.47f, 0.28f);
            bridge.cost = new List<ItemAmount> { new ItemAmount(wood, 3) };
            bridge.description = "A plank tile laid on WATER. Lets you and your belts cross rivers/lakes. Drag to lay a span. Place bridges strategically — they're the only way across water until later transport.";
            // Liquid logistics (Bronze): pipes + a mechanical pump move water from a river/lake
            // into your storage continuously — the EVOLUTION of the hand-carried Water Hole, not
            // a new system. Continuous flow over a connected network (topology matters).
            var pipe = ScriptableObject.CreateInstance<BuildingDefinition>();
            pipe.displayName = "Pipe"; pipe.kind = BuildingKind.Pipe; pipe.unlockAge = 2;
            pipe.color = new Color(0.40f, 0.62f, 0.85f);
            pipe.cost = new List<ItemAmount> { new ItemAmount(stone, 1) };
            pipe.description = "A liquid-network segment — continuous flow, NOT items. Drag to run pipes from a Water Pump to your Water Barrels. The pump only fills storage its pipes actually reach, so layout/topology matters.";
            var pump = ScriptableObject.CreateInstance<BuildingDefinition>();
            pump.displayName = "Water Pump"; pump.kind = BuildingKind.Pump; pump.item = water; pump.unlockAge = 2;
            pump.color = new Color(0.30f, 0.55f, 0.78f);
            pump.cost = new List<ItemAmount> { new ItemAmount(planks, 4), new ItemAmount(stone, 4) };
            pump.description = "Place next to water (river/lake) and connect pipes: it pushes water through the network into reachable Water Barrels AND directly into adjacent water-using buildings (Campfire/Farm/Bakery) — no workers carrying it. Pressure fades over distance — far consumers starve unless you add a Booster Pump. The Bronze-age evolution of the Water Hole.";
            // Booster Pump: re-pressurises a pipe network so it reaches further (the solution to
            // the distance/pressure problem). No water source needed — place it on a long run.
            var booster = ScriptableObject.CreateInstance<BuildingDefinition>();
            booster.displayName = "Booster Pump"; booster.kind = BuildingKind.Pump; booster.booster = true; booster.unlockAge = 2;
            booster.color = new Color(0.45f, 0.62f, 0.72f);
            booster.cost = new List<ItemAmount> { new ItemAmount(planks, 3), new ItemAmount(stone, 3) };
            booster.description = "Re-pressurises a pipe network next to it, extending how far water reaches. No water source needed — place it partway along a long pipe run so distant consumers stop starving. Chain several for very long networks.";
            var sawmill = MakeWorkshop("Sawmill", planks, 1, 2.0f, 2, 12, new Color(0.66f, 0.50f, 0.30f),
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
            depot.displayName = "Station"; depot.kind = BuildingKind.Depot; depot.unlockAge = 0;
            depot.item = null; depot.capacity = 80; depot.color = new Color(0.50f, 0.45f, 0.55f);
            depot.cost = new List<ItemAmount> { new ItemAmount(wood, 10), new ItemAmount(stone, 6) };
            depot.description = "Transport Station. Belt goods IN; then SELECT it and '+ Add route' to another Station — a vehicle shuttles goods across the map (load → travel → unload, so distance & handling cap throughput). Build a Station at each end of a supply line; add more vehicles/routes to move more.";
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
            var hunter = MakeCollector("Hunter's Hut", meat, 1, 2.0f, 2, 12, new Color(0.66f, 0.34f, 0.34f),
                new ItemAmount(wood, 6)); hunter.unlockAge = 1;
            var clayPit = MakeCollector("Clay Pit", clay, 1, 2.0f, 2, 12, new Color(0.68f, 0.46f, 0.36f),
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
            woodBelt.interval = 1.0f; // 60 items/min — exactly one collector's output (the baseline lane)
            woodBelt.color = new Color(0.60f, 0.50f, 0.35f);
            woodBelt.cost = new List<ItemAmount> { new ItemAmount(wood, 1) };
            woodBelt.description = "Carries items along its arrow at 60/min — exactly one collector's output (one Wood Hut fills one belt). Drag to lay a line; R rotates. Belt colour shows trouble: RED = dead end, YELLOW = backed up (downstream full).";
            var fastBelt = ScriptableObject.CreateInstance<BuildingDefinition>();
            fastBelt.displayName = "Conveyor Belt"; fastBelt.kind = BuildingKind.Belt; fastBelt.unlockAge = 2;
            fastBelt.interval = 0.5f; // 120 items/min — exactly 2× the wooden lane (the clean upgrade)
            fastBelt.color = new Color(0.72f, 0.63f, 0.40f);
            fastBelt.cost = new List<ItemAmount> { new ItemAmount(planks, 1) };
            fastBelt.description = "The belt upgrade: 120/min — 2× the wooden lane. One conveyor carries two collectors' worth, or feeds a 2-worker machine on a single line.";
            // Splitter: a 1→2 belt that distributes items EVENLY between two outputs. Lets one
            // supply line feed two machines. (Belt kind, flagged splitter — placed like a belt.)
            var splitter = ScriptableObject.CreateInstance<BuildingDefinition>();
            splitter.displayName = "Splitter"; splitter.kind = BuildingKind.Belt; splitter.splitter = true; splitter.unlockAge = 0;
            splitter.interval = 0.6f;
            splitter.color = new Color(0.45f, 0.62f, 0.72f);
            splitter.cost = new List<ItemAmount> { new ItemAmount(wood, 2) };
            splitter.description = "1→2 SPLITTER: pulls from behind and sends items EVENLY to two outputs — forward and to its right (R rotates). If one output backs up it sends to the other, so it never stalls. Feed two machines from one supply line. (Smart/filtered splitters come later.)";

            // Exploration payoff: Ore is mined from distant veins, hauled home, and is
            // required to reach the Iron Age.
            var mine = MakeCollector("Mine", ore, 1, 2.5f, 2, 12, new Color(0.50f, 0.48f, 0.40f),
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
            var cottonFarm = MakeCollector("Cotton Farm", fiber, 1, 2.0f, 2, 12, new Color(0.70f, 0.78f, 0.55f),
                new ItemAmount(wood, 6)); cottonFarm.unlockAge = 2;
            var weaver = MakeWorkshop("Weaver", cloth, 1, 3.5f, 2, 12, new Color(0.80f, 0.78f, 0.70f),
                new List<ItemAmount> { new ItemAmount(fiber, 2) },
                new ItemAmount(wood, 8), new ItemAmount(planks, 4)); weaver.unlockAge = 3;
            var tailor = MakeWorkshop("Tailor", clothes, 1, 4.0f, 2, 12, new Color(0.50f, 0.58f, 0.80f),
                new List<ItemAmount> { new ItemAmount(cloth, 2) },
                new ItemAmount(planks, 6), new ItemAmount(bricks, 4)); tailor.unlockAge = 4;
            // Gems (Iron, mined from distant deposits) -> Jewelry (Industrial) for the Monument.
            var gemMine = MakeCollector("Gem Mine", gems, 1, 2.5f, 2, 10, new Color(0.45f, 0.70f, 0.66f),
                new ItemAmount(wood, 8), new ItemAmount(stone, 6)); gemMine.unlockAge = 3;
            var jeweler = MakeWorkshop("Jeweler", jewelry, 1, 4.5f, 2, 10, new Color(0.85f, 0.78f, 0.45f),
                new List<ItemAmount> { new ItemAmount(gems, 2) },
                new ItemAmount(planks, 8), new ItemAmount(metal, 4)); jeweler.unlockAge = 4;

            // --- Hand-written descriptions for buildings that need strategic context
            //     (everything else auto-generates a full tooltip from its data). ---
            sawmill.description = "Wood → Planks. The baseline chain: a 1-worker Sawmill eats 60 Wood/min — exactly one Wood Hut down one belt (1 gatherer → 1 belt → 1 machine). Add a 2nd worker and it needs 120/min (a 2nd hut, or a conveyor).";
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

            // --- RESEARCH SYSTEM: the progression spine. Each age is unlocked by crafting that
            //     tier's RESEARCH ITEM (a multi-input factory product) and delivering it to a
            //     Research Lodge, which converts items → research points. Gathering earns nothing;
            //     you must build (and scale) production chains. Maker workshops are age-gated so the
            //     NEXT tier's item is craftable only once you reach its age (no circular locks). ---
            var researchLodge = MakeResearch("Research Lodge", 0, new Color(0.62f, 0.55f, 0.78f),
                new ItemAmount(wood, 10), new ItemAmount(stone, 8));
            researchLodge.description = "Delivers research: belt (or place beside) the current RESEARCH ITEM here and it converts each into research points. Reaching the point cost advances the Age. No workers — the limit is how fast your factory makes research items. Open the Build panel (top) to see the current target + progress.";
            var ideaBench = MakeWorkshop("Idea Bench", ideaTablet, 1, 2.0f, 2, 12, new Color(0.80f, 0.74f, 0.46f),
                new List<ItemAmount> { new ItemAmount(planks, 1), new ItemAmount(stone, 1) },
                new ItemAmount(wood, 6), new ItemAmount(stone, 4)); // age 0 — first research chain
            ideaBench.description = "Planks + Stone → Idea Tablet (a RESEARCH item). The first research chain: feed it from a Sawmill + Stone Pit, then belt the Tablets to a Research Lodge to reach the Tribal Age.";
            var scrollMaker = MakeWorkshop("Scroll Maker", studyScroll, 1, 2.0f, 2, 12, new Color(0.84f, 0.78f, 0.50f),
                new List<ItemAmount> { new ItemAmount(charcoal, 1), new ItemAmount(planks, 1) },
                new ItemAmount(wood, 8), new ItemAmount(stone, 6)); scrollMaker.unlockAge = 1;
            scrollMaker.description = "Charcoal + Planks → Study Scroll (a RESEARCH item). Research the Bronze Age. Charcoal is also shared with the Smelter/Kiln later — a real chain to scale.";
            var draftingTable = MakeWorkshop("Drafting Table", schematic, 1, 2.5f, 2, 12, new Color(0.52f, 0.66f, 0.82f),
                new List<ItemAmount> { new ItemAmount(bricks, 1), new ItemAmount(pot, 1) },
                new ItemAmount(planks, 6), new ItemAmount(bricks, 4)); draftingTable.unlockAge = 2;
            draftingTable.description = "Bricks + Pottery → Schematic (a RESEARCH item). Research the Iron Age. Needs two Bronze chains (Kiln + Potter) feeding it — scaling research now means scaling your factory.";
            var engineeringLab = MakeWorkshop("Engineering Lab", blueprint, 1, 3.0f, 2, 12, new Color(0.40f, 0.56f, 0.82f),
                new List<ItemAmount> { new ItemAmount(metal, 1), new ItemAmount(tools, 1) },
                new ItemAmount(planks, 8), new ItemAmount(metal, 4)); engineeringLab.unlockAge = 3;
            engineeringLab.description = "Metal + Tools → Blueprint (a RESEARCH item). Research the Industrial Age — the deepest chain (Smelter + Toolmaker), so the final unlock demands a real factory.";

            // Tiers = which research item the Lodge consumes at each age + its point value (later
            // items are worth more because their chains are deeper).
            Research.Reset();
            Research.Tiers = new List<Research.Tier>
            {
                new Research.Tier { targetAge = 1, item = ideaTablet,  pointsPerItem = 1 },  // craft at Stone
                new Research.Tier { targetAge = 2, item = studyScroll, pointsPerItem = 2 },  // craft at Tribal
                new Research.Tier { targetAge = 3, item = schematic,   pointsPerItem = 3 },  // craft at Bronze
                new Research.Tier { targetAge = 4, item = blueprint,   pointsPerItem = 5 },  // craft at Iron
            };
            // The spendable research TREE (press T to open). Age spine (each needs the prior) + a few
            // building-unlock branches you CHOOSE to spend points on. Age costs scale 20→50→100→200.
            Research.Tree = new List<Research.Tech>
            {
                new Research.Tech { id = "tribal",     name = "Tribal Age",     cost = 12,  advanceToAge = 1, prereq = null,     desc = "Advance to the Tribal Age — hunting, clay, charcoal, the Longhouse." },
                new Research.Tech { id = "bronze",     name = "Bronze Age",     cost = 50,  advanceToAge = 2, prereq = "tribal", desc = "Advance to the Bronze Age — kilns, farming/baking, masonry, smelting." },
                new Research.Tech { id = "iron",       name = "Iron Age",       cost = 100, advanceToAge = 3, prereq = "bronze", desc = "Advance to the Iron Age — toolmaking, weaving, gem mining." },
                new Research.Tech { id = "industrial", name = "Industrial Age", cost = 200, advanceToAge = 4, prereq = "iron",   desc = "Advance to the Industrial Age — power, the Monument, the endgame." },
                new Research.Tech { id = "splitters",  name = "Splitters",      cost = 15,  prereq = "tribal", unlocks = new List<BuildingDefinition>{ splitter },          desc = "Unlocks the 1→2 Splitter — feed two machines from one supply line." },
                new Research.Tech { id = "conveyors",  name = "Conveyor Belts", cost = 30,  prereq = "bronze", unlocks = new List<BuildingDefinition>{ fastBelt },          desc = "Unlocks the fast Conveyor Belt (120/min — 2× the wooden lane)." },
                new Research.Tech { id = "pipes",      name = "Pipe Network",   cost = 30,  prereq = "bronze", unlocks = new List<BuildingDefinition>{ pipe, pump, booster }, desc = "Unlocks liquid logistics — Pipes, the Water Pump and Booster Pump." },
            };
            // Gate those buildings behind their Tech (and off the age gate, so the Tech IS the gate).
            splitter.requiredTech = "splitters";
            fastBelt.requiredTech = "conveyors"; fastBelt.unlockAge = 0;
            pipe.requiredTech = "pipes"; pipe.unlockAge = 0;
            pump.requiredTech = "pipes"; pump.unlockAge = 0;
            booster.requiredTech = "pipes"; booster.unlockAge = 0;

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
            MakeSprite("Ground", Color.white, Vector2.zero, 420f, -100, PlaceholderArt.Ground(new Color(0.22f, 0.31f, 0.19f)));
            // World as a system: biome map with a clear starting basin. Water blocks building,
            // so geography forces routing/expansion decisions. Rendered after resource spawns.
            TerrainGrid.Generate(200, Random.value * 1000f, 22f); // big world (~400 across); open start basin

            // --- Player ---
            var player = MakeSprite("Player", new Color(0.92f, 0.82f, 0.25f), Vector2.zero, 0.7f, 10, PlaceholderArt.Circle());
            player.AddComponent<PlayerController>();
            var gatherer = player.AddComponent<PlayerGatherer>();
            var builder = player.AddComponent<BuildController>();
            builder.gatherer = gatherer;
            builder.placeNodeRange = 6f;
            builder.buildables = new List<BuildingDefinition>
            { woodHut, stonePit, foragerHut, waterHole, sawmill, campfire,
              woodStore, stoneStore, foodStore, waterStore, warehouse, house, buildYard, bridge, pipe, pump, booster,
              researchLodge, ideaBench, scrollMaker, draftingTable, engineeringLab,
              hunter, clayPit, charcoalBurner, clayStore, smokehouse, longhouse,
              kiln, farm, mill, bakery, brickStore, mason, stoneHouse, woodBelt, fastBelt, splitter,
              mine, oreStore, smelter, toolmaker, monumentBldg, generator,
              potter, cottonFarm, weaver, tailor, gemMine, jeweler,
              depot };
            // Transport vehicles are NOT in the build menu — they're created from a Station's panel.
            builder.routeTiers = new List<BuildingDefinition> { caravan, oxCart, wagonTrain, cargoDrone };

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;

            // --- Fog of war (explore to reveal the map) ---
            var fog = new GameObject("FogOfWar").AddComponent<FogOfWar>();
            fog.target = player.transform; // size/res come from FogOfWar defaults (set for the big world)

            // --- Colony (population) ---
            var colony = new GameObject("Colony").AddComponent<Colony>();
            colony.foodItem = food;
            colony.waterItem = water;
            colony.carried = gatherer.Inventory;
            colony.SetStartingPopulation(5);
            gatherer.Inventory.Add(food, 90);   // generous starting larder while you learn the ropes
            gatherer.Inventory.Add(water, 90);  // generous starting water
            gatherer.Inventory.Add(wood, 10);   // starter kit: enough for the first hut or two so the
            gatherer.Inventory.Add(stone, 10);  // opening isn't a long manual-gather grind (still <12 wood, so the "gather 12 wood" objective still teaches clicking)
            // Age advancement is now driven by the Research system (crafted research items →
            // Research Lodge → points), set up above — there is no resource/pop "advance" cost here.
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
            hud.meatItem = meat;
            hud.clayItem = clay;
            hud.oreItem = ore;
            hud.monumentItem = monument;
            hud.debugItems = new List<ItemDefinition>
            { wood, stone, food, water, planks, cookedFood, meat, clay, charcoal, bricks, grain, flour, bread, ore, metal, tools, monument, fiber, cloth, clothes, pot, gems, jewelry, stoneBlock };

            // --- Guided objectives ladder (the "what next / why advance" hook) ---
            var carriedInv = gatherer.Inventory;
            int Have(ItemDefinition i) => Economy.Available(i, carriedInv);
            bool HasCollectorOf(ItemDefinition i) { foreach (var p in ProductionBuilding.All) if (p != null && p.produces == i) return true; return false; }
            bool HasWorkshopOf(ItemDefinition i) { foreach (var w in WorkshopBuilding.All) if (w != null && w.output == i) return true; return false; }
            bool HasStorageOf(ItemDefinition i) { foreach (var s in StorageBuilding.All) if (s != null && s.accepts == i) return true; return false; }
            int Pop() => Colony.Instance != null ? Colony.Instance.Population : 0;
            int AgeNow() => Colony.Instance != null ? Colony.Instance.Age : 0;
            var objectives = new GameObject("Objectives").AddComponent<Objectives>();
            objectives.quests = new List<Quest>
            {
                new Quest { title = "Gather 12 Wood by hand",            done = () => Have(wood) >= 12,      reward = () => carriedInv.Add(stone, 8),  rewardText = "+8 Stone" },
                new Quest { title = "Build a Forager Hut for food",      done = () => HasCollectorOf(food),  reward = () => carriedInv.Add(food, 20),  rewardText = "+20 Food" },
                new Quest { title = "Build a Water Hole + a Water Barrel beside it", done = () => HasCollectorOf(water) && HasStorageOf(water), reward = () => carriedInv.Add(water, 20), rewardText = "+20 Water" },
                new Quest { title = "Build a Sawmill, an Idea Bench & a Research Lodge", done = () => HasWorkshopOf(planks) && HasWorkshopOf(ideaTablet) && ResearchBuilding.All.Count > 0, reward = () => carriedInv.Add(stone, 12), rewardText = "+12 Stone" },
                new Quest { title = "Deliver your first Idea Tablet to research", done = () => Research.TotalDelivered >= 1, reward = () => carriedInv.Add(planks, 8), rewardText = "+8 Planks" },
                new Quest { title = "Grow your settlement to 8 people",  done = () => Pop() >= 8,            reward = () => carriedInv.Add(wood, 25),  rewardText = "+25 Wood" },
                new Quest { title = "Advance to the Tribal Age",         done = () => AgeNow() >= 1,         reward = () => carriedInv.Add(stone, 25), rewardText = "+25 Stone" },
                new Quest { title = "Build a Sawmill & make 25 Planks",  done = () => Have(planks) >= 25,    reward = () => carriedInv.Add(planks, 10),rewardText = "+10 Planks" },
                new Quest { title = "Lay 5 conveyor belts",              done = () => Belt.Count >= 5,       reward = () => carriedInv.Add(wood, 15),  rewardText = "+15 Wood" },
                new Quest { title = "Build 2 Stations & add a transport route", done = () => RouteVehicle.All.Count >= 1, reward = () => carriedInv.Add(wood, 30), rewardText = "+30 Wood" },
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

            // --- Resource patches: natural CLUSTERS (groves & outcrops), never lone nodes, so
            //     resources read as PART OF THE WORLD. Starter clusters are SMALL (bootstrap only);
            //     biome clusters are LARGER & DENSER — the visible upgrade that rewards expansion.
            //     A clear central area stays open as the player's base/processing yard. ---
            const float baseClear = 11f;
            // STARTER BASIN — a few SMALL clusters of the BASICS only (wood/stone/food + the carved
            // water pond below). Enough to get going, NOT to scale: you outgrow them and must push out.
            SpawnClusters("Tree", wood, new Color(0.27f, 0.55f, 0.22f), PlaceholderArt.Triangle(),
                new Vector2(24f, 4f), 13f, 3, 3, 5, 2.2f, new Vector2(1.0f, 1.5f), 30, 1, baseClear);
            SpawnClusters("Rock", stone, new Color(0.55f, 0.55f, 0.6f), PlaceholderArt.Hexagon(),
                new Vector2(-24f, 4f), 13f, 3, 3, 5, 2.2f, new Vector2(1.0f, 1.5f), 30, 1, baseClear);
            SpawnClusters("Bush", food, new Color(0.45f, 0.55f, 0.25f), PlaceholderArt.Circle(),
                new Vector2(0f, -24f), 12f, 3, 3, 4, 1.9f, new Vector2(0.7f, 1.0f), 30, 1, baseClear);

            // --- GUARANTEED EXPANSION REGIONS: 3 meandering corridors out of spawn, each leading
            //     to a distinct, reachable region — so exploration is intentional (follow a path →
            //     find something) and Iron can NEVER soft-lock for want of findable ore. Each region
            //     is seeded with DENSE clusters — a clear density upgrade over the starter basin. ---
            TerrainGrid.CarveCorridors(3, 95f, 1);
            void Region(int k, float dist, Terrain biome, System.Action<Vector2> place)
            {
                float a = TerrainGrid.CorridorAngle(k, 3);
                var center = new Vector2(Mathf.Cos(a) * dist, Mathf.Sin(a) * dist);
                TerrainGrid.Paint(new Vector3(center.x, center.y, 0f), 16f, biome);
                place(center);
            }
            // Corridor 0 — nearest: a PLAINS region, SPARSE but MIXED (meat + clay) — first expansion.
            Region(0, 46f, Terrain.Plains, c => {
                SpawnClusters("Herd", meat, new Color(0.66f, 0.34f, 0.34f), PlaceholderArt.Circle(),
                    c, 7f, 2, 3, 4, 2.0f, new Vector2(0.8f, 1.2f), 30, 1, 0f);
                SpawnClusters("Clay", clay, new Color(0.68f, 0.46f, 0.36f), PlaceholderArt.Hexagon(),
                    c, 7f, 2, 3, 5, 2.4f, new Vector2(1.0f, 1.5f), 40, 1, 0f);
            });
            // Corridor 1 — a FOREST region: DENSE lumber groves + forage so wood/food scale by expanding.
            Region(1, 72f, Terrain.Forest, c => {
                SpawnClusters("Tree", wood, new Color(0.27f, 0.55f, 0.22f), PlaceholderArt.Triangle(),
                    c, 11f, 3, 6, 9, 3.2f, new Vector2(1.0f, 1.6f), 35, 1, 0f);
                SpawnClusters("Bush", food, new Color(0.45f, 0.55f, 0.25f), PlaceholderArt.Circle(),
                    c, 11f, 2, 4, 6, 2.6f, new Vector2(0.7f, 1.0f), 30, 1, 0f);
            });
            // Corridor 2 — a HILLS region: DENSE stone outcrops + the guaranteed reachable ORE (Iron unlock).
            Region(2, 86f, Terrain.Hills, c => {
                SpawnClusters("Rock", stone, new Color(0.55f, 0.55f, 0.6f), PlaceholderArt.Hexagon(),
                    c, 11f, 3, 5, 8, 3.0f, new Vector2(1.0f, 1.6f), 35, 1, 0f);
                SpawnClusters("Ore Vein", ore, new Color(0.62f, 0.58f, 0.42f), PlaceholderArt.Hexagon(),
                    c, 11f, 2, 4, 6, 2.8f, new Vector2(1.1f, 1.6f), 80, 0, 0f); // finite
            });

            // --- Welcome / starter guidance (fades after a few seconds) ---
            Toast.Show("<color=#ffd24d>Welcome, chief!</color>  Click trees & rocks to gather by hand.");
            Toast.Show("<size=15>Goal: grow from caveman to a self-running civilisation — build the Monument to win.</size>");
            Toast.Show("<size=14><color=#9cf>Progress = RESEARCH:</color> build a Sawmill + an <b>Idea Bench</b> (Planks+Stone → Idea Tablet), belt Tablets to a <b>Research Lodge</b>, then press <b>T</b> to spend points & advance.</size>");
            Toast.Show("<size=14>Press <b>H</b> help · <b>G</b> Guide · <b>B</b> build · <b>T</b> research tree · follow the Objectives (top-right).</size>");

            // --- BIOME FRONTIER: each region's resources live in DENSE clusters, so a biome is a
            //     real place worth routing home (find the region → route it home). FORESTS = lumber
            //     + fibre + forage; HILLS = stone, ore, gems; PLAINS = sparse but mixed (herds +
            //     clay). Finite ore/gems out here are the late-game economy. ---
            SpawnClustersInBiome("Tree", wood, new Color(0.27f, 0.55f, 0.22f), PlaceholderArt.Triangle(),
                Terrain.Forest, 11, 5, 8, 3.2f, 50f, 35, 1, new Vector2(1.0f, 1.6f));
            SpawnClustersInBiome("Cotton", fiber, new Color(0.80f, 0.84f, 0.66f), PlaceholderArt.Circle(),
                Terrain.Forest, 4, 3, 5, 2.6f, 50f, 36, 1, new Vector2(0.7f, 1.1f));
            // Forage (berries) in forests too, so FOOD can scale by expanding (start bushes are limited).
            SpawnClustersInBiome("Bush", food, new Color(0.45f, 0.55f, 0.25f), PlaceholderArt.Circle(),
                Terrain.Forest, 5, 4, 6, 2.6f, 30f, 30, 1, new Vector2(0.7f, 1.0f));
            SpawnClustersInBiome("Rock", stone, new Color(0.55f, 0.55f, 0.6f), PlaceholderArt.Hexagon(),
                Terrain.Hills, 10, 5, 7, 3.0f, 50f, 35, 1, new Vector2(1.0f, 1.6f));
            SpawnClustersInBiome("Ore Vein", ore, new Color(0.62f, 0.58f, 0.42f), PlaceholderArt.Hexagon(),
                Terrain.Hills, 6, 4, 6, 2.8f, 55f, 80, 0, new Vector2(1.1f, 1.6f)); // finite
            SpawnClustersInBiome("Gem Deposit", gems, new Color(0.50f, 0.82f, 0.76f), PlaceholderArt.Hexagon(),
                Terrain.Hills, 3, 2, 3, 2.2f, 90f, 60, 0, new Vector2(1.0f, 1.5f)); // finite, far
            // Meat + clay are the FIRST expansion target — just outside the basin (minClear 26).
            SpawnClustersInBiome("Herd", meat, new Color(0.66f, 0.34f, 0.34f), PlaceholderArt.Circle(),
                Terrain.Plains, 5, 3, 4, 2.2f, 26f, 30, 1, new Vector2(0.8f, 1.2f));
            SpawnClustersInBiome("Clay", clay, new Color(0.68f, 0.46f, 0.36f), PlaceholderArt.Hexagon(),
                Terrain.Plains, 5, 3, 4, 2.4f, 26f, 40, 1, new Vector2(1.0f, 1.5f));

            // Guarantee a reachable water feature just outside the starting basin so you can
            // build a Water Hole early (carved last so resource ClearAround can't erase it).
            TerrainGrid.CarveWater(new Vector3(15f, 3f, 0f), 3.0f);
            // Bake the biome map into its visual now that resource cells have been cleared.
            TerrainGrid.SpawnRenderer();
        }

        // Spawn `clusterCount` natural clusters in an AREA (areaCenter ± areaSpread), each a tight
        // knot of nodes — so resources look like groves/outcrops, not isolated dots. Clusters are
        // kept at least `minClear` from the world origin so the base area stays open.
        private static void SpawnClusters(string name, ItemDefinition item, Color color, Sprite sprite,
            Vector2 areaCenter, float areaSpread, int clusterCount, int minNodes, int maxNodes,
            float clusterRadius, Vector2 sizeRange, int capacity, int regen, float minClear)
        {
            for (int k = 0; k < clusterCount; k++)
            {
                Vector2 c = areaCenter + Random.insideUnitCircle * areaSpread;
                if (c.magnitude < minClear) c = c.normalized * minClear; // keep the base clear
                int n = Random.Range(minNodes, maxNodes + 1);
                SpawnCluster(name, item, color, sprite, c, n, clusterRadius, sizeRange, capacity, regen);
            }
        }

        // Spawn `clusterCount` clusters onto random cells of a BIOME (forest/hills/plains), each a
        // dense knot — so each biome reads as a resource-rich region worth expanding into.
        private static void SpawnClustersInBiome(string name, ItemDefinition item, Color color, Sprite sprite,
            Terrain biome, int clusterCount, int minNodes, int maxNodes, float clusterRadius,
            float minClear, int capacity, int regen, Vector2 sizeRange)
        {
            for (int k = 0; k < clusterCount; k++)
            {
                if (!TerrainGrid.TryRandomCellOfBiome(biome, minClear, 50, out var c)) continue;
                int n = Random.Range(minNodes, maxNodes + 1);
                SpawnCluster(name, item, color, sprite, c, n, clusterRadius, sizeRange, capacity, regen);
            }
        }

        // One cluster: `nodeCount` patches packed within `radius` of `center` (a grove / outcrop).
        private static void SpawnCluster(string name, ItemDefinition item, Color color, Sprite sprite,
            Vector2 center, int nodeCount, float radius, Vector2 sizeRange, int capacity, int regen)
        {
            for (int i = 0; i < nodeCount; i++)
            {
                Vector2 pos = center + Random.insideUnitCircle * radius;
                float size = Random.Range(sizeRange.x, sizeRange.y);
                float b = Random.Range(0.9f, 1.1f); // slight per-node colour variation
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

        // A Research Lodge: consumes the current age's research item into research points.
        private static BuildingDefinition MakeResearch(string name, int unlockAge, Color color, params ItemAmount[] cost)
        {
            var def = ScriptableObject.CreateInstance<BuildingDefinition>();
            def.displayName = name;
            def.kind = BuildingKind.Research;
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
            TerrainGrid.ClearAround(pos, 2.5f); // keep the patch + adjacent build cells off water
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
