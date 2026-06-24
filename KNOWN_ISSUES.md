# CavemanPrototype — Known Issues & Cleanup Log

A running record so progress/problems don't get lost. Newest first. Move items to
**Fixed** when done. Maintained alongside the code — see DESIGN.md for the roadmap.

## Fixed
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
- **BIGGEST remaining (next focused batch): multi-cell building footprints + explicit
  input/output port sides.** Buildings are 1 cell, so only 4 belt connections and no
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
- *(2026-06-23)* Belts no longer scan every building each tick — buildings register
  their cell in `WorldGrid` (cell→building dicts) so belt pickup/delivery/connection
  checks are O(1). Item-on-belt shown as one pooled sliding dot per belt (cheap).

## Open — optimisation / redundancy

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

## Open — content / polish (deliberately deferred)
- Placeholder art (code-drawn shapes, debug `OnGUI` HUD). One real art pass once
  the systems are locked.
- LF/CRLF warnings on commit — cosmetic, handled by `.gitattributes`.

## Process
Run a quick optimisation/redundancy pass roughly every few feature batches and
append findings here; fix the safe/high-value ones immediately, log the rest.
