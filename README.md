# Caveman Prototype

A 2D **factory automation** game (Unity 6, URP): start by hand-gathering wood & stone and evolve
across ages (Stone → Tribal → Bronze → Iron → Industrial) into a self-running factory. Inspirations:
Factorio, Satisfactory, Dyson Sphere Program.

> **FACTORY-FIRST (current direction):** there is **no population / food / water / housing /
> happiness** survival layer. The whole game is **gather → store → process → automate → research →
> expand**. Buildings run when built & supplied; labour is free. See [GAME_DESIGN.md](GAME_DESIGN.md)
> for the pivot rationale and the planned deeper age arc.

> **New thread / new machine? Start here, then read [GAME_DESIGN.md](GAME_DESIGN.md).**

## How to run
- **Unity `6000.5.0f1`**, 2D (URP). Open the project, then open the scene **`Assets/UnitySave.unity`**
  and press **Play**. (If you see a blank/blue screen you're on the wrong scene — open that one.)
- The **entire game is built in code at runtime** by `GameBootstrap` (one component on one
  GameObject). There is **no Inspector wiring and no prefabs** — to change content you edit code in
  `Assets/Scripts/`. Placeholder art is generated procedurally (`PlaceholderArt`). Art pass is last.

## Where the design lives (read in this order)
1. **[GAME_DESIGN.md](GAME_DESIGN.md)** — canonical system synthesis (audit, the unifying rules,
   age model, interaction map, implementation order). **This wins over the per-system docs.**
2. Per-system detail: [AGES](AGES.md) · [WORLD](WORLD.md) · [LIQUIDS](LIQUIDS.md) ·
   [CONSTRUCTION](CONSTRUCTION.md) · [BUILD_SYSTEMS](BUILD_SYSTEMS.md) · [DESIGN](DESIGN.md)
3. **[KNOWN_ISSUES.md](KNOWN_ISSUES.md)** — running log of bugs, tuning watchpoints, and what's
   built vs deferred. Keep it current.

The one rule that ties it together: **"stored, not summoned"** — resources are only usable once
logistics delivers them to storage/HQ/your hands; nothing teleports. Each age changes *how*
logistics works, never *what* the loop is.

## Current build status (what's actually in the game)
- **Ages** Stone→Tribal→Bronze→Iron→Industrial. Advancing is **research-driven**: craft a research
  item (Idea Tablet → Study Scroll → Schematic → Blueprint) and deliver it to a **Research Lodge**
  for points (no resource/pop "advance" button). Power is the Industrial constraint.
- **Economy** "stored, not summoned" (`Economy.StoredOnly`) — usable pool = carried + storages;
  local production for workshops (adjacent/belt-fed).
- **Solids logistics** hand-carry → belts (drag, auto-corner, I/O **ports**: output arrow / input
  notch, rotate R) + **Splitters** (1→2 even split) → **Stations** + transport routes (cross-region,
  created from a Station's panel; vehicles load → travel → unload).
- **Resources** appear in natural **clusters** (groves/outcrops): small starter clusters → dense
  biome clusters. **Throughput ratios** are normalized to *1 lane = 60/min* (wooden belt 60,
  conveyor 120, collector ≈60, standard machine eats 60 at 1 worker → "1 gatherer → 1 belt → 1
  machine"). **Bottleneck cues:** belt red=dead-end / yellow=backed-up; depleting nodes grey out;
  machine status dots + a `Needs X/min` panel line.
- **Placement/collision** water buildings need an adjacent water cell; belts can't sit on buildings;
  the player collides with solid buildings (workers don't yet — pending pathfinding).
- **Liquids** (distinct system) carried water (Water Hole) → **pipes + mechanical Pump** → direct
  pipe-fed consumers + barrels → **pressure/range + Booster Pumps**. Water is a liquid: never on belts.
- **Construction** logistics-driven (builders haul materials) — **build cost is paid up front** at
  placement; **Construction Yards** scale the free builder cap.
- **No survival layer (factory-first):** no food/water/population/housing/happiness. Buildings run
  when built & supplied; **labour is free** — each collector/workshop has a 1..max worker *speed dial*
  (+ NPC charm). Progress is measured by automation: **Industry** score + **Rank**; **Monument** = win.
- **World/geography** biome `TerrainGrid` (Plains/Forest/Hills/Water); **water is a hard barrier**
  (no walking/building/belts) crossed by **Bridges**; **rivers** divide regions. Big world (~400 units),
  finite frontier **ore** out in the hills. Biomes now form large contiguous regions (smoothing pass).
- **Power** global supply/demand + brownouts from the Industrial age (Coal Generator burns charcoal).
- **Shelved (in code, not in the build menu):** the old water/liquids system (Water Hole, pipes,
  pumps) and the comfort/textile/masonry/jewelry chains — kept compiling but removed from play.

## Controls
WASD/arrows move (Shift sprint) · mouse-wheel zoom · **M** overview · **B** build menu (accordion
categories + Recent) · **T** research tree (spend points) · **G** Guide · **H** help · **N** hide
minimap · Space pause · click a building to manage · **X** demolish · **C** copy selected · **R**
rotate (belt / building output) · hover anything for a name tooltip.

**Sandbox/testing hotkeys:** **F1** +500 of every resource · **F3** advance age · **F4** free/instant
build · **F5** game speed · **F7** toggle local/global production · **F8** reveal whole map · **F9**
toggle stored-only economy. (**J** = start all idle buildings.)

## Dev notes
- **Blind-build discipline:** the assistant can't run Unity — edits are brace-checked + reference-
  grepped before each commit; built in small chunks; the user recompiles and reports Console errors.
- **Repo:** private GitHub, default branch `main`. Auth via a Personal Access Token in the OS
  credential manager (no secrets committed). Per-repo commit identity: `Caveman Dev`.
- Keep the repo *outside* OneDrive so Unity's `Library/` cache isn't synced.
