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
- **Economy:** combined pool. Build costs spend from the player's carried inventory +
  ALL building buffers/storage. Manual gathering fills carried; collectors fill their
  own buffer then push to adjacent storage.
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

## Next steps
1. **NPC Haulers** — workers that carry resources between buildings that aren't adjacent
   (distance logistics without belts; the "human chain").
2. First **production chain** (e.g. Wood → Planks via a workshop) so one building feeds
   another — and **age-based routing** (where output goes changes as you evolve).
3. First **age unlock** that changes *how* you build (e.g. gravity ramps → pulleys →
   conveyors). Manual worker assignment polish (priorities), multiple workers tuning.
4. Later: the **art pass** (turn placeholder shapes into real sprites).

## Dev environment / repo
- Unity `6000.5.0f1`, 2D (URP). Repo lives at `C:\Users\charl\Projects\CavemanPrototype`
  (outside OneDrive so the `Library/` cache isn't synced).
- Private GitHub repo `Charlesbarnard-maker/CavemanPrototype`. The remote URL embeds the
  username; auth is via a Personal Access Token stored in Windows Credential Manager
  (not browser OAuth — that picks the wrong account on this multi-account machine).
  Per-repo commit identity: `Caveman Dev` / `charles.barnard@hotmail.com`.
