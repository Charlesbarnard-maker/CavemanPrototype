# CavemanPrototype — Liquids: a Core System that Evolves Across Ages

The rule: **liquids exist in every age** and are a logistics system distinct from solids —
but they are **never introduced as a brand-new system**. The same problem ("get water from a
map feature to where it's needed") is solved with continuously more powerful tools as you
advance. Same goes for all logistics (solids/transport/construction): every age has them; the
*form* evolves.

Status: ✅ built · 🔶 partial · 📝 designed.

---

## Why liquids ≠ solids
Solids (belts) move **discrete items** cell-by-cell along a **directed path**. Liquids move
as **continuous flow over a connected network** — you build connectivity, not a path, and a
source pushes fluid to every sink the network reaches. Different problems: **network
starvation** (one source can't feed all sinks), **topology** (is the sink actually connected?),
and later **pressure/range**. This is a separate mental model the player must learn.

## Age progression (the same water problem, evolving tools)
| Age | Form of liquid logistics | Status |
|---|---|---|
| **Stone** | **Hand/local**: the **Water Hole** collector draws from adjacent water terrain; workers + the player carry it. Highly local, labour-bound. | ✅ (exists) |
| **Tribal/Bronze** | **Constructed channels → pipes + a mechanical Pump**: pump sits on a river/lake and pushes water through a connected **Pipe** network into your Water Barrels — no carrying. Limited range, topology-dependent. | ✅ (built this pass) |
| **Industrial** | **Pressurised networks**: higher-throughput pipes, multiple pumps, 📝 **pressure/range** (far sinks starve unless you add pumps), 📝 valves/junctions, fluids beyond water (steam/oil for cooling/power). | 📝 |
| **Late / Unknown** | **Abstracted fluids**: energy-like fluids / data flow on the same network substrate; highly automated, multi-layer distribution (ties into AGES.md Pattern/Entropy ages). | 📝 |

Each step **reuses the prior idea** (a source → a network → sinks) and adds capability — it
never replaces the system wholesale. Early carried-water still works in later ages; pipes just
make it scale.

## What's built this pass ✅
- **`Pipe`** — undirected liquid-network segments (drag to lay, on land or bridged water).
  Carry no items; they're pure connectivity (`PipeNet` registry).
- **`WaterPump`** (Bronze) — placed next to **water terrain**; each tick it floods its
  connected pipe network and tops up every **Water Barrel** the pipes reach. Continuous flow,
  no workers. Out of water / disconnected → no flow.
- This already delivers the distinct model + the three liquid problems in embryo: **starvation**
  (pump flow rate vs barrel demand), **topology** (only reached barrels fill), **geography**
  (must source at a real river/lake — reinforces world layout).

## Distinct liquid problems (req 2)
- **Supply/pressure** 🔶→📝: today a flat per-tick flow split across sinks; 📝 add pressure that
  falls with distance so far sinks need booster pumps — the headline Industrial problem.
- **Network starvation** ✅: too many sinks for the pump's flow → some don't fill. Visible via
  barrels not topping up.
- **Pumps/distribution nodes** ✅ (pump) → 📝 valves, junctions, tanks (buffer + pressure).
- **Topology sensitivity** ✅: a barrel only fills if pipes actually connect it to the pump.

## Geography integration (req 3) ✅
Pumps can only source from **water terrain** (rivers/lakes/coast from `TerrainGrid`), so where
water is on the map directly shapes where liquid infrastructure goes — and rivers (which divide
regions) make piping across them a real routing problem (bridge the river, run pipe over it).

## Cross-system continuity (the unifying rule)
Every logistics system already follows this: **solids** (carry → belts → faster belts →
vehicles), **construction** (manual builders → Construction Yards → 📝 automated), **transport**
(carry → caravans → 📝 trains), and now **liquids** (carry → pipes+pump → 📝 pressurised →
abstract). The player solves the same fundamentals with better tools over time.

## Build order
1. ✅ Pipes + mechanical pump (Bronze) — the core distinct model.
2. ✅ **Pipe-fed consumers** — the pump now fills adjacent water-using workshops
   (Campfire/Farm/Bakery) DIRECTLY into their input buffer, as well as barrels. Barrels are
   now optional **buffers** (evolution, not replacement): pipe a pump straight to a Campfire
   and it runs without any barrel; add a barrel to buffer surges. Pipe feeding ignores the
   workshop's belt input-port side for now (pipes are a separate network).
3. ✅ **Pressure/range + booster pumps** — a pump's pressure now fades over distance: it floods
   its network tracking pipe-steps and only feeds sinks within `range` (16). Far consumers
   STARVE on big networks ("works locally, breaks at scale") until you place a **Booster Pump**
   on the run — it resets pressure to full, extending reach another full range; chain several
   for long hauls. Solid liquids never ride belts (`isLiquid`). NOTE: pressure is a simple
   distance model (not true fluid pressure/back-pressure) — fine for the gameplay; deepen later.
4. **More fluids** 📝 — steam/oil for power & cooling (industrial water usage), then abstract
   fluids in the future ages.

## Balance knobs (first-pass)
`WaterPump.flowPerTick` (4) + `interval` (0.5s), pipe cost (1 stone), pump cost (4 planks/4
stone), unlock age (Bronze). BFS guard cap 1024 cells.
