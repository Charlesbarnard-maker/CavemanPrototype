# CavemanPrototype — Master Design Synthesis

The canonical, system-level design that ties together the per-system docs
([AGES](AGES.md) · [WORLD](WORLD.md) · [LIQUIDS](LIQUIDS.md) · [CONSTRUCTION](CONSTRUCTION.md)
· [BUILD_SYSTEMS](BUILD_SYSTEMS.md) · [DESIGN](DESIGN.md)). Where this doc and a per-system doc
disagree, **this doc wins** — the others are detail.

> **The whole game in one sentence:** *Raw materials have locations; logistics moves them to machines
> that process them into ever-deeper goods; research turns goods into progress; and each age changes
> HOW logistics works and WHAT you can build, never the core loop.*

Status tags: ✅ in game · 🔶 partial · 📝 designed/proposed.

---

# ⭐ FACTORY-FIRST PIVOT (2026-06-26 — supersedes the colony/survival framing below)

The game pivoted from "half colony-survival, half factory" to a **pure factory automation game**.
This section WINS over any survival/population wording elsewhere in this doc (kept for history).

**Removed entirely as gameplay:** population, food, water, housing, happiness, comfort goods,
starvation/thirst, **and — as of 2026-06-26 #15 — ALL workers/staffing/labour and construction
units.** (`Worker`/`IStaffable`/`BuilderWorker` deleted; the rest neutralised/hidden so the build
keeps compiling — a later cleanup can remove it.)

**The core loop (the whole game):**
> **gather → store → process → automate (belts/stations) → research → expand → next age**

- **No workers. Every building is a fully automated machine.** It runs by itself at a FIXED rate the
  moment it's built and supplied — nothing to staff. Collectors auto-gather (60/min at the 2.0s
  baseline) and auto-rebind to a fresh node when one runs dry; processors run whenever fed. Pause is
  kept as a routing tool (halt a machine to free a shared input). **Construction is INSTANT** — the
  cost is paid at placement and the finished building appears; no builder units, no hauling.
- **Research is the only progression.** Craft a research item in a production chain → deliver to a
  **Research Lodge** → spend points to advance the Age / unlock buildings. Scaling research = scaling
  the factory.
- **Pressure** comes from production dependencies, shared-intermediate bottlenecks (charcoal feeds
  Kiln + Smelter + Generator), finite/far ore driving expansion + routes, and Industrial **power**.
- **Score** is automation-based: **Industry** (collectors/workshops/belts/routes + age) → **Rank**.

**Current production spine (✅ in game):**
`Wood→Planks` (Sawmill) · `Wood→Charcoal` (Burner) · `Stone` · `Clay→Bricks` (Kiln, +Charcoal) ·
`Clay→Pottery` (Potter) · `Ore→Metal` (Smelter, +Charcoal) · `Metal+Planks→Tools` (Toolmaker) ·
Research: Idea Tablet→Study Scroll→Schematic→Blueprint · `Charcoal→Power` (Coal Generator) ·
Monument (Metal+Tools+Bricks+Planks) = win.

**Planned deeper age arc (📝 next — structure already supports it):**
`Stone → Copper/Bronze → Iron → Steel/Industrial → Electrical/Automation → Space → Beyond`.
Add metal tiers (copper ore→copper ingot→wire; iron→steel) and component chains (gears, circuits,
motors, science packs) feeding harder research items, with later milestones pointing at engines /
rockets. Build these with the EXISTING item/recipe/collector/workshop/research systems — no new
framework. Implement incrementally so the playable build never breaks.

---

---

# PROGRESSION = RESEARCH (production-driven, the only way to advance an age)  ✅

There are **no automatic / resource-cost age unlocks**. The single progression spine:

> **Craft a RESEARCH ITEM (a multi-input factory product) → deliver it to a Research Lodge →
> it converts items to research POINTS → reaching a tier's cost advances the Age → that Age's
> buildings unlock (`unlockAge` gate).**

Gathering earns **zero** research; only delivered crafted items do. You cannot hand-craft (there is
no manual crafting — workshops need workers), so progress *requires* building & scaling a factory.

**Research tree (data-driven; append a Tier per future age):**

| Tier | Advances | Research item | Recipe (maker, unlock) | Worth | Cost | ≈ items |
|---|---|---|---|---|---|---|
| 1 | Stone → Tribal | **Idea Tablet** | Planks + Stone (Idea Bench, age 0) | 1 pt | **20** | 20 |
| 2 | Tribal → Bronze | **Study Scroll** | Charcoal + Planks (Scroll Maker, age 1) | 2 pt | **50** | 25 |
| 3 | Bronze → Iron | **Schematic** | Bricks + Pottery (Drafting Table, age 2) | 3 pt | **100** | 33 |
| 4 | Iron → Industrial | **Blueprint** | Metal + Tools (Engineering Lab, age 3) | 5 pt | **200** | 40 |

- **Maker workshops are age-gated to the PRIOR age**, so the next item is craftable only once you
  reach the age before it — no circular locks. Each tier needs a deeper chain than the last
  (planks → charcoal+planks → bricks+pottery → metal+tools), so the *required output* climbs hard
  even though item-counts climb gently. **Scaling research = scaling the factory.**
- **Research Lodge** = a worker-free SINK: belt the current research item in (or place it beside a
  stock of it); it eats ≤ 1/sec → so throughput is capped by how fast you PRODUCE items. Build more
  Lodges + more makers to research faster.
- **UI:** top status bar shows `🔬 <item> pts/cost`; the Build panel shows the target age + a
  progress bar + items-remaining; the Lodge's panel shows holdings + pts/min.
- **Files:** `Research` (static tree + points), `ResearchBuilding` (the Lodge), `Colony.ResearchAdvance`.

---

# SYSTEM RATIOS (canonical v1 — the numbers must have logic)

Every throughput number hangs off **one base unit** so the player can reason, not guess:

> **The lane = 60 items/min (1/sec)** — the rate of a collector and a Conveyor belt. Everything is a
> clean multiple of it. The starter **Wooden Belt is a deliberate ½-lane (30/min)** so upgrading belts
> is an early, meaningful goal (the belt ladder: Wooden 30 → Conveyor 60 → Geared 120 → Steel 240).

| System | Rate | In lanes | Notes |
|---|---|---|---|
| **Wooden Belt** | **30/min** (interval 2.0s) | **½ lane** | the slow starter — half a collector's output, so it backs up until upgraded |
| **Conveyor Belt** | **60/min** (interval 1.0s) | **1 lane** | the reference; matches one collector. The first belt upgrade (cheap, early) |
| **Geared Belt** | **120/min** (interval 0.5s) | **2 lanes** | Bronze tier (research `geared_belts`) |
| **Steel Belt** | **240/min** (interval 0.25s) | **4 lanes** | Iron tier (research `steel_belts`) — the fastest |
| **Splitter / Merger** | **240/min** (interval 0.25s) | — | run at the top belt rate so they never throttle a line |
| **Collector** (auto, on its node) | **60/min** (2 units / 2.0s interval) | fills **1 lane** | fully automatic; no workers |
| **Processor** (auto) | **1 craft / 2.0s = 30 crafts/min** (slower exceptions exist, e.g. Sawmill 3.0s) | — | a 2-input recipe consumes **60/min = 1 lane** |

**The rule the player learns:** build MORE machines + lanes — and UPGRADE belts — to scale; all fixed rates, no workers.
Flagship chain: **1 Wood Hut (60 wood/min) → Wooden Belt → Sawmill.** The Wooden Belt carries only 30/min — half the Hut's output (so it backs up at the Hut) and less than the Sawmill wants (~40/min) — so the first lesson is **upgrade the belt** (research the cheap Conveyor, 60/min, and overlay it on the wooden belt) or run a 2nd line. Then deeper tiers (Geared 120, Steel 240) pace later growth. Scaling = more machines + lanes + belt tiers, never workers.

- **Workers scale linearly:** N workers = N× the rate (so a 2-worker machine needs 2 lanes in).
- **Deliberate exceptions** (slower = harder, by design, not by accident): distant/finite collectors (Mine, Gem Mine) at 2.5s; advanced recipes (Smelter/Toolmaker/Monument) at 3.5–6.0s. Everything *early* is on the clean baseline.
- **Where to read it live:** a machine's panel shows `Needs X/min` per input next to `1 belt lane = 60/min`, and `Output: Y/min`. Compare the two numbers to size your supply.

---

# PHASE 1 — SYSTEM AUDIT (what's actually wrong)

Honest findings from the real codebase, not generic ones.

### A. The core contradiction: a global "teleport" pool vs the logistics pillar  ⚠️ biggest
`Economy.Available()` treats **carried inventory + every building buffer + every storage** as one
pool, anywhere on the map. Survival (food/water), **comfort-good consumption**, and **build-cost
affordability** all draw from it. But the design pillar is *"logistics is the main source of
progression,"* and **local production** (workshops) already requires adjacency/belts. So the game
says two opposite things at once:
- *Workshops* must have inputs physically delivered (adjacency/belts/pipes). ✅ logistics matters.
- *Survival, comfort, and build costs* pull from a settlement-wide teleport pool that ignores
  distance, belts, and geography. ❌ logistics doesn't matter.
A collector in a far corner you've never connected still feeds your population and pays for
buildings. **This is the single biggest internal inconsistency** and it quietly undercuts every
geography/logistics system we've built (terrain, water barrier, pipes, depots).

### B. Overlapping / unclear logistics tools
Solids have **belts** (local) + **depots & caravans/vehicles** (long-distance). Liquids have
**pipes + pumps**. Construction has **builder-haul**. These are *four* movement systems with
*four* mental models. They're not contradictory, but the player has no single rule for "how do I
move X," and the belt-vs-caravan boundary ("when is something 'long distance'?") is fuzzy.

### C. Ages add content more than they transform mechanics
[AGES.md](AGES.md) commits to *"each age changes the rules."* Reality: only **Bronze** (pipes) and
**Industrial** (power) are genuine mechanic shifts. **Tribal** and **Iron** are mostly *new
buildings + new comfort goods* — i.e. the "Factorio-with-skins" trap the design explicitly warns
against. Progression-by-transformation is under-delivered in the middle ages.

### D. Shallow systems (depth deficits)
- **Forest / Hills** are *visual only* — no construction/movement/throughput effect. Geography is
  half a system: water constrains hard, everything else is paint.
- **Power** is a single global number — no grid, no regions, no spatial meaning (you can't *route*
  power, so it doesn't interact with layout the way belts/pipes do).
- **Routes/vehicles** are point-to-point A↔B with no multi-stop, scheduling, or network.
- **Comfort** is consumed from the global pool (see A) — it isn't local, so housing location and
  distribution are meaningless.

### E. Missing dependencies (systems that *should* touch but don't)
- **Power ✗ Liquids / Construction:** pumps are "mechanical/free," advanced belts and construction
  ignore power. At Industrial, power should be the constraint *over everything*, but it only
  touches workshops.
- **Geography ✗ Construction/Throughput:** terrain blocks placement (water) but never affects build
  time, belt/pipe routing cost, or movement (beyond the hard water wall).
- **Population ✗ Location:** labour is one global pool; workers teleport to jobs. Fine as
  abstraction, but it means geography never constrains *labour*, only materials.

### F. Unclear progression spine
There are ~40 buildings across 5 ages but no crisp "you must do A→B→C this age" backbone; the
objectives ladder guides it narratively, but the *systemic necessity* is loose, so a new player
can't infer the intended path from the systems alone.

---

# PHASE 2 — SYSTEM CONSOLIDATION (unify, don't add)

### Fix A (headline): one resource rule — **"Stored, not summoned"**
Collapse the dual economy into a single, geography-respecting model:

> **A resource is only usable once it has been DELIVERED to the settlement's stock (storage / HQ /
> the player's hands), or delivered ADJACENT to a local consumer. Nothing is consumed from a field
> buffer it hasn't been moved out of.**

Concretely: redefine the "pool" that survival, comfort, and build-costs draw from as
**carried + storages + HQ only** — *not* raw collector/workshop output buffers. Field production
must be transported (carry → belt → pipe → route) into storage to "count." Local production keeps
its adjacency rule. This **makes logistics matter for every system at once** and removes the
contradiction in (A) with *one* definition. (Implementation note: today `Economy.Available` sums
field buffers too; the change is to scope it to stored stock. Balance-sensitive — see Phase 5.)

This single rule is the spine the whole game hangs on. Everything below assumes it.

### Fix B: one logistics meta-pattern, four implementations
Every movement system is the **same shape** — *Source → Network/Carrier → Sink* — and every age
gives a better implementation. Document and present it that way so the player learns one model:

| | Source | Carrier | Sink | Distinct because |
|---|---|---|---|---|
| **Solids** | collector / workshop output port | belts (local) → vehicles+depots (cross-region) | input port / storage | discrete items, directed path |
| **Liquids** | pump at water terrain | pipe network (pressure/range + boosters) | water consumer / barrel | continuous flow, undirected network, no belts (`isLiquid`) |
| **Construction** | storage (materials) | builder squad (capacity = yards) | construction site | consumes materials over time; needs a build *workforce* |
| **People** | housing | (abstract commute) | jobs | the labour supply that powers the other three |

The belt-vs-vehicle boundary becomes a *clean rule*: **belts move things within a region; vehicles
move things between regions** (regions defined by geography — Fix C).

### Fix C: geography as one universal modifier
Replace ad-hoc terrain effects with a single per-tile model every system reads:
`Terrain → { buildable?, walkable?, beltable?, buildSpeed×, moveSpeed× }`.
- **Water:** not buildable/walkable/beltable except via **bridge**; the only liquid **source**.
- **Forest:** buildable but **buildSpeed×0.6 + moveSpeed×0.7** until cleared (yields wood). Resource-rich (wood/forage).
- **Hills:** **mining only** (or buildSpeed×0.5 to "terrace"); resource-rich (ore/stone/gems).
- **Plains:** the optimal factory ground (all ×1).
Rivers carve the map into **regions**; that's what makes "local vs cross-region" logistics real.

### Fix D: population is the single demand+labour engine
One model: **population consumes** (survival food/water + age-tiered comfort goods, all from
*stored* stock per Fix A) and **supplies labour** (workers for collectors/workshops/builders/
pumps). Growth needs surplus + housing; **Happiness** (comfort fulfilment) scales productivity and
growth. More people → more demand → must scale production *and* logistics → the core pressure.
Comfort being drawn from *stored* stock (Fix A) finally makes distribution/housing location matter.

---

# PHASE 3 — EXPANSION (only the shallow parts from the audit)

Expand **only** where Phase 1 found insufficient depth.

1. **Make the middle ages transform, not decorate (audit C).** Assign each age ONE rule-change, so
   progression is mechanical (this is the AGES.md thesis, enforced):
   - **Stone** — *manual + adjacency*: you are the logistics.
   - **Tribal** — *workforce & local flow*: assign labour; first belts + dug water channels; the
     "stored, not summoned" rule starts to bite (you must carry/belt to storage).
   - **Bronze** — *networks*: belts + pipe networks; output/input ports make layout a puzzle.
   - **Iron** — *regions*: finite ore/gems force expansion across rivers/biomes → depots + vehicles
     become mandatory (cross-region logistics is the new problem, not just "more").
   - **Industrial** — *power as a universal constraint*: machines, advanced belts, **and pumps** all
     draw power; brownouts cascade; ratios + power-routing dominate.
   - **Modern/Future** — demand-pull networks → goal/agent automation → **Pattern** (logistics
     becomes information/bandwidth) → **Entropy** (efficiency gains a cost; homeostasis). (AGES.md.)
2. **Geography depth (audit D):** implement the Forest/Hills modifiers from Fix C (the cheapest way
   to turn geography from half a system into a whole one).
3. **Power becomes spatial at Industrial (audit D/E):** regional power (a generator powers a radius
   / a connected "wire" network), so power *routing* joins belts and pipes as a layout problem —
   and pumps/advanced machines depend on it (closes the Power✗Liquids gap).
4. **Vehicles → real routes (audit D):** multi-stop routes + per-route item filters, so cross-region
   logistics is a planning activity, not a single A↔B shuttle.

That's it — **no new top-level systems**. Every expansion deepens an existing one or wires two
existing ones together.

---

# PHASE 4 — CROSS-SYSTEM INTEGRATION MAP

```
                         ┌──────────────┐
            consumes ◀───│  POPULATION  │───▶ supplies labour
        (food/water/     └──────┬───────┘     (collectors, workshops,
         comfort, from          │              builders, pumps)
         STORED stock)          │ demand scales with pop × age
                                ▼
   ┌───────────────────────────────────────────────────────────┐
   │                       LOGISTICS                             │
   │  Solids (belts→vehicles)   Liquids (pipes+pumps)            │
   │  Construction (builder haul)   — all: Source→Carrier→Sink   │
   │  RULE: "stored, not summoned" — nothing teleports           │
   └───────▲───────────────────────────────────────────▲────────┘
           │ shapes routes, regions, build sites         │ powers (Industrial+)
           │                                             │
   ┌───────┴────────┐                            ┌───────┴────────┐
   │   GEOGRAPHY    │ water=liquid source +      │     POWER      │
   │ (terrain/rivers)│ hard barrier; biomes bias  │ (global→regional)│
   │                │ resources; friction ×      │                │
   └────────────────┘                            └────────────────┘
                         ▲
                         │ each AGE changes the RULES above
                   ┌─────┴──────┐
                   │    AGES    │
                   └────────────┘
```

**Key interactions (each is a *rule*, not a vibe):**
- **Geography → Logistics:** rivers split the map into regions → belts work in-region, vehicles
  cross regions; water needs bridges (feet/belts) or pipes (liquids); forest/hills slow build/move.
- **Geography → Construction:** build time scales with the site's terrain (forest/hills slower).
- **Geography → Population/Liquids:** water terrain is the *only* early liquid source → your base
  location is chosen around water + resources.
- **Population → Everything:** demand (survival+comfort) drains *stored* stock → forces production +
  logistics to scale; labour supply caps how many systems can run at once.
- **Power → Logistics/Construction (Industrial):** advanced belts, pumps, smelters need power;
  under-supply browns out the whole region → power-routing is a layout problem.
- **Liquids ≠ Solids (enforced):** continuous network vs discrete belts; `isLiquid` forbids water on
  belts; pumps/pressure vs splitters/ports. Different problems, same Source→Carrier→Sink skeleton.
- **Ages → Behaviour:** each age changes the *rule* of one system (manual→flow→networks→regions→
  power→abstract), never just the building list.

---

# PHASE 5 — SECONDARY REVIEW (risks from the above + mitigations)

- **Fix A makes early game harsh?** If field buffers no longer count, a fresh player could stall.
  *Mitigation:* the **HQ is starting storage**; ship 1–2 pre-set storages; keep generous early
  buffers; and the player can always hand-carry. The rule should feel like "bring it home," not a
  wall. Roll it out with telemetry on time-to-first-stall.
- **Player-understanding of "stored vs field":** must be legible. *Mitigation:* the top-bar totals
  already read storage; label field-only goods distinctly; the hover tooltip + finder arrows guide
  delivery. A one-time tip when a collector backs up (already exists for workshops) generalised.
- **Early/mid/late balance:** comfort escalates to 5 goods by Industrial (crater risk). *Mitigation:*
  stagger comfort unlocks; tune `comfortTick` / per-good want; never let a *new* comfort tier drop
  Happiness below the growth threshold instantly.
- **Overcomplication:** Phase 3 adds *rules to existing systems*, not new systems — the net change
  in player-facing concepts is roughly **zero** (and Fix A *removes* the confusing dual economy).
- **New contradictions introduced?** Re-checked: regional power (Fix 3) must not retro-break the
  current global-power MVP → ship as an Industrial upgrade *over* the global baseline, not a
  replacement (continuity rule). Multi-stop routes must not obsolete belts → keep the in-region vs
  cross-region boundary (Fix B).

---

# PHASE 6 — FINAL REFINEMENT OUTPUT

## 1. Unified System Architecture
**One simulation, five subsystems, one rule.** Population (demand + labour) and Geography (space +
sources + friction) generate *pressure*; Logistics (solids/liquids/construction, all
Source→Carrier→Sink, all obeying *"stored, not summoned"*) relieves it; Power is the Industrial
constraint layered over logistics; Ages change the *rules* of each. Complexity emerges from **scale
× geography × age**, never from bolted-on mechanics.

## 2. Clean Age Progression Model (each age = one rule change)
| Age | The rule that changes | New problem | Old solutions stressed |
|---|---|---|---|
| Stone | manual + adjacency | survival, my own time | — |
| Tribal | labour + local flow; *stored-not-summoned* begins | feeding chains, division of labour | hand-carry can't keep up |
| Bronze | **networks** (belts + pipes + ports) | layout/topology, liquid vs solid | adjacency-only stops scaling |
| Iron | **regions** (finite resources, cross-river) | expansion + cross-region supply lines | local belts can't reach |
| Industrial | **power** over everything (+ regional) | ratios, brownouts, power-routing | "just add a machine" now costs power |
| Modern→Future | demand-pull → agents → information → entropy | specification, bandwidth, stability | hand-tuning / maximising stop working |

## 3. Core Gameplay Loop (what the player is ALWAYS doing)
**Observe a shortage → trace it to a logistics/geography bottleneck → redesign or extend the
network → growth (population/scale) creates the next shortage.** At every age the *verb set* is the
same (source, move, store, consume, expand); only the *tools and constraints* change.

## 4. System Interaction Map
See the Phase 4 diagram + rule list. The load-bearing edges: **Population→demand→Logistics**,
**Geography→regions→Logistics**, **Power→Industrial logistics**, **Ages→rules-of-each**.

## 5. Identified Problems → Fixes (traceable)
| # | Problem (Phase 1) | Fix (Phase 2/3) | Status |
|---|---|---|---|
| A | Global teleport pool vs logistics pillar | "Stored, not summoned" — one resource rule | ✅ BUILT (`Economy.StoredOnly`, F9; liquids carried to barrels) |
| B | 4 logistics tools, no shared model | One Source→Carrier→Sink pattern; in-region vs cross-region rule | 🔶 mostly exists, needs framing + the boundary rule |
| C | Middle ages decorate, don't transform | One rule-change per age (table above) | 🔶 Bronze/Industrial done; Tribal/Iron need their shift |
| D | Forest/Hills/Power/Routes shallow | Terrain friction; regional power; multi-stop routes | 📝 expansion targets |
| E | Power/Geography don't touch construction/liquids | Industrial power gates pumps/belts; terrain affects build time | 📝 |
| F | No legible progression spine | Age-rule table + objectives already guide it | 🔶 |

## 6. Expansion Opportunities (only what emerges naturally)
- **Pollution / environment** (future ages) rides the *same* network model (a "negative fluid" on
  the pipe/air graph) — fits Entropy age; no new system.
- **Pressure model for power** mirrors the liquid pressure model (range + boosters → wires +
  substations) — reuse the proven mechanic.
- **Information/Pattern age** reuses the network graph with bandwidth instead of throughput.
None of these are needed now; they're listed because they fall out of the unified model, not
because they add anything.

---

## Recommended implementation order (turning this doc into game)
1. ✅ **Fix A — "stored, not summoned"** DONE — `Economy.StoredOnly` (carried + storages only,
   F9 to compare); solids back up / belt to storage, liquids are worker-carried to barrels.
   Big world (terrain half 200, ~400 units) + far frontier resources shipped alongside, so the
   change has somewhere to play out. *Watch:* early-game balance (see Phase 5 mitigations).
2. **Geography friction (Fix C / Phase 3.2)** — forest/hills build+move modifiers. Cheap, turns
   geography into a whole system. ← NEXT
3. **Tribal & Iron rule-shifts (Phase 3.1)** — make the mid ages transform (labour specialisation;
   region-gating via finite resources + cross-region vehicles).
4. **Industrial power → regional + gates pumps/belts (Phase 3.3 / Fix E).**
5. **Multi-stop routes (Phase 3.4).**
Each is one system deepened or two wired together — never a new disconnected mechanic.
