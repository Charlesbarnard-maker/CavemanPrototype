# CavemanPrototype — Known Issues & Cleanup Log

A running record so progress/problems don't get lost. Newest first. Move items to
**Fixed** when done. Maintained alongside the code — see DESIGN.md for the roadmap.

---
## 📌 NEW-THREAD HANDOFF — current state (2026-06-28, all prior work + the ART session committed & pushed)
Pinned pointer so a fresh thread (incl. on a DIFFERENT PC) can continue. Everything below was committed and
pushed to `origin/main` at the end of the "bespoke art for all ages" session — so a clean clone has the whole
game. Open the project, Play, and continue from "LIKELY NEXT".

**⚠️ FIRST THING IN A NEW THREAD: verify the full Unity compile.** All code is static- + standalone-compile-checked
(see below), but the authoritative check is Unity's own recompile of the whole `Assembly-CSharp`. On the new PC:
open the project in Unity `6000.5.0f1` → it imports + recompiles → read `Logs/Editor.log`, grep `error CS` after
the LAST "Requested script compilation" (ignore stale errors ~line 1200). Editor closed → `Tools/compile-check.ps1`.

- **Build:** Unity `6000.5.0f1`, 2D/URP. Open `Assets/UnitySave.unity` → Play. ALL built in code at runtime by
  `GameBootstrap.cs` (no prefabs/Inspector wiring — script `.meta` GUIDs don't matter, regenerated on import).
  Commit as `Caveman Dev <caveman@local>`, NO AI trailer (only when asked).
- **NEW THIS SESSION — bespoke art for EVERY buildable structure (28 buildings, ages 0–4).** See entry below.
  Each lives in its own partial-class file under `Assets/Scripts/Bespoke/PlaceholderArt.<Name>.cs`; all dispatched
  from `PlaceholderArt.BespokeBuilding(displayName)` (called FIRST by `SpriteDatabase.ForBuilding`, so it wins over
  the Roguelike pack + the generic archetype). VERIFIED to compile via a standalone `dotnet build` of the 28 files
  against `UnityEngine.CoreModule.dll` (0 errors) — but NOT yet eyeballed in Unity (visual review owed).
- **⚠️ OWED: (1) a Unity recompile + VISUAL eyeball of the new building art** (it's blind procedural art — Play
  through the ages, age-skip with F3, and check each building reads well / tints right; tweak the offending
  `PlaceholderArt.<Name>.cs` method). **(2) a full PLAYTEST** of the whole accumulated game.

**PRIOR SESSION CONTENT (factory-first caveman game; all now committed):** belts (per-tier, seamless, corners,
splitters/mergers) · rail with one/two-way signals + multi-stop train lines · boats (shore harbours, cargo-ship
lines, boat island, buyable personal boat) · LIQUIDS (Oil Well→pipes→Refinery→Oil Generator; Water Pump) · power
(belt-fed generators, poles, batteries) · timed construction · manual paid age-upgrades · visible caveman workers
+ a per-age player avatar (3-frame walk) · flyout build menu · per-resource top bar · 2×2 buildings (Monument 3×3)
with single I/O ports · stone-age bespoke art (Wood Hut/Stone Pit/Sawmill/Idea Bench/Research Lodge/Woodpile +
Tree/Rock/Caveman). New files across the whole arc: `WorkerUnit.cs`, `PlayerAvatar.cs`, `Battery.cs`, `PowerNode.cs`,
`RailTile.cs`, `Signal.cs`, the `Assets/Scripts/Bespoke/` art folder, and the `Assets/Roguelike/` + `Assets/Resources/`
sprite packs.

- **🔜 LIKELY NEXT (user's open threads, roughly priority order):**
  1. **Unity recompile + visual pass on the 28 new building sprites** (the immediate owed item — iterate any that
     look off; they're procedural + un-eyeballed).
  2. **Player VEHICLE/MOUNT visuals per age** (deferred — was about to start when paused). `PlayerController` already
     names the tiers (On Foot→Horseback→Ox Cart→Wagon→Motorbike); give each a sprite shown by `PlayerAvatar`. The
     fuller version is a buyable mount + a limited GARAGE to park them.
  3. **Make the WHOLE map feel hand-designed** (only the island was reshaped so far) — `TerrainGrid` / resource zones.
  4. **Tighten 2×2 single-port belt I/O to ONE cell** (belts still functionally connect anywhere along a side though
     only one port marker shows — see `Ports.singlePort` + `Belt.cs` deposit/pull).
  5. **#35 deferred audit items** (PERF: `DrawTopBar`/minimap OnGUI caching, `PowerNet.Rebuild` per-frame allocs,
     `SolidBuildingAt` allocs; BUGS: rail head-on deadlock, signal false-green lamp at a junction; dead survival-era
     wiring to delete) — see the #35 entry for the full list.
  6. Fuller UI/tutorial polish; bespoke ITEM icons (currently shape-by-family).
- **Canon:** GAME_DESIGN.md is partly SUPERSEDED (factory-first, but workers are back as a COSMETIC layer).
  Keep KNOWN_ISSUES newest-first. CavemanPrototype stays OUT of global memory.
---

## #38 BESPOKE ART for every buildable structure — all ages 0–4 (2026-06-28)
Extended the stone-age bespoke-art treatment to EVERY remaining building the player can place (28 structures),
so nothing falls back to the generic tinted-archetype box any more. Generated via a fan-out workflow (one focused
artist per building: draft → refine → write), each method hand-following the established house style.
- **What's new (28 buildings):**
  - Age 0: Station (timber platform), Harbour (plank dock over water), Warehouse (gabled storehouse), Water Barrel.
  - Age 1: Clay Pit, Copper Mine, Iron Mine, Charcoal Burner (turf clamp), Basic Smelter (stone bloomery), Scroll
    Maker, Clay Pile.
  - Age 2: Kiln (brick dome), Potter (wheel), Advanced Smelter, Refinery (still), Drafting Table, Brick Yard, Oil
    Tank, Wood Generator, Battery, Water Pump, Booster Pump, Oil Well (derrick).
  - Age 3: Toolmaker (forge/anvil), Engineering Lab, Coal Generator, Oil Generator.
  - Age 4: Monument (tiered gold-capped ziggurat).
- **Structure:** `PlaceholderArt` is now `partial`; each sprite is its own file `Assets/Scripts/Bespoke/
  PlaceholderArt.<Method>.cs` (e.g. `BronzeKiln`, `IronToolmaker`, `IndustrialMonument`). All wired into
  `PlaceholderArt.BespokeBuilding(displayName)` (grouped by age). Style = 64×64, `fy=0` bottom, white structural
  body (tinted by `def.color`) + baked 3-shade detail lit upper-left + a dark silhouette outline pass.
- **One wiring fix:** `Depot.cs` now renders its deck via `SpriteDatabase.ForBuilding(def)` (was a plain `Square()`),
  so the Harbour shows its full dock and the Station shows a plank platform under its rails.
- **Compile VERIFIED (not just static):** a standalone `dotnet build` of all 28 files against Unity's
  `UnityEngine.CoreModule.dll` (via Unity's bundled .NET SDK) with a 5-method shim → **0 errors, 0 warnings**.
  Also brace/paren-balanced, no helper/field/method-name collisions, signatures match the dispatch. **Still NOT
  eyeballed in Unity** — the art is procedural + blind, so a Unity recompile + visual pass is owed (iterate any
  sprite that reads poorly by editing its `PlaceholderArt.<Name>.cs` method).
- **Belts/track/pipes/poles/signals** keep their existing dedicated procedural tile art (already per-tier/neighbour
  aware) — `BespokeBuilding` returns null for them so they fall through.

## #37 Conveyor look: per-tier sprites + neighbour-aware corners (2026-06-28)
Conveyor visuals. **NOT yet Unity-compiled by Claude** (Editor open; #36 also still awaiting recompile) —
static-checked, brace-balanced, grep-verified.
- **Per-AGE belt look** (point 1): NEW `PlaceholderArt.BeltSprite(tier, shape)` — 4 tier looks × 3 shapes,
  cached (12). Tier from the belt's name: Wooden = **rollers** (horizontal lines), Conveyor = **chevrons**,
  Geared = **chevrons + side gear teeth**, Steel = **bold arrows + bright rails**. Drawn white+dark so the
  per-tier `_baseColor` tint still shows + the flow-state colour (red/yellow) still works.
- **Corners actually connect** (point 2): `Belt.UpdateBeltShape()` (called each frame for plain belts) picks
  STRAIGHT vs a curved CORNER from neighbours — `BeltShape()`/`FeedsInto()` detect whether the feeder is
  behind (straight) or to the left/right (corner). The corner sprite is a quarter-turn belt band whose width
  matches the straight body at the shared edge, so bends join up instead of two straights meeting at a gap.
  Sprite re-picked only when tier/shape changes (cheap). Splitters/mergers keep their junction sprite.
- **All tiers interconnect** (point 3): already true — the sim (`AcceptsHandoffFrom`/`ReceiveItem`) and the
  new shape detection never check belt tier, so Wooden↔Geared↔Steel mix freely (mechanically + visually).
- **In/out of buildings**: a belt fed from a building (behind) or feeding one shows STRAIGHT chevrons pointing
  the right way; the building's green-out/cyan-in port markers sit on the shared edge. **PARTIAL** — a belt
  fed by a building from a perpendicular SIDE still draws straight (corner detection only inspects belts), and
  the BUILD GHOST still shows the generic Conveyor sprite (placed belt is per-tier). Both noted for follow-up.
- **Recompile check (intended):** lay a Wooden run (rollers) then upgrade to Steel (arrows) — looks change per
  tier; turn a belt — the corner curves and connects; mix tiers in one line — they connect and items flow.

## #36 Straight-line conveyor drag + neighbour-shaped track (corners/junctions) (2026-06-28)
Two placement/visual asks. **NOT yet Unity-compiled by Claude** (Editor open) — static-checked, brace-balanced,
grep-verified. (#29–#35 CONFIRMED compiling + playtested.)
- **Conveyors lay as STRAIGHT lines** (point 1): a belt drag now plans a single straight run along the
  DOMINANT axis (no auto-corner) — easier to pull a clean line; to turn, do a second drag. New `ReplanStraight`
  (rebuilds the plan from the stroke start each move) replaces the L-path `PlanPath` (removed). Belts flow in
  the drag direction. Rail keeps its L-path drag (so you can corner) — the corner now LOOKS right (below).
- **Track corners curve + junctions meet** (point 2): NEW `PlaceholderArt.RailMask(mask)` — a track sprite
  shaped to the tile's NEIGHBOURS (N=1/E=2/S=4/W=8, cached per of 16). A perpendicular pair → a CURVED quarter
  corner (rails meet the edge centres so they line up with straights); opposite pair → straight; 3 → T; 4 →
  cross; isolated → straight. `RailTile.Update` computes its mask (station lanes count via `RailNet.IsRail`)
  and sets the sprite only when the mask changes (cheaper than the old per-frame rotate). So bends look like
  bends and where tracks meet they visibly join. (Station through-lanes still draw a straight E–W rail.)
- **Recompile check (intended):** drag a belt — it lays a straight line in the drag direction; drag again to
  turn. Lay a track L / cross / T → the bend shows a curve, the crossing/T shows rails meeting; track meeting
  a station platform joins cleanly.

## #35 Audit pass: leak fixes, perf, cleanup (+ a held-off bit) (2026-06-28)
Autonomous top-to-bottom review (4 parallel reviewer agents) + fixes. **NOT yet Unity-compiled by Claude**
(Editor open) — static-checked, brace-balanced, grep-verified. (#29–#34 CONFIRMED compiling; user playtested
through #34.) **Fixed:**
- **Material LEAK (4 sites):** every `LineRenderer` did `new Material(Shader.Find(...))` (wires, route lines,
  reach ring, line preview) — Unity never GCs those. Now one shared `PlaceholderArt.LineMaterial()` (they
  colour via the renderer's start/endColor, so sharing is safe). Was a per-wire / per-vehicle leak.
- **Phantom rail occupancy across Play sessions:** `RailGraph.Occ` / `RailNet.StationLane` are static and
  survive when domain-reload-on-play is off → stale "cell occupied" → deadlocked trains / stuck-red signals.
  Added `RailGraph.Reset()` + `StationLane.Clear()` at `GameBootstrap.Start`, and `RailGraph.Clear(cell)` in
  `RailTile.OnDisable` / `Depot.OnDisable` (demolishing track/station under a train frees the cell).
- **PERF — Signal lamp:** each signal walked up to 512 cells EVERY frame for its red/green lamp; now throttled
  to ~5×/sec (cosmetic).
- **PERF — blueprint ghosts:** belt/rail plan-ghost rebuilds (a `Physics2D.OverlapPointAll` per planned cell)
  ran every frame even when idle; now gated behind a dirty flag (rebuild only when the plan changes).
- **PERF — HUD (2 hot panels):** `DrawStatus` (starved/backed/monument scan of `*.All` ×2-3) and
  `DrawResourceFinder` (`ResourceNode.All` × materials + collector scan + string alloc) recomputed EVERY
  OnGUI pass (2–4×/frame); both now cache once per frame in `Update` (like `_totals`) — OnGUI just draws.
- **Held-off bit added — platform hold:** a train now KEEPS its platform cell while loading (releases on
  departure), so a second line's train can't pass through an occupied platform. (Build a passing loop.)
- **Cleanup:** removed dead `RouteVehicle._railPts` (built each leg, never read) + dead `DragBeltPath`;
  null-guarded `InventoryHud.HasStorage`; fixed the stale research-cost comment (data is 12→600→1600→3600).

### ⚠️ DEFERRED ISSUES (found in the audit, NOT yet fixed — verify/fix when the Editor can recompile)
- **PERF (HUD, remaining):** `DrawTopBar` still builds chip strings + `CalcSize` every OnGUI pass, and the
  minimap/map redraw all `*.All` dot loops every pass — both cacheable but lower-priority than the two done.
- **PERF (power):** `PowerNet.Rebuild` allocates `new float[nComp]`×2 + `new List<Battery>[nComp]` every frame
  (it runs every frame). Reuse cached arrays sized to a high-water mark.
- **PERF:** `BuildController.SolidBuildingAt` uses `Physics2D.OverlapPointAll` (allocs) + up to 9 `GetComponent`
  per hit. Convert to a NonAlloc overload + a reused buffer (left alone — Unity 6 overlap-API choice needs a
  compile to confirm).
- **BUG (rail):** two trains head-on on a single bidirectional track DEADLOCK with no detection/timeout (amber
  forever). Intended mitigation is signals/one-way loops — but consider a wait-timeout re-path or a warning.
- **BUG (signal):** `Signal.BlockAheadOccupied` follows ONE branch via `NextAlong` — at a junction it can miss
  a train on the other branch (false-green LAMP only; the train's own gating is correct). Fine on non-branching
  track. Document or walk all branches.
- **NIT:** merger fed by BUILDINGS on multiple sides isn't round-robin-fair (belt inputs are); `Battery`
  discharge is greedy (battery[0] does all the work — uneven Flow display); `RouteVehicle.b` property unused;
  `Colony.foodItem/waterItem` + `Economy.FoodPoints/SpendFoodPoints/FoodPointsIn` + `InventoryHud.food/water/
  meatItem` look like dead survival-era wiring (verify with a usage search, then delete).
- **NIT:** `PlaceholderArt.Ground(baseCol)` caches and ignores `baseCol` on later calls (only called once now,
  but a latent trap).

## #34 RAIL polish: one-way signal routing + live block lamps, track blueprint, through-stations, track art (2026-06-28)
Four rail refinements. **NOT yet Unity-compiled by Claude** (Editor open) — static-checked, brace-balanced,
grep-verified. (#29–#33 CONFIRMED compiling + playtested.)
- **Signals make the train take the legal route** (point 1a): `RailNet.FindPath` is now SIGNAL-AWARE — it
  won't enter a signal cell against its facing direction. So on a one-way loop the path runs the legal way
  round (the train keeps flowing, doesn't double back); the runtime one-way check in `RouteVehicle` is now a
  belt-and-braces backstop. *(Caveat: a one-way signal on a non-loop makes the reverse leg unroutable → it
  falls back to a straight line; build a LOOP for one-way running.)*
- **Live block lamps** (point 1b): a `Signal` now drives its own lamp every frame — it walks the track ahead
  (following turns) to the next signal and shows **RED while any train occupies that block**, GREEN once the
  train clears the next signal. New `RailGraph.AnyTrainAt`. (Removed the train-driven `SetClear`.)
- **Track blueprint placement** (point 2): rail is laid like belts now — **drag to plan** (cyan ghosts, no
  cost), **click to build**, right-click cancel — and only **90° L-paths** (no diagonals), so track stays
  tidy. New `_railPlan` + `PlanRailPath`/`BuildRailPlan`/`RebuildRailPlanGhosts`; `RailCellFree` helper.
- **Through-stations** (point 3): the Station is now a **3×1 platform with a TRACK LANE running straight
  through it** (east–west). Its footprint cells register in new `RailNet.StationLane` (counted as rail by
  `IsRail`/`FindPath`/`RailNear`), so trains route THROUGH or stop. Visible track is drawn over the platform
  deck; belt IN = south, OUT = north. Root transform kept uniform (collider sized explicitly) so port markers
  aren't distorted. *(Lane is fixed E–W — no rotation yet; a train stopped at a platform briefly frees its
  cell during loading, so two lines sharing one platform could overlap — build a passing loop.)*
- **Track ART** (point 4): NEW `PlaceholderArt.Rail()` — two steel rails over wooden sleepers on a ballast
  bed (was a grey "broken rock" block). `RailTile` + station lanes use it; rails orient to neighbours
  (incl. station lanes via `RailNet.IsRail`). Dropped the roguelike Station/Track skins (drawn procedurally now).
- **Recompile check (intended):** make a one-way loop (track circle + signals all facing the flow) with 2+
  stations on it → the train circulates the loop instead of reversing; a signal goes RED while a train is in
  its block, GREEN after. Lay track by drag-plan-then-click (90° only). A station shows a platform with rails
  running through it; a train stops on it or passes straight through. Track looks like track.

## #33 Multi-stop train LINES (pass-through) + strict station I/O gating (2026-06-28)
Follow-ups the user OK'd after #32. **NOT yet Unity-compiled by Claude** (Editor open) — static-checked +
grep-verified. (#29–#32 are CONFIRMED compiling clean + were playtested.)
- **Multi-stop LINES** (`RouteVehicle` reworked from a/b → `List<Depot> stops`). A vehicle visits the stops
  in a loop. Travel between stops is the #32 track-gated movement, so it **passes stations not on its line**
  (and passes line-stations that aren't its current target) — real pass-through. **Load/unload model:** stop 0
  is the PICKUP (loads its commodity up to capacity); every other stop is a DROP-OFF (receives what fits, the
  rest rides on). Predictable, no ping-pong, and a 2-stop line == the old A↔B shuttle. One commodity per line.
- **Line-building UI:** "+ Add line" → click each stop in order → click the FIRST station (or right-click) to
  close the loop. An **amber preview** traces the stops + cursor while building (`UpdateLinkPreview`). Station
  panel now lists the LINES serving it (N stops · commodity) + "✕ Remove a line". `a`/`b` kept as
  first/last-stop properties + `Serves(d)`/`StopCount` so existing reads still work; `Depot.Role` → "On a line".
- **Strict 2-in / 2-out station gating** (the markers are now ENFORCED): belts deliver IN only on the station's
  SOUTH edge (moving north into a bottom-row cell) and take OUT only on the NORTH edge (a belt north of a
  top-row cell). New `Depot.IsInputDeposit`/`IsOutputPull` (computed from the footprint's min/max row); wired
  into `Belt.cs` deposit + pull + connectivity (depots are no longer omnidirectional). Track still connects on
  the EAST ▮ marker. So a station reads as: track east, belt-in south, belt-out north.
- **Known partial / next:** true **through-platform track** (rail running THROUGH the 2×2 footprint, vs the
  train pulling up adjacent) isn't done — the train stops adjacent and passes adjacent. Per-stop load/unload
  CONFIG (so any stop can be a pickup, not just stop 0) is a future nicety.
- **Recompile check (intended):** build 3 Stations, lay track linking them, set the first's item + belt goods
  into its SOUTH edge; '+ Add line' → click stops 2,3 → click the first to close. The vehicle loads at stop 1,
  delivers along 2→3, loops; belts only connect on south(in)/north(out). It passes a 4th station that's not on
  the line. Belt into the wrong side → no connection (by design).

## #32 RAIL system overhaul: signals (one-way + blocks), no-crossing, gap-fix, bigger stations (2026-06-28)
Deep pass on trains, modelled on Workers-&-Resources. **NOT yet Unity-compiled by Claude** (Editor open) —
static-checked + grep-verified.
- **Long-route "goes straight" bug FIXED** (point 2). Root cause: rail was placed one tile per cursor cell,
  so a fast/diagonal drag skipped cells → the 4-neighbour BFS saw a broken track → fell back to straight
  line. Rail drag now fills a continuous ORTHOGONAL L-path (`LayRail`/`LayRailPath`), so the track is always
  4-connected and the path search succeeds. Also bumped `RailNear` radius 2.6→3.2 (for the bigger station).
- **Trains no longer cross over** (point 1a): NEW `RailGraph` occupancy (cell → train). A vehicle CLAIMS the
  cell it's entering (holds 1–2) and releases behind it, so two trains never share a cell — junction crossings
  are mutually-excluded automatically, no signals needed. Releases on demolish (OnDisable).
- **SIGNALS** (point 1b) — NEW `Signal.cs` + a "Signal" tool (place on a track cell, R aims it; green=clear,
  red=block occupied). Enforced in `RouteVehicle`: a train may pass a signal only when **travelling its way**
  (one-way → two opposing signals on parallel tracks = a one-way loop) AND only when the **block ahead** (path
  cells up to the next signal) is **clear of other trains**. So you plan one-way routes + one-train-per-block.
  A waiting train tints **amber**. (Faithful W&R caveat: a one-way signal blocks the return trip on a single
  bidirectional track — build a loop. Documented in the tooltip.)
- **`RouteVehicle` rewritten** to cell-gated track following (`TravelTo`/`CanEnter`/`TravelDirAt`/
  `BlockAheadClear`/`BuildRailRoute`/`ReleaseHeld`); ALL tiers (incl. Cargo Drone) follow track now; straight-
  line fallback kept for un-tracked routes.
- **Bigger stations** (point 3): Station is now **2×2** with **2 belt-IN markers (cyan, south)** + **2 belt-OUT
  (green, north)** + a **▮ track marker (east)** showing where to run rail. Cost bumped (wood 12/stone 8).
  **PARTIAL:** I/O is still accepted on ANY side (markers are guidance, not yet strict gating), and trains
  STOP at their route endpoints — true **pass-through stations need multi-stop line routing (a follow-up)**.
- **Wiring:** BuildingKind +Signal; BuildController +rail-path-fill +`UpdateSignalPlacement` +signal in
  under-cursor (prefers the signal over the rail it sits on) / demolish; GameBootstrap +Signal buildable
  (stone 1) + enlarged Station; InventoryHud +Signal menu category + rail/signal banners.
- **Recompile check (intended):** lay a long, turning track between two Stations → the train follows ALL of
  it (no more straight-line shortcut). Two routes sharing a junction → one train waits (amber) while the other
  crosses. Place a Signal facing the travel way → train passes when the block's clear, holds (red) when a
  train's in the block ahead. Place opposing signals on a two-track loop → one-way running. Station shows
  2 cyan-in / 2 green-out / 1 track marker.

## #31 Playtest batch: merger fix + ports, belt BLUEPRINT placement, RAIL track + trains follow it (2026-06-28)
Four playtest asks (belts/mergers/conveyors/trains) + a crash fix. **NOT yet Unity-compiled by Claude**
(Editor open) — static-checked + grep-verified (no dangling refs).
- **Merger now WORKS (pull, round-robin).** Root cause: feeders PUSHED into the merger in fixed cell order,
  so the same input always won its single slot and the others starved ("items don't go in the sides").
  Now the merger PULLS its three input sides (back/right/left) round-robin (`MergerPullFromBelts`,
  `_mergeNext`); `TryDepositTo` no longer pushes into mergers; a feeder only counts if it POINTS INTO the
  merger (a belt that doesn't is never touched → fixes "stops an adjacent belt"). `_blocked` is merger-aware
  (no false yellow while waiting to be pulled).
- **Merger/splitter ghost shows IN/OUT** (point 1): the placement ghost now draws the junction's ports
  (green ▸ out, cyan in) via `ShowGhostJunctionPorts` (parented to the rotated ghost, follows R), so you can
  orient it before placing. Banner updated ("green=out, cyan=in").
- **Belt BLUEPRINT placement** (point 3): plain belts are no longer instant. **Drag = sketch a plan** (cyan
  ghosts, no cost), **click = build it**, **right-click = cancel**. New `_beltPlan` (cell→dir dict) +
  `_planGhosts`; `PlanPath`/`PlanCell`/`BuildBeltPlan`/`ClearBeltPlan`/`RebuildPlanGhosts`. Splitters/Mergers
  stay one-click. (Single belt = drag a short run, or tap to plan 1 tile then tap to build.)
- **RAIL TRACK + trains follow it** (point 4): NEW `RailTile.cs` (drag-laid like pipes; reserves its cell via
  `WorldGrid.Rails`; auto-orients H/V) + `RailNet` (BFS path + `RailNear` station hook-up). `RouteVehicle`
  now PATHS along the laid track between stations (`BuildRailPath`/`TravelTo`/`NearestRailIdx`), drawing the
  polyline; **falls back to a straight line when no track connects** (old routes unaffected). New "Track"
  buildable (BuildingKind.Rail, stone 1, Logistics menu). **Signals are the stated next step — NOT built yet.**
- **Reserve-block wired** (SR-P4 groundwork): `WorldGrid.IsReserved` now actually blocks — belts (`EnsureBelt`
  + ghost) and buildings (`FootprintBlocked`) can't sit on rail, and rail can't sit on them.
- **Crash fix:** the build menu's "Recent" row threw `Collection was modified` every OnGUI once you clicked a
  recent entry (it placed a building → mutated `_recent` mid-foreach). Now iterates a snapshot.
- **Menu-category fix:** **Battery** (from #30) and **Track** weren't in any build-menu category, so they
  never appeared — Battery was un-buildable in the last playtest. Added Battery→Production, Rail→Logistics.
- **Tunables (verify in play):** wire length 8 (#30); merger still 1-cell (round-robins, doesn't widen
  throughput — that's by design); rail cost stone 1×2.5; train speed unchanged (cart→train tiers). 
- **Recompile check (intended):** place a Merger → its 3 in-sides + out show on the ghost; feed it from two
  belts → both lines now flow through evenly; a parallel belt beside it is untouched. Drag a belt run → cyan
  blueprint → click → it builds; right-click cancels. Lay Track between two Stations → the route vehicle
  follows the rails (polyline drawn); remove the track → it reverts to straight. Battery + Track now appear
  in the build menu.

## #30 ENERGY REDESIGN #2: WIRED power grid (player-drawn cables) + batteries + smart draw (2026-06-28)
User scrapped the proximity-radius supply model: power now flows along **wires the player draws** between
nodes. **NOT yet Unity-compiled by Claude** (Editor open) — static-checked; no dangling refs (grep-verified).
- **NEW `PowerNode.cs`** — a component on every power participant (Generator/Pole/Battery/Consumer) with a
  `maxConnections` cap (poles/gens/batteries **4**, a machine **1**) + an adjacency `links` list. Plus
  **`PowerWire`** (a LineRenderer per cable; cyan = live, grey = no source; auto-created/destroyed with the
  link). A node drives `PowerNet.EnsureFresh()` each frame so batteries integrate even when nothing queries.
- **NEW `Battery.cs`** (BuildingKind.Battery, Bronze) — a wired store: `Absorb` surplus / `Draw` on deficit
  (capacity 200, rate 30; tints by charge). Smooths brownouts so a small generator covers a peaky base.
- **`PowerNet.cs` REWRITTEN** — wire-graph solver (BFS components over `PowerNode.links`, no more spatial
  union-find / ranges). Per network: gen (+battery discharge) vs demand → factor (1 / brownout floor 0.35 /
  **0 if no source or unwired**); surplus charges batteries. New `MaxWireLength = 8` (a single cable's reach
  → chain poles to span). Kept the public API (`Active`/`FactorOf`/`TotalGen`/`TotalDemand`/`EnsureFresh`)
  + added `TotalStored`/`TotalCapacity`. **Bronze-gated** still (Active = Age ≥ 2; pre-Bronze runs free).
- **Smart draw** (`WorkshopBuilding.CurrentDraw`): **full** `powerDraw` while actively processing, a **10%
  trickle** while stalled (starved / output-full), **0 when switched OFF** (the existing Pause toggle now
  cuts power to zero — the "turn a machine off" the user asked for). Feeds PowerNet demand (no flat draw).
- **Wiring UX** (mirrors Station route-linking): select a power building → **🔌 Connect wire** in its panel
  → click the target. Validated (cap + range + dup) with a Toast on failure; Esc/right-click cancels. New
  selected-panel block shows wires `n/max`, battery charge ▲/▼, **Connect** + **✂ Disconnect** for ANY node.
  Top power readout gained a 🔋 stored/capacity figure. NoPower hint reworded to "wire to a Generator/Pole/Battery".
- **Wiring/plumbing:** `PowerPlant`/`PowerPole` lost their ConnectRange/SupplyRange + the pole's supply ring
  (radius gone), gained a PowerNode. `BuildingDefinition` +Battery kind +batteryCapacity/Rate (connectRange/
  supplyRange kept as inert legacy). ConstructionSite +Battery case. BuildController: wire-link mode +
  Battery/Pole added to under-cursor/solid/copy/demolish/selectedDef (**poles are now demolishable** — they
  fell through the rdef chain before). GameBootstrap: Battery buildable (copper 6 + bricks 4) + descriptions.
- **Tunables (verify in play):** wire length 8; pole/gen/battery cap 4, machine cap 1; battery 200 cap / 30
  rate; idle draw 10%; brownout floor 0.35. **Coal still deferred** (SR-P5 Boiler/steam).
- **Recompile check (intended):** Bronze+, place a Wood Gen + a smelter → smelter shows "⚡ No power" until
  you select the gen → Connect wire → click the smelter (cable appears, smelter runs). Chain poles past 8
  cells. Place a Battery, wire it in → it charges on surplus (▲), discharges to cover a stalled gen (▼).
  Switch a machine OFF (Pause) → its draw drops to 0 (grid demand falls). Demolish a pole → its wires vanish.

## #29 ROGUELIKE sprite pack wired into SpriteDatabase (2026-06-28) — CONFIRMED COMPILING
Wired the Kenney **Roguelike** pack (`Assets/Roguelike/`) into the #28 sprite layer. **Claude-verified clean
compile** (Editor recompiled 11:40, `error CS` count 0; the sheet imported via TextureImporter). In-game look
not yet eyeballed by the user.
- **Sheet → Resources:** copied `roguelikeSheet_transparent.png` (+meta) to `Assets/Resources/Roguelike/`
  (original pack untouched). Meta edited on the COPY: fresh GUID (no dup), **PPU 100→16** (a 16px tile = 1
  grid cell, matching the procedural 1-unit sprites), **filterMode→Point** (crisp), pivots→center. It's a
  sliced multi-sprite (57-wide grid, sub-sprites `roguelikeSheet_transparent_<row*57+col>`).
- **`SpriteDatabase` extended:** `LoadExternal` now tries the pack sheet first (lazy `Resources.LoadAll`,
  indexed by sub-sprite name) then `Resources/art/<name>`; unmatched → procedural fallback (so nothing breaks
  if the sheet's absent). Added an `Item` icon map, an `RL(col,row)` helper, and `ApplyRoguelikeSkin()` (the
  hand-picked tile table), called right after `Seed()` in GameBootstrap (+ re-resolve of skinned item icons).
- **Skinned (verified against the art):** Kiln/Campfire/Toolmaker/Potter/Basic+Advanced Smelter/Coal Gen
  (forges/anvil/furnaces), Warehouse/Granary (chest/sack), Research Lodge (bookshelf), Station (minecart);
  the 5 resource nodes (tree/boulders/clay/copper-ore); a few item icons (ingots, scroll/books). **Belts kept
  procedural** (the conveyor rotates correctly; the pack's arrows point the wrong way). Everything else =
  procedural fallback. To extend: add `RL(col,row)` entries in `ApplyRoguelikeSkin()` (read col/row off the sheet).

## #28 SPRITE VISUAL LAYER — central SpriteDatabase abstraction (no assets yet) (2026-06-28)
Refactor: route ALL entity rendering through a central sprite layer so a future pack drops in with no other
code change. **No external assets added; visuals are identical** (every Resolve falls back to today's
procedural shape). No gameplay logic changed. **NOT yet Unity-compiled by Claude** (Editor open) — static-checked.
- **NEW `SpriteDefinition`** (`name` + `PlaceholderShape` fallback) — every building & item carries one
  (`BuildingDefinition.sprite`, `ItemDefinition.sprite`). **NEW `SpriteDatabase`** (static): `Resolve()` =
  external sprite from `Resources/art/<name>` if present, ELSE the procedural fallback (cached). Central
  type→name maps: **Building** (displayName→name), **Resource** (item id→name), **Belt** (type→name);
  `Seed()` pre-fills them with the expected filename = sanitised type name (e.g. "Basic Smelter" → `basic_smelter`).
  Helpers `ForBuilding/ForBelt/ForResource/ForItem`.
- **Routed through the DB:** every building Spawn (Production/Workshop/Storage/Depot/Research/PowerPlant/
  PowerPole/WaterPump/Bridge/Pipe/ConstructionSite → `ForBuilding(def)`), Belt (Spawn/ConvertTo → `ForBelt`),
  belt item dots + item icons (`ForItem`), resource-node spawns (`ForResource`), and the build ghost
  (building + belt) so the preview matches. Item icons now resolved via the DB (set each item's fallback shape).
- **Deliberately left procedural** (functional/scene visuals a pack doesn't replace, documented): I/O port
  arrows (Ports), status dots (Status), the machine work-FX (MachineWorkFX), route vehicles (RouteVehicle),
  player + world ground, and translucent preview ghosts for bridge/pipe + the ghost initial frame.
- **To skin the game later:** drop sprites into `Assets/Resources/art/` named per the seeded map (or set names
  in `SpriteDatabase.Building/Resource/Belt`), call `SpriteDatabase.ClearCache()` — everything re-skins.
- **Recompile check:** game looks identical to before (all fallbacks = current shapes); no errors.

## #27 Playtest feedback batch: belt names, power@Bronze, costs, research 10x, UI fixes (2026-06-28)
Acting on a playtest feedback list. **NOT yet Unity-compiled by Claude** (Editor open, log stale) — static-checked.
1. **Belt names** — placed belts hovered as "Building". Belt now carries `DisplayName` (Wooden/Conveyor/Geared/
   Steel Belt, Splitter, Merger), threaded through Spawn/SetTier/ConvertTo + def.displayName; HoverText shows it.
   Also fixed generic "Power Plant" → real generator name, + Power Pole hover.
2. **Selected panel cut off** (smelter ⚠ warning hidden) — panel is now taller, bottom-anchored (still clears
   minimap), and the info is in a SCROLL VIEW with Demolish/Close pinned below → nothing clips.
8/7. **Electricity introduced in BRONZE age** (was age 0) — `PowerNet.Active` = Colony.Age ≥ 2; pre-Bronze
   requiresPower machines run FREE. Wood Generator + Power Pole unlockAge → 2. This also fixes "copper smelting
   didn't work": a Tribal smelter no longer needs power/heat (the recipe path itself is correct — verified).
3. **F-key age skip also unlocks tech** — `Research.DebugUnlockTo(age)` force-purchases every tech reachable by
   the new age (cascading prereqs); F3 now advances age AND unlocks its tech so it's testable.
4. **Research costs ×10** — Bronze 60→600, Iron 160→1600, Industrial 360→3600 (Tribal 12 kept as the quick opening).
5. **Build costs ↑** — one `CostScale = 2.5` loop bumps every buildable's build cost (recipe inputs untouched);
   starter kit raised to 32 wood / 26 stone so the opening still works. (Both tunable.)
6. **Top tips cut off by build menu** — build menu top 100→118 so it sits below the status + objective/tips bars.
- Tooling: confirmed (via the live Editor log) the prior power-network commit compiled clean before this batch.

## #26 ENERGY REDESIGN: power NETWORK (generators + poles) replaces the radius hearth (2026-06-28)
User scrapped the radius-hearth model: buildings are now powered by a **connected network of generators +
power poles** (early game onward), not proximity heat. **NOT yet Unity-compiled by Claude** (Editor open holds
the project lock) but fully static-checked (no dangling refs, braces balanced).
- **DELETED** `Hearth.cs`, `HeatField.cs`, and the old global `Power.cs` (its global Industrial-brownout role
  is superseded by the spatial network).
- **NEW `PowerNet.cs`** — spatial solver: generators + poles are nodes, linked within range (union-find →
  connected networks). Each network shares its generation among the consumers it supplies. A `requiresPower`
  machine: factor 1 if connected with enough gen, brownout `gen/demand` (floor 0.35) if oversubscribed, **0 if
  not connected** (hard-stop). Frame-guarded rebuild. Exposes `FactorOf(w)` + `TotalGen/TotalDemand`.
- **NEW `PowerPole.cs`** (BuildingKind.Pole) — relay node: `ConnectRange` (links poles/generators) +
  `SupplyRange` (powers nearby consumers); faint world-space supply ring. Single-click placement.
- **`PowerPlant` is now a network node** (ConnectRange 6 / SupplyRange 5) — a generator alone powers nearby
  machines; poles extend reach. Added a **Wood Generator** (burns Wood, output 40, age 0 — the early source)
  alongside the existing **Coal Generator** (60, age 3).
- **Seam unchanged in shape:** `WorkshopBuilding.EffectivePowerFactor` → `PowerNet.FactorOf` for
  `requiresPower` machines (Basic/Advanced Smelter, Kiln, Potter). `NoPower` + blue status + panel hint
  ("⚡ No power — connect a Generator/Pole") updated. Removed `DrawsPower`/`Power.EnsureFresh`.
- **Wiring:** BuildingKind Hearth→Pole; BuildingDefinition heatRadius → connectRange/supplyRange (requiresPower
  kept); ConstructionSite spawn case; InventoryHud Production category + power readout (PowerNet totals,
  "OVERLOADED"); buildables (woodGen + pole added, hearth removed); quest text. Belt-independent.
- **Tunables (balance, verify in play):** wood gen 40 / coal 60; workshop draw 10; pole connect 7 / supply 4;
  gen connect 6 / supply 5; brownout floor 0.35. Coal still deferred to SR-P5 (Boiler/steam).
- **Recompile check:** Build → Production shows Wood Generator + Power Pole; place a Wood Gen by a smelter →
  smelter runs (its supply ring covers it); move the smelter out of range → blue "no power" + stall; chain
  Power Poles from the gen to a distant smelter → it runs; let the gen run out of Wood → connected machines stop.

## #25 SR-P3: Stone-age hearth RADIUS energy (SUPERSEDED by #26) + headless compile-check tool (2026-06-28)
Energy phase 3 of the systems redesign + a way for Claude to self-verify compilation.
- **NEW `Hearth.cs`** (BuildingKind.Hearth): burns Wood (PowerPlant-style auto loop, no workers) and
  projects a **heat radius** (default 7) with a faint coverage ring. **NEW `HeatField.cs`**: static
  registry of lit hearths + `FactorAt(pos)` = 1 inside any lit hearth, else 0 (HARD-STOP, per user choice).
- **Seam wired:** `WorkshopBuilding.EffectivePowerFactor` pre-Industrial branch → `requiresPower` machines
  read `HeatField.FactorAt` (0 outside → stall); Industrial still uses `Power.Factor`. `requiresPower`
  set on the HEAT machines only: **Basic Smelter, Advanced Smelter, Kiln, Potter** (smelters/kilns — not
  every workshop, so the early economy isn't bricked). Hearth is age 0 so it's always available before them.
- **Feedback:** new "no heat" status (blue dot) + selected-panel hint "⚡ No heat — place a Hearth so this
  sits inside its ring", so a heat-stalled machine doesn't look like it's working.
- **Wiring:** BuildingDefinition gains `heatRadius`/`requiresPower`; BuildingKind.Hearth + ConstructionSite
  spawn case + InventoryHud Production category + buildables entry. Hearth def: Wood fuel, interval 2.5,
  radius 7, age 0, cost stone 6 + wood 4. Coal deferred to SR-P5 (lands with the Boiler that burns it).
- **NEW `Tools/compile-check.ps1`** — headless `Unity.exe -batchmode -quit` compile + `error CS` parse, so
  Claude can self-verify compilation when the Editor is CLOSED (it locks the project while open). Confirmed
  the whole prior blind stack already compiles CLEAN (read the Editor recompiled June 28 08:38 — no errors).
- **Recompile check:** Hearth appears in Build → Production; place + feed Wood → warm ring, lit; a Smelter/
  Kiln inside the ring runs, outside it shows the blue "no heat" status + stalls; let the hearth run dry → it
  darkens and machines in range stall. Belt-independent.

## #24 Smelters: Basic + Advanced (configurable multi-recipe), replacing 4 fixed buildings (2026-06-27)
Request: drop separate Copper/Iron smelters → a **Basic Smelter** (ore→bar, like the old ones) + an
**Advanced Smelter** (combine materials → alloy bars). User chose: **configurable** recipe (select in panel,
like the warehouse) + **keep Bronze/Steel** consolidated (no new alloys). **NOT yet Unity-compiled.**
- **NEW: configurable multi-recipe workshops.** `Recipe{output,outputPerCycle,processTime,inputs,unlockAge}`
  + `BuildingDefinition.recipes` (empty = legacy single recipe). `WorkshopBuilding` gains `recipes`/
  `ApplyRecipe`/`FirstUnlockedRecipe`/`CycleRecipe`/`DumpInBuffer`; the SELECTED recipe drives
  output/inputs/processTime so all existing logic (CanMake/ConsumeInputs/WantsInput/ports) works unchanged.
  Spawn picks the first age-unlocked recipe; switching dumps belt-fed inputs back to hand (no stranded items).
- **Basic Smelter** (unlockAge 1): recipes = Copper Ore+Charcoal→Copper · Iron Ore+Charcoal→Iron (both age 1).
  Replaces Copper Smelter + Iron Smelter.
- **Advanced Smelter** (unlockAge 2): recipes = Copper+Bricks→Bronze Plate (age 2) · Iron+Charcoal→Steel
  (age 3 — Steel stays Iron-age-gated via per-recipe unlockAge). Replaces Bronzeworks + Steel Foundry.
- **Rewired:** research age-gates now require Basic Smelter (Bronze) / Advanced Smelter (Iron + Industrial) —
  the delivered gate item (Study Scroll→copper, Schematic→bronze, Blueprint→steel) still enforces the real
  chain. Buildables list, objectives, item + maker descriptions all updated. Toolmaker/ore mines unchanged.
- **UI:** the workshop panel shows "Making: X from A + B" + a "↻ Change recipe" button when a smelter has a
  choice (cycles to the next age-unlocked recipe). Belt-independent change.
- **Recompile check:** build a Basic Smelter → it defaults to Copper, "↻ Change recipe" flips it to Iron
  (and dumps any loaded ore to hand); Advanced Smelter defaults to Bronze, offers Steel only at Iron age;
  age-gates still advance (build smelter + deliver the gate item).

## #23 SYSTEMS REDESIGN (energy/storage/resources/transport) — plan + SR-P0/P1 (2026-06-27)
Big logistics-first redesign (multi-agent analysis → cohesive plan; user signed off the decisions). Goal:
turn "place a machine, it just works anywhere" into "plan a central, powered, zoned hub fed by routed
supply lines." **NOT yet Unity-compiled.**

**Locked design decisions (user):** energy out-of-range = **hard-stop, processors-only**; **revive liquids +
real steam fluid** (pipes→Boiler(water+coal→steam)→Steam Engine); **add coal as a mined raw** fuel; carts =
**reserve+block first, path-following next**. Defaults adopted: reuse Depot/RouteVehicle; keep Inventory
single total-capacity; down-scope (not delete) universal warehouse to solid-goods; keep background biomes.

**Keystone architecture:** ONE energy seam `WorkshopBuilding.EffectivePowerFactor()` that radius(now)→
steam(mid)→electricity(late, existing Power.cs, DEFERRED) all flow through — energy evolves with zero
machine-code churn. ONE `WorldGrid.IsReserved(cell)` that every placement gate routes through so roads/rail
block belts/buildings uniformly. Existing Power.cs/PowerPlant.cs stay the inert Industrial target.

**Phase plan (each shippable + verifiable):**
- **SR-P0 (DONE here):** seam refactor, zero gameplay change. `EffectivePowerFactor` extracted (behaviour-
  identical: Industrial brownout else full speed). `WorldGrid.Roads/Rails` (MonoBehaviour dicts) + `IsReserved`
  (empty → false for now). Belt-independent.
- **SR-P1 (DONE here):** resource concentration. DELETED the map-wide `SpawnClustersInBiome` smear (+ its
  helper). Replaced the 3-corridor Region block with a data-driven `Zone()` table: **5 FEW/LARGE/DISTINCT
  single-resource zones** on their own corridors + biomes, far out (Clay 46/Copper 60/Wood 72/Stone 86/Iron
  100), disc radius ~30-34. Starter wood+stone basin kept; basin stays clear; SpawnNode still ClearAround per
  node. Coal deferred to SR-P3 (lands with the hearth that burns it). Belt-independent.
- **SR-P2 (next):** specialised storage — `StorageClass{Liquid,SolidGood,RawResource}` on ItemDefinition
  (`isLiquid` kept as shim), `accepts` on BuildingDefinition; Tank(pipe-only)/Warehouse/Silo presets; class-
  aware belt+pump guards; down-scope universal warehouse. (Touches Belt.cs I/O guards → after belt confirm.)
- **SR-P3:** Stone-age radius hearth power + COAL (item + Coal Mine + zone + fuel use). `heatRadius`/
  `requiresPower` on BuildingDefinition; new HeatField + Hearth (reuse PowerPlant burn loop + DrawRing);
  EffectivePowerFactor's pre-Industrial branch consults HeatField; processors hard-stop outside range.
- **SR-P4:** roads (RoadTile + WorldGrid.Roads) that reserve+block (IsReserved wired into all gates), carts
  straight-line first (P4a) then RouteVehicle waypoint-follow laid tiles (P4b). (Touches Belt placement guard.)
- **SR-P5:** liquids revival (re-add pipe/pump/booster to buildables) + steam (Boiler/Steam Engine, steam =
  isLiquid fluid via the fluid-generic WaterPump BFS); retune pump flow (#12).
- **SR-P6:** rail (RailTile + WorldGrid.Rails) high-capacity, RouteVehicle requiresTrack, Depot stations.
- **SR-P7 (DEFERRED, do NOT build):** electricity switch-on — flip EffectivePowerFactor to Power.Factor at
  Age 4 + re-add Coal Generator. Seam pre-laid; proves the radius layer evolves into Power.cs.

**Recompile check for SR-P0+P1:** game runs unchanged (no power/behaviour difference — Power.Active still
gates brownout); new map shows ~5 LARGE distinct single-resource regions far from spawn (not sprinkled
everywhere), corridors reach each, nodes on cleared land, basin open + starter wood/stone present.

## #22 Splitter is now 1→3 (was 1→2) (2026-06-27)
Request: splitters should split 3 ways. Extended the splitter from 2 outputs to 3 — forward (`dir`),
right (`RotateCW`), and **left (new `RotateCCW`)**; input stays the back edge. **NOT yet Unity-compiled.**
- Distribution: `_splitToggle` (bool) → `_splitNext` (round-robin index 0/1/2). On handoff it tries the
  preferred output then falls through to the next available ones (never stalls), advancing the preference
  on each successful send for an even 3-way split. New `SplitDir(i)` maps 0=fwd/1=right/2=left.
- `LeadHeadLimit` (max over 3 outputs) + `HasForwardTarget` (OR over 3) + `AddPortMarkers` (3 output arrows
  N/E/W + 1 input notch S) all updated. `DistToSink` still follows the primary output (`dir`) for ordering
  (heuristic only — correctness-neutral).
- Wording synced everywhere (1→2 → 1→3): GameBootstrap splitter def + research desc, BuildingDefinition
  tooltip. Merger unchanged (N→1). Feed THREE machines from one supply line now.

## #21 Collector "machine working" animation (cosmetic) (2026-06-27)
Request: "resource buildings should show their workers gathering as a UI feature." This collides with the
canon ("fully automated machines, no workers" — the Harvester NPC was deliberately removed in #18), so it
was surfaced + clarified. User chose the **no-humans, machine-working** option, so the canon is UNCHANGED.
- **NEW `MachineWorkFX.cs`** — a purely cosmetic component: a pumping mechanical **arm/drill** aimed at the
  resource node the collector taps, plus **dust puffs** kicked out on each gather cycle. Procedural sprites,
  no assets. Standalone GameObject at the building position (not parented — the building transform is
  non-uniformly footprint-scaled, which would skew children); the collector Destroys it in OnDisable.
- **Zero logic / zero balance impact** — driven entirely by `ProductionBuilding`'s existing gather loop:
  `SetWorking(working)` + `SetTarget(node)` each frame, `Strike()` on each `Pulse()` (gather tick). The
  arm hides when the collector isn't actually extracting (paused/starved/backed-up). No workers, no pathing,
  no NPC agents — keeps the auto-machine design. Existing white gather-flash + status dot retained.
- Scope: collectors (the buildings that "gather resources"). Reusable on workshops later if wanted.
- **NOT yet Unity-compiled** (blind build) — recompile + eyeball that collectors show a pumping arm + dust
  while working, nothing while idle, and that demolish cleans up the FX (no leftover arm).

## #20 BELT REDESIGN — P0+P1: central deterministic sim + continuous flow (2026-06-27)
Production-grade belt redesign (multi-agent analysis → phased plan; user signed off: ship-hundreds-now,
single-lane, add a 5th hover tier). This is the ENGINE increment. **NOT yet Unity-compiled** (blind build).
- **NEW `BeltSim.cs`** — one central, fixed-timestep (1/60), deterministic simulation drives ALL belts;
  replaces every belt's individual `Update()` tick. Motion is now provably independent of GameObject
  update order (kills the historic "items stuck / throttle to half-rate" class of bug at the source).
  Created in GameBootstrap (`BeltSim.Ensure()`). Catch-up cap 12 steps (covers 4× speed at ~20fps).
- **`Belt.cs` rewritten** from a per-cell `(item, count)` BUCKET into CONTINUOUS per-item positions:
  each cell holds `List<BeltItem>{def, p∈[0,1], entryEdge}`; items advance at `speed = 1/interval`
  cells/sec, the lead capped a `MinGap` behind the LIVE downstream tail → **deterministic spacing, no
  overlaps, smooth cross-tile motion, correct fast↔slow backpressure.** Step passes: snapshot(connectivity)
  → advance → handoff → pull. Belts processed **downstream-first** (`Belt.DistToSink`, cached on belt-set
  change) so throughput stays **exact**.
- **Throughput + visual speed PRESERVED EXACTLY**: `MinGap = 1.0` (one item per cell) ⇒ throughput =
  (1/interval)/MinGap = 1/interval, i.e. the existing 30/60/120/240 per-min caps are unchanged. MinGap is
  the SINGLE density/throughput knob — lowering it packs denser (Factorio look) but multiplies throughput
  by 1/MinGap and needs an economy re-tune. Documented one-liner; deferred.
- **Verified before commit (multi-agent + a Python sim):** compile-correctness / I-O semantics (byte-identical
  deposit+pull rules) / integration (all callers compile) all PASS. The reviewers caught a throughput
  shortfall (snapshot was stale-by-one-step → ~3–6%, worse on fast/long belts); FIXED by switching the
  forward read to LIVE + downstream-first ordering. A faithful Python sim confirms: exact throughput
  (≤0.5% drift) across all tiers AND min-gap = 1.000 (no overlap) in flow, backpressure, and wrong-order
  cases. Splitter/merger, all building I/O, connectivity, liquid exclusion carried over verbatim.
- **Compatibility shims** kept (`item` = lead type, `count` = item count, `CanAccept`, `Receive`) so no
  external caller changed. Old `_timer`/`_inDir`/mutable `item`/`count`/`capacity` removed.
- **NEXT (queued, all verifiable increments):** P2 event-driven connectivity (kills the per-step 256-cell
  walk + makes the downstream-first order refresh on sink edits) · P3 procedural auto-tiling corner/T/cross
  sprites · P4 multi-cell drag preview + commit-on-release + snapping · P5 data-driven tier table + the new
  5th hover/glow tier (use a 60-divisible interval, e.g. 450/min) + multi-age visuals + animation · P6 debug
  overlay. P7/P8 (segment compression, instanced rendering for thousands, two-lane belts) deferred per user.

## #19 playtest batch — belt-stall ROOT CAUSE + map screen + gather popup + minimap legend (2026-06-27)
Second Unity playtest (after the #18 cleanup, which compiled & ran). Four items. **NOT yet Unity-compiled** (blind build — recompile + retest).
- **Belts STILL jammed — found the real root cause (the fair-share fix #17 was a different layer).** `Belt.Receive`
  (and all 4 `PullFromNeighbour` blocks) reset `_timer = 0f` on EVERY item arrival, even when piling. On a
  continuously-fed straight run, whichever belt `Update()`s first each frame keeps zeroing the *downstream*
  belt's dwell timer, so the lead never matures → the cell throttles to ~½ rate and items stack to capacity
  (4). The `4984d8a` piling renderer made that long-standing pile *visible* (1 dot → up to 4 nose-to-tail), so
  it newly read as "stuck since the conveyor change" — but the flow logic was unchanged; the bug predates it.
  **Fix:** new `Belt.GainFrom` only (re)starts the dwell timer + entry edge when the cell was EMPTY (a fresh
  lead item); piling a follower keeps the lead's dwell → throughput is now Update-order-independent, full rate.
  Also unified `UpdateDots` so the lead animates by its timer and followers pack behind it (no pop, holds at
  exit when blocked). Pure timer/render change; push gating (1 item / interval) untouched.
- **Map (M) reworked from "zoom camera all the way out" → a real pan/zoom MAP SCREEN.** Removed the overview-zoom
  from `CameraFollow` (now wheel-zoom only). `M` (owned by `InventoryHud`, a modal Panel) opens a full square
  view of the explored fog with live building/resource/player dots; **drag to pan, wheel to zoom toward the
  cursor (1–8×), M/Esc to close.** Reuses the minimap's fog tex + dot logic. World camera untouched (HUD overlay).
- **Hand-gather "+1" popup.** New `GatherPopup` (sibling of `Toast`): a small bold world-anchored "+1 <item>"
  in the item's colour floats up from the node and fades (~0.85s) on each manual harvest; also added the chop
  `Nudge()` recoil to manual hits. `PlayerGatherer` fires it; `InventoryHud.DrawGatherPopups` renders it.
- **Minimap legend was illegible + clipped.** Was `<size=10>` left-aligned in a map-width rect hugging the
  screen's right edge → clipped. Now `<size=13>`, right-aligned in a wider rect that extends LEFT, sat higher
  above the map. Footer/help wording updated: **M = world map · N = minimap on/off**.

## #18 Audit cleanup — part 1: remove the Harvester NPC + stop silent demolish loss (2026-06-26)
Acting on the deep multi-agent systems audit. **Recompile.**
- **CRITICAL fix — Harvester worker-NPC removed (audit C1).** Deleted `Harvester.cs` and the per-collector
  `_cutter` spawn/destroy in `ProductionBuilding` — the one LIVE worker/agent (a man-like NPC that walked out
  to "chop" on every collector), the sole flagrant violation of "no workers / fully automated machines."
  Collectors keep their white gather-flash as the "doing something" cue; reworded the stale "worker walks there"
  comments.
- **CRITICAL fix — demolish no longer silently destroys stored goods (audit C2).** `BuildController.
  DemolishSelected` now dumps a building's Buffer/InBuffer/Store/depot.store into the player's (unlimited)
  carried inventory before Destroy, then refunds half the build cost as before. No more losing a full
  Warehouse/Station/smelter to an accidental X/Delete.
- **Part 2 (done):** stripped `Colony`'s population/comfort/worker API (Population, FreeWorkers, Comfort,
  Happiness, UnmetComforts, Capacity, DebugAddPopulation, SetStartingPopulation) + the inert `Productivity`
  multiplier coupling in ProductionBuilding/WorkshopBuilding.
- **Part 3 (done):** DELETED the dead worker/population classes — `HousingBuilding`, `ConstructionYard`,
  `TransportHub`, `Transporter` (4 files) — plus their `BuildingKind` enum values (Housing/Logistics/Build),
  `BuildingDefinition` fields (houseCapacity/builderSlots/mechanical/logisticsRange), the GameBootstrap
  helpers + housing defs, and every wiring (SpawnFinished cases, BuildController GetComponents, InventoryHud
  selected-panel/minimap/hover/Describe/Cats branches). Adversarial compile-check PASS, 0 dangling refs.
- **Remaining cleanup (queued):** shelved food/liquids/comfort chains (items + makers + `Economy.FoodPoints`
  + Pipe/WaterPump); the inert `maxWorkers` field (high-touch param threading); gate the sandbox F-key footer;
  static-registry reset (F42); raise ore searchRadius (F10/F41); F3 debug age-desync (F37).

## #17 playtest FIXES — fair-share input stall + building reach indicator (2026-06-26)
First real Unity playtest of #17 surfaced two issues; root-caused by a diagnostic workflow, fixed + 2-lens
verified (the verify caught a 3+-input edge in the first attempt → re-fixed + re-verified PASS). **Recompile + retest.**
- **Items "got stuck" on conveyors — FIXED (the real bug).** `WorkshopBuilding.CanAcceptBeltInput` hard-capped
  each input at `capacity/distinct` (≈12 for a 2-input machine), so a belt feeding ONE input of a 2-input
  machine (every new copper/iron/steel smelter!) backed up at ~12 items while the machine sat half-empty
  waiting for the OTHER input — exactly "the first few go through then pile up even though there's space."
  Now an input fills DEEPLY (~28 of 32) while every other input keeps a guaranteed 4-slot floor; the gate is
  `Total + 1 + owed-reserve-of-others ≤ capacity`, which also stops two fast belts starving a third input on
  Campfire/Monument (2/3/4-input cases all verified). InBuffer capacity 24→32.
- **Range circle "didn't appear" — FIXED (scope gap, not a render bug).** The ring worked for COLLECTORS but
  only when selected; the user clicked a SAWMILL (a workshop — no harvest radius), so nothing drew. Now: the
  collector harvest ring ALSO shows during PLACEMENT (see the reach before you commit), and any OTHER selected
  building (sawmill, smelter, storage, lodge…) shows a cyan BOX outline of its input-adjacency reach (a
  processor pulls inputs from the cells around it + belts — no harvest radius). `BuildController.
  UpdateReachIndicator` (renamed from UpdateCollectorRange) + DrawRing/DrawBox.
- **Watchpoint (deliberate, not changed):** a 60/min collector still over-feeds the 30/min wooden belt, so the
  collector backs up until you upgrade the belt (research the cheap early Conveyor) — the intended "upgrade
  your belt" pressure, now that machines actually DRAIN (the fair-share fix). If the early collector-backup
  still feels off in play, we can ease the wooden rate (30→40/min) next.

## New playtest feedback #17 — part 4: progression depth — ore split + per-age special-item/build gate (2026-06-26)
The big one — the "Bronze came too quick / no new buildings" fix. 3-lens adversarial verify (compile +
soft-lock + playability all PASS, 0 blockers). **NOT yet Unity-compiled.**
- **5. Each age now REQUIRES building its new chain + crafting a special item (#5).** New `Research.Tech`
  gate: an age node won't unlock until you've BUILT the required new building(s) AND delivered N of that
  age's special research item — on top of the points. The panel shows what's missing:
  `🔒 Bronze Age — 60 pts (build Copper Smelter · deliver Study Scroll 3/8)`. Engine: `Research.RequirementsMet`
  /`HasBuilding`/`DeliveredByItem`/`MissingRequirementsText`; `CanBuy` extended; `InventoryHud.RenderNode`
  shows the amber locked-reason. (Tribal is ungated — the opening stays quick.)
- **5b. ORE SPLIT into a deeper metal tree (#5).** Generic ore/metal relabelled **Iron Ore → Iron**; a new
  **Copper** branch added — Copper Ore (new finite deposits, guaranteed in the nearest expansion corridor +
  Plains frontier) → Copper (new **Copper Smelter**) → **Bronze Plate** (new **Bronzeworks**), and **Steel**
  (new **Steel Foundry**: Iron+Charcoal). "Smelter"→"Iron Smelter", "Mine"→"Iron Mine".
- **5c. Multi-stage research chains (#5).** The age research items now pull from the new chain: Study Scroll
  = **Copper**+Planks · Schematic = **Bronze Plate**+Pottery · Blueprint = **Steel**+Tools. So Bronze/Iron/
  Industrial each demand a new resource + multi-stage processing. Gates: Bronze→Copper Smelter + 8 scrolls ·
  Iron→Bronzeworks + 6 schematics · Industrial→Steel Foundry + 5 blueprints (counts are a floor below the
  points-derived item count, so no extra grind). GAME_DESIGN spine + research table updated (also fixed the
  stale 20/50/100/200 costs → 12/60/160/360).
- **No circular locks (verified):** each age's new makers unlock in the PRIOR age (copper at Tribal,
  bronzeworks at Bronze, steel foundry at Iron); gate items are research-tier items the Lodge accepts; copper
  is on a guaranteed-carved corridor (no soft-lock). `Research.Reset` clears the new per-item delivery counts.
- **Playtest watchpoints (from verify, all non-blocking):** (a) **Charcoal is now a 4-way shared bottleneck**
  (Kiln + Copper/Iron Smelters + Steel Foundry) — the likeliest mid/late stall; scale Charcoal Burners.
  (b) Copper is ~46u away (nearest guaranteed region) — a real trek before Bronze; shorten or lean on the
  closer Plains-frontier copper if it walls. (c) Finite copper feeds the Bronze gate + ongoing Bronze Plates —
  watch for late exhaustion forcing a 2nd copper region (intended expansion). (d) Counts (8/6/5) could rise
  slightly for more "feel the chain" without becoming binding.
- **Queued (per your call):** the Copper→Steel→**Space** NEW-AGE arc (beyond the 5 ages) · a Tin/true-bronze
  alloy stage · the Station overhaul (#9) · and the owed real Unity compile + playtest of this whole #17 batch.

## New playtest feedback #17 — part 3: belt tier ladder + overlay-upgrade (2026-06-26)
Belt-balance slice. 2-lens adversarial verify (compile + logic PASS, 0 blockers). **NOT yet Unity-compiled.**
- **6b. Wooden belt SLOWED to 30/min + a 4-tier ladder (#6).** Wooden 30 (½ a collector) → Conveyor 60 →
  Geared 120 → Steel 240 (intervals 2.0/1.0/0.5/0.25s). Each tier costs richer materials (wood → planks →
  planks+metal → metal+planks) and is gated by its own research tech. The slow wooden belt is now the early
  bottleneck, so upgrading belts is a real goal. Splitters/Mergers run at the top rate (240) so they never
  throttle. Per-tier belt COLOURS via `def.color` (Belt.Spawn gained a colour param).
- **6c. Overlay-upgrade — no delete needed (#6).** Dropping/dragging a FASTER belt tier over an existing belt
  upgrades it in place (`Belt.SetTier`; `BuildController.EnsureBelt`), keeping direction + carried items.
  Same/slower just re-orients (no charge); re-touching an upgraded cell doesn't re-charge. Ghost reads green
  over any plain belt.
- **Research rewiring.** The first belt upgrade (Conveyor) is now CHEAP + EARLY (tech `conveyors`, cost 10, no
  age prereq) so the wooden bottleneck is a brief first-research payoff; new techs `geared_belts` (Bronze, 40)
  and `steel_belts` (Iron, 80). Updated GAME_DESIGN SYSTEM RATIOS to the new belt speeds (and noted the Sawmill
  3.0s exception, resolving the stale-doc contradiction the canon audit flagged).
- **No machine retune** (deliberate): the slow wooden belt + the "Needs X/min" cue ARE the upgrade lesson; the
  Sawmill (~40/min want) is intentionally under-fed by one wooden belt (30) until you upgrade. **Playtest
  watchpoint:** if the opening feels too slow, ease it by speeding the first Conveyor unlock or the Wood Hut.
- **Next:** C progression depth — split ore→copper/iron/tin, multi-stage chains, a special per-age item +
  new-building gate (the "Bronze too quick" fix). Design proposed; pending go-ahead / first Unity compile.

## New playtest feedback #17 — part 2: belt look/feel — splitter/merger arrows, item piling, conveyor visuals (2026-06-26)
Second slice of the playtest batch. 3-lens adversarial verify (compile + geometry PASS; belt-flow caught
1 blocker — drag mass-convert/over-charge — now FIXED + re-verified PASS). **NOT yet Unity-compiled.**
- **2. Splitter/Merger now show IN vs OUT (#2).** A Splitter gets two green output arrows (forward + its
  right) and a cyan input notch (back); a Merger gets one green output arrow (forward) + cyan input
  notches on the other three sides. Markers are belt children using LOCAL dirs, so they stay correct when
  the belt is re-oriented. `Belt.AddPortMarkers`; reuses `Ports.MakeOutputArrow/MakeInputNotch`.
- **2b. Drop a Splitter/Merger straight onto a built belt (#2).** Placing one on an existing PLAIN belt
  CONVERTS it in place (keeps its direction + carried items) — no need to delete first (`Belt.ConvertTo`;
  `BuildController.EnsureBelt`). Splitters/Mergers are now SINGLE-CLICK tools (not drag-laid), so a sweep
  can't mass-convert/over-charge a whole line (the verify blocker). Ghost reads green over a convertible belt.
- **4. Items PILE UP nose-to-tail (#4).** A belt cell holds up to `capacity` items; we now draw one dot
  per held item, PACKED from the exit edge when backed up — so a blockage visibly queues goods instead of
  showing one frozen dot. Flow logic is byte-for-byte unchanged (purely the renderer). `Belt.UpdateDots`.
- **6a. Belts look like CONVEYORS, not triangles (#6 visual).** New `PlaceholderArt.Conveyor()` — a
  rounded-rect tile with two baked-dark forward chevrons (replaces the pine-tree triangle). Items now follow
  a quadratic-Bézier path through the cell centre → smooth ARCS around corners ("nice turns"). Splitters/
  Mergers keep their distinct hexagon body.
- **Nits left (non-blocking):** a Splitter's single in-flight dot animates toward `dir` even when it will
  exit to the right output (cosmetic); no ghost-port preview before a splitter/merger is placed (the built
  one shows arrows). True elbow belt sprites deferred — the chevron + arced item path read as turns.
- **Next:** B2 belt tiers (wooden→30/min, Conveyor 60 / Geared 120 / Steel 240 + costs + overlay-upgrade)
  · C progression depth.

## New playtest feedback #17 — part 1: collector range ring + click-to-close age card (2026-06-26)
First slice of a 6-item playtest batch (range indicator / popup / belts / progression). Whole batch
mapped with a 6-agent workflow; this slice 2-lens adversarially verified (PASS, 0 compile blockers).
**NOT yet Unity-compiled — needs the recompile + playtest.**
- **1. Collector RANGE ring + node glow (#1).** Selecting a collector now draws a translucent yellow
  ring at its harvest radius (`sourceRange` = 6u) via a `LineRenderer`, and brightens the resource
  nodes within reach (reuses `ResourceNode.SetHighlighted`). Clears on deselect / hidden while placing.
  New: `BuildController.UpdateCollectorRange` + `DrawRangeRing` (called before the placement glow so
  starting a placement re-lights cleanly). Placement still glows all nodes of the target type, as before.
- **3. Age-advance card is CLICK-TO-CLOSE (#3).** The "🎉 \<Age\> reached!" card no longer auto-fades
  after 8.5s — it persists until dismissed: a **"Got it ✓" button** OR a click anywhere on the card.
  `_ageCardT` is now a shown-flag (set 1f, cleared 0f); the card rect is added to the `PointerOverUI`
  test so the dismiss click doesn't leak into the world. Always enough time to read the new-buildings tips.
  (`InventoryHud.DrawAgeCard` + the removed per-frame decrement.)
- **Watch / nits (non-blocking, from verify):** (a) a range-lit node that depletes WHILE its collector
  stays selected keeps glowing (won't show the exhausted shade) until you deselect — cosmetic only.
  (b) `ResourceNode.SetHighlighted` is a plain bool toggle shared by the placement glow + the range
  glow; they don't fight today only because "collector selected" and "placing a collector" are
  mutually exclusive in time (could refcount later if that ever changes).
- **Also:** committed the previously-untracked `Harvester.cs.meta` (Unity-generated GUID, was missed in #16).
- **Next in this batch:** B1 belt look/feel (splitter/merger in-out arrows + drop-on-belt, item piling,
  conveyor visuals) · B2 belt tiers (wooden→30/min, 4-tier ladder + costs + overlay-upgrade) · C
  progression depth (ore→iron/copper, multi-stage chains, per-age special item + build/delivery gate).

## Playtest feedback batch — 8 of 9 items (2026-06-26 #16)
Verified by a 3-lens adversarial workflow (PASS, 0 blockers). Items by the user's list:
1. **Visual cutters BACK (cosmetic).** New `Harvester.cs` — a little NPC each collector sends out to
   walk → chop (shakes the node) → carry home, on a loop. Pure eye-candy; does NOT gather or gate
   output (production is still the fixed timer). Cleaned up on demolish. Upgrade-per-age hook noted.
2. **Sawmill slowed 2.0→3.0s** (eats ~40 wood/min, makes ~20 planks/min). One Wood Hut (60/min)
   over-feeds it → wood backs up → split the belt to a 2nd Sawmill: the first "build more / split"
   bottleneck. Tooltip + GAME_DESIGN updated to the new numbers.
3. **Research Lodge has an INPUT side** (`OutputSide` + input notch via Ports; belt delivery gated to
   that side, R rotates). No output. Adjacency feeding still works.
4. **Tips + age-card no longer cut off.** GUI styles got `wordWrap`; toasts are width-constrained &
   `CalcHeight`-sized (multi-line); the age card auto-sizes its height to content. Age card moved
   below the toast stack.
5. **Mergers.** Plain belts now accept a hand-off from only ONE feeder (straight, or a single
   corner) — a 2nd feeder backs up (yellow). To combine two lanes you place a **Merger** (N→1, new
   buildable). `Belt.AcceptsHandoffFrom`; mergers accept any feeder. (Verified: straight chains,
   corners, splitters all still flow.)
6. **Research scaling raised** (was "3 ages in no time"): Tree age costs 12 / **60 / 160 / 360**;
   tier value schematic **4**, blueprint **8**. Later ages need meaningfully more delivered items
   (sane counts at ~30/min makers) — deeper chains do the rest.
7. **Storage compacted:** removed the dedicated Stone Storage + Ore Stockpile from the menu (the
   configurable **Warehouse** holds any one resource — stone/ore/planks/…); Warehouse name/desc
   clarified. (Defs remain as unused locals — harmless.)
8. **Station "can't afford" bug FIXED.** `BestRouteTier` now offers the best AFFORDABLE tier (falls
   back to the cheapest unlocked), so you're not dead-ended on a Wagon Train. The "+ Add route"
   button shows tier **+ cost** (green/red), and the toast/panel clarify: click 2 Stations, a vehicle
   **auto-shuttles — no track to lay**.
- **9. DEFERRED (next batch):** the Station overhaul — 2×2 footprint with 2-in/2-out conveyor ports,
  a liquid-station variant (chosen from the station panel), and an in/out direction arrow for where
  the vehicle pulls in. Substantial subsystem (footprint + ports + belt depot side-gating + liquid)
  — doing it half-way risks placement/I-O bugs, so it's its own focused pass. (The route already
  draws a line + direction arrow between stations.)
- **Watch:** (a) a rejected 2nd belt-feeder reads as yellow "backed up" (not red) — correct but note
  it's the "use a Merger" cue. (b) first research loop is a touch grindier now (planks 20/min) — the
  panel surfaces it (`Waiting for: Planks`). (c) NOT yet Unity-compiled — needs the playtest.

## REMOVE WORKERS/POPULATION — fully automated machines + instant build (2026-06-26 #15)
Completes the factory-first pivot: there are now NO workers, NO population, NO staffing, NO worker
slots, and construction is INSTANT. Buildings are pure automated machines. Mapped (5-agent workflow)
+ adversarially verified (3-agent workflow: PASS, 0 compile blockers) before commit.
- **Deleted:** `Worker.cs`, `IStaffable.cs`, `BuilderWorker.cs` (+ their `.meta`).
- **Collectors** (`ProductionBuilding`) now auto-gather on a fixed timer straight into their Buffer —
  `GatherPerCycle = 2` units / `interval` → 60/min at the standard 2.0s (the canonical "1 collector →
  1 belt → 1 machine" lane). No NPC walk. Auto-rebind to the nearest live node within `searchRadius`
  on depletion is unchanged.
- **Workshops** (`WorkshopBuilding`) run automatically at a FIXED rate (the old 1-worker rate) when
  not paused and inputs are present — dropped the `AssignedWorkers` gate + the `×workers` speed
  multiplier. Power/brownout still applies. Pause kept (a factory tool, not a worker mechanic).
- **Construction is INSTANT:** placement = `Economy.Spend(cost)` + `ConstructionSite.SpawnFinished`
  (no site, no builder units). Sandbox/FreeBuild stays free.
- **Removed:** Town Hall + Construction Yard (from the menu), the builder squad (`Colony` Builders/
  MaxBuilders/AddBuilder/RemoveBuilder/ManageBuilders/SyncSquad) + `AssignedTotal`, all worker UI in
  `InventoryHud` (±worker, Max-speed, J "staff all", F2 add-pop, the idle bottleneck count), the
  worker hotkeys/AssignSelected in `BuildController`. Guide/Help/Describe/tooltips reworded to
  "fully automatic / no workers / instant construction".
- **Kept INERT (so the blind build still compiles — later cleanup):** `BuildingDefinition.maxWorkers`
  + `builderSlots`, `HousingBuilding.isHQ`, `Colony.Population/FreeWorkers/DebugAddPopulation/Comfort`,
  the `ConstructionSite` MonoBehaviour body + `ConstructionYard` + `TransportHub`/`Transporter`
  classes (none placeable), `GameBootstrap.MakeBuildYard/MakeLogistics/MakeHousing` helpers.
- **Verify findings (all non-blocking):** collector `outputPerCycle` is ignored (gather uses the
  fixed `GatherPerCycle`) — intended, the field was already dead under the old Worker model; a few
  stale dev-only doc-comments remain. **Still owed: a real Unity compile + playtest** (static
  analysis + the prior pivot's CS0136 fix are strong signals, not a substitute).
- **Brief's other asks confirmed already DONE** (mapping workflow): collector auto-rebind w/ radius,
  larger grouped biomes (low-freq Perlin + SmoothLand), transport tiers by age (belt + route tiers,
  UpgradeAllRoutes on age-up), visually-traceable movement (belt dot + route line/arrow), age-up
  "what unlocked" card, minimal early UI (3 toasts + scrollviews), input/output/status panel.

## FACTORY-FIRST RESTRUCTURE (2026-06-26 #14)
Big identity pivot: removed the colony/survival layer so the game is a pure factory automation loop.
Also bundles the prior (uncommitted) survival/UI/depot/collector/biome passes into one commit.
- **Removed survival pressure.** `Colony` no longer consumes food/water, no starvation/thirst, no
  growth, no comfort/happiness. `Starving/Thirsty=>false`, `Happiness/Productivity=>1`. Prosperity
  is now an **automation-only "Industry" score**.
- **Labour is free.** `FreeWorkers=>9999`; builders no longer pop-gated. Buildings run when built;
  "workers" are a free per-building **speed dial** (1..max) + NPC charm. (Old `IStaffable` kept.)
- **Build menu curated to factory buildings only** — survival/comfort buildings (forager, water hole,
  granary, campfire, farm/mill/bakery, hunter, housing, pipes/pumps, textiles, jewelry, masonry) are
  still DEFINED in code (compile-safe) but removed from `builder.buildables` and the world spawns.
- **UI/objectives/toasts/Guide/Help** rewritten to the factory flow (gather→store→process→research→
  automate→expand). Removed food/water HUD chips, starving alert, population/happiness stats.
- **Research is the sole progression spine** (already was): craft research item → Research Lodge →
  spend points → advance age. Pipe-network tech node removed.
- **Watchpoints:** (a) blind-built — NOT yet Unity-compiled; needs a recompile + 10-min playtest.
  (b) Dead-but-defined survival code (Economy food helpers, hidden building defs, Colony.foodItem)
  left in place to keep compiling; a later cleanup can delete them. (c) A few CS0219 "unused local"
  warnings expected (foodStore/waterStore/house). (d) The deeper Copper→Steel→Space age arc is
  DESIGNED in GAME_DESIGN but not yet implemented.

## Scale & player-pressure design pass (2026-06-25 #13)
Evaluated how the factory FEELS at small/medium/large scale (not numbers). Finding: the systems
already create strong scaling pressure (local production → logistics, depletion → expansion,
shared intermediates → cascades, escalating research/comfort). The weak spot was **LABOR** — the
dominant *soft* bottleneck at scale was the least visible and the most tedious to manage.
- **FIXED — labor scarcity now visible:** the status-bar bottleneck summary counts idle
  (unpaused, unstaffed) buildings: "⚪ N idle — press J to staff / grow population". Turns a
  hidden wall ("why did my factory stop?") into a clear, actionable pressure (it's people, not
  logistics). Completes the 3 failure modes on the dashboard: starved / backed-up / idle.
- **FIXED — staffing tedium:** one-shot **J** = staff every idle building from the free pool.
  No more clicking 100 buildings after a population gain. LIMITED workers stay the real
  constraint (the J toast tells you to grow population when none are free).
- **Watch (left as-is, design calls):** (a) **pause does NOT free the worker** — a paused
  building still holds its labour, so pause isn't a reallocation lever (use −worker for that);
  making pause release labour would turn it into a clean priority tool (future). (b)
  `EnforceAssignment` unassigns COLLECTORS before workshops when over-staffed, so a starvation
  crunch can pull workers off food gathering first and accelerate a death-spiral — protect
  production collectors first if it bites in play. (c) belt-laying at large scale is still manual
  (no blueprint/copy-region) — inherent genre friction, deferred.

## System throughput / coherence pass (2026-06-25 #12)
Audited every core rate against the canonical **1 lane = 60/min** baseline. Good news: the
baseline is **genuinely coherent at the default 1 worker** (collectors auto-assign 1 on place):
- Belts: wooden 1.0s = 60/min, conveyor 0.5s = 120/min (clean 2×). ✓
- Workshops (1 worker): Sawmill eats 2 wood/2.0s = **exactly 60 wood/min** (1 lane), makes 30
  planks/min. The "1 collector → 1 belt → 1 machine" baseline holds. ✓
- Collectors (1 worker): carry 3 / 2.0s chop + travel ≈ 60/min at typical node distance. ✓
- Research Lodge: 1.0s = 60/min consume = 1 lane. ✓
- **FIXED — Splitter hidden throttle:** was 0.6s = 100/min, so it silently capped a 120/min
  conveyor line (lost 20/min for no reason). Now 0.5s = 120/min — matches the fastest belt.
**Deviations left as-is (deliberate or need playtest, NOT changed blind):**
- **Water Pump ≈480/min total** (flowPerTick 4 × 2/s) — ~8× a lane; water is effectively
  unconstrained once piped. Plausibly intended (liquids = infrastructure, not a bottleneck),
  but flag to confirm; if water should require scaling, lower `flowPerTick`.
- **Collector rate is travel-variable (~40–90/min for 1 worker)** and **2 workers ≈1.85×**
  (travel overhead) vs workshops' clean 2×. Inherent to the walk-to-node model; tune only with
  real play. Placing a collector adjacent to its node ≈90/min (overfeeds a wooden belt) — a
  feature (placement matters) more than a bug, but watch it.
- **Farm** (3 workers / 2 output) and **Mine/Gem Mine** (2.5s vs 2.0s) are pattern outliers —
  likely intentional (bulk grain; rare ore slower). Left alone.
- **Code cruft:** `ProductionBuilding.outputPerCycle` is set per collector but never used
  (Worker uses `carryAmount`). Harmless, designer-facing only — make it drive carryAmount or
  delete it in a cleanup.

## Full logic & flow sweep + research soft-lock FIX (2026-06-25 #11)
A correctness audit of all systems (crafting, economy, logistics, liquids, power, terrain,
research, colony, construction). Most systems verified **correct** — no item loss/dup, no
divide-by-zero, no claim leaks, no infinite loops. Findings:
- **FIXED — Research soft-lock.** The Research Lodge only accepted/credited the NEXT age's
  research item (`CurrentItem`). At the final age `CurrentItem` is null → no more points could
  be earned → any building-unlock node (Splitters/Conveyors/Pipes) not bought by then was
  permanently unaffordable; and items belt-fed right after an age advance were consumed for 0
  points (wasted). Now the Lodge accepts/credits **any** research item at its tier's value
  (`Research.IsResearchItem`/`PointsFor`; `Deliver` reworked; `ResearchBuilding.Accepts`/consume
  widened; status uses `AllResearched`). `CurrentItem` still drives the "craft this next" hint.
- **Multi-input crafting/storage CONFIRMED OK.** Shared InBuffer (24) fair-shares per input
  (`24/#inputs`); every recipe fits (widest is the 4-input Monument → 6 each ≥ needed). Belts
  deliver each input on any non-output side; `CanMake`/`ConsumeInputs` source InBuffer→adjacent.
- **Investigated, NOT bugs:** (a) Pump booster beyond range — by design a booster must be within
  the pump's pressure range to relay; resetting cells with no incoming pressure would be
  water-from-nowhere. (b) Configurable warehouse capacity edge only matters if `def.capacity==0`,
  which never happens (all storages set positive capacity). (c) Splitter even-ness skews under
  one-sided blockage — intended fallback, no item loss.
- **Watchpoints (tuning, not bugs):** solid collector output is only usable once a belt drains it
  to storage (manual gather bootstraps the start) — intended "stored, not summoned", but a steep
  early requirement; and a mid-size colony can hover just under the growth food threshold.

## Multi-input deadlock FIX + early-flow ease (2026-06-25 #10)
- **BUG FIXED: Idea Bench (any multi-input workshop) stuck "waiting for stone".** The shared
  `InBuffer` (24 total across all types) could be entirely filled by the faster/first-arriving
  input, leaving no room for the other → permanent starve while the missing item backed up on its
  belt. Added `WorkshopBuilding.CanAcceptBeltInput` giving each input a FAIR SHARE of the buffer
  (capacity / #inputs); belt delivery now uses it. Fixes the deadlock for unbalanced belts, a
  not-yet-staffed bench, or a momentarily-dry source. Helps every multi-input machine (Smelter,
  Kiln, Campfire, …), not just the Idea Bench.
- **Early-flow ease:** first research node (Tribal) cost 20 → **12** so the opening research loop
  pays off quickly (later ages stay 50/100/200). Validates the build→produce→research→unlock loop
  in the first couple of minutes.
- Self-test note: with wooden belts (60/min) one Sawmill + one Stone Pit comfortably feed one Idea
  Bench (needs ~30 planks + ~30 stone/min at 1 worker). Conveyors/pipes are research-gated now, so
  early game is wooden-belt only — intended, and sufficient for the first research factory.

## Spendable research TREE + multi-input ports + tutorial/UI (2026-06-25 #9)
Addresses: "Idea Bench needs 2 inputs", "add a research tree where we spend points", tutorial/UI, QoL.
- **Research is now a SPENDABLE TREE (press T).** Delivered research items add to a research POINT
  pool (no longer auto-advances). A new **Research Tree panel** lists Tech nodes — age spine
  (Tribal 20 / Bronze 50 / Iron 100 / Industrial 200, each needs the prior) + building-unlock
  branches (Splitters 15, Conveyors 30, Pipes 30) — each with cost / prereq / buy button. The
  player SPENDS points to advance ages and unlock buildings. (`Research` rewritten: `Tiers` =
  what the Lodge consumes per age; `Tree` = spend nodes; `Buy/CanBuy/IsPurchased`.)
- **Buildings gated behind research:** new `BuildingDefinition.requiredTech` checked in
  `BuildController.IsUnlocked`. Splitter→"splitters", fast Conveyor→"conveyors",
  Pipe/Pump/Booster→"pipes" (their unlockAge dropped to 0 so the Tech is the gate; prereqs keep
  the age pacing). Locked build-menu entries show "🔒 … — research <Tech>".
- **Multi-input ports (the Idea Bench fix):** workshops with >1 input now accept belt deliveries on
  ANY non-output side (`Belt.AcceptsInputSide`), and show input notches on every non-output side
  (placed `Ports.PlacePorts` + the placement GHOST `PlaceGhostSides`). So you can belt Planks into
  one side and Stone into another. Single-input workshops are unchanged. (Also fixes the Smelter etc.)
- **Tutorial/UI:** status-bar `🔬 N pts (T)` token; build-panel research section (points + current
  item to craft + progress bar + "Research Tree (T)" button); Research Lodge panel updated; Guide (G)
  has a "Research drives progress" section; welcome toasts + controls hint + README mention T; a
  one-time "Research available — press T" toast when a node first becomes affordable.
- **Watch:** (a) first advance now needs the research factory AND a T-press to spend — verify the
  guidance makes that obvious (lots of hints added). (b) Splitter now needs Tribal+15pts (was free)
  — minor. (c) research tree panel scroll/height on small windows. (d) multi-input workshops show
  many port notches (e.g. 2×2 Smelter) — busy but functional. (e) age-spend competes with
  building-unlock spend — intended tension; watch it isn't too grindy early.

## Transport → Station rework (2026-06-25 #8)
Phase 6 of the factory brief. Transport is now managed INSIDE buildings, not via a global tool.
- **Depot rebranded "Station"** (display only — class stays `Depot` to avoid churn across 6 files).
- **The 4 vehicles (Caravan/Ox Cart/Wagon/Drone) are OUT of the build menu.** They're now
  `BuildController.routeTiers` (data), surfaced through the Station's panel instead.
- **Routes are created from a Station's panel:** select a Station → "+ Add route (<tier>)" →
  click a destination Station. Uses the newest unlocked tier (`BestRouteTier`). The panel shows
  route count and a "✕ Remove a route" button. Linking state lives in `BuildController.LinkFrom`
  / `BeginStationLink` / `CompleteStationLink` / `CancelLink`; Esc/right-click/placing cancels it.
- **Vehicles now LOAD → TRAVEL → UNLOAD with timing** (`RouteVehicle` states ToSource/Loading/
  ToDest/Unloading, ~1.5s load + 1.5s unload) instead of instant transfer — so distance + handling
  cap throughput, which is what makes multiple vehicles/routes worth building.
- **Legacy global Route tool REMOVED** (cleanup #9): deleted `UpdateRoutePlacement`,
  `RoutePickingFirst`, `_routeA`, the Route placement-mode hint, the `BuildingKind.Route` Cats entry
  and Describe case, and the stale help line. The `BuildingKind.Route` enum value remains (the
  vehicle tier defs still use it as data). Quest reworded to "Build 2 Stations & add a transport route".
- **Polish (cleanup #9):** selected-building panel is taller (210px, bottom-anchored) so the
  Station's route buttons don't crowd the Demolish/Close row; splitter placement GHOST now shows the
  hexagon (matches the placed splitter); README build-status refreshed.
- **Watch:** (a) Station panel is now ~6 widgets tall — verify it doesn't clip the Demolish/Close
  row at the fixed 188px height (shrink/group if so). (b) only ONE vehicle per "+ Add route" press;
  add several for more throughput — confirm that reads clearly. (c) tier is auto-selected (newest);
  a tier picker can come later. (d) a vehicle whose Station is demolished self-destroys next frame
  (a/b null) — confirm no stragglers.

## Placement/collision fixes + Splitters (2026-06-25 #7)
A "layout matters" pass (the factory-feel brief). NOTE: that brief's Research, tree-clusters and
debug-visibility asks were already delivered in passes #5/#6 — this pass does the genuinely-new
parts (placement/collision + splitters) and scopes the transport rework separately.
- **Water buildings now need ADJACENT water** (BuildController `waterAdj = 1.6f`, was the loose
  `placeNodeRange` = 6). A Water Hole / Pump must sit on land in a cell touching water, not "nearby".
- **Belts can't be laid on buildings** — belt placement (ghost validity + `EnsureBelt`) now rejects
  any cell with a solid building (`BuildController.SolidBuildingAt`). Belts still go on land/bridges.
- **Player collides with solid buildings** (`PlayerController`, per-axis like the water check) so you
  route around your factory. Escape valve: if you're already inside a building (one built on you) you
  can still walk out. Belts/bridges/pipes/nodes are NOT solid (you walk over them).
- **Splitters (1→2, even distribution).** New `Splitter` buildable (Belt kind + `def.splitter`).
  `Belt.isSplitter`: pulls from behind, sends items EVENLY to two outputs — forward + right
  (`RotateCW(dir)`, R rotates) — alternating, with fallback to the other output if one is full (never
  stalls). Refactored `PushForward`→`TryDepositTo(Dir)` and `HasForwardTarget`→`OutputConnected(Dir)`
  so normal belts are unchanged and the splitter just drives both outputs. Distinct hexagon sprite.
  Upgrade path (smart/filtered splitters) is open via the same flag pattern.
- **Deliberately DEFERRED (need a supervised batch — too risky/large to do blind):**
  - **Worker collision with buildings.** Workers move in straight lines (no pathfinding); blocking
    them would FREEZE gatherers against buildings → softlock. Needs simple pathfinding/steering
    first. Player collision (above) is the safe half.
  - **Transport/Station rework (Phase 6).** Plan: a "Station" building that owns transport units +
    a route-management panel (load → travel → unload timing), replacing the global Caravan/Ox
    Cart/Wagon/Drone "Route" tool. Removing those from the menu BEFORE the Station exists would
    remove the only cross-region transport → kept them for now to stay playable. Do as its own pass
    with a compile/playtest loop (touches BuildController route mode, Depot panel, RouteVehicle).
- **Watch:** (a) player bumping its own buildings should feel "layout matters", not annoying — tune
  if it sticks at corners (per-axis slide should handle it). (b) splitter dragged (vs single-click)
  lays a row of splitters — harmless but odd; single-click is the intended use. (c) splitter dot
  animation always shows the forward edge even when it routed right — cosmetic.

## Production-driven RESEARCH progression (2026-06-25 #6)
Replaced automatic/resource-cost age unlocks with a research system. **All progression now flows
through research.** See the new **PROGRESSION = RESEARCH** section in GAME_DESIGN.md.
- **The loop:** craft a RESEARCH ITEM (multi-input factory product) → deliver to a **Research Lodge**
  → it converts items to research **points** → reaching a tier's cost **advances the Age** → that
  age's buildings unlock (existing `unlockAge` gate). Gathering earns 0 research; there's no manual
  crafting, so you MUST build & scale a factory to progress.
- **New files:** `Research.cs` (static tree: tiers, points, deliver/advance), `ResearchBuilding.cs`
  (the Lodge — worker-free sink, belt-fed InBuffer + adjacent pickup, ≤1 item/sec → points).
- **New content (GameBootstrap):** 4 research items (Idea Tablet / Study Scroll / Schematic /
  Blueprint), 4 age-gated maker workshops (Idea Bench a0 / Scroll Maker a1 / Drafting Table a2 /
  Engineering Lab a3), the Research Lodge (a0), and the tier table (cost 20→50→100→200, worth
  1/2/3/5 pt). Recipes: Planks+Stone → Charcoal+Planks → Bricks+Pottery → Metal+Tools.
- **Integration:** new `BuildingKind.Research` + `WorldGrid.Research` dict + Belt sink branches
  (HasForwardTarget/LeadsToSink/PushForward, omnidirectional like a Depot) + `ConstructionSite`
  SpawnFinished case + BuildController place/select/demolish/copy + InventoryHud category
  ("Production"). **Removed:** `Colony.AgeReq`/`ageReqs`/`CanAdvance`/`AdvanceAge` and the build-menu
  "▲ Advance" button; added `Colony.ResearchAdvance`. The age-up announcer (watches `Age`) still
  fires. F3 still force-advances age (sandbox). Two early objectives teach the loop.
- **UI:** status bar `🔬 <item> pts/cost`; Build panel shows target age + progress bar + items-left;
  Lodge panel shows holdings + pts/min.
- **Balance / watch (needs the playtest):** (a) the FIRST advance now requires a real factory
  (Wood→Sawmill→Planks + Stone Pit → Idea Bench → Lodge) — intended, but verify it's not too steep
  vs the old "40 wood, pop 8"; tier-1 is deliberately cheap (20 × 1-pt tablets, short chain). (b)
  Lodge eats ≤1/sec so one maker (~30/min) → tier-1 in ~40s; confirm it feels right and that
  scaling (more makers/lodges) visibly speeds it. (c) leftover old-tier research items are ignored
  by the Lodge (belt shows red dead-end) — clear, but watch for confusion. (d) it leans on the
  60/min ratio system from pass #5 (maker demand = collector/belt output) — a nice tie-in to verify.

## System clarity + resource distribution pass (2026-06-25 #5)
A clarity/balance/readability pass (NOT a feature add). Goal: the player can *diagnose* problems
and reason about throughput. See the new **SYSTEM RATIOS** section in GAME_DESIGN.md.
- **Resource distribution → CLUSTERS.** Lone scattered nodes replaced by natural groves/outcrops
  (`SpawnClusters` / `SpawnClustersInBiome` / `SpawnCluster` in GameBootstrap, replacing
  `SpawnPatches`/`ScatterInBiome`). Starter basin = small clusters (3–5, bootstrap only); biome
  regions = dense clusters (forest lumber 5–9, hills stone/ore 4–8); plains = sparse but mixed
  (3–4). Node *totals* kept ≈ the same — only the spatial shape changed. Resources now read as
  part of the world, and a collector on a cluster has many nodes to chew before it must rebind.
- **Canonical throughput ratios defined + tuned to clean numbers.** The base unit = **1 lane =
  60/min.** Wooden belt 1.1→**1.0s (60/min)**; conveyor 0.45→**0.5s (120/min)**; basic collectors
  normalised to **2.0s (~60/min)**; **Sawmill 2.5→2.0s** so 1 Wood Hut → 1 belt → 1 Sawmill is an
  EXACT 60/min match (the flagship "1 gatherer → 1 belt → 1 machine" lesson). Distant/finite
  collectors (Mine, Gem Mine 3.5→**2.5s**) and advanced recipes stay deliberately slower.
- **Bottleneck VISIBILITY (no new UI):**
  - **Belts** now have 3 states: **red** = dead end, **yellow** = backed up (downstream full),
    base brown = flowing/empty (empty = upstream/supply issue). (Was red-or-base only.)
  - **Resource nodes** fade toward a dull grey-brown as they deplete (`ResourceNode.ApplyTint`),
    so an over-harvested patch reads as "tapped out" — not just smaller.
  - **Machine panel** gains a `Needs X/min · 1 belt lane = 60/min` line (`WorkshopBuilding.
    InputDemandText`) so supply vs demand is a direct numeric comparison. (Machine status dots
    green/yellow/red/grey + pulse already existed.)
  - Wooden/Conveyor belt + Sawmill tooltips now teach the actual ratio numbers in-context.
- **Open / watch (needs the playtest to confirm):** (a) cluster overlap — nodes within a cluster
  can visually overlap at tight radii; bump `clusterRadius` if it looks mushy. (b) Verify the
  flagship chain actually saturates at 1 worker and *visibly* starves at 2 (the core loop). (c)
  Collector commute still lowers real rate below 60/min if the hut isn't placed on the cluster —
  intended ("build close"), but watch it doesn't feel broken. (d) processor baseline is only
  enforced early; mid/late processTimes left as-is (didn't blind-rebalance the whole economy).

## Warehouse fixes — auto-register, ghost ports, empty button (2026-06-25 #4)
Three player-reported issues from the #3 belt/port test, all fixed:
- **Configurable Warehouse now AUTO-REGISTERS its type** from the first item belted in
  (`Belt.PushForward` + `HasForwardTarget`): an unset (`accepts == null`) configurable warehouse
  adopts the delivered item's type and stores it, instead of the item stalling/vanishing at an
  "unset" store. After that it only accepts that one type (unchanged). Non-configurable stores
  (fixed `def.item`) are unaffected.
- **Placement GHOST now shows per-cell ports** (`BuildController.PlaceGhostSide`/`GhostMarker`):
  a 2×2 warehouse previews **2 output arrows + 2 input notches**, matching the built building
  (`Ports.PlacePorts`). Was a single marker before. Belt I/O was already per-cell (WorldGrid
  registers every footprint cell → both port cells work), so this was purely the ghost preview.
- **Empty button** on a configurable warehouse's panel (`InventoryHud.EmptyStorage`): moves all
  contents into the player's carried hands (unlimited, so no loss — carried+storages is the
  "stored" pool) and clears the type, so you can re-purpose a full warehouse to a new resource.
- **Open/next:** unchanged — per-cell biome FRICTION (forest slow build/move, hills mining-favoured)
  is still the next planned gameplay step.

## Belt / port / storage polish (2026-06-25 #3)
- **Warehouses now have a belt OUTPUT** (belts pull from a storage's output side) — you can
  conveyor warehouse → sawmill. I/O audit confirmed all belt buildings symmetric: collector=out,
  workshop=in+out, storage=in+out, depot=both; power/yard/housing/bridge/pipe/pump have no belt I/O.
- **Per-cell ports**: multi-cell buildings (2×2 warehouse/smelter) draw a port marker PER EDGE CELL
  (`Ports.PlacePorts`), aligned to the belt grid → a 2×2 warehouse = 2 inputs + 2 outputs that line
  up with conveyors. (Belt logic was already per-cell; only the markers were wrong.)
- **Per-item belt icons**: `ItemDefinition.icon` (placeholder shapes by material family now — wood/
  planks=triangle, minerals=hexagon, manufactured=square, organics=round dot). **Real per-item art
  slots into `icon` later with no rewrite.**
- **Belt-delete visual fixed**: a blocked belt (dead-end after deleting a middle piece, or backed
  up) now HOLDS its item at the front and stops animating (`Belt._blocked`) instead of looping
  toward the gap — broken belts read as "stuck", not phantom-travelling.
- **Open/next:** placement GHOST still shows a single orientation marker (not per-cell) — cosmetic;
  per-cell friction (forest slow / hills mining-only) is the next planned gameplay step.

## Refinement + critical-review pass (2026-06-25 #2)
**Issues found (review of the prior pass):**
1. **Discoverability / Iron soft-lock [worst]:** ore/forest/clay were placed in *randomly-located,
   fogged* biomes → a player could follow a corridor and find nothing, or never find hills →
   Iron blocks with no understandable cause ("world feels empty / I went the wrong way").
2. **No expansion guidance:** the finder only pointed at home basics (wood/stone/food/water), never
   at the expansion targets → "where do I go next?" unclear.
3. **Artificial corridors:** straight rays read as generated, not natural.
4. **Food scaling:** berries only at home (limited) + forest forage random → food could stall.

**Changes made (+ why):**
- **3 corridors now each lead to a GUARANTEED, distinct region** (`Region()` + `TerrainGrid.Paint`):
  corridor 0 → plains **meat + clay** (~46m, first expansion), corridor 1 → **forest** with bulk
  wood + forage (~72m, food/wood scale), corridor 2 → **hills** with stone + **ore** (~86m, Iron can
  never soft-lock). Exploration is now intentional (follow a path → find something); broad biome
  scatter still adds density elsewhere. Fixes #1 + #4.
- **Finder = expansion guide:** from the Tribal age it also points to nearest **meat / clay / ore**
  (drops each once you build its collector). Subtle, reuses the existing arrows. Fixes #2 →
  "see the next step before you're blocked."
- **Corridors meander + vary width** (`CarveCorridors` rewritten) → natural valleys, not rays. Fixes #3.

**Tuning:** region distances 46/72/86; corridor length 95, width ~3 (+jitter); ore region capacity
80 finite. **Pressure point (Task 5):** the existing wooden-belt (1.1s) vs conveyor (0.45s) gap is
the throughput hook — left as-is (clear "want faster belts" without new complexity).

**Remaining risks:** (a) regions sit on the corridor *heading* but corridors meander — verify the
region is actually on/near the cleared path (bump corridor width or align if not). (b) Water Hole
still needs a Water Barrel beside it (taught, but a missed step → thirst). (c) three fixed corridor
headings could feel samey across playthroughs. **Next step:** per-biome build/move *friction*
(forest slow, hills mining-only) so the regions *play* differently, not just contain different goods.

## Early-game vertical-slice pass (2026-06-25) — what changed & why
Goal: make the first 5–10 min clear, and make expansion *forced by the systems*.
- **Water → routes, not walls:** + **3 guaranteed dry corridors** carved out of spawn
  (`TerrainGrid.CarveCorridors`) so the basin is never water-locked; basin widened to 22; lakes
  rare + rivers thin + ocean rim (from prior pass). *Why:* navigation friction was too high.
- **Starter basin = BASICS only** (wood / stone / food / water). Removed the near meat/clay/cotton
  and the near ore/gem clusters. *Why:* a critical resource must NOT be at home → forces expansion.
- **Resource arc by distance:** basics at home → **meat + clay just outside** (plains scatter,
  minClear 26 = first expansion) → **forests** (lumber, fibre, **berries so food scales**) →
  **hills** (stone, **ore = Iron**, gems) far out. Ore/gems are hills-only: the critical pull.
- **Expansion pressure (soft limit):** few starter patches + limited regen vs growing demand +
  "stored, not summoned" → you outgrow home and must push out. Not told to expand; *made* to.
- **UI:** build menu consolidated to **4 meta-groups** (Production / Logistics / Infrastructure /
  Settlement) instead of 11 per-kind headers + pinned/dim-obsolete/delete-under-cursor (prior pass).
- **Self-review (can't run Unity — reasoned):** likely-good early flow (hand-gather → Forager+belt+
  Granary, Water Hole+Barrel; 90 buffers cover setup). **Watchpoints:** (a) hills can be far/sparse →
  ore hunt may be long; if so, raise hills frequency (`e>0.70`) or lower ore minClear. (b) meat/clay
  are random-direction just-outside — corridors help but verify they're findable. (c) confirm
  corridors don't read as ugly straight lines. **Not added** (per brief): liquids/pipes already
  separate (system prep done); no new transport this pass.
- **Next logical steps:** per-biome build/move *friction* (forest slow, hills mining-only) so biomes
  *play* differently; then multi-stop routes/stations for mid-game route-thinking.

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
