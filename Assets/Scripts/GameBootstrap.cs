using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Builds the entire MVP scene in code so there's no Inspector wiring: camera,
    /// player, Colony (age/Industry-score holder), resource patches,
    /// the build system, and the HUD. Add this component to one empty GameObject
    /// and press Play.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        void Start()
        {
            // Wipe runtime rail state so a fresh game can't inherit phantom occupancy / station lanes from
            // a previous Play session (Unity keeps statics alive when domain-reload-on-play is disabled).
            RailGraph.Reset();
            RailNet.StationLane.Clear();
            PlayerController.HasBoat = false; // fresh game: no boat yet (statics persist with domain-reload-off)
            PlayerController.ResetMounts();    // fresh game: on foot, no mounts owned
            Garage.BuiltCount = 0; PlayerController.RecomputeGarageSlots();
            WorkshopBuilding.ResetPowerHint();

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
            var ore = MakeItem("ore", "Iron Ore", new Color(0.62f, 0.58f, 0.42f)); // FINITE — far in the hills (Iron age)
            var metal = MakeItem("metal", "Iron", new Color(0.66f, 0.68f, 0.74f)); // smelted from Iron Ore
            var tools = MakeItem("tools", "Tools", new Color(0.55f, 0.60f, 0.68f));
            // LIQUIDS: Oil (pumped from deposits, moved by pipes) + Fuel (refined from Oil + Water).
            var oil = MakeItem("oil", "Oil", new Color(0.22f, 0.18f, 0.26f)); oil.isLiquid = true;
            var fuel = MakeItem("fuel", "Fuel", new Color(0.88f, 0.66f, 0.26f));
            oil.description = "OIL — a LIQUID pumped from deposits by an Oil Well and moved through PIPES (never belts). Refined with Water into Fuel.";
            fuel.description = "FUEL — refined from Oil + Water at a Refinery. Belt it to an Oil Generator to burn for lots of Power.";
            // ORE SPLIT (deeper material tree): metals now branch — Copper (nearer, Bronze age), Iron
            // (far, Iron age), Steel (refined Iron, Industrial). Each age needs its own metal chain.
            var copperOre = MakeItem("copper_ore", "Copper Ore", new Color(0.80f, 0.52f, 0.30f));
            var copper = MakeItem("copper", "Copper", new Color(0.85f, 0.55f, 0.35f));
            var bronzePlate = MakeItem("bronze_plate", "Bronze Plate", new Color(0.78f, 0.58f, 0.32f));
            var steel = MakeItem("steel", "Steel", new Color(0.58f, 0.62f, 0.70f));
            // MECHANICAL CHAIN (the deeper late-game tree): bars/alloys -> shaped COMPONENTS (forged at the
            // Toolmaker) -> assembled ASSEMBLIES (built at the Engineering Lab) -> the final Engine. Each stage
            // combines more processed inputs than the last, so complexity scales with the age you reach.
            var bronzeGear = MakeItem("bronze_gear", "Bronze Gear", new Color(0.80f, 0.56f, 0.28f)); // Bronze-age component
            var steelBeam  = MakeItem("steel_beam",  "Steel Beam",  new Color(0.62f, 0.66f, 0.74f)); // Iron-age component
            var machinePart = MakeItem("machine_part", "Machine Part", new Color(0.50f, 0.58f, 0.66f)); // Iron-age assembly (3 inputs)
            var engine     = MakeItem("engine",      "Engine",      new Color(0.36f, 0.42f, 0.50f)); // Industrial final product (needs Fuel)
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
            studyScroll.description = "RESEARCH item: Copper + Planks at a Scroll Maker. Deliver to a Research Lodge to research the Bronze Age.";
            schematic.description = "RESEARCH item: Bronze Plate + Pottery at a Drafting Table. Deliver to a Research Lodge to research the Iron Age.";
            blueprint.description = "RESEARCH item: Steel + Tools at an Engineering Lab. Deliver to a Research Lodge to research the Industrial Age.";

            // --- Item descriptions (shown in the in-game Guide, key G) ---
            wood.description = "The starter resource — chop it from trees. Used by nearly every building, and refined into Planks and Charcoal.";
            stone.description = "Basic building material — mine it from rocks. Used in most early buildings and recipes (and to research the Tribal Age).";
            food.description = "Berries gathered from bushes (Forager Hut). Keeps people alive; cook it for far more nourishment.";
            water.description = "Drawn from lakes (Water Hole). People drink it every day; also needed for cooking, farming and baking.";
            planks.description = "A Sawmill turns Wood into Planks — a core material used by advanced machines and many recipes.";
            cookedFood.description = "A Campfire cooks Food (+Wood +Water). More nourishing than raw, and your people's first comfort good (Tribal).";
            meat.description = "Hunted from animal herds (Hunter's Hut). A nourishing food; keep it in a Smokehouse.";
            clay.description = "Dug from clay deposits (Clay Pit). Fired into Bricks, and shaped into Pottery.";
            charcoal.description = "A Charcoal Burner turns Wood into Charcoal. It fuels the Kiln AND both Smelters (Basic & Advanced) — a key shared bottleneck.";
            bricks.description = "A Kiln fires Clay + Charcoal into Bricks — a sturdy material for advanced buildings.";
            grain.description = "A Farm grows Grain from Water. Milled into Flour.";
            flour.description = "A Mill grinds Grain into Flour. Baked into Bread.";
            bread.description = "A Bakery bakes Flour + Water into Bread — high nourishment, and a Bronze comfort good.";
            ore.description = "IRON ORE — mined from FINITE veins far in the hills. Smelted into Iron. Its scarcity drives Iron-age exploration.";
            metal.description = "IRON — the Basic Smelter melts Iron Ore + Charcoal into Iron. The backbone of Tools, Steel and the Monument.";
            tools.description = "A Toolmaker crafts Iron + Planks into Tools — needed for Blueprints (Industrial-Age research) and the Monument.";
            monument.description = "Built at the Monument from Iron + Tools + Bricks + Planks. Collect 10 Blocks to WIN the game.";
            copperOre.description = "COPPER ORE — the first metal ore, from copper deposits in the nearest expansion region. Smelted into Copper (the Bronze-age chain).";
            copper.description = "COPPER — the Basic Smelter melts Copper Ore + Charcoal into Copper. Feeds Study Scrolls (Bronze research) and Bronze Plates.";
            bronzePlate.description = "BRONZE PLATE — the Advanced Smelter presses Copper + Bricks into a plate. A deeper part needed to research the Iron Age (Schematics).";
            steel.description = "STEEL — the Advanced Smelter (set to Steel) forges Iron + Charcoal into Steel. The hardest metal; needed to research the Industrial Age (Blueprints).";
            fiber.description = "Plant Fiber harvested from Cotton fields (Cotton Farm). Woven into Cloth.";
            cloth.description = "A Weaver turns Fiber into Cloth. Tailored into Clothes.";
            clothes.description = "A Tailor sews Cloth into Clothes — an Industrial luxury comfort good.";
            pot.description = "A Potter shapes Clay into Pottery — combined with Bricks at a Drafting Table to make Schematics (Iron-Age research).";
            gems.description = "The rarest resource, in FINITE deposits far out in the map. Cut into Jewelry; reaching them rewards exploration.";
            jewelry.description = "A Jeweler crafts Gems into Jewelry — a high-value luxury good.";
            stoneBlock.description = "A Mason cuts Stone into Stone Blocks — used to build the sturdy Stone House.";

            // --- Belt icons (placeholder shapes by material family, so items are distinguishable
            //     on conveyors; real per-item art drops into ItemDefinition.icon later). ---
            // Item visuals route through SpriteDatabase: set each item's fallback SHAPE by material family,
            // then resolve its icon (external sprite if one exists, else the procedural shape — identical
            // look for now). Real per-item art drops in by adding Resources/art/<id> sprites later.
            foreach (var it in new[] { wood, planks }) it.sprite = SpriteDefinition.Of(PlaceholderShape.Triangle);              // woody
            foreach (var it in new[] { stone, ore, clay, bricks, stoneBlock, gems, charcoal }) it.sprite = SpriteDefinition.Of(PlaceholderShape.Hexagon); // mineral
            foreach (var it in new[] { metal, tools, monument, cloth, clothes, pot, jewelry, ideaTablet, studyScroll, schematic, blueprint }) it.sprite = SpriteDefinition.Of(PlaceholderShape.Square); // manufactured
            foreach (var it in new[] { wood, planks, stone, ore, clay, bricks, stoneBlock, gems, charcoal, metal, tools, monument, cloth, clothes, pot, jewelry, ideaTablet, studyScroll, schematic, blueprint })
                it.icon = SpriteDatabase.ForItem(it);
            // food / cooked / meat / grain / flour / bread / fiber keep the default round dot.

            // --- Buildings ---
            var woodHut = MakeCollector("Wood Hut", wood, 1, 2.0f, 2, 12, new Color(0.80f, 0.52f, 0.25f),
                new ItemAmount(wood, 5), new ItemAmount(stone, 3));
            var stonePit = MakeCollector("Stone Pit", stone, 1, 2.0f, 2, 12, new Color(0.45f, 0.52f, 0.62f),
                new ItemAmount(wood, 5), new ItemAmount(stone, 5));
            var foragerHut = MakeCollector("Forager Hut", food, 1, 2.0f, 2, 12, new Color(0.78f, 0.40f, 0.40f),
                new ItemAmount(wood, 4));
            foragerHut.autoStore = true; // SURVIVAL: its worker carries Food to the nearest Granary (no belt needed)
            foragerHut.description = "Auto-gathers berries from a nearby bush at a fixed rate (no workers). Belt or place a Granary next to it to store the Food.";
            var waterHole = MakeCollector("Water Hole", water, 1, 2.0f, 2, 12, new Color(0.40f, 0.62f, 0.85f),
                new ItemAmount(wood, 4));
            waterHole.fromWaterTerrain = true; // must sit next to real water terrain (river/lake)
            waterHole.autoStore = true; // SURVIVAL: its worker carries Water to the nearest Water Barrel (no belt/pipe needed)
            waterHole.description = "Auto-draws Water from adjacent water terrain at a fixed rate (no workers). Adjacent buildings can use it directly; pipes (Bronze) move it further.";
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
            var sawmill = MakeWorkshop("Sawmill", planks, 1, 5.0f, 2, 12, new Color(0.66f, 0.50f, 0.30f),
                new List<ItemAmount> { new ItemAmount(wood, 2) }, // 2 wood -> 1 plank / 5s = 24 wood/min — BELOW a wooden belt (40), so wood PILES UP: build more Sawmills
                new ItemAmount(wood, 6), new ItemAmount(stone, 4));
            // Campfire: needs Wood as fuel and Water to cook, plus the raw Food.
            var campfire = MakeWorkshop("Campfire", cookedFood, 1, 3.0f, 2, 12, new Color(0.85f, 0.45f, 0.25f),
                new List<ItemAmount> { new ItemAmount(food, 2), new ItemAmount(wood, 1), new ItemAmount(water, 1) },
                new ItemAmount(wood, 4), new ItemAmount(stone, 2));
            var woodStore = MakeStorage("Woodpile", wood, 100, new Color(0.62f, 0.40f, 0.20f),
                new ItemAmount(wood, 8));
            woodStore.description = "Resource-specific storage: holds Wood only. (Each basic store is named for what it holds — Woodpile, Stone Storage, Granary, Water Barrel. The configurable 'Warehouse' is the ONE general store you assign yourself.)";
            var stoneStore = MakeStorage("Stone Stockpile", stone, 160, new Color(0.66f, 0.67f, 0.70f),
                new ItemAmount(wood, 8));
            var foodStore = MakeStorage("Granary", food, 100, new Color(0.70f, 0.45f, 0.35f),
                new ItemAmount(wood, 8));
            var waterStore = MakeStorage("Water Barrel", water, 100, new Color(0.35f, 0.50f, 0.72f),
                new ItemAmount(wood, 8));
            // Generic warehouse: the player picks what it stores (e.g. Planks, Cooked Food).
            var warehouse = MakeStorage("Warehouse", null, 300, new Color(0.55f, 0.52f, 0.45f),
                new ItemAmount(wood, 14), new ItemAmount(stone, 6)); warehouse.configurable = true;
            warehouse.footprintW = 3; warehouse.footprintH = 3; // a proper HUB store — big, with 3 in + 3 out slots
            // (Construction is INSTANT now — no Construction Yard / builders.)
            // Long-distance logistics: depots + caravan routes (replaces the old haulers).
            var depot = ScriptableObject.CreateInstance<BuildingDefinition>();
            depot.displayName = "Station"; depot.kind = BuildingKind.Depot; depot.unlockAge = 0;
            depot.item = null; depot.capacity = 80; depot.color = new Color(0.50f, 0.45f, 0.55f);
            depot.footprintW = 3; depot.footprintH = 1; // a platform straddling an east–west TRACK LANE
            depot.cost = new List<ItemAmount> { new ItemAmount(wood, 12), new ItemAmount(stone, 8) };
            depot.description = "Transport Station — a 3×1 platform with a TRACK LANE running straight through it (east–west). Lay rail up to either end and trains run through OR stop here. Belt goods IN only on the SOUTH edge (cyan), OUT only on the NORTH edge (green). Build a line: select a station → '+ Add line' → click each stop in order → click the FIRST one to close the loop. The vehicle LOADS at the first stop (the pickup) and DELIVERS at the rest, passing any stations not on its line — and auto-upgrades Donkey Cart → wagon → train.";
            // Harbour: a dock placed on the SHORE (next to water). Belt goods in/out like a Station, but it
            // runs CARGO SHIPS over the water between harbours instead of trains on rail — the way to reach
            // (and haul back from) resources across the sea. Lives under the Boats build tab.
            var harbour = ScriptableObject.CreateInstance<BuildingDefinition>();
            harbour.displayName = "Harbour"; harbour.kind = BuildingKind.Depot; harbour.unlockAge = 0;
            harbour.isHarbour = true; harbour.menuCategory = "Boats";
            harbour.item = null; harbour.capacity = 80; harbour.color = new Color(0.40f, 0.52f, 0.62f);
            harbour.footprintW = 3; harbour.footprintH = 1; // a dock straddling the shore
            harbour.cost = new List<ItemAmount> { new ItemAmount(wood, 14), new ItemAmount(stone, 6) };
            harbour.description = "HARBOUR — build it on the SHORE (next to water). Belt goods IN on the SOUTH edge / OUT on the NORTH, like a Station — but instead of rail it runs CARGO SHIPS over the water. Select a harbour → '+ Add line' → click another harbour to ship goods across the sea (the ship sails straight over water). The way to reach island resources no road can.";
            // Track: drag-laid rail tiles. Route vehicles (donkey cart → … → train) path ALONG the
            // track between stations instead of cutting straight across — so you plan the line.
            var rail = ScriptableObject.CreateInstance<BuildingDefinition>();
            rail.displayName = "Track"; rail.kind = BuildingKind.Rail; rail.unlockAge = 0;
            rail.color = new Color(0.42f, 0.40f, 0.36f);
            rail.cost = new List<ItemAmount> { new ItemAmount(stone, 1) };
            rail.description = "TRACK — drag to lay rail. A route vehicle (the Station's donkey cart → wagon → train) follows a continuous track between two Stations rather than cutting straight across; lay the line you want it to run. Where there's no connecting track it still travels straight. Reserves its cell (no belts/buildings on top).";
            // Signal: one-way + block control for trains (place on a track cell). Trains can't cross.
            var signal = ScriptableObject.CreateInstance<BuildingDefinition>();
            signal.displayName = "Signal"; signal.kind = BuildingKind.Signal; signal.unlockAge = 0;
            signal.color = new Color(0.88f, 0.82f, 0.32f);
            signal.cost = new List<ItemAmount> { new ItemAmount(stone, 1) };
            signal.description = "RAIL SIGNAL — place ON a track cell, facing a direction (R rotates). A train may pass a signal only when travelling its way — so two opposing signals on parallel tracks make a ONE-WAY loop — AND only when the BLOCK AHEAD (track up to the next signal) is clear of other trains. Use them to stop trains crossing and to plan one-way routes. Green = clear, red = occupied. (A single bidirectional track with no passing loop will deadlock two trains — build a loop, like a real railway, or use Two-Way Signals to share a stretch.)";
            // Two-way signal: a BLOCK signal that permits travel in EITHER direction (no one-way restriction)
            // while still enforcing one-train-per-block — for sharing a bidirectional stretch safely.
            var twoWaySignal = ScriptableObject.CreateInstance<BuildingDefinition>();
            twoWaySignal.displayName = "Two-Way Signal"; twoWaySignal.kind = BuildingKind.Signal; twoWaySignal.unlockAge = 0;
            twoWaySignal.bothWaySignal = true;
            twoWaySignal.color = new Color(0.55f, 0.78f, 0.95f);
            twoWaySignal.cost = new List<ItemAmount> { new ItemAmount(stone, 1) };
            twoWaySignal.description = "TWO-WAY RAIL SIGNAL — like a Signal, but trains may pass in EITHER direction along the track (R aims its axis). It still enforces ONE train per block (green = clear, red = occupied), so it protects a shared bidirectional stretch without forcing a one-way route. Pair with passing loops so opposing trains don't deadlock head-on.";
            // Route vehicle tiers — each links two depots; bigger/faster as ages advance (the
            // donkey → train upgrade path). Existing routes upgrade in place on age-up.
            var caravan = MakeRoute("Donkey Cart", 12, 3.5f, 0, new Color(0.62f, 0.48f, 0.34f),
                new ItemAmount(wood, 8));
            var oxCart = MakeRoute("Ox Cart", 18, 4.5f, 1, new Color(0.60f, 0.45f, 0.30f),
                new ItemAmount(wood, 10), new ItemAmount(planks, 4));
            var wagonTrain = MakeRoute("Wagon Train", 36, 6.5f, 3, new Color(0.45f, 0.45f, 0.52f),
                new ItemAmount(planks, 10), new ItemAmount(metal, 8));
            var cargoDrone = MakeRoute("Cargo Drone", 24, 12f, 4, new Color(0.50f, 0.70f, 0.85f),
                new ItemAmount(metal, 10), new ItemAmount(tools, 4));
            // Cargo SHIP tiers — run on HARBOUR lines (over water). Available from the start so boats are an
            // early option for crossing rivers/lakes; a bigger steam ship arrives in the Iron age.
            var cargoShip = MakeRoute("Cargo Ship", 40, 5.0f, 0, new Color(0.40f, 0.60f, 0.85f),
                new ItemAmount(wood, 12), new ItemAmount(planks, 4));
            var steamShip = MakeRoute("Steam Ship", 80, 7.5f, 3, new Color(0.45f, 0.55f, 0.70f),
                new ItemAmount(metal, 12), new ItemAmount(planks, 8));

            // --- Age 1: Tribal ---
            var hunter = MakeCollector("Hunter's Hut", meat, 1, 2.0f, 2, 12, new Color(0.66f, 0.34f, 0.34f),
                new ItemAmount(wood, 6)); hunter.unlockAge = 1;
            var clayPit = MakeCollector("Clay Pit", clay, 1, 2.0f, 2, 12, new Color(0.68f, 0.46f, 0.36f),
                new ItemAmount(wood, 5)); clayPit.unlockAge = 1;
            var charcoalBurner = MakeWorkshop("Charcoal Burner", charcoal, 2, 4.0f, 2, 12, new Color(0.62f, 0.58f, 0.54f),
                new List<ItemAmount> { new ItemAmount(wood, 3) },
                new ItemAmount(wood, 6), new ItemAmount(stone, 4)); charcoalBurner.unlockAge = 1; // 2 charcoal / 4s = 30/min; Kiln+both Smelters want ~57/min → you need SEVERAL — the shared bottleneck
            var clayStore = MakeStorage("Clay Pile", clay, 100, new Color(0.60f, 0.42f, 0.34f),
                new ItemAmount(wood, 8)); clayStore.unlockAge = 1;
            var smokehouse = MakeStorage("Smokehouse", meat, 100, new Color(0.55f, 0.32f, 0.30f),
                new ItemAmount(wood, 8), new ItemAmount(stone, 2)); smokehouse.unlockAge = 1;

            // --- Age 2: Bronze ---
            var kiln = MakeWorkshop("Kiln", bricks, 1, 5.0f, 2, 12, new Color(0.70f, 0.42f, 0.34f),
                new List<ItemAmount> { new ItemAmount(clay, 2), new ItemAmount(charcoal, 1) }, // 1 brick / 5s — clay piles up: build more Kilns
                new ItemAmount(wood, 8), new ItemAmount(stone, 6)); kiln.unlockAge = 2; kiln.requiresPower = true;
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
            // BELT TIER LADDER (each ~2× the last; overlay a faster tier on a slower belt to upgrade
            // in place — no need to delete). Wooden is a deliberately SLOW starter (½ a collector's
            // output) so upgrading belts is an early, meaningful goal: Wooden 30 → Conveyor 60 →
            // Geared 120 → Steel 240. Splitters/Mergers run at the top rate so they never throttle.
            var woodBelt = ScriptableObject.CreateInstance<BuildingDefinition>();
            woodBelt.displayName = "Wooden Belt"; woodBelt.kind = BuildingKind.Belt; woodBelt.unlockAge = 0;
            woodBelt.interval = 1.5f; // 40 items/min — a starter belt; still below a collector's 60/min so upgrading matters
            woodBelt.color = new Color(0.60f, 0.50f, 0.35f);
            woodBelt.cost = new List<ItemAmount> { new ItemAmount(wood, 1) };
            woodBelt.description = "Carries items along its arrow at 40/min — a starter belt that lags a collector (60/min), so a single line backs up at full tilt. Research the cheap Conveyor (60/min) early to keep up, or run a 2nd line. Drag to lay; R rotates. RED = dead end, YELLOW = backed up.";
            var fastBelt = ScriptableObject.CreateInstance<BuildingDefinition>();
            fastBelt.displayName = "Conveyor Belt"; fastBelt.kind = BuildingKind.Belt; fastBelt.unlockAge = 0;
            fastBelt.interval = 1.0f; // 60 items/min — 2× wooden, a match for one collector
            fastBelt.color = new Color(0.74f, 0.66f, 0.46f);
            fastBelt.cost = new List<ItemAmount> { new ItemAmount(planks, 1) };
            fastBelt.description = "Conveyor Belt — 60/min, 2× the Wooden Belt and a match for one collector. OVERLAY it on a wooden belt to upgrade that segment in place (no delete needed). The first rung of the ladder → Geared (120) → Steel (240).";
            var gearedBelt = ScriptableObject.CreateInstance<BuildingDefinition>();
            gearedBelt.displayName = "Geared Belt"; gearedBelt.kind = BuildingKind.Belt; gearedBelt.unlockAge = 0;
            gearedBelt.interval = 0.5f; // 120 items/min — 2× a Conveyor
            gearedBelt.color = new Color(0.80f, 0.55f, 0.34f);
            gearedBelt.cost = new List<ItemAmount> { new ItemAmount(planks, 2), new ItemAmount(metal, 1) };
            gearedBelt.description = "Geared Belt — 120/min, 2× a Conveyor. Overlay it on a slower belt to upgrade in place. For high-throughput lines once the factory grows (Bronze).";
            var steelBelt = ScriptableObject.CreateInstance<BuildingDefinition>();
            steelBelt.displayName = "Steel Belt"; steelBelt.kind = BuildingKind.Belt; steelBelt.unlockAge = 0;
            steelBelt.interval = 0.25f; // 240 items/min — the fastest tier, 4× a Conveyor
            steelBelt.color = new Color(0.62f, 0.66f, 0.72f);
            steelBelt.cost = new List<ItemAmount> { new ItemAmount(metal, 2), new ItemAmount(planks, 2) };
            steelBelt.description = "Steel Belt — 240/min, the fastest tier (4× a Conveyor). Overlay to upgrade. For the densest late-game lines (Iron).";
            // Splitter: a 1→3 belt that distributes items EVENLY between three outputs. Lets one
            // supply line feed three machines. (Belt kind, flagged splitter — placed like a belt.)
            var splitter = ScriptableObject.CreateInstance<BuildingDefinition>();
            splitter.displayName = "Splitter"; splitter.kind = BuildingKind.Belt; splitter.splitter = true; splitter.unlockAge = 0;
            splitter.interval = 0.25f; // runs at the TOP belt rate (240/min) so it never throttles any tier
            splitter.color = new Color(0.45f, 0.62f, 0.72f);
            splitter.cost = new List<ItemAmount> { new ItemAmount(wood, 2) };
            splitter.description = "1→3 SPLITTER: pulls from behind and sends items EVENLY to three outputs — forward, left and right (R rotates). If one output backs up it sends to the others, so it never stalls. Feed three machines from one supply line. (Smart/filtered splitters come later.)";
            // Merger: the reverse of a splitter — COMBINES two belt lines into one. Needed because a
            // plain belt now refuses a 2nd feeder (no silent merging by pointing a belt onto a line).
            var merger = ScriptableObject.CreateInstance<BuildingDefinition>();
            merger.displayName = "Merger"; merger.kind = BuildingKind.Belt; merger.merger = true; merger.unlockAge = 0;
            merger.interval = 0.25f; // runs at the TOP belt rate (240/min) so it never throttles any tier
            merger.color = new Color(0.72f, 0.55f, 0.45f);
            merger.cost = new List<ItemAmount> { new ItemAmount(wood, 2) };
            merger.description = "N→1 MERGER: combines belt lines — point two belts into it and it pushes their items out forward (R rotates). Plain belts refuse a 2nd feeder, so a Merger is how you deliberately join two lanes of the same item.";

            // --- MID-GAME SMART LOGISTICS (research "Smart Logistics", needs Bronze) — placed one click at a
            //     time like a junction; filter/gate also OVERLAY a plain belt to convert it. Gated behind a
            //     tech + the Bronze age so they arrive once a base is established (not too early). ---
            var undergroundBelt = ScriptableObject.CreateInstance<BuildingDefinition>();
            undergroundBelt.displayName = "Underground Belt"; undergroundBelt.kind = BuildingKind.Belt; undergroundBelt.underground = true;
            undergroundBelt.unlockAge = 2; undergroundBelt.requiredTech = "smart_logistics";
            undergroundBelt.interval = 1.0f; // 60/min — matches a Conveyor
            undergroundBelt.color = new Color(0.85f, 0.85f, 0.92f);
            undergroundBelt.cost = new List<ItemAmount> { new ItemAmount(metal, 1), new ItemAmount(planks, 1) };
            undergroundBelt.description = "UNDERGROUND BELT — items travel HIDDEN up to 3 tiles, so other belts and TRACK can cross over the gap. Click to place the ENTRANCE (facing the flow, R rotates), then click up to 4 tiles ahead in the SAME direction for the EXIT — they auto-pair. An unpaired end shows red until you place its partner.";

            var filterBelt = ScriptableObject.CreateInstance<BuildingDefinition>();
            filterBelt.displayName = "Filter Belt"; filterBelt.kind = BuildingKind.Belt; filterBelt.filter = true;
            filterBelt.unlockAge = 2; filterBelt.requiredTech = "smart_logistics";
            filterBelt.interval = 1.0f;
            filterBelt.color = new Color(0.45f, 0.72f, 0.55f);
            filterBelt.cost = new List<ItemAmount> { new ItemAmount(planks, 1), new ItemAmount(metal, 1) };
            filterBelt.description = "FILTER BELT — conveys ONLY one item type and turns the rest away (they back up / take another route). It LOCKS onto the first item that reaches it. Pair with a Splitter to SORT a mixed line: send the one you want down the filtered lane, the rest carry on. Overlay it on a plain belt to convert in place.";

            var prioritySplitter = ScriptableObject.CreateInstance<BuildingDefinition>();
            prioritySplitter.displayName = "Priority Splitter"; prioritySplitter.kind = BuildingKind.Belt; prioritySplitter.splitter = true; prioritySplitter.priority = true;
            prioritySplitter.unlockAge = 2; prioritySplitter.requiredTech = "smart_logistics";
            prioritySplitter.interval = 0.25f; // top belt rate so it never throttles
            prioritySplitter.color = new Color(0.55f, 0.55f, 0.80f);
            prioritySplitter.cost = new List<ItemAmount> { new ItemAmount(planks, 2), new ItemAmount(metal, 1) };
            prioritySplitter.description = "PRIORITY SPLITTER — fills its FORWARD output first and only sends OVERFLOW to the sides (left/right). Keep a key machine fed at full rate and spill the surplus elsewhere. Pulls from behind; R rotates; never stalls.";

            var conditionalGate = ScriptableObject.CreateInstance<BuildingDefinition>();
            conditionalGate.displayName = "Gate Belt"; conditionalGate.kind = BuildingKind.Belt; conditionalGate.gate = true;
            conditionalGate.unlockAge = 2; conditionalGate.requiredTech = "smart_logistics";
            conditionalGate.interval = 1.0f;
            conditionalGate.color = new Color(0.80f, 0.62f, 0.38f);
            conditionalGate.cost = new List<ItemAmount> { new ItemAmount(planks, 1), new ItemAmount(metal, 1) };
            conditionalGate.description = "GATE BELT — only passes items while the line it feeds still has ROOM: it watches the nearest downstream STORAGE and SHUTS (backs up, turns amber) once that store is ~90% full, re-opening as space frees. Stops you over-filling one buffer while starving another. Overlay it on a plain belt to convert.";

            // ELEVATED TRACK — a raised viaduct (Rail kind) that a train line crosses OVER belts on. Part of the
            // same Smart Logistics tech as the underground belt: belts go under, trains go over.
            var elevatedRail = ScriptableObject.CreateInstance<BuildingDefinition>();
            elevatedRail.displayName = "Elevated Track"; elevatedRail.kind = BuildingKind.Rail; elevatedRail.elevated = true;
            elevatedRail.unlockAge = 2; elevatedRail.requiredTech = "smart_logistics";
            elevatedRail.color = new Color(0.70f, 0.74f, 0.82f);
            elevatedRail.cost = new List<ItemAmount> { new ItemAmount(metal, 1), new ItemAmount(stone, 1) };
            elevatedRail.description = "ELEVATED TRACK — a raised viaduct your train line runs on, so it can cross OVER conveyor belts (the belt passes underneath). Drag to plan like normal track (90° paths); lay it across a belt corridor and join ground track at each end. Crosses belts — not other train track.";

            // Exploration payoff: Ore is mined from distant veins, hauled home, and is
            // required to reach the Iron Age.
            var mine = MakeCollector("Iron Mine", ore, 1, 2.5f, 2, 12, new Color(0.50f, 0.48f, 0.40f),
                new ItemAmount(wood, 6), new ItemAmount(stone, 4)); mine.unlockAge = 1;
            var oreStore = MakeStorage("Ore Stockpile", ore, 160, new Color(0.60f, 0.48f, 0.37f),
                new ItemAmount(wood, 8)); oreStore.unlockAge = 1;
            oreStore.description = "ORE STOCKPILE — an OPEN-AIR heap for Iron Ore (no roof). Belt ore in, belt it out to a Smelter. Holds a big pile.";
            stoneStore.description = "STONE STOCKPILE — an OPEN-AIR heap for Stone (no roof). Belt stone in and out. Cheap, holds a big pile — the natural home for raw Stone instead of a Warehouse.";
            // --- SMELTERS (configurable, multi-recipe). A BASIC smelter for ore→bar and an ADVANCED
            //     smelter that combines materials into alloy bars. Each replaces TWO old fixed-recipe
            //     buildings (Iron+Copper Smelters → Basic; Bronzeworks+Steel Foundry → Advanced). Click
            //     a smelter to switch its recipe; the selected recipe drives its inputs/output. Charcoal
            //     is shared across both smelters + the Kiln — the key bottleneck. ---
            var basicSmelter = MakeWorkshop("Basic Smelter", copper, 1, 4.0f, 2, 12, new Color(0.74f, 0.52f, 0.42f),
                new List<ItemAmount> { new ItemAmount(copperOre, 1), new ItemAmount(charcoal, 1) },
                new ItemAmount(stone, 8), new ItemAmount(clay, 4)); basicSmelter.unlockAge = 1;
            basicSmelter.recipes = new List<Recipe>
            {
                new Recipe(copper, 1, 4.0f, 1, new ItemAmount(copperOre, 1), new ItemAmount(charcoal, 1)), // 15/min — ore + charcoal both pile up: parallel smelters
                new Recipe(metal,  1, 4.5f, 1, new ItemAmount(ore, 1),       new ItemAmount(charcoal, 1)),
            };
            basicSmelter.description = "BASIC SMELTER — select its recipe: Copper Ore + Charcoal → Copper, or Iron Ore + Charcoal → Iron. One smelter for both basic metals (click it to switch). Charcoal is shared with the Kiln + Advanced Smelter — a key bottleneck.";
            var advancedSmelter = MakeWorkshop("Advanced Smelter", bronzePlate, 1, 4.5f, 2, 12, new Color(0.66f, 0.60f, 0.52f),
                new List<ItemAmount> { new ItemAmount(copper, 1), new ItemAmount(bricks, 1) },
                new ItemAmount(bricks, 8), new ItemAmount(metal, 4)); advancedSmelter.unlockAge = 2;
            advancedSmelter.recipes = new List<Recipe>
            {
                new Recipe(bronzePlate, 1, 4.5f, 2, new ItemAmount(copper, 1), new ItemAmount(bricks, 1)), // ~13/min — copper + bricks pile up
                new Recipe(steel,       1, 5.0f, 3, new ItemAmount(metal, 1),  new ItemAmount(charcoal, 1)),
            };
            advancedSmelter.description = "ADVANCED SMELTER — combines materials into alloy bars; select its recipe: Copper + Bricks → Bronze Plate, or Iron + Charcoal → Steel (Steel unlocks in the Iron Age). One furnace for both alloys (click it to switch).";
            // Smelters need POWER — they run only while connected to a powered network (a Generator,
            // extended by Power Poles). See the Wood Generator + Power Pole defs below.
            basicSmelter.requiresPower = true;
            advancedSmelter.requiresPower = true;
            // Make power an actual decision (it was wildly over-supplied): the heavy smelters draw 20 each, so a
            // Wood Generator (40) runs ~2, a Coal Generator (60) ~3 — you plan generation as the base grows.
            basicSmelter.powerDraw = 20;
            advancedSmelter.powerDraw = 20;
            // Toolmaker: Metal + Planks -> Tools (an Iron-age comfort good).
            var toolmaker = MakeWorkshop("Toolmaker", tools, 1, 5.0f, 2, 12, new Color(0.50f, 0.55f, 0.60f),
                new List<ItemAmount> { new ItemAmount(metal, 1), new ItemAmount(planks, 1) },
                new ItemAmount(planks, 8), new ItemAmount(bricks, 6)); toolmaker.unlockAge = 2;
            // The Toolmaker is a FORGE: one building shapes alloys/metals into the mechanical COMPONENTS, picked
            // by recipe (click it to switch). Reuses a single building for the whole component tier (less clutter).
            toolmaker.recipes = new List<Recipe>
            {
                new Recipe(bronzeGear, 1, 4.5f, 2, new ItemAmount(bronzePlate, 1), new ItemAmount(planks, 1)), // Bronze age — components are slow: parallel Toolmakers
                new Recipe(tools,      1, 5.0f, 3, new ItemAmount(metal, 1),       new ItemAmount(planks, 1)), // Iron age
                new Recipe(steelBeam,  1, 5.0f, 3, new ItemAmount(steel, 1),       new ItemAmount(planks, 1)), // Iron age
            };
            toolmaker.description = "FORGE / TOOLMAKER — shapes metal into mechanical COMPONENTS; pick its recipe (click to switch): Bronze Plate + Planks → Bronze Gear (Bronze age), Iron + Planks → Tools, or Steel + Planks → Steel Beam (both Iron age). One forge for the whole component tier; build more to run several at once.";
            // --- DEEPER METAL TREE (ore split): copper (Bronze chain) + iron (Iron chain). The ores
            //     have their own mines; both basic metals + both alloys are made in the configurable
            //     Basic / Advanced Smelters defined above (no separate per-metal smelter buildings). ---
            var copperMine = MakeCollector("Copper Mine", copperOre, 1, 2.5f, 2, 12, new Color(0.78f, 0.52f, 0.32f),
                new ItemAmount(wood, 6), new ItemAmount(stone, 4)); copperMine.unlockAge = 1;
            copperMine.description = "Build ON a Copper Deposit (in the nearest expansion region). Copper Ore is FINITE — the start of the metal chain, and the key to the Bronze Age.";
            // Power: the Industrial age's new constraint. The Coal Generator burns Charcoal
            // to supply electrical power; from the Industrial age machines need it (or they
            // brown out and slow down). Unlocks in Iron so you can prepare before it bites.
            var generator = MakePower("Coal Generator", 60, charcoal, 1, 3f, 3, new Color(0.30f, 0.30f, 0.34f),
                new ItemAmount(bricks, 12), new ItemAmount(metal, 6));
            generator.description = "Burns Charcoal to supply Power to the grid — a bigger, steadier source than the Wood Generator for a large powered base. BELT Charcoal into its cyan fuel edge (R aims it) to automate it, or it sips from your carried pile. Wire it (up to 4 cables) to poles, batteries or machines. Too little generation and wired machines brown out (slow down).";
            // BRONZE-AGE POWER: electricity is introduced in the Bronze age. A Wood Generator feeds the
            // grid; you draw WIRES from it to Poles/Batteries/machines (no radius). From Bronze,
            // requiresPower machines must be WIRED to a powered network to run (before Bronze they run free).
            var woodGen = MakePower("Wood Generator", 40, wood, 1, 2.5f, 2, new Color(0.55f, 0.40f, 0.25f),
                new ItemAmount(wood, 6), new ItemAmount(stone, 4));
            woodGen.description = "WOOD GENERATOR — burns Wood to supply POWER (introduced in the Bronze age). BELT Wood into its cyan fuel edge (R aims it) to automate it, or it sips from your carried pile. Select it and 'Connect wire' to a machine, Battery or Power Pole (up to 4 wires). Wired machines run; unwired ones stop. Powers ~4 machines.";
            var pole = ScriptableObject.CreateInstance<BuildingDefinition>();
            pole.displayName = "Power Pole"; pole.kind = BuildingKind.Pole; pole.unlockAge = 2;
            pole.color = new Color(0.55f, 0.42f, 0.28f);
            pole.cost = new List<ItemAmount> { new ItemAmount(wood, 2) };
            pole.description = "POWER POLE — relays power across distance. Draw WIRES (up to 4 per pole) to Generators, Batteries, machines or other poles. Each wire reaches a limited distance, so chain poles to span big gaps.";
            // Battery (Bronze): a wired store that soaks surplus generation and covers demand spikes /
            // generation dips — smooths brownouts so a small generator can cover a peaky base.
            var battery = ScriptableObject.CreateInstance<BuildingDefinition>();
            battery.displayName = "Battery"; battery.kind = BuildingKind.Battery; battery.unlockAge = 2;
            battery.batteryCapacity = 200f; battery.batteryRate = 30f;
            battery.color = new Color(0.32f, 0.62f, 0.50f);
            battery.cost = new List<ItemAmount> { new ItemAmount(copper, 6), new ItemAmount(bricks, 4) };
            battery.description = "BATTERY — stores surplus power and releases it when the grid runs short (a demand spike or a generator running dry). Wire it in like a pole (up to 4 wires). Charges when generation > demand, discharges when it's less.";
            // Endgame: the Monument (Industrial age). A long resource sink you pour the
            // top of every production chain into — completing it (10 blocks) is the win.
            var monumentBldg = MakeWorkshop("Monument", monument, 1, 6.0f, 3, 12, new Color(0.88f, 0.84f, 0.62f),
                new List<ItemAmount> { new ItemAmount(engine, 1), new ItemAmount(jewelry, 1), new ItemAmount(bricks, 4) },
                new ItemAmount(bricks, 20), new ItemAmount(metal, 15), new ItemAmount(tools, 8)); monumentBldg.unlockAge = 4;
            monumentBldg.footprintW = 3; monumentBldg.footprintH = 3; // the endgame monument is the biggest structure

            // --- Textiles & pottery chains (comfort goods) ---
            // Pottery (Bronze): Clay -> Pottery. Reuses the clay you already mine.
            var potter = MakeWorkshop("Potter", pot, 1, 3.0f, 2, 12, new Color(0.72f, 0.50f, 0.40f),
                new List<ItemAmount> { new ItemAmount(clay, 2) },
                new ItemAmount(wood, 6), new ItemAmount(stone, 4)); potter.unlockAge = 2; potter.requiresPower = true;
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
            sawmill.description = "Wood → Planks. Runs automatically once fed. A Sawmill wants ~40 Wood/min — MORE than one slow Wooden Belt carries (30/min), so it can't run flat-out on wooden belts. Research the Conveyor (60/min) and overlay it to feed the Sawmill fully, or run a 2nd Wood line. Scaling planks = scaling belts + Sawmills.";
            campfire.description = "Food + Wood + Water → Cooked Food. Runs automatically once its inputs are delivered.";
            charcoalBurner.description = "Wood → Charcoal. Charcoal feeds the Kiln AND both Smelters (Basic & Advanced) — scaling one can starve the others. A key shared-bottleneck.";
            kiln.description = "Clay + Charcoal → Bricks. Charcoal is shared with the Smelters, so watch that bottleneck. Bricks build advanced structures.";
            toolmaker.description = "Iron + Planks → Tools. Tools feed Blueprints (Industrial-age research) and the Monument.";
            mine.description = "Build ON a distant Iron Ore vein (the far Hills). Iron Ore is FINITE — veins deplete and vanish, so keep exploring outward and hauling it home.";
            gemMine.description = "Build ON a far Gem Deposit (the rarest, finite resource). Gems → Jewelry. Reaching them rewards exploration + good transport.";
            jeweler.description = "Gems → Jewelry, a high-value luxury good. Pairs with long-haul routes to bring distant gems home.";
            monumentBldg.description = "ENDGAME: pour Metal + Tools + Bricks + Planks in to produce Monument Blocks. Make 10 to WIN. A massive, sustained resource sink.";
            mason.description = "Stone → Stone Blocks (Stone's own processing chain). Stone Blocks build the sturdy Stone House.";
            warehouse.description = "Warehouse — stores ONE resource of your choice (Stone, Ore, Planks, anything). It adopts the first item a belt delivers, or pick it in the panel. This one building replaces a separate store per resource.";

            // --- RESEARCH SYSTEM: the progression spine. Each age is unlocked by crafting that
            //     tier's RESEARCH ITEM (a multi-input factory product) and delivering it to a
            //     Research Lodge, which converts items → research points. Gathering earns nothing;
            //     you must build (and scale) production chains. Maker workshops are age-gated so the
            //     NEXT tier's item is craftable only once you reach its age (no circular locks). ---
            var researchLodge = MakeResearch("Research Lodge", 0, new Color(0.60f, 0.48f, 0.34f),
                new ItemAmount(wood, 10), new ItemAmount(stone, 8));
            researchLodge.description = "Delivers research: belt (or place beside) the current RESEARCH ITEM here and it converts each into research points. Reaching the point cost advances the Age. No workers — the limit is how fast your factory makes research items. Open the Build panel (top) to see the current target + progress.";
            var ideaBench = MakeWorkshop("Idea Bench", ideaTablet, 1, 2.0f, 2, 12, new Color(0.80f, 0.74f, 0.46f),
                new List<ItemAmount> { new ItemAmount(planks, 1), new ItemAmount(stone, 1) },
                new ItemAmount(wood, 6), new ItemAmount(stone, 4)); // age 0 — first research chain
            ideaBench.description = "Planks + Stone → Idea Tablet (a RESEARCH item). The first research chain: feed it from a Sawmill + Stone Pit, then belt the Tablets to a Research Lodge to reach the Tribal Age.";
            var scrollMaker = MakeWorkshop("Scroll Maker", studyScroll, 1, 2.0f, 2, 12, new Color(0.84f, 0.78f, 0.50f),
                new List<ItemAmount> { new ItemAmount(copper, 1), new ItemAmount(planks, 1) },
                new ItemAmount(wood, 8), new ItemAmount(stone, 6)); scrollMaker.unlockAge = 1;
            scrollMaker.description = "Copper + Planks → Study Scroll (a RESEARCH item) to research the Bronze Age. Copper means you must first build a Copper Mine + a Basic Smelter (set to Copper) — the Bronze Age demands a real new chain.";
            var draftingTable = MakeWorkshop("Drafting Table", schematic, 1, 2.5f, 2, 12, new Color(0.52f, 0.66f, 0.82f),
                new List<ItemAmount> { new ItemAmount(bronzePlate, 1), new ItemAmount(pot, 1) },
                new ItemAmount(planks, 6), new ItemAmount(bricks, 4)); draftingTable.unlockAge = 2;
            draftingTable.description = "Bronze Plate + Pottery → Schematic (a RESEARCH item) to research the Iron Age. Needs an Advanced Smelter set to Bronze (Copper + Bricks) feeding it — a deeper multi-stage chain.";
            var engineeringLab = MakeWorkshop("Engineering Lab", machinePart, 1, 4.0f, 2, 12, new Color(0.40f, 0.56f, 0.82f),
                new List<ItemAmount> { new ItemAmount(steelBeam, 1), new ItemAmount(bronzeGear, 1), new ItemAmount(tools, 1) },
                new ItemAmount(planks, 8), new ItemAmount(metal, 4)); engineeringLab.unlockAge = 3;
            // The Engineering Lab is the ASSEMBLY building — it combines the components into multi-input
            // assemblies, the deepest stage. One building, several recipes (click to switch): build a few.
            engineeringLab.recipes = new List<Recipe>
            {
                new Recipe(machinePart, 1, 4.0f, 3, new ItemAmount(steelBeam, 1), new ItemAmount(bronzeGear, 1), new ItemAmount(tools, 1)), // Iron — 3-input assembly
                new Recipe(blueprint,   1, 3.5f, 3, new ItemAmount(steel, 1),     new ItemAmount(machinePart, 1)),                            // Iron — the RESEARCH item, now deeper
                new Recipe(engine,      1, 5.0f, 4, new ItemAmount(machinePart, 1), new ItemAmount(steel, 1), new ItemAmount(fuel, 1)),       // Industrial final product (needs Fuel)
            };
            engineeringLab.description = "ENGINEERING LAB — ASSEMBLES components into the deepest products; pick its recipe (click to switch): Steel Beam + Bronze Gear + Tools → Machine Part; Steel + Machine Part → Blueprint (the Industrial RESEARCH item); or Machine Part + Steel + Fuel → Engine (the final product). Build several (one per recipe). Needs POWER.";
            engineeringLab.requiresPower = true; engineeringLab.powerDraw = 20; // Industrial machines draw power → a real use for the Oil chain's big generation

            // Tiers = which research item the Lodge consumes at each age + its point value (later
            // items are worth more because their chains are deeper).
            Research.Reset();
            Research.Tiers = new List<Research.Tier>
            {
                new Research.Tier { targetAge = 1, item = ideaTablet,  pointsPerItem = 1 },   // craft at Stone
                new Research.Tier { targetAge = 2, item = studyScroll, pointsPerItem = 5 },   // craft at Tribal (deeper chain → worth more)
                new Research.Tier { targetAge = 3, item = schematic,   pointsPerItem = 10 },  // craft at Bronze
                new Research.Tier { targetAge = 4, item = blueprint,   pointsPerItem = 20 },  // craft at Iron (now needs a Machine Part)
            };
            // The spendable research TREE (press T to open). Age spine (each needs the prior) + a few
            // building-unlock branches you CHOOSE to spend points on. Age costs RETUNED to a smoother curve
            // (#47-balance): 12 → 150 → 400 → 800 — roughly ~12 / 30 / 40 / 40 item deliveries per age, not 300+.
            Research.Tree = new List<Research.Tech>
            {
                new Research.Tech { id = "tribal",     name = "Tribal Age",     cost = 12,  advanceToAge = 1, prereq = null,     desc = "Advance to the Tribal Age — Charcoal & Clay open up deeper production." },
                new Research.Tech { id = "bronze",     name = "Bronze Age",     cost = 150,  advanceToAge = 2, prereq = "tribal", requiredBuildings = new List<BuildingDefinition>{ basicSmelter },    gateItem = studyScroll, gateItemCount = 8, desc = "Advance to the Bronze Age — but first BUILD a Basic Smelter (set it to Copper) and deliver Study Scrolls (Copper + Planks)." },
                new Research.Tech { id = "iron",       name = "Iron Age",       cost = 400,  advanceToAge = 3, prereq = "bronze", requiredBuildings = new List<BuildingDefinition>{ advancedSmelter }, gateItem = schematic,   gateItemCount = 6, desc = "Advance to the Iron Age — but first BUILD an Advanced Smelter (set it to Bronze) and deliver Schematics (Bronze Plate + Pottery)." },
                new Research.Tech { id = "industrial", name = "Industrial Age", cost = 800,  advanceToAge = 4, prereq = "iron",   requiredBuildings = new List<BuildingDefinition>{ advancedSmelter }, gateItem = blueprint,   gateItemCount = 5, desc = "Advance to the Industrial Age — but first deliver Blueprints (Steel + Machine Part) — the deepest chain." },
                new Research.Tech { id = "splitters",   name = "Splitters",     cost = 15,  prereq = "tribal", unlocks = new List<BuildingDefinition>{ splitter },   desc = "Unlocks the 1→3 Splitter — feed three machines from one supply line." },
                // The belt-upgrade ladder: a cheap EARLY first rung (the wooden belt is deliberately
                // slow), then deeper tiers gated by age so faster belts pace your factory's growth.
                new Research.Tech { id = "conveyors",   name = "Conveyor Belts", cost = 4,  prereq = null,     unlocks = new List<BuildingDefinition>{ fastBelt },   desc = "Your first belt upgrade: the Conveyor (60/min — keeps up with a collector). Cheap on purpose so you can grab it early without sacrificing the Tribal-age unlock. Overlay it on wooden belts to upgrade in place." },
                new Research.Tech { id = "geared_belts", name = "Geared Belts",  cost = 40,  prereq = "bronze", unlocks = new List<BuildingDefinition>{ gearedBelt }, desc = "The Geared Belt (120/min — 2× a Conveyor) for high-throughput lines." },
                new Research.Tech { id = "steel_belts",  name = "Steel Belts",   cost = 80,  prereq = "iron",   unlocks = new List<BuildingDefinition>{ steelBelt },  desc = "The Steel Belt (240/min — the fastest tier) for the densest late-game lines." },
                new Research.Tech { id = "smart_logistics", name = "Smart Logistics", cost = 30, prereq = "bronze", unlocks = new List<BuildingDefinition>{ undergroundBelt, filterBelt, prioritySplitter, conditionalGate, elevatedRail }, desc = "Mid-game logistics tools: the UNDERGROUND BELT + ELEVATED TRACK (cross belts and rail over/under each other), FILTER BELT (sort a mixed line by item), PRIORITY SPLITTER (overflow routing), and GATE BELT (stop over-filling a buffer)." },
            };
            // Gate those buildings behind their Tech (and off the age gate, so the Tech IS the gate).
            splitter.requiredTech = "splitters";
            fastBelt.requiredTech = "conveyors"; fastBelt.unlockAge = 0;
            gearedBelt.requiredTech = "geared_belts"; gearedBelt.unlockAge = 0;
            steelBelt.requiredTech = "steel_belts"; steelBelt.unlockAge = 0;

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
            // Size MUST cover the terrain extent (>= 2*Half + slack) — kept in sync with Generate(280) below
            // and FogOfWar.worldSize (580). Miss one and the world edge shows the void clear-colour / clips the map.
            MakeSprite("Ground", Color.white, Vector2.zero, 580f, -100, PlaceholderArt.Ground(new Color(0.30f, 0.37f, 0.21f)));
            // World as a system: biome map with a clear starting basin. Water + mountain block building, so
            // geography forces routing/expansion decisions. Half=280 → a big ~560-wide continent. Rendered after spawns.
            TerrainGrid.Generate(280, Random.value * 1000f, 22f); // big world (~560 across); open start basin

            // --- Player ---
            var player = MakeSprite("Player", Color.white, Vector2.zero, 0.85f, 10, PlaceholderArt.Caveman(0));
            player.AddComponent<PlayerController>();
            player.AddComponent<PlayerAvatar>(); // the caveman re-skins as ages advance
            var gatherer = player.AddComponent<PlayerGatherer>();
            var builder = player.AddComponent<BuildController>();
            builder.gatherer = gatherer;
            builder.placeNodeRange = 6f;

            // --- LIQUIDS chain: an Oil Well pumps Oil into PIPES → an Oil Tank / a Refinery (Oil + Water →
            //     Fuel) → Fuel belts → an Oil Generator (lots of Power). The Water Pump + Booster + Pipe
            //     (defined earlier) revive the water side; the Refinery needs Water too, so both matter. ---
            var oilWell = ScriptableObject.CreateInstance<BuildingDefinition>();
            oilWell.displayName = "Oil Well"; oilWell.kind = BuildingKind.Pump; oilWell.item = oil; oilWell.unlockAge = 2;
            oilWell.footprintW = 2; oilWell.footprintH = 2;
            oilWell.color = new Color(0.26f, 0.22f, 0.28f);
            oilWell.cost = new List<ItemAmount> { new ItemAmount(planks, 6), new ItemAmount(metal, 4) };
            oilWell.description = "OIL WELL — place ON/next to an Oil deposit; pumps Oil into connected PIPES (on to Oil Tanks / a Refinery). Oil is FINITE — wells run dry, so keep exploring.";
            var oilTank = MakeStorage("Oil Tank", oil, 200, new Color(0.30f, 0.28f, 0.34f), new ItemAmount(metal, 6), new ItemAmount(planks, 4));
            oilTank.unlockAge = 2;
            oilTank.description = "OIL TANK — liquid storage for Oil. Run a PIPE from the Oil Well to it (belts can't carry liquids).";
            var refinery = MakeWorkshop("Refinery", fuel, 1, 3.0f, 2, 14, new Color(0.42f, 0.37f, 0.30f),
                new List<ItemAmount> { new ItemAmount(oil, 2), new ItemAmount(water, 1) },
                new ItemAmount(metal, 8), new ItemAmount(bricks, 6)); refinery.unlockAge = 2;
            refinery.requiresPower = false;
            refinery.description = "REFINERY — Oil + Water → Fuel. Both inputs arrive by PIPE (run pipes from an Oil Well AND a Water Pump up to it); the Fuel output leaves on a BELT. Feeds the Oil Generator.";
            var oilGen = MakePower("Oil Generator", 120, fuel, 1, 2.5f, 3, new Color(0.30f, 0.26f, 0.22f),
                new ItemAmount(metal, 12), new ItemAmount(bricks, 8));
            oilGen.description = "OIL GENERATOR — burns FUEL (refined from Oil) for LOTS of Power, far more than a Coal Generator. Belt Fuel into its cyan intake — the payoff of the whole oil chain.";

            // --- The GARAGE: parks your bought MOUNTS. Build one, then BUY the age-gated mount from its
            //     panel and pick which to ride. HYBRID travel: age gives a baseline speed bump on foot;
            //     the mount adds the full tier speed + the look. "Limited" = 2 parking slots per garage. ---
            var garage = ScriptableObject.CreateInstance<BuildingDefinition>();
            garage.displayName = "Garage"; garage.kind = BuildingKind.Garage; garage.unlockAge = 1;
            garage.footprintW = 2; garage.footprintH = 2;
            garage.capacity = 2;                 // parking slots (the "limited" garage)
            garage.menuCategory = "Mounts";
            garage.color = new Color(0.60f, 0.50f, 0.38f);
            garage.cost = new List<ItemAmount> { new ItemAmount(planks, 6), new ItemAmount(stone, 4) };
            garage.description = "GARAGE — parks your travel MOUNTS. Build it, then BUY the age-gated mount (Horseback → Ox Cart → Wagon → Motorbike) from its panel and pick which to ride. Holds 2 mounts; build another Garage for more slots. On foot you always get a small per-age speed boost; the mount adds the full speed + the look.";

            // Mount purchase costs (tier 1..4). Set here where the ItemDefinitions live. NOT run through the
            // building CostScale below (that only touches buildables' placement cost), so these stay as-is.
            PlayerController.MountCost = new List<ItemAmount>[]
            {
                null, // 0 = On Foot (free)
                new List<ItemAmount> { new ItemAmount(planks, 8),  new ItemAmount(food, 6) },   // Horseback
                new List<ItemAmount> { new ItemAmount(planks, 8),  new ItemAmount(bricks, 6) }, // Ox Cart
                new List<ItemAmount> { new ItemAmount(planks, 10), new ItemAmount(metal, 6) },  // Wagon
                new List<ItemAmount> { new ItemAmount(steel, 8),   new ItemAmount(fuel, 6) },   // Motorbike
            };
            // FACTORY-FIRST build menu: gathering → processing → research → logistics → storage →
            // power/endgame. Survival/comfort buildings (forager, water hole, granary, campfire,
            // farm/mill/bakery, hunter, housing, pipes, textiles, jewelry, masonry) are intentionally
            // NOT offered — the game is now a pure production/automation loop.
            builder.buildables = new List<BuildingDefinition>
            { // Gather
              woodHut, stonePit, clayPit, copperMine, mine,
              // Process — incl. the deeper metal chain (copper → bronze → steel) that gates each age + the oil Refinery
              sawmill, charcoalBurner, kiln, potter, basicSmelter, advancedSmelter, toolmaker, refinery,
              // Research
              ideaBench, scrollMaker, draftingTable, engineeringLab, researchLodge,
              // Logistics — belt tier ladder (wooden→conveyor→geared→steel) + junctions + smart-logistics tools + transport
              woodBelt, fastBelt, gearedBelt, steelBelt, splitter, merger,
              undergroundBelt, filterBelt, prioritySplitter, conditionalGate, depot, rail, elevatedRail, signal, twoWaySignal, bridge, harbour,
              // Liquids — pipes carry oil/water (never belts); Oil Well pumps oil, Water Pump pumps water, Booster relays pressure
              pipe, pump, booster, oilWell,
              // Storage — open-air piles for raw materials (Woodpile / Stone + Ore Stockpiles / Clay / Brick),
              // the big configurable Warehouse hub, and liquid tanks (Oil Tank / Water Barrel)
              woodStore, stoneStore, oreStore, clayStore, brickStore, warehouse, oilTank, waterStore,
              // Infrastructure / power / endgame — Wood/Coal/Oil Generators + Power Poles + Battery
              woodGen, pole, battery, generator, oilGen, monumentBldg,
              // Mounts — the Garage (buy/park your rides)
              garage };

            // --- Manual paid AGE-UPGRADES (stone tools → metal → machines): each production building can be
            //     upgraded from its panel once you reach the tier's age, spending resources for a faster rate
            //     + a look change. Collectors get a "tools" ladder; workshops an "automation" ladder. ---
            void AddUpgrades(BuildingDefinition d, string n1, string n2, string n3)
            {
                d.upgrades = new List<UpgradeTier>
                {
                    new UpgradeTier(n1, 2, 1.5f, new Color(0.80f, 0.55f, 0.35f), new ItemAmount(planks, 6), new ItemAmount(copper, 3)),
                    new UpgradeTier(n2, 3, 2.2f, new Color(0.70f, 0.74f, 0.80f), new ItemAmount(metal, 5),  new ItemAmount(planks, 6)),
                    new UpgradeTier(n3, 4, 3.5f, new Color(0.55f, 0.80f, 0.95f), new ItemAmount(steel, 4),  new ItemAmount(tools, 3)),
                };
            }
            foreach (var c in new[] { woodHut, stonePit, clayPit, copperMine, mine })
                AddUpgrades(c, "Bronze Tools", "Iron Tools", "Powered Machine");
            foreach (var wbDef in new[] { sawmill, charcoalBurner, kiln, potter, basicSmelter, advancedSmelter, toolmaker })
                AddUpgrades(wbDef, "Geared Parts", "Reinforced Frame", "Automation");

            // Central sprite-name table: pre-fill the building/belt/resource maps with expected names
            // (filename = sanitised type name) so a future pack drops straight in via SpriteDatabase.
            SpriteDatabase.Seed(builder.buildables, new[] { wood, stone, clay, copperOre, ore });
            // Then overlay the imported Kenney Roguelike pack on the entities it can dress (machines,
            // resource nodes, storage, research, a few item icons). Buildings + nodes resolve their
            // sprite at spawn (all after this), so they pick it up automatically; the item icons below
            // were already resolved at creation, so re-resolve the skinned ones.
            SpriteDatabase.ApplyRoguelikeSkin();
            foreach (var it in new[] { copper, metal, steel, bronzePlate, studyScroll, schematic, blueprint })
                it.icon = SpriteDatabase.ForItem(it);
            // BUILD-COST SCALE: bump every building's build cost so placing things is a real resource
            // decision (more reason to plan + collect, not spam). One knob — tune CostScale. (Recipe
            // INPUT costs are untouched; this is the one-time placement cost only.)
            const float CostScale = 2.5f;
            foreach (var bd in builder.buildables)
                if (bd != null && bd.cost != null)
                    foreach (var c in bd.cost)
                        if (c != null) c.amount = Mathf.Max(1, Mathf.CeilToInt(c.amount * CostScale));

            // Transport vehicles are NOT in the build menu — they're created from a Station's panel.
            builder.routeTiers = new List<BuildingDefinition> { caravan, oxCart, wagonTrain, cargoDrone };
            builder.shipTiers = new List<BuildingDefinition> { cargoShip, steamShip }; // harbour (boat) lines

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;

            // --- Fog of war (explore to reveal the map) ---
            var fog = new GameObject("FogOfWar").AddComponent<FogOfWar>();
            fog.target = player.transform; // size/res come from FogOfWar defaults (set for the big world)

            // --- Belt simulation (central, deterministic, fixed-timestep — drives ALL belts) ---
            BeltSim.Ensure();

            // --- Colony (now just age + automation-score holder; no survival loop) ---
            var colony = new GameObject("Colony").AddComponent<Colony>();
            colony.carried = gatherer.Inventory;
            // FACTORY-FIRST: no food/water consumption, so no larder is needed. A small starter
            // kit of Wood + Stone lets you place your first hut/storage without a long gather grind.
            gatherer.Inventory.Add(wood, 32);  // starter kit scaled with the higher build costs (CostScale)
            gatherer.Inventory.Add(stone, 26);
            // Age advancement is driven entirely by RESEARCH (craft research items → deliver to a
            // Research Lodge → spend points). No comfort/happiness demand sink any more.

            // (No Town Hall / HQ / builders — factory-first: construction is instant, buildings auto-run.)

            // --- HUD ---
            var hud = new GameObject("HUD").AddComponent<InventoryHud>();
            hud.gatherer = gatherer;
            hud.builder = builder;
            hud.woodItem = wood;
            hud.stoneItem = stone;
            hud.clayItem = clay;
            hud.oreItem = ore;
            hud.monumentItem = monument;
            // Factory-relevant items only (shown in the Guide reference + the "Goods" chip + F1 dump).
            hud.debugItems = new List<ItemDefinition>
            { wood, stone, planks, charcoal, clay, bricks, pot, copperOre, copper, bronzePlate, ore, metal, steel, tools, oil, fuel, monument };

            // --- Guided objectives ladder (the "what next / why advance" hook) ---
            var carriedInv = gatherer.Inventory;
            int Have(ItemDefinition i) => Economy.Available(i, carriedInv);
            bool HasCollectorOf(ItemDefinition i) { foreach (var p in ProductionBuilding.All) if (p != null && p.produces == i) return true; return false; }
            bool HasWorkshopOf(ItemDefinition i) { foreach (var w in WorkshopBuilding.All) if (w != null && w.output == i) return true; return false; }
            bool HasStorageOf(ItemDefinition i) { foreach (var s in StorageBuilding.All) if (s != null && s.accepts == i) return true; return false; }
            int AgeNow() => Colony.Instance != null ? Colony.Instance.Age : 0;
            var objectives = new GameObject("Objectives").AddComponent<Objectives>();
            // FACTORY-FIRST objective ladder: gather → store → process → component → research →
            // automate → advance ages → Monument. No survival/population/comfort goals.
            objectives.quests = new List<Quest>
            {
                new Quest { title = "Gather 12 Wood by hand",                         done = () => Have(wood) >= 12,        reward = () => carriedInv.Add(stone, 8),   rewardText = "+8 Stone" },
                new Quest { title = "Build a Wood Hut, then a Woodpile to store it",   done = () => HasCollectorOf(wood) && HasStorageOf(wood), reward = () => carriedInv.Add(wood, 15), rewardText = "+15 Wood" },
                new Quest { title = "Build a Stone Pit (gather Stone)",               done = () => HasCollectorOf(stone),   reward = () => carriedInv.Add(stone, 15),  rewardText = "+15 Stone" },
                new Quest { title = "Build a Sawmill — process Wood into 15 Planks",   done = () => Have(planks) >= 15,      reward = () => carriedInv.Add(planks, 10), rewardText = "+10 Planks" },
                new Quest { title = "Build an Idea Bench + Research Lodge (side by side)", done = () => HasWorkshopOf(ideaTablet) && ResearchBuilding.All.Count > 0, reward = () => carriedInv.Add(stone, 12), rewardText = "+12 Stone" },
                new Quest { title = "Deliver your first Idea Tablet to research",     done = () => Research.TotalDelivered >= 1, reward = () => carriedInv.Add(planks, 8), rewardText = "+8 Planks" },
                new Quest { title = "Automate it: lay 5 belts to feed a machine",     done = () => Belt.Count >= 5,         reward = () => carriedInv.Add(wood, 20),   rewardText = "+20 Wood" },
                new Quest { title = "Research the Tribal Age",                        done = () => AgeNow() >= 1,           reward = () => carriedInv.Add(stone, 25),  rewardText = "+25 Stone" },
                new Quest { title = "Make 10 Charcoal (build a Charcoal Burner)",     done = () => Have(charcoal) >= 10,    reward = () => carriedInv.Add(planks, 10), rewardText = "+10 Planks" },
                new Quest { title = "Build the Copper chain: a Copper Mine + a Basic Smelter (set to Copper)", done = () => HasWorkshopOf(copper), reward = () => carriedInv.Add(copper, 8), rewardText = "+8 Copper" },
                new Quest { title = "Research the Bronze Age",                        done = () => AgeNow() >= 2,           reward = () => carriedInv.Add(stone, 30),  rewardText = "+30 Stone" },
                new Quest { title = "Fire 15 Bricks (Clay + Charcoal → Kiln)",        done = () => Have(bricks) >= 15,      reward = () => carriedInv.Add(clay, 20),   rewardText = "+20 Clay" },
                new Quest { title = "Build 2 Stations & add a transport route",       done = () => RouteVehicle.All.Count >= 1, reward = () => carriedInv.Add(wood, 30), rewardText = "+30 Wood" },
                new Quest { title = "Research the Iron Age",                          done = () => AgeNow() >= 3,           reward = () => carriedInv.Add(planks, 40), rewardText = "+40 Planks" },
                new Quest { title = "Explore far & mine 25 Iron Ore",                 done = () => Have(ore) >= 25,         reward = () => carriedInv.Add(stone, 40),  rewardText = "+40 Stone" },
                new Quest { title = "Smelt 20 Iron (Basic Smelter set to Iron: Iron Ore + Charcoal)",  done = () => Have(metal) >= 20,       reward = () => carriedInv.Add(planks, 20), rewardText = "+20 Planks" },
                new Quest { title = "Craft 10 Tools (Iron + Planks → Toolmaker)",     done = () => Have(tools) >= 10,       reward = () => carriedInv.Add(planks, 30), rewardText = "+30 Planks" },
                new Quest { title = "Research the Industrial Age",                    done = () => AgeNow() >= 4,           reward = () => carriedInv.Add(planks, 50), rewardText = "+50 Planks" },
                new Quest { title = "Power up: build a Generator + connect a smelter",  done = () => PowerPlant.All.Count >= 1, reward = () => carriedInv.Add(metal, 10), rewardText = "+10 Metal" },
                new Quest { title = "Reach 600 Industry (automation score)",          done = () => Colony.Instance != null && Colony.Instance.PeakProsperity >= 600, reward = () => carriedInv.Add(tools, 10), rewardText = "+10 Tools" },
                new Quest { title = "Begin your legacy: build the Monument",          done = () => HasWorkshopOf(monument), reward = () => carriedInv.Add(planks, 40), rewardText = "+40 Planks" },
                new Quest { title = "🏆 Complete the Monument — 10 Blocks. YOU WIN!", done = () => Have(monument) >= 10, isWin = true },
            };

            // --- Random world events (variety / surprise hook) ---
            var events = new GameObject("WorldEvents").AddComponent<WorldEvents>();
            events.carried = gatherer.Inventory;
            events.wood = wood;
            events.stone = stone;

            // --- Resource patches: natural CLUSTERS (groves & outcrops), never lone nodes, so
            //     resources read as PART OF THE WORLD. Starter clusters are SMALL (bootstrap only);
            //     biome clusters are LARGER & DENSER — the visible upgrade that rewards expansion.
            //     A clear central area stays open as the player's base/processing yard. ---
            const float baseClear = 11f;
            // STARTER BASIN — small Wood + Stone clusters: enough to bootstrap your first factory,
            // NOT to scale on (you outgrow them and must push out to the biome regions). Centres are
            // off-mirror (not a perfect E/W reflection) so the opening reads natural, not formulaic.
            SpawnClusters("Tree", wood, Color.white, PlaceholderArt.Tree(),
                new Vector2(25f, 3f), 13f, 3, 3, 5, 2.2f, new Vector2(1.0f, 1.5f), 30, 1, baseClear);
            SpawnClusters("Rock", stone, Color.white, PlaceholderArt.Rock(),
                new Vector2(-23f, 8f), 12f, 3, 3, 5, 2.2f, new Vector2(1.0f, 1.5f), 30, 1, baseClear);

            // --- RESOURCE ZONES (logistics-first redesign): FEW, LARGE, DISTINCT, SINGLE-resource
            //     regions — each on its own corridor + biome, pushed FAR from spawn. One resource per
            //     zone at a different angle/distance means a multi-input recipe (e.g. Steel = Iron +
            //     fuel) can NEVER be co-located with all its inputs → you must haul each raw to a
            //     CENTRAL processing hub. (Replaces the old 3-corridor + map-wide biome-scatter, which
            //     smeared every resource everywhere so a micro-base could self-supply anywhere.)
            //     Data-driven: one Zone() call per row; SpawnNode clears water per node so nothing
            //     strands. Coal (a mined fuel) is added with the hearth that burns it in a later phase. ---
            const int ZoneCount = 5;
            TerrainGrid.CarveCorridors(ZoneCount, 190f, 1);
            void Zone(int k, float dist, Terrain biome, string name, ItemDefinition item, Color color, Sprite sprite,
                      float discR, int clusters, int minN, int maxN, float clusterR, Vector2 size, int cap, int regen)
            {
                float a = TerrainGrid.CorridorAngle(k, ZoneCount);
                Vector2 center;
                // BIOME-GATED: prefer the NATURAL home biome found along this corridor (you explore out and
                // DISCOVER, say, the forest there) — only fall back to PAINTING a biome region if the ray
                // doesn't cross a real patch of it. Either way the zone sits on the (cleared) corridor → reachable.
                if (TerrainGrid.FindBiomeAlong(a, biome, dist * 0.7f, dist * 1.35f, out var found))
                    center = new Vector2(found.x, found.y);
                else
                {
                    float d = dist + (TerrainGrid.Hash01(k + 7) - 0.5f) * 20f;
                    center = new Vector2(Mathf.Cos(a) * d, Mathf.Sin(a) * d);
                    TerrainGrid.PaintBlob(new Vector3(center.x, center.y, 0f), discR, biome, 4, 0.45f);
                }
                float dd = center.magnitude;
                float distScale = 0.7f + dd / 180f;                           // far zones are genuinely RICHER (rewards expansion)
                int capD = Mathf.RoundToInt(cap * distScale);
                int clustersD = Mathf.RoundToInt(clusters * Mathf.Lerp(1f, 1.4f, Mathf.Clamp01(dd / 160f)));
                // Spread clusters across the disc (~0.6×radius) so the zone reads as ONE rich field with a dense core.
                SpawnClusters(name, item, color, sprite, center, discR * 0.6f, clustersD, minN, maxN, clusterR, size, capD, regen, 0f);
            }
            //   k  dist  biome           name             item       colour                         sprite                      discR clusters minN maxN clustR  size                     cap regen
            Zone(0, 64f, Terrain.Plains, "Clay Pit",       clay,      Color.white, PlaceholderArt.ClayMound(),  30f, 5, 5, 8, 3.0f, new Vector2(1.0f,1.5f),  60, 1); // nearest — Bronze chain start
            Zone(1, 84f, Terrain.Hills,  "Copper Deposit", copperOre, Color.white, PlaceholderArt.OreCopper(),  30f, 5, 4, 7, 3.0f, new Vector2(1.0f,1.6f), 180, 0); // finite — Bronze metal
            Zone(2,100f, Terrain.Forest, "Forest",         wood,      Color.white, PlaceholderArt.Tree(), 34f, 6, 6, 9, 3.4f, new Vector2(1.0f,1.6f),  40, 1); // lumber at scale
            Zone(3,120f, Terrain.Hills,  "Stone Outcrop",  stone,     Color.white, PlaceholderArt.Rock(),  32f, 6, 5, 8, 3.2f, new Vector2(1.0f,1.6f),  50, 1); // stone at scale
            Zone(4,140f, Terrain.Hills,  "Iron Ore Field", ore,       Color.white, PlaceholderArt.OreIron(),  32f, 5, 4, 7, 3.0f, new Vector2(1.1f,1.6f), 220, 0); // finite — Iron age

            // Oil field — a Bronze-age LIQUID resource on the plains, out along the clay corridor. Finite.
            {
                float oa = TerrainGrid.CorridorAngle(0, ZoneCount);
                var oc = new Vector2(Mathf.Cos(oa) * 80f, Mathf.Sin(oa) * 80f);
                TerrainGrid.Paint(new Vector3(oc.x, oc.y, 0f), 14f, Terrain.Plains);
                SpawnClusters("Oil Field", oil, Color.white, PlaceholderArt.OilPatch(), oc, 8f, 4, 4, 6, 3.0f, new Vector2(1.2f, 1.8f), 200, 0, 0f);
            }

            // --- BOAT-ONLY ISLAND: a generous resource across the sea, reachable only with a Harbour + Cargo
            //     Ship. A wide ring of water isolates it from the mainland (off the carved land corridors),
            //     so the only way to bring the goods home is by boat. ---
            {
                float ia = TerrainGrid.CorridorAngle(2, ZoneCount) + 0.9f; // between the carved land corridors
                var ic = new Vector2(Mathf.Cos(ia) * 168f, Mathf.Sin(ia) * 168f);
                TerrainGrid.CarveWater(new Vector3(ic.x, ic.y, 0f), 34f); // a wide sea around the island
                // Irregular, hand-shaped coastline: several overlapping land blobs instead of one perfect disc
                // (so the island reads as a designed landmass, not a procedural circle).
                Vector2[] blobs = { new Vector2(0f, 0f), new Vector2(7f, 3f), new Vector2(-6f, 5f), new Vector2(4f, -7f), new Vector2(-5f, -5f) };
                float[] radii = { 9f, 6f, 6.5f, 5.5f, 5f };
                for (int b = 0; b < blobs.Length; b++)
                    TerrainGrid.Paint(new Vector3(ic.x + blobs[b].x, ic.y + blobs[b].y, 0f), radii[b], Terrain.Hills);
                SpawnClusters("Stone Outcrop", stone, Color.white, PlaceholderArt.Rock(),
                    ic, 7f, 4, 5, 8, 3.0f, new Vector2(1.0f, 1.6f), 150, 1, 0f);
            }

            // --- Welcome / starter guidance (fades after a few seconds) ---
            // Keep the opening minimal (Factorio focus): just WELCOME → SURVIVAL → one compact
            // controls/goal line. Adjacency, research and the rest are taught contextually (the
            // Objectives ladder, the one-time tips, the age card, and the Guide on G).
            Toast.Show("<color=#ffd24d>Welcome, chief!</color>  Click trees & rocks to gather Wood and Stone by hand.");
            Toast.Show("<size=14><color=#9f9>Build your first factory:</color> a <b>Wood Hut</b> + <b>Woodpile</b>, then a <b>Sawmill</b> to turn Wood into Planks. Machines run on their own — feed them by placing them side by side, or with belts.</size>");
            Toast.Show("<size=13><b>B</b> build · <b>T</b> research · <b>G</b> guide · <b>H</b> help.  Craft research items → deliver to a <b>Research Lodge</b> → advance the Age.  Goal: build the Monument.</size>");

            // (Removed: the old map-wide SpawnClustersInBiome smear — it scattered every resource
            //  across all biome cells, which let a micro-base self-supply anywhere and defeated the
            //  centralisation goal. Resources now live ONLY in the concentrated zones carved above.)

            // Bake the biome map into its visual now that resource cells have been cleared.
            TerrainGrid.SpawnRenderer();

            // --- World "juice": make it feel warm, filled-in and alive ---
            new GameObject("Sway").AddComponent<SwayAnimator>();   // must exist before scatter registers soft props
            DecorScatter.Populate();                                // grass/bushes/flowers/rocks/reeds across the biomes
            var clouds = new GameObject("CloudShadows").AddComponent<CloudShadows>();
            clouds.target = player.transform;                       // soft shadows drift over the ground
            var shimmer = new GameObject("WaterShimmer").AddComponent<WaterShimmer>();
            shimmer.target = player.transform;                      // glints twinkle on the water
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
                Vector2 c = areaCenter + FalloffOffset(areaSpread, 1.5f, out _); // clusters bunch toward the zone core
                if (c.magnitude < minClear) c = c.normalized * minClear;          // keep the base clear
                bool rich = Random.value < 0.25f;                                 // ~1 in 4 is a big, rich cluster (size variety)
                int n = Random.Range(minNodes, maxNodes + 1) * (rich ? 2 : 1);
                float cr = clusterRadius * Random.Range(0.7f, 1.5f) * (rich ? 1.5f : 1f);
                int capK = Mathf.RoundToInt(capacity * (rich ? 1.6f : 1f));
                SpawnCluster(name, item, color, sprite, c, n, cr, sizeRange, capK, regen);
            }
        }

        // Radial sample with density falling off toward the rim (falloff>1 pulls mass to the centre, so a
        // patch/zone reads as a dense core fading to a sparse edge). t01: 0 at the core, 1 at the rim.
        private static Vector2 FalloffOffset(float radius, float falloff, out float t01)
        {
            float r = radius * Mathf.Pow(Random.value, falloff);
            float a = Random.value * 6.2831853f;
            t01 = radius <= 0f ? 0f : r / radius;
            return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }

        // One cluster: `nodeCount` patches packed within `radius` of `center` (a grove / outcrop).
        private static void SpawnCluster(string name, ItemDefinition item, Color color, Sprite sprite,
            Vector2 center, int nodeCount, float radius, Vector2 sizeRange, int capacity, int regen)
        {
            for (int i = 0; i < nodeCount; i++)
            {
                Vector2 pos = center + FalloffOffset(radius, 2.0f, out float t); // nodes densest at the core, sparse at the rim
                float size = Mathf.Lerp(sizeRange.y, sizeRange.x, t);            // bigger nodes in the core
                int cap = Mathf.Max(1, Mathf.RoundToInt(capacity * Mathf.Lerp(1f, 0.40f, t))); // rich core, thin rim
                float b = Random.Range(0.9f, 1.1f); // slight per-node colour variation
                var c = new Color(Mathf.Clamp01(color.r * b), Mathf.Clamp01(color.g * b), Mathf.Clamp01(color.b * b));
                SpawnNode(name, item, c, pos, size, sprite, cap, regen);
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
            def.footprintW = 2; def.footprintH = 2; // buildings are 2×2 — clearly bigger than 1-cell belts
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
            def.footprintW = 2; def.footprintH = 2;
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
            def.footprintW = 2; def.footprintH = 2;
            def.powerDraw = 10; // only matters for requiresPower machines (Kiln/Potter/Smelters) from the Bronze age
            def.cost = new List<ItemAmount>(cost);
            return def;
        }

        // A power plant: burns `fuel` to supply `output` electrical power (electricity starts in the Bronze age).
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
            def.footprintW = 2; def.footprintH = 2;
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
            def.footprintW = 2; def.footprintH = 2;
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
