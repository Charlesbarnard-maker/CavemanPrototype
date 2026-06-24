# CavemanPrototype — Design & Direction

A 2D PC automation/factory game (Unity 6, URP) where you start as a caveman doing
everything by hand and evolve across historical ages into self-running, factory-like
systems. Inspirations: **Factorio** (production chains), **Satisfactory** (factory
scaling), **Spore** (evolution across stages).

## Direction shift (2026-06-24): system-driven, not goal-driven
The hook must come from **systems that create problems**, not authored objectives. Target
loop: build something that works → it strains under scale → a bottleneck appears → it
starves multiple downstream systems → you redesign/expand → the fix reveals a new
inefficiency → repeat. Feeling: *"I'll just fix this one thing" → 3 new problems.*

**Root cause being fixed:** the combined **global Economy pool** let workshops pull inputs
from anywhere instantly, making belts/layout/distance cosmetic and preventing cascades.
**Fix (in progress):** production consumption is going **local** — workshops consume only
belt-delivered (InBuffer) or **adjacent** storage/collector/machine inputs
(`Economy.LocalProduction`, default on, F7 to compare). Survival + build costs stay global.

**Pressure roadmap (priority):** ①local production ✅ → ②universal depletion (expansion via
exhaustion, *replacing* the artificial "need gems" gate) → ③throughput ceilings that bite →
④food spoilage (steady throughput, not stockpiles) → ⑤more shared-intermediate chokepoints.
All emergent, 2D-clear (adjacency + visible belts), reusing the red "starved" status dot.

## Ages = mechanical transformation (see AGES.md)
The headline differentiator: **each age changes the RULES, not just the contents** — the
dominant constraint and the unit of the puzzle transform (manual/adjacency → workforce/flow →
throughput+**power** → demand-pull **networks** → **goals/agents** → **information/bandwidth**
→ **entropy/stability**), while the core loop stays constant. Full ladder, transition shocks,
two original post-modern ages, and the pragmatic build order are in **`AGES.md`**. First
concrete task to deliver on it: **Power as a global constraint at the Industrial boundary**
(the smallest change that proves an age can transform play). The current game is mechanically
only Age 1–2; later ages exist as *content*, not yet *mechanic shifts*.

## Construction is a system (see CONSTRUCTION.md)
Building is "another production chain to optimise", not a delay: sites hold a material list,
builders haul it from storage then build (supply rate gates speed), builders spread into
**visible slots** around the site, and you scale construction by building **Construction
Yards** (+builder cap) + having spare population — never a slider. Full design, bottleneck
curve, age progression, and what's built vs planned are in **`CONSTRUCTION.md`**.

## Core pillar
> Players should evolve from **manual survival → to systems that play the game for them.**

Manual early game → partial automation mid → fully optimized systems late.
Emotional hook: *"I started with nothing and built a self-running civilization."*

## The design rule (test every mechanic against it)
> **Does this reduce manual work, or just make manual work faster?**

- Only speeds up manual work (e.g. "better axe → +20% wood") → ❌ reject. It keeps the
  player doing manual labour longer, delays automation, and turns the game into a
  generic survival grind.
- Removes manual work (a hut auto-produces, an NPC hauls for you) → ✅ keep.

> ⚠️ Mistake already made & corrected: an early build added a Stone Axe tool-upgrade.
> It was removed. Do **not** reintroduce tool tiers as "progression."

## Progression spine = logistics evolution
The progression axis is **better ways for systems to move resources without player
input** — NOT better tools. Each age changes the *shape* of the factory, not just stats.

1. **Manual** — player carries everything (manual gathering bootstraps the economy).
2. **Primitive logistics (Stone/Tribal):** drop piles + **NPC haulers** (human chains
   passing items hand-to-hand) + gravity (logs roll downhill, stone drops into trenches).
3. **Mechanical (first "real" automation):** ramps, rolling logs, pulleys, simple
   directional flow.
4. **True conveyors (Industrial):** belts, splitters, factory logic.

Do **not** introduce modern conveyor belts early — they evolve gravity → human →
mechanical → belts.

## Locked technical decisions
- **Transport model:** abstract throughput. Buildings have storage buffers; links/
  haulers move X items/sec. Items are NOT individually simulated. Belts/haulers are
  visual; the sim is logical. (Chosen for tractability + scale.)
- **Economy:** combined pool. Build costs (and workshop inputs) spend from the player's
  carried inventory + ALL building buffers/storage. Manual gathering fills carried;
  collectors fill their own buffer; **Transporters physically carry buffers → storage
  (no instant transfer)**. A full buffer with no transport stalls the building.
- **Art:** placeholder shapes (squares/circles) until systems are locked, then one art
  pass. Don't build art on top of changing mechanics.

## Current implementation state
Scene is built entirely in code by `GameBootstrap` (one component on one GameObject —
no Inspector wiring). Scripts in `Assets/Scripts/`:

- **Manual gathering:** click resource patches (`ResourceNode`) — finite, slowly
  regenerating, recoil/shake when chopped; player carries the result (`PlayerGatherer`).
  Patches are placeholder shapes (`PlaceholderArt`): trees=triangles, rocks=hexagons,
  bushes=circles.
- **Collectors** (`ProductionBuilding`): bind to a nearby patch and require ASSIGNED
  workers — each assigned worker is one `Worker` NPC that walks out, chops, and carries
  loads back to the building buffer, which is pushed into an adjacent matching
  **Storage** (`StorageBuilding`). Unstaffed/full → idle (backpressure).
- **Settlement** (`Colony`): population grows toward the housing cap while fed, declines
  (starvation) when food runs out. `HousingBuilding` (Town Hall pre-placed houses 3,
  House +2) sets the cap. Workers are the shared labour pool (free vs assigned).
- **Food:** Food item, bushes, Forager Hut, Granary; Colony eats Population food/tick.
- **Placement/staffing** (`BuildController`): number keys 1–N, ghost preview; collectors
  need a nearby patch, storage/housing place anywhere; auto-assign one worker on place;
  hover + `[` / `]` to adjust workers; `X` demolishes (half refund, frees workers).
- **Economy** (`Economy`): combined pool = carried + all buffers/storage. Camera follows
  the player (`CameraFollow`); HUD via `OnGUI` (`InventoryHud`).

## Recently built
- **In-game info completeness.** Every building now has a full build-menu tooltip
  (auto-generated for all kinds — Depot/Route were blank — plus hand-written strategic
  notes for ~11 key buildings via `BuildingDefinition.description`). Every item has a
  `description`. New **in-game Guide (key G)**: scrollable reference covering the goal,
  local-logistics rule, people/comfort/happiness, prosperity/rank, ages, bottlenecks,
  expansion — and a per-resource list (what it is, source, use). Discoverable from the
  help overlay + welcome toast.
- **Fixed:** Build Settings pointed at a deleted `SampleScene.unity`, so the project
  opened a blank scene (blue screen). Now points at `Assets/UnitySave.unity` (the real
  game scene, built at runtime by `GameBootstrap`). To run: open that scene, press Play.
- **Visual / UX organisation pass.** Fixed the minimap drawing over panels (+ **N** to hide
  it); consistent **dark panel** backgrounds; manage panel shows a plain-language **status
  line** + **Pause/Resume**; **Perlin-noise ground** (not a flat slab); status dots **pulse**
  when starved/backed-up; **minimap legend** + **discovered resource patches** shown on the
  minimap (fog-gated, so exploration still matters); **selection highlight** ring around the
  clicked building. (Building footprints + I/O port sides still NOT done — see KNOWN_ISSUES;
  it's the one subsystem held for a supervised compile/test loop.)
- **Systemic toolkit: see, diagnose & manage bottlenecks** (supports the local-production
  shift; all read-only/agency, no balance changes):
  - **Minimap** colours collectors/workshops by live status (red starved / yellow backed-up)
    and tints storages by fullness + shows depots — read the whole supply network at a glance.
  - **Status bar** shows a bottleneck summary (`⚠ N starved · ⏸ M backed-up`) only when there's
    a problem.
  - **Click a stalled workshop** → "⚠ Waiting for: X" tells you the exact missing input.
  - **Throughput readouts** (units/min) on workshops *and* collectors — measure whether a
    tweak actually improved output (the optimisation loop).
  - **Pause/Resume** any collector or workshop — halt one to free a scarce shared input for
    another (player-driven prioritisation under pressure).
- **Hook & optimisation wave.**
  - **Prosperity milestones** (250/500/1k/2k/4k) pop a celebratory toast — the "one more
    level" pull. **Settlement rank** (Camp → Hamlet → Village → Town → City → Metropolis)
    derived from peak prosperity, shown in the status bar, with promotion toasts.
  - **Random world events** (`WorldEvents`) every ~1 min: bountiful harvest, wandering
    nomads (+pop), rockslide/timber finds, or hard times (−food) — variety & surprise.
  - **Perf:** starved workshops back off their input re-check (~2×/sec instead of every
    frame), cutting the main per-frame pool-scan cost. (More perf items logged.)
- **Transport tiers + exploration-to-win + masonry (content wave).**
  - **Route vehicle tiers:** Caravan/Elephant (Stone) → **Ox Cart** (Tribal) → **Wagon
    Train** (Iron) → **Cargo Drone** (Industrial), each bigger/faster. Vehicle speed is now
    a data field (`BuildingDefinition.vehicleSpeed`); `MakeRoute` helper added.
  - **Gems → Jewelry, and Jewelry is now a required Monument ingredient.** Gems are the
    rarest resource, in finite deposits **far out in a third map corner** — so winning now
    *demands* exploration + good long-haul transport (trains/drones) to feed the Monument.
    Chain: Gem Deposit → **Gem Mine** (Iron) → **Jeweler** (Industrial) → Jewelry.
  - **Masonry:** Stone finally has its own chain — **Mason** (Stone → Stone Blocks) →
    **Stone House** (sturdy Bronze housing, cap 6). Parallels Wood → Planks → House.
  - **Welcome/starter toasts** orient new players (goal + H/B/objectives) on first load.
  *(All content is first-pass / untested — balance + compile to be verified.)*
- **Textile + pottery economy (deeper demand sink).** Two new comfort-good chains:
  **Pottery** (Clay → Pottery, Bronze) and **Textiles** (Cotton patches → Fiber via a
  **Cotton Farm** → **Weaver** makes Cloth → **Tailor** makes Clothes, an Industrial
  luxury). Pottery (Bronze) and Clothes (Industrial) are now **comfort goods** the colony
  consumes, so the happiness sink scales to **5 goods** by the late game (cooked food,
  bread, pottery, tools, clothes) — more parallel production to plan as you grow. New
  Cotton resource patches, F1-sandbox coverage, and four objectives wire it in.
  *(First-pass balance — the heavier comfort load likely needs `comfortTick`/want-rate
  retuning; see KNOWN_ISSUES.)*
- **Prosperity score + Monument win goal (the endless pull).** Added a climbing
  **Prosperity** score = population × happiness + tech age + an **automation weight**
  (collectors/workshops/belts/routes), so *systems* — not raw headcount — drive it,
  on-pillar. Shown in the status bar; a never-dropping **peak** is tracked too. Added
  the **Monument** (Industrial age): a building you pour metal/tools/bricks/planks into;
  producing **10 Monument Blocks** completes it and **wins the game**. New objectives
  tie it together (reach 600 Prosperity → build the Monument → complete it). Also: the
  **Town Hall (HQ) is now protected from demolition**.
- **The hook — depth pass (5 systems).** Built the "just one more thing" loop the game
  was missing:
  1. **Population demand sink + happiness.** Citizens consume **comfort goods** beyond
     survival — cooked food (Tribal), bread (Bronze), tools (Iron) — at a rate that
     scales with population. How much you can supply sets **Happiness (0–1)**, which
     boosts/throttles **Productivity** and **growth rate**. Growth raises demand →
     the escalating consumer that makes scaling production worthwhile. Shown as
     "Happy %" in the status bar.
  2. **Bottleneck feedback.** Every collector/workshop shows a **status dot**:
     🟢 working · 🟡 output full (needs transport) · 🔴 starved (no input) · ⚪ no worker.
     Bottlenecks are now visible at a glance (`Status` helper; legend in H help).
  3. **Finite rich resources.** Ore veins are **finite** (no regen) — they deplete and
     **vanish**, so local ore runs out and you must keep exploring outward, expanding
     logistics. (`SpawnNode`/`SpawnPatches` gained a `regen` param; 0 = finite.)
  4. **Shared-intermediate chain.** **Smelter** (Ore + Charcoal → Metal, Bronze) and
     **Toolmaker** (Metal + Planks → Tools, Iron). Charcoal now feeds **both** the Kiln
     (bricks) and the Smelter (metal) — scaling one starves the other (cascade). Tools
     are an Iron-age comfort good and gate the new **Industrial Age** (metal 30, tools
     15, planks 40, pop 28).
  5. **Mass placement.** Normal building placement **stays in mode** and supports
     **hold-and-drag** to stamp a whole row (one per cell, occupancy-checked); **C**
     copies the selected building's type for quick repeats. The mid-game tedium-killer.
- **The hook — guided objectives ladder.** A running list of escalating goals (gather
  → forage → grow → advance age → planks → belts → routes → cook → mine ore → Iron Age
  → smelt metal → craft tools → keep people happy → Industrial Age → thrive) shown
  top-right. Completing one pays a small **reward**, pops a celebratory **toast**, and
  reveals the next — constant direction + dopamine. Reaching a new **age pops a toast
  listing what it unlocked** (advancement feels like a power-up).
  *Done since:* the prosperity **score** and a long-term **win goal** (the Monument) now
  exist (see the top of this section). *Still open:* per-objective rewards that unlock new
  *buildings* (tech), and a post-Industrial **Future age** to extend the ladder further.
- **Logistics reworked → belts (local) + caravan routes (long distance).** Removed the
  abstract haulers. **Depots** are transfer stations (hold one resource, belt in/out);
  a **Caravan route** links two depots with an **elephant** that physically shuttles
  goods between them (travel time = distance, so depot placement matters). Build:
  Depot ×2, then the **Caravan** tool → click FROM depot, then TO depot. Belt → Depot →
  (route) → Depot → Belt brings distant resources home. Future: track-laying, and
  vehicle tiers (cart → train) by age.
- **HUD cleanup**: grouped, k-formatted, single-line resource bar (hover Food/Goods for
  a breakdown) + a **minimap** (explored area, your buildings, the player).
- **Explore → grow loop**: **Ore** is a rare resource found only in **distant veins**
  (far out in the fog). Build a **Mine** on a vein, haul the ore home, and it's
  **required to reach the Iron Age** — so you must explore, then plan a supply line
  back. **Ox Cart** = a long-range hauler (Iron) for reaching distant mines.
- **Items visibly ride belts** (a sliding dot per belt). Belt lookups are now O(1)
  via `WorldGrid` (no per-tick building scans).
- **Conveyor belts (spatial logistics!)** (`Belt`): place directional belt segments on
  a grid (R to rotate, click/drag to lay). A belt pulls from an adjacent collector/
  workshop buffer and carries items in its facing direction belt→belt→into a storage.
  You physically route goods — the factory-planning itch. Unlocks in Bronze; cheap
  (1 wood/segment); demolish by selecting + X.
- **Abstract haulers limited** — Mammoth Shack/Transporters now only serve sources
  within ~14 units of the hub, so beyond a small base you *must* lay belts.
- **Belts improved**: drag-to-route with **auto-direction** (corners flow), buildings
  **snap to the belt grid**, and belts now feed **workshop inputs** (a workshop eats
  belt-delivered inputs from its InBuffer before the global pool) — so you can wire
  collector → belt → workshop → belt → storage chains spatially.
- **Belt clarity + tiers**: build-menu **hover tooltips** explain what each building
  produces/stores (and that the Warehouse stores any one resource you pick). Belts now
  **don't pull goods into a dead end** (a disconnected belt turns red and won't run).
  Two tiers: slow **Wooden Belt** (early) and faster **Conveyor Belt** (Bronze) — the
  reason to upgrade.
- **Sandbox/test hotkeys** (F1 resources, F2 pop, F3 age, F4 free build, F5 speed).

## North-star (reference)
Target depth ≈ **Workers & Resources: Soviet Republic** logistics, but stone-age → up:
a **hybrid of conveyors and people** moving goods. People (population) are both the
workforce (staffing buildings) and an early transport tier (haulers/carriers); belts/
mechanisation gradually take over. Later transport tiers evolve toward the reference's
trucks/trains/etc. Population is the thread tying building, hauling, and survival together.
- **Configurable Warehouse** — a generic storage where the player picks the resource
  it holds (e.g. Planks, Cooked Food). Building panels now show **contents**
  (Holds/Stores X of Y). Workers **haul their own load to storage when their buffer is
  full** instead of stalling.
- **Bigger world + wide-spread, randomised resources** + smaller fog reveal → real
  exploration. **M = full-map overview**; max zoom raised.
- **Ages & tech progression** (`Colony.Age`, `BuildingDefinition.unlockAge`): Stone →
  Tribal → Bronze → … Advancing costs resources + population; the build menu shows the
  current age, an Advance button, and locks/greys future buildings (hides far ones).
  This is the progressive-unlock that also fixes early-game clutter.
- **Tribal content:** Hunter's Hut (meat), Clay Pit (clay), Charcoal Burner
  (wood→charcoal), Clay/Meat storage, Longhouse.
- **Bronze content:** Kiln (clay+charcoal→bricks), Farm (water→grain), Mill
  (grain→flour), Bakery (flour+water→bread, foodValue 4), Brick Yard.
- **Wooden Roller** — first automated transport: a worker-free conveyor (mechanical
  TransportHub) that hauls faster than a hand-transporter. Unlocks in Bronze.
- **Transporter priorities** — each transport building can be set to prioritise a
  resource (cycled in its panel), the first real logistics "choice".
- **Productivity from food** — a global work-speed multiplier: starving/thirsty slows
  everything to 60%; food *variety* (berries/meat/cooked/bread) boosts it up to +30%.
  Makes the whole food chain matter for output (shown as "Output %" in the status).
- **Ground backdrop** (placeholder terrain) behind the world.
- **Exploration — fog of war** (`FogOfWar`): the map starts dark and is permanently
  revealed in a radius around the player as they move; the M overview shows what's
  been discovered. Encourages scouting for resources.
- **Survival tuned gentler**: slower food/water consumption, bigger starting buffers,
  slower decline, and a big on-screen ⚠ warning so running out can't sneak up on you.
- **Water** (survival + crafting): ponds → **Water Hole** collector → **Water Barrel**.
  The colony **drinks water** each tick (THIRSTY → population decline, like starving).
  **Campfire** now needs **Wood (fuel) + Water** to cook, not just raw food.
- **Top resource bar** (full-width) replaces the inline resource text; resource
  patches are now **spread out + randomised** each game. Start with **5 people**.
- **Physical transport** (`TransportHub` + `Transporter`): the **Mammoth Shack** is
  staffed with workers who become Transporters that carry goods from collector/
  workshop buffers to the matching storage. Replaces the old instant teleport — this
  is the basic manual logistics tier (later: wooden rollers → conveyors).
- **Workshop/Processor engine** (`WorkshopBuilding`): generic recipe building (inputs
  → output over time, needs workers). Chains are just recipes. **Sawmill** (2 Wood →
  1 Planks) and **Campfire** (2 Food → 1 Cooked Food). Planks build Houses; Cooked
  Food nourishes more (foodValue 3 vs raw 1).
- **Builders = capped HQ job** (`Colony.MaxBuilders`): auto-filled while building,
  manually adjustable from the Town Hall; shared squad services all sites.
- **Shared staffing** via `IStaffable` (collectors, workshops, transport hubs draw
  from one worker pool). **Multi-type food** via `ItemDefinition.foodValue`.

## Planned systems (designed, not yet built)

**Food → cooking → worker buffs.** Food is currently a single resource that just
prevents starvation. The plan: many food *types* and **cooking recipes** (raw →
cooked at a campfire/kitchen). Better/more varied food gives workers stronger
**stat boosts** (work speed, carry, etc.); a well-fed colony outperforms a
barely-fed one. Starvation still causes population decline. Workers gain a small
**stats** model that these buffs feed into.

**Balanced population growth (partly done).** Growth now needs housing space AND a
stored-food **surplus** above a threshold, and each new citizen **costs** stored
food — so houses no longer instantly fill; population reflects real food progress.
Future: tie growth rate / happiness to food *variety* and cooked meals.

**Production chains, gated by age.** The core long-term challenge is *getting the
right resources to the right place to keep production optimal* — i.e. logistics.
Chains deepen each age (rough plan, to refine):
- **Stone Age:** raw gathering (wood/stone/food) + first processing: **Campfire**
  (raw Food → Cooked Food). Simple, 1-input.
- **Tribal:** **Sawmill** (Logs → Planks), pottery (Clay → Pots), basic farming.
  First "output of A feeds B" chains.
- **Medieval:** smelting (Ore → Metal), milling (Grain → Flour → Bread) —
  multi-stage, multi-input.
- **Industrial:** factories/assembly, many-input recipes, conveyors/roads for
  throughput.
- **Future:** large-scale optimization.
Each age also upgrades *logistics* (haulers → carts/animals → ramps/pulleys →
conveyors → roads), which is the real progression axis.

**Categorised build menu.** The flat number-key list won't scale as buildings grow.
Need a proper menu grouped by category/industry (Gathering, Storage, Workshops,
Logistics, Housing…), clickable + scrollable, with number-key shortcuts kept for the
common ones. (Currently: keys 1–9 plus 0 for a 10th — temporary.)

**Varied food + varied storage.** Multiple food sources need distinct gathering and
storage: **berries** from bushes, **meat** from **animals** (a huntable node that
moves / depletes differently), etc. Meat and berries store differently (e.g. a smoker/
cold store vs a basket), spoil differently, and feed cooking recipes. Needs the build
menu first (more buildings).

**Spatial conveyor belts (the real factory itch) — IMPORTANT direction.** Current
transport (Mammoth Shack/Transporters and the Wooden Roller) is *abstract*: a hauler
picks the nearest source→storage job, so there's no spatial routing puzzle. The player
wants the Factorio itch: **place directional belt segments** that physically carry
items along a path you lay out, connect source→belt→machine→storage, and **plan/solve
routing messes**. Plan:
- Belts are **placed segments with a direction**; items ride them; you must connect
  things up. Junctions/splitters/undergrounds later.
- **Limit the abstract haulers** so they don't "take over": short range and/or low
  throughput, so beyond a small base you *must* build belts. Haulers stay the Stone/
  Tribal stopgap; belts are the Mechanical-age payoff and the core optimisation game.
- Belts gated by age; upgrade tiers (wood rollers → better belts) increase speed.
This is the headline next system — design it deliberately, don't rush it.

**More player choice (less full-auto).** Right now the loop runs itself once set up;
it needs meaningful decisions: **worker & transporter priorities** (what to prioritise
when demand > supply), **placement/layout** that actually matters (distance, routing),
**branching tech/age unlocks** (choose a path), and **manual overrides**. The game
should pose trade-offs — which resource to favour, where to expand, what to research —
rather than playing itself. This is the key "make it a game, not a watch-it" gap.

**Terrain & paths.** Ground tiles (grass/dirt/water edges) for visual variety, and
**paths/roads** the player lays down — initially cosmetic, later functional (speed up
transporters / required for carts), feeding into the logistics-optimisation loop.

**Varied food sources.** Beyond bushes (berries): **animals → meat** (a huntable node
that moves / depletes differently), with distinct gathering + storage (basket vs
smoker/cold store) and different spoilage, feeding cooking recipes.

## Next steps
1. **Categorised build menu** (prerequisite for adding more buildings).
2. **Varied food gathering** (animals→meat, bushes→berries) + matching storages,
   feeding the cooking/worker-buff system.
3. **First age unlock** that changes *how* you build/transport (wooden rollers, etc.).
4. Cleanup: extract a shared base for collector/workshop (see `KNOWN_ISSUES.md`),
   protect the HQ from demolition.
5. Later: the **art pass**.

## Dev environment / repo
- Unity `6000.5.0f1`, 2D (URP). Repo lives at `C:\Users\charl\Projects\CavemanPrototype`
  (outside OneDrive so the `Library/` cache isn't synced).
- Private GitHub repo `Charlesbarnard-maker/CavemanPrototype`. The remote URL embeds the
  username; auth is via a Personal Access Token stored in Windows Credential Manager
  (not browser OAuth — that picks the wrong account on this multi-account machine).
  Per-repo commit identity: `Caveman Dev` / `charles.barnard@hotmail.com`.

## Dev process
- **Sandbox/test hotkeys** (in-play): **F1** +500 of every resource · **F2** +5 people ·
  **F3** advance age · **F4** toggle free/instant build · **F5** cycle game speed (1/2/4×).
  Lets you jump straight to testing without grinding. (Strip or gate before release.)
- **`KNOWN_ISSUES.md`** is the running log of bugs + cleanup items — keep it current.
- Every few feature batches, run a quick **optimisation/redundancy pass** and append
  findings to `KNOWN_ISSUES.md`; fix the safe ones, log the rest.
