# CavemanPrototype — The World as a System

The world must **constrain, shape, and challenge** factory expansion — geography as a system,
not a backdrop. It should create problems that force expansion, force logistics, and force
layout redesign.

Status: ✅ built · 🔶 partial · 📝 designed, not yet built.

---

## Foundation — the biome grid ✅ (Step 1)
`TerrainGrid`: a per-cell biome map (**Plains / Forest / Hills / Water**) generated from
layered Perlin noise (elevation + moisture), rendered as a point-filtered biome texture.
A clear plains **basin** around spawn keeps the early game compact. This is the substrate
every system below hangs off — terrain is now queryable (`At`, `Buildable`) by placement,
belts, movement, and worldgen.

## 1. Meaningful terrain (effects, not decoration)
- **Water (rivers + lakes)** ✅ is now a **hard barrier**: the player **cannot walk on it**
  (per-axis movement block, slides along shores) and buildings/belts can't sit on it.
  Winding **rivers** (noise-band) snake across the map and **divide it into regions**. Water
  is real **terrain**, not abstract nodes.
- **Bridges** ✅ — a plank tile placed ON water (drag to span a river) that makes the cell
  passable for **feet + belts** (not normal buildings). Placed instantly (builders can't
  reach water); strategic chokepoints; cheap now (3 wood) — 📝 tune cost/age-gate later.
- **Forest** 📝 — resource-rich (wood/forage bias) but **slower to build** (construction-time
  multiplier on sites placed on forest) and **slower to move** through, until **cleared**
  (a clear-land action that yields some wood and converts the cell to plains). Trade-off:
  rich but high-friction.
- **Hills** 📝 — the **mining** biome (ore/stone/gems bias) but **hard to build on** (only
  mines/limited structures, or a costly "terrace/level" action to flatten). Forces mines out
  to the hills and a supply line back.
- **Plains** ✅ default — the **optimal** factory ground (cheap, fast, unconstrained).
- Implementation hook: `TerrainGrid` already exposes per-cell type; add `BuildSpeedMul`,
  `MoveCostMul`, and `BuildableBy(kind)` lookups + the clear/level/bridge actions.

## 2. Biome-based regional differences 📝
Bias **resource spawns by biome** (forests carry wood/berries/cotton, hills carry
ore/stone/gems, water edges carry fish/clay, plains are open). Generate terrain first, then
place each resource only on its biome. Result: **different regions demand different
strategies** — a wood economy lives in forest, a metal economy out in the hills, and you must
connect them. (Currently resources are placed by fixed direction; this step makes placement
biome-driven, replacing the hand-placed clusters.)

## 3. Expansion = a logistics problem 🔶
Water already forces routing ✅. With biome resource bias (above), the **richest deposits sit
in distant, awkward biomes**, so you need: belts around water, **remote outposts** (a
collector + small storage cluster out at the resource), and **supply lines** (caravan routes
/ future trains) back to the core. Distance + terrain friction make expansion a deliberate
act, not a free spread. Ties directly into the logistics tiers already in the game
(belts → depots/caravans → vehicles).

## 4. Terrain-based build restrictions 🔶→📝
- ✅ Water blocks building/belts now.
- 📝 **Modify-to-build**: clear forest, level hills, bridge water — each an action with a
  cost and an **age gate** (e.g. bridges at Bronze, hill-levelling at Iron), so the world
  **opens up progressively with technology**. Early you're hemmed into the basin; later ages
  literally unlock more of the map. This is the world's version of the AGES.md progression.

## 5. Structured scaling (not a big empty map) 🔶
- **Big world shipped:** terrain `half` is now **200 (~400 units across, ~2.8× area)**, fog
  `worldSize/res` 420, camera maxZoom 140 / overview 130. The start stays compact (basin 15 +
  near resources), and **frontier resource clusters** (incl. finite ore/gems) are scattered far
  out (±100–180) as expansion targets. With "stored, not summoned" + finite distant ore, reaching
  and supply-lining the frontier home is the late-game economy. ✅
- **Early:** the plains **basin** ✅ — compact, everything reachable on foot.
- **Mid:** the basin fills; surrounding biomes hold what you now need, gated by distance (and 📝
  terrain friction) → you push outward and build outposts.
- **Late:** a **multi-region logistics network** 📝 — specialised regions (forest lumber, hill
  metals, plains assembly) tied by long-haul transport. Tie region unlocks to ages so scale is paced.
- Knobs: `TerrainGrid.Generate` **half (200)** + fog `worldSize` (must match), basin radius (15),
  water/forest/hills noise thresholds, frontier cluster distance.

---

## Anti-goals (from the brief)
- ❌ Purely decorative terrain · ❌ large empty maps with no impact · ❌ visual-only expansion.
- ✅ Geography that constrains logistics/construction/efficiency and forces redesign.

## Build order
1. ✅ Biome grid + water-blocks-building + basin (done).
2. **Biome resource bias** (req 2) — highest impact next: makes regions mean different things.
3. **Bridges + clear/level actions, age-gated** (req 1/4) — water/forest/hills become
   solvable, and the map opens with tech.
4. **Forest/hills build-speed & movement effects** (req 1) — friction that rewards plains.
5. **Region/scaling pass** (req 5) — paced multi-region unlocks.

## Balance / knobs (Step 1, first-pass)
- `TerrainGrid.Generate(half, seed, basinRadius)`: water threshold `e < 0.34`, hills `e > 0.70`,
  forest `m > 0.62`. Tune so water is a *winding obstacle*, not a maze, and the basin feels
  open. Resource `ClearAround` radius = 2.5. Watch: too much water near spawn = frustrating;
  too little = no constraint.
