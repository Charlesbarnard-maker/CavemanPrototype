# CavemanPrototype — Construction as a System

Goal: construction should feel like **another production chain you optimise**, not "a delay
before the building appears" — and the player must **see the system working** at all times.

> Build faster by designing better construction systems (yards, supply, logistics), never
> by nudging a cap slider or letting all population build for free.

Status key: ✅ built · 🔶 partial/built-this-pass · 📝 designed, not yet built.

---

## 1. Construction as a system — what it already is
A construction site is **not** a timer. ✅ Each `ConstructionSite` holds an outstanding
**material list** (from the building's cost). The colony's builders must physically:
1. **Fetch** materials from a storage that holds them (consumed from the pool on pickup,
   reserved so builders don't over-fetch), then
2. **Deliver** them to the site, then
3. **Build** (progress only accrues once all materials are delivered — `MaterialsDone`).

So construction already depends on **resource delivery + workforce**, and a material the
site needs but you aren't storing/producing **stalls the build** — exactly the
"production chain" framing. What was missing was **scaling** and **visible feedback**.

## 2. Scaling mechanics — Construction Yards ✅ (built this pass)
The builder cap is no longer a fixed 2. `Colony.MaxBuilders = baseBuilders(2) +
Σ ConstructionYard.slots`. A **Construction Yard** (build-menu → Construction) adds **+3
builder capacity** each. To build faster you must:
- **Build & place yards** — they cost resources and space (infrastructure), and
- **have spare population** to actually fill the new builder jobs (builders draw from
  `FreeWorkers`), and
- **keep materials flowing** to storages (supply rate caps build speed — see §3).

Three independent constraints = scaling requires *planning*, not a slider. Anti-overpowered
by construction: no "all population builds free" — every builder is a person taken off
gathering/working, gated by yard capacity.

📝 *Future scaling layers:* construction **zones** (drag a region; everything inside queues
and builds by priority); a **build queue** panel (reorder/prioritise pending sites);
per-yard **worker assignment** (staff a yard like a workshop instead of the global squad).

## 3. Logistics integration 🔶
Builders haul materials **storage → site** today, so **supply rate already gates build
speed**: if a site needs planks and no planks are stored/produced, builders idle at the
site and progress stalls (the bottleneck is visible — they're standing around with nothing
to deliver). 📝 *Deepen it:* let **belts/depots deliver materials directly to a site's
input buffer** (a site becomes a temporary belt sink), so for big late-game builds you wire
supply lines to the construction zone instead of relying on hauler trips. This is the
natural merge of construction with the belt/route logistics already in the game.

## 4 & 5. Visual clarity — builder slots ✅ (built this pass, CRITICAL)
Every `ConstructionSite` has **8 builder slots** arranged in a ring around its footprint.
A builder **claims a free slot** and stands at it to deliver/build, so multiple builders
**spread out visibly** around the structure instead of stacking on one pixel. While actively
building, a builder **pulses** (scale) — a clear "work happening here" cue. Reading the site
now instantly tells you *how many builders are contributing*.

Overflow handling (more builders than slots): extra builders get slot **-1** — they **still
add build progress** (speed keeps scaling) but aren't given a unique position (abstracted),
keeping visuals clean. 8 slots is plenty for normal play; only very large yard counts
overflow.

📝 *Polish later:* distinct carry vs hammer poses/sprites; scaffolding that fills in with
`BuildFraction`; small progress-burst particles on each delivery.

## 6 & 7. Bottlenecks by stage (the intended difficulty curve)
- **Early — builder scarcity.** Base 2 builders; manual construction is slow but fine at
  small scale. You feel "I can only build one thing at a time."
- **Mid — material shortages.** As you queue many/again-bigger buildings, the bottleneck
  moves to **supply**: builders outrun your stored planks/bricks, so you must scale
  *production + storage near where you build*. Building competes with your economy for goods.
- **Late — logistics inefficiency.** Builders walking far for materials is slow; the fix is
  **infrastructure**: yards near the work + (📝) belt/depot-fed construction zones so whole
  areas go up quickly. The bottleneck becomes *layout/routing*, the core optimisation game.

## 8. Progression across ages (ties into AGES.md)
- **Stone/Tribal — manual builders.** A small HQ squad hauls and builds by hand. Yards
  unlock at Tribal so you can begin organising teams.
- **Bronze/Iron — organised construction teams.** Multiple yards + dedicated material
  storages near build areas; construction is a managed workforce + supply problem.
- **Industrial+ — efficient/automated construction.** 📝 Belt/depot-fed construction zones,
  and eventually (per AGES.md Automated age) **constructor agents** that build toward
  declared targets — construction becomes a system you *specify*, not staff. Population's
  role in building abstracts the same way it does everywhere (workers → capacity → agents).

---

## What's built vs not (so the next session knows)
- ✅ Material-haul construction (pre-existing), ✅ builder slots + pulse, ✅ Construction
  Yards scaling `MaxBuilders`.
- 📝 Build queue/zone UI, per-yard staffing, belt-fed construction sites, builder
  carry/hammer sprites, scaffolding fill, constructor agents (Automated age).

## Balance knobs (first-pass, tune on playtest)
- `Colony.baseBuilders` (2), Construction Yard `builderSlots` (3) + cost (wood 12/stone 8)
  + unlock age (Tribal).
- `ConstructionSite.SlotCount` (8), slot ring radius, builder `moveSpeed`/`carryCapacity`,
  `ConstructionSite.buildTime`.
