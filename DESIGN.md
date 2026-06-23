# CavemanPrototype — Design & Direction

A 2D PC automation/factory game (Unity 6, URP) where you start as a caveman doing
everything by hand and evolve across historical ages into self-running, factory-like
systems. Inspirations: **Factorio** (production chains), **Satisfactory** (factory
scaling), **Spore** (evolution across stages).

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

**Conveyors / automated transport (the long-term goal).** The transport tiers above
Transporters: **wooden rollers** (slow, early-mechanical) → carts/animals → belts,
**gradually unlocked** across ages. The Mammoth Shack stays as the cheap early helper.
Both transporters and conveyors need **priorities / filters** — *what* to collect,
which source and which destination, and ordering when demand exceeds supply. This
routing/priority layer is the core optimisation challenge.

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
- **`KNOWN_ISSUES.md`** is the running log of bugs + cleanup items — keep it current.
- Every few feature batches, run a quick **optimisation/redundancy pass** and append
  findings to `KNOWN_ISSUES.md`; fix the safe ones, log the rest.
