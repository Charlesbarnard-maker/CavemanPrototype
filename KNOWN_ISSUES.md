# CavemanPrototype — Known Issues & Cleanup Log

A running record so progress/problems don't get lost. Newest first. Move items to
**Fixed** when done. Maintained alongside the code — see DESIGN.md for the roadmap.

## Latest state + top tuning watchpoints (2026-06-24) — read first
- **"Stored, not summoned" is LIVE** (`Economy.StoredOnly`, F9 to toggle off). Usable pool =
  carried + storages only. **Watch early-game balance:** solids must be belted/hand-carried to
  storage; liquids are worker-carried to a Water Barrel (so a Water Hole needs a barrel beside
  it). Starting 90 food/90 water are in `carried`. If it feels harsh, tune starting buffers /
  pre-place a Water Barrel, or run on F9 while tuning.
- **Big world shipped** (terrain half 200 ≈ 400 units; fog 420; camera maxZoom 140). Frontier
  resource clusters (incl. finite ore/gems) far out. Tune size via `TerrainGrid.Generate` half +
  fog `worldSize` (keep them matched). Watch: river density / early reachability on the bigger map.
- **Liquids:** pipes + pump + pipe-fed consumers + pressure(range 16)/Booster Pumps; water can't
  ride belts (`isLiquid`). Tune pump `flowPerTick`/`range`, booster/pipe costs.
- **Next planned (GAME_DESIGN order):** geography friction (forest/hills build+move) → Tribal/Iron
  age rule-shifts → regional power → multi-stop routes.

## World / water — hard rules (2026-06-24, NEEDS TESTING)
- **Water is now a HARD barrier**: player can't walk on water (`PlayerController` per-axis
  `TerrainGrid.Walkable` check), and buildings/belts can't sit on it. **Bridges** (`Bridge`,
  build menu → Bridges) are placed ON water (drag to span) and make cells walkable + belt-
  traversable. **Rivers** (noise bands) divide the map. **Water is terrain, not nodes** — the
  old abstract "Lake" nodes are removed; the **Water Hole** now draws from adjacent water
  TERRAIN (its `Bind` spawns an infinite invisible source at the nearest water cell). A
  guaranteed starter pond is carved at ~(15,3) so early water is reachable.
- **Test/watch:**
  - Early-game reachability: can you always reach the starter pond + near wood/food without
    needing a bridge first? If a river rings the basin and soft-locks early wood, widen the
    basin or thin the river band (`rF` / `0.035` in `TerrainGrid.Generate`).
  - Workers still walk onto water to "draw" from the water source node (only the PLAYER is
    barred). Intentional (laborers at the shore), but note the inconsistency.
  - Bridge cost (3 wood), no age gate is first-pass; consider cost/age-gate + requiring
    bridges connect to land so they aren't spammed.

## Fixed
- **2026-06-24 — Belts lost goods into dead-ends + warped at corners + corners fiddly.**
  (1) `HasForwardTarget` treated *any belt ahead* as connected, so belts pumped goods into
  chains that ended in empty space, draining stock into limbo. Now `LeadsToSink` walks the
  chain and a belt only pulls if it actually reaches a storage/workshop/depot. (2) The
  sliding item-dot always entered "from behind", so at a corner it teleported across the
  cell — now it enters from the edge it actually arrived at (`_inDir`) and tracks the bend.
  (3) Belt drag now lays an L-shaped path (`DragBeltPath`/`EnsureBelt`): drag from A to B and
  every cell is placed + auto-oriented toward the next, so corners snap and you can pull full
  90° lines in one motion.
- **2026-06-24 — HQ (Town Hall) could be demolished.** `BuildController.DemolishSelected`
  now refuses to demolish a `HousingBuilding` flagged `isHQ` (shows a toast instead), so
  builder management and the starting pop cap can't be lost.
- **2026-06-23 — Resources teleported to warehouses.** Buffer→storage was instant.
  Now physical: build a **Mammoth Shack** (`TransportHub`) and assign workers who
  become **Transporters** that carry goods from collector/workshop buffers to storage.
  (Note: workshop *inputs* are still pulled from the global pool, not hauled — see Open.)
- **2026-06-23 — Builders hogged all workers.** Each construction site claimed up
  to 3 free workers, starving resource collectors and making it impossible to keep
  gathering during a build. Reworked: builders are now a **capped HQ job**
  (`Colony.MaxBuilders`, default 2), auto-filled when construction exists, and
  manually adjustable from the Town Hall (HQ) panel. Sites are serviced by the
  shared builder squad rather than each grabbing workers.
- **2026-06-23 — Two scenes** (empty `SampleScene` vs working scene). Removed the
  unused template; `UnitySave.unity` is the working scene.

## Fixed (continued)
- **2026-06-23 — Population died fast & silently (ran out of water).** Slowed food/
  water consumption, bigger starting buffers (90 each), slower decline (more grace),
  and a big on-screen ⚠ warning when out of food/water.

## Open — SYSTEMIC SHIFT: local production (2026-06-24, NEEDS TESTING FIRST)
- **`Economy.LocalProduction` (default ON, F7 toggles).** Workshops now consume inputs only
  from InBuffer (belt) or an **adjacent** storage/collector/workshop — not the global pool.
  This is the core "logistics actually matters / bottlenecks cascade" change. **Test this
  before layering more pressure on top.** Things to verify:
  - Early game is still doable: can you feed a Sawmill by placing it next to a wood source/
    storage or belting wood in? Is the difficulty spike fair or brutal?
  - Do bottlenecks now *cascade* (one starved upstream machine starves everything after it)?
  - Compare with **F7** (old global-pool mode) to feel the difference.
  - Collectors now **auto-rebind** to a fresh nearby patch when one depletes; only go idle
    when the whole local cluster is dry → the cue to expand. Verify it doesn't thrash.
- **DELIBERATELY NOT YET STACKED** (would obscure whether the above works): deeper universal
  depletion tuning, throughput ceilings, food spoilage, more shared-intermediate chokepoints.
  These are the next pressure layers — add one at a time AFTER local production is validated.

## Open — POWER (Industrial age, first age-as-mechanic-shift — NEEDS TESTING)
- **`Power` (global) + `PowerPlant` + `BuildingKind.Power`.** From the Industrial age
  (age index 4) workshops DRAW power (`def.powerDraw`, default 10 via MakeWorkshop) and
  the **Coal Generator** burns charcoal to SUPPLY it (`powerOutput` 60, unlock Iron so you
  can prep). Supply < demand → global brownout scales machine speed down to a 0.35 floor
  (`Power.Factor`, applied in WorkshopBuilding.Update). Status bar shows `⚡ gen/dem` (red
  + BROWNOUT when short). This is the proof-of-thesis for AGES.md ("an age changes the rules").
- **Balance is first-pass & built blind — verify on a playtest:**
  - Advancing to Industrial makes ALL running workshops demand power at once → if you have
    no generators it instantly drops everything to 35%. Intended "shock", but maybe too harsh
    — tune `BrownoutFloor`, `powerDraw` (10), generator `powerOutput` (60), or stagger which
    machines need power.
  - Generator pulls fuel from the GLOBAL pool (not local/belt-fed) for now — simpler, but
    inconsistent with LocalProduction. Next: belt-feed fuel + local draw.
  - Generator is 1×1, no workers. Consider footprint (2×2) + a worker once footprints land.
- **Next power layers (after it's validated):** regional grids (wires/range) instead of one
  global pool; power as a prerequisite (machine off, not just slow, below a threshold); a
  brownout status dot on starved machines.

## Open — routes (MVP built — iterate)
- Caravan routes are point-to-point (elephant shuttles A↔B along a straight line, no
  laid track). Next: **track/path laying** (vehicle follows it; placement puzzle),
  **vehicle tiers by age** (elephant → cart → **train**, faster/bigger), route **load
  filters**, multi-stop routes, and a route-management panel. The old `TransportHub`/
  `Transporter` classes have no buildables, but are NOT trivially deletable — they're
  still referenced in **5 files** (`Colony` AssignedTotal/EnforceAssignment,
  `BuildController` CellOccupied/Demolish/BuildingGOUnderCursor, `ConstructionSite`
  `case BuildingKind.Logistics`, `InventoryHud` lines ~258/379/424, plus `MakeLogistics`
  in `GameBootstrap`). Removing them is a multi-file pass best done **supervised** (with
  a compile after), not blind. Related stale-UX: `InventoryHud.CurrentObjective` still
  tells the player to "Build a Mammoth Shack" (line ~258) — a building that no longer
  exists; fix when the classes are removed.

## Open — belts (strong now — iterate)
- Belts: place/drag with **auto-direction** (path + corners), building→belt→storage
  **and** building→belt→**workshop input** (workshops consume belt-fed inputs before
  the pool). Buildings snap to the belt grid. Haulers range-limited.
- Belts now: **don't run when disconnected** (dead-end = red, won't pull), **two speed
  tiers** (Wooden/Conveyor), and build-menu **tooltips** explain each building.
- **I/O PORTS — Step A done (2026-06-24), NEEDS TESTING.** Collectors + workshops now have a
  rotatable **output side** (`OutputSide`, R during placement, green arrow on the edge +
  ghost). Belts only **pull** a building's output from that side (`Belt.PullFromNeighbour`
  gated by `OutputSide == Opposite(scanDir)`). All buildings default output = East, so belts
  must sit on the arrow side. *Step B (not done):* input-side ports (deliver only on chosen
  sides), per-input ports (Smelter: Ore port + Charcoal port on different sides), and gating
  depots/storages + the workshop local-`AdjacentConsume` (currently still omnidirectional for
  inputs). Watch for: collectors whose belt was on a non-east side now not pulling (rotate
  with R); AutoBeltDir doesn't know about output sides yet.
- **I/O PORTS — Step B done (2026-06-24).** Workshops + storages now also have an INPUT side
  (opposite the output, both rotate together with R). Belts **deliver only on the input side**
  (`dir == OutputSide`) — input notch (cyan) + output arrow (green) shown on the ghost AND
  the placed building. Storages got an `OutputSide` (orientation only; belts don't pull from
  storage yet). NOTE: worker hauling + workshop `AdjacentConsume` still move goods
  omnidirectionally (only BELTS are port-gated) — this is intentional (keeps the game
  playable) but means belts and people follow different rules; unify later if desired.
  *Step C (not done):* per-input ports (Smelter: Ore port + Charcoal port on different
  sides), depot port-gating, belt-pull FROM storages, AutoBeltDir port awareness.
- **Belt item flow fixed (2026-06-24):** dots no longer jump at hand-offs — receiving a belt
  resets its move timer (item dwells one interval) and the 0.5 edge offset makes one belt's
  exit point coincide with the next's entry point (continuous slide, item visibly enters
  buildings).
- **(superseded) multi-cell footprints + I/O port sides.** Buildings are 1 cell, so only 4 belt connections and no
  size variety. Want: buildings sized by function (e.g. 2×2 workshop, big warehouse),
  with dedicated output/input port cells (facing, rotatable) — belts must connect to
  the right slot. This is the spatial-challenge depth the player is asking for; it's a
  sizeable subsystem (footprint occupancy, ports, markers, belt port-aware I/O) best
  done as its own carefully-iterated batch.
- Other belt items: **splitters/junctions/undergrounds**; **collector tiers** (manual→
  machine); **item groups** so a storage holds a category (all "lumber"); re-gate belts.

## Open — bugs / behaviour
- **Collectors don't re-bind when their source depletes (matters now ore is finite).**
  `ProductionBuilding.Bind()` picks the nearest matching node *once* (at spawn). With
  finite ore veins (Hook #3), a Mine whose vein depletes goes permanently 🔴 Starved even
  if another vein is in range — the player must demolish + rebuild. Options: re-`Bind()`
  when `_source` is null/exhausted, or surface "source depleted — relocate" in the panel.
  (Decide if auto-rebind is desired or if relocation is the intended logistics pressure.)
- **Workshop inputs & build costs still pull from the global pool, not hauled.**
  Buffer→storage is now physical (Transporters), but a workshop's *inputs* and a
  build site's *materials* are taken from the combined pool regardless of distance.
  Next: route inputs through transport too (and conveyors).
- *(Fixed 2026-06-23)* ~~Build menu doesn't scale~~ — now a categorised, scrollable,
  clickable menu (Gathering / Workshops / Logistics / Storage / Housing). Number keys
  1–9 + 0 remain as shortcuts. Categories are derived from building kind for now;
  finer "industries" sub-grouping can come later.
- **No food variety yet.** Berries (bushes) exist as "Food"; planned: **meat** from
  **animals** (moving/huntable node) with distinct gathering + storage + spoilage.
- **Transporters have no priorities.** They grab the nearest available job; no way to
  say "prioritise food" or set source→destination routes. Needed for real logistics
  (and for conveyors later).
- **No terrain / paths.** Flat background only; planned ground tiles + player-laid
  paths/roads (cosmetic first, then speed/route function).
- **Reducing builders mid-haul wastes a little.** A removed builder drops its claim
  (the picked-up units were already spent); another re-fetches. Rare, low impact.
- **HQ (Town Hall) can be demolished**, which would break builder management and
  drop the starting pop cap. Should be protected from demolition.

## Optimisation done
- *(2026-06-24 pass)* **Belt connectivity throttled** — `HasForwardTarget`/`LeadsToSink`
  (a chain walk up to 256 cells) now runs only on the move-interval tick and is cached
  (`_connected`), not every frame → big win with many belts. **Workshop `AdjacentAvailable`**
  no longer allocates a `HashSet` per call (reuses a static). **Worker self-haul removed**
  (gameplay + perf: one fewer per-frame storage scan). Verified no `FindObjectsByType`/LINQ
  allocations in hot paths. Remaining (logged below, needs supervised refactor): `OutputBuilding`
  base class + `Util.Nearest<T>` to dedupe the ~6 nearest-scans.
- *(2026-06-23)* Belts no longer scan every building each tick — buildings register
  their cell in `WorldGrid` (cell→building dicts) so belt pickup/delivery/connection
  checks are O(1). Item-on-belt shown as one pooled sliding dot per belt (cheap).

## Open — optimisation / redundancy

### Audit 2026-06-24 (blind review)
- *(Done)* **Workshop starved-retry backoff** — starved workshops now re-check inputs
  ~twice/sec instead of every frame, cutting the worst per-frame `Economy.Available` cost.
- **Status bar is getting crowded** — Population/Working/Free/Age/Rank/Output/Happy/need/
  Prosperity/Monument on one line. Widened to `Screen.width-320`, but on a small window it
  may still clip; consider a second row or grouping if it looks cramped in testing.
- **`WorldEvents` cadence/effects are first-pass** — every ~55–105s; tune gaps and the
  grant/loss amounts so events feel meaningful but not swingy.

### Audit 2026-06-24 (blind review — logged, not yet fixed)
- **`OutputBuilding` base class is the clear win (confirms "biggest" below).** Concretely,
  `ProductionBuilding` and `WorkshopBuilding` share: an **identical `UpdateVisual`**
  (flash + dim), identical `Awake` (cache `_sr`/`_baseColor`), the `_flash/_sr/_baseColor/
  _statusDot` field set, near-identical `UpdateStatus` (only the "starved" test differs),
  near-identical `TryAssign`/`Unassign`, and the same `OnEnable/OnDisable` WorldGrid
  registration shape. A shared `abstract OutputBuilding : MonoBehaviour, IStaffable`
  could hold all of it; subclasses override only the per-tick produce logic + the
  starved test. ~80 lines removed. (Supervised refactor — touches the two hottest files.)
- **"Nearest-in-list" pattern is copy-pasted ~6×.** `Worker.NearestStorage`,
  `BuilderWorker.NearestActiveSite`/`DepotFor`/`NearestHousing`, `ProductionBuilding.Bind`,
  `BuildController.HasMatchingNodeNear` all re-implement the same min-`sqrMagnitude` scan.
  A generic `Util.Nearest<T>(IEnumerable<T>, Vector3, predicate)` would dedupe all of them.
- **`Worker` vs `BuilderWorker`** share `MoveTo` (Worker scales by `Prod`, Builder doesn't),
  `UpdateColor` (identical carry-tint), and the nearest-housing logic — a small shared
  `CarrierBase` would help once the Nearest helper exists.
- **Workshop hot path: `CanMake` scans the whole pool every frame while starved.**
  When inputs are missing, `Update` sets `_timer = processTime`, so each starved workshop
  calls `Economy.Available` (which iterates all storages+collectors+workshops) **per input,
  per frame**. With many workshops + buildings this is the main per-frame cost. Fix idea:
  a once-per-frame cached pool snapshot shared by all consumers (extends the existing
  `Economy.Totals` caching), or back off the retry cadence when starved.
- **Stale doc comments:** `ProductionBuilding`/`WorkshopBuilding` class summaries still say
  buffers are "emptied by Transporters (TransportHub)" — that model was replaced by belts/
  workers. Harmless, but update when the TransportHub classes are removed.

- **Collector/Workshop duplication [biggest].** `ProductionBuilding` and
  `WorkshopBuilding` duplicate `PushToStorage` / `DrawLink` / `UpdateVisual` / flash
  handling. Extract a shared base class (e.g. `OutputBuilding`). Deferred to a
  dedicated refactor to avoid destabilising feature batches.
- **Worker/BuilderWorker duplication.** Shared `MoveTo` / `NearestHousing` /
  `UpdateColor` movement helpers could live in a common base.
- *(Improved 2026-06-23)* HUD perf: `Economy.Totals()` is now cached once per frame
  (was ~2×/frame), and the build menu only runs its per-building affordability checks
  while it's open (B-toggled). Remaining: per-idle-transporter `FindJob` scans the
  building registries each frame — fine for now, revisit if building counts get large.

## Open — the hook (keep deepening)
- *(Done 2026-06-24)* ~~population demand sink~~ (comfort goods + happiness),
  ~~prosperity/output score that climbs~~ (Prosperity, automation-weighted, with a peak),
  and ~~a long-term win goal~~ (the Monument — 10 blocks = win). Still open: per-objective
  rewards that unlock *new buildings* (tech) rather than just resources, and a
  post-Industrial **Future age** to extend the ladder.
- **Balance: the new endgame numbers are first-pass and UNTESTED** (built blind).
  Tune after a playtest: the 600-Prosperity objective threshold (vs the Prosperity
  formula in `Colony.ComputeProsperity`), the Monument's build cost + recipe + the
  10-block win target, and whether the Industrial-age gate paces into it well.
- **Balance: the Monument win now requires Jewelry** (→ Gem Mine → distant gems →
  Jeweler), so the endgame is a long chain gated on exploration + long-haul transport.
  Intended as the climactic logistics test, but verify it's reachable, not a brick wall —
  tune gem yield/spawn count (3 deposits), Jeweler/Gem Mine rates, and the 1-jewelry-per-
  block Monument recipe. F1 sandbox grants gems+jewelry so the win is still testable fast.
- **Balance: new buildings/recipes are first-pass** (Ox Cart/Wagon Train/Cargo Drone
  capacities & speeds; Mason/Stone House costs; Jeweler/Gem Mine rates). Sanity-check the
  vehicle tiers feel like real upgrades and Stone House is worth it vs House/Longhouse.
- **Balance: comfort demand grew from 3 → 5 goods** (added Pottery@Bronze,
  Clothes@Industrial). `Colony` Happiness = fraction of *all* unlocked comforts supplied,
  each wanted at ~Population/2 per `comfortTick` (9s). At Industrial the colony now wants
  cooked food + bread + pottery + tools + clothes simultaneously — likely too punishing at
  first. Tune `comfortTick`, the per-comfort want rate (`Colony` comfort loop), or stagger
  the unlock ages. Watch that early "Happy %" doesn't crater the moment Bronze adds pottery.

## Open — design
- **Too automated / not enough choice** — *partly addressed*: ages now force choices
  (what to build toward, advance when ready), transporter **priorities** added, and
  food **variety→productivity** rewards decisions. Still want: worker job priorities,
  layout that matters more, branching (not just linear) tech.

## Open — onboarding / UX
- **Too much at once at the start.** All buildings/categories are visible immediately,
  which is a lot for a new player. Plan: progressive unlock (via ages) so early game
  shows only a few options, plus a cleaner first-run / tidier build panel.

## UX / visuals pass (2026-06-24)
- *(Done)* Minimap no longer overlaps panels (reordered + repositioned), **N** hides it,
  and all panels share a dark background for an organised look. Selected panel shows a
  plain-language status line. Ground uses soft Perlin noise instead of a flat slab.
- **Building footprints + input/output port sides: STILL NOT DONE** (asked for repeatedly).
  It's the one big subsystem held back because doing it blind is too risky: it touches
  placement (`BuildController.CellOccupied`/ghost), multi-cell occupancy (`WorldGrid` would
  need to register every footprint cell), belt I/O (`Belt.PullFromNeighbour`/`PushForward`
  must respect designated sides), and building orientation/rotation. A bug here breaks
  *placement itself* → can't build anything → ruins a whole test session. Recommend doing
  it as a dedicated batch with a compile/playtest loop. Scope when tackled:
  1. `footprintW/H` + `outputDir`/input sides on `BuildingDefinition`.
  2. Register all covered cells in `WorldGrid`; placement checks all cells; bigger ghost.
  3. Belts only connect at the matching port cell/side; rotate building with R.
  4. Visual port markers (in = small notch, out = arrow).

## Open — content / polish (deliberately deferred)
- Placeholder art (code-drawn shapes, debug `OnGUI` HUD). One real art pass once
  the systems are locked.
- LF/CRLF warnings on commit — cosmetic, handled by `.gitattributes`.

## Process
Run a quick optimisation/redundancy pass roughly every few feature batches and
append findings here; fix the safe/high-value ones immediately, log the rest.
