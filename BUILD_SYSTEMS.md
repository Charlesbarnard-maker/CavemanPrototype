# CavemanPrototype — Build Systems: Placement, Menu, I/O Slots, Planning

Design for four interlocking systems. Principle throughout: **system design over UI
convenience, planning over reaction, logistics depth over raw speed.** Buildings are
**nodes in a resource network**, not isolated objects.

Status: ✅ built · 🔶 partial · 📝 designed, not yet built.

---

## 1. Placement UX — mass vs intentional
**Problem:** one placement system for everything → click-drag spam accidentally mass-places
expensive/key buildings.

**Model: placement behaviour is a property of the building, not one global mode.**
- **Drag-placeable (mass infrastructure):** belts ✅ (and 📝 pipes, walls, paths). Click-drag
  lays a continuous run; the belt path auto-snaps corners and 90° lines.
- **Intentional (structured buildings):** production/storage/housing/power/yards. ✅ Now
  place **one per deliberate click** (no hold-to-spam), stay in placement mode for repeats,
  right-click/Esc to finish. A `PointerOverUI` guard stops a menu click from also dropping
  one.
- 📝 **Confirm step for "key"/expensive buildings** (Town Hall-tier, Monument): a flag
  `confirmPlacement` → ghost stays until a second click (or Enter) commits; Esc cancels. Use
  sparingly so it never nags for cheap buildings.

This already removes accidental spam. The fuller answer is the **planning layer (§4)**, where
*nothing* is committed until you execute.

**Visual language (consistent across modes):** green ghost = valid, red = invalid, footprint-
sized; drag-placeables show the whole run as ghosts; intentional buildings show a single
ghost that follows the cursor.

---

## 2. Build-menu structure — hierarchical & scalable
Goal: find any building in **1–2 interactions**, even late game.

**Built ✅:** category headers (Gathering / Workshops / Belts / Routes / Power / Construction
/ Storage / Housing), **collapsible** (click a header to fold), a **Recent** row of your
last-placed buildings, age-gated (locked shown greyed with unlock age + hover description),
number-key shortcuts for the top entries, hover tooltips.

**Category structure (target):**
```
Production   → Gathering · Workshops · Power
Logistics    → Belts · Depots/Routes · (📝 Pipes)
Construction → Yards · (📝 blueprints)
Settlement   → Housing · Storage
(📝 Defence, when it exists)
```
- 📝 **Subcategory by age/system** inside a category once lists get long (e.g. Workshops →
  Food / Materials / Refining), shown as a second collapse level.
- 📝 **Pinned** buildings (a star toggle) above Recent, persisted per session.
- **Search:** intentionally *not* a typed field — it conflicts with the game's WASD/B key
  polling (typing would move the player / toggle the menu). Collapse + Recent + pins cover
  findability without that conflict. (If we later add a proper modal with input capture, a
  search field can live there.)

---

## 3. Input/Output slots — buildings as network nodes (CORE)
The deep system. Today a workshop pulls inputs from **any adjacent** cell and outputs to
**any adjacent** cell (local production). The I/O-slot system makes I/O **directional and
positional**, so layout/routing becomes the puzzle. This is the same subsystem as the
**footprint I/O ports** already queued (see KNOWN_ISSUES) — this is its formal design.

**Data model (`BuildingDefinition`):**
- `footprintW/H` ✅ (multi-cell already in).
- `inputs[]` ✅ (recipe items) and `outputPort` / `inputPorts[]` 📝: each port is
  `{ side: N/E/S/W, cell-offset within footprint, item-filter (optional) }`.
- Buildings **rotate** (R) → ports rotate with them.

**Behaviour 📝:**
- A belt/pipe may only **deliver** to a cell that is an **input port**, and only **pull**
  from a cell that is an **output port** (replaces "any adjacent cell"). Wrong side = no
  connection (clear red feedback).
- Multi-input buildings get **one input port per input type** (e.g. Smelter: an Ore port and
  a Charcoal port on different sides) → you must route *each* feedstock to its *own* side.
  This is the "force meaningful logistics planning / natural bottlenecks" payoff.
- Output port(s) feed the next chain link. Splitters/mergers (📝) manage fan-out.

**Visual 📝:** input port = a small **notch** (item-tinted), output port = an **arrow**, on
the building edge. Belt connects only when its head/tail aligns with a matching port; a
mis-aligned belt shows red (reuses the existing dead-end red).

**Scales across ages:** Stone/Tribal — single in/out, adjacency-forgiving. Bronze/Iron —
multi-input ports force real routing. Industrial+ — many ports, port priorities, and (per
AGES.md) network/demand-pull routing where ports become network endpoints.

**Why staged:** this touches placement, belts, rotation, and rendering — a bug breaks
*placing/connecting anything*. Build it as a dedicated supervised pass with a compile/test
loop (it's task #9), AFTER the footprint *size* system is confirmed stable.

---

## 4. Planning vs Execution layer (📝 recommended core feature)
Turns building from reaction into design — and structurally prevents accidental/ messy
expansion.

**Planning mode (a toggle, e.g. P):**
- You place **ghost** buildings/belts freely — full factory sections — with **no resources
  consumed** and no construction started.
- Ghosts show footprints, ports, and belt runs; invalid overlaps flag red. You can move/
  delete ghosts, copy/paste blocks, and see the **total material cost** of the plan.
- This is also the natural home for **blueprints** (save/stamp a designed block).

**Execution:**
- Hit **Execute** (or commit a region) → every ghost becomes a **ConstructionSite**, and the
  existing construction system takes over: builders (scaled by Construction Yards) haul
  materials from storage and build, spreading into visible builder slots. Build order can
  follow priority/zone.
- Materials are consumed **as builders deliver** (already how sites work), so the plan's cost
  is paid over time by your logistics — not instantly.

**Technical sketch:** a `PlanGhost` component (no economy footprint) + a `PlanLayer` manager
holding the ghost set; Execute iterates ghosts → `ConstructionSite.Spawn`. Reuses footprint
occupancy for validity and the construction/builder system for execution. Visual: ghosts at
~40% alpha with a blueprint tint; a plan-cost readout; an Execute button.

**Player experience across ages:** Early — place directly (planning optional, low
complexity). Mid — plan structured production chains before building. Late — design entire
factory sections as systems, then execute; **logistics complexity becomes the main
challenge**, exactly as targeted.

---

## Build order (recommendation)
1. ✅ Placement separation + menu collapse/recent (done — immediate pain).
2. **I/O ports (§3)** — the highest-impact *system* change; it's what makes buildings a
   network and routing the game. Do it next as the supervised footprint-ports pass.
3. **Planning/Execution (§4)** — layer on top once ports exist (plans need ports to be
   meaningful). Blueprints fall out of this.
4. Polish: pinned buildings, subcategories, confirm-step for key buildings, pipes/walls as
   drag-placeables.
