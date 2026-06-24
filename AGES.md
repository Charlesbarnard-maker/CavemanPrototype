# CavemanPrototype — The Ages: Mechanical Transformation Design

The differentiator. Most factory games are **one ruleset with a tech tree on top** — you
unlock fancier buildings but play the same game from hour 1 to hour 100. Our bet is the
opposite:

> **Each age changes the RULES of the game, not just the contents.** The core loop stays;
> the *kind of problem you solve* and the *unit you think in* transform.

This doc defines the age ladder, what mechanically changes at each step, the transition
"shock" that retires your old solutions, and two genuinely novel post-modern ages.

---

## The invariant: the core loop never changes
At every age:
**build a system → it scales → a bottleneck emerges → you redesign/expand → growth creates
a new class of problem.** What changes per age is the *dominant constraint* and the *unit of
the puzzle*. If a proposed mechanic doesn't preserve this loop, cut it.

## The design test for "is this a real age?"
A new age must do **all four** (else it's a skin):
1. **Change system logic** — *how* production/logistics/labour fundamentally works.
2. **Introduce a new problem TYPE** — not "more demand," a different category of challenge.
3. **Stress or invalidate old solutions** — yesterday's optimal layout becomes inefficient.
4. **Force a new way of thinking** — the player must adapt their mental model, not just scale.

## The two axes that transform
- **The unit of the puzzle:** a building → a line → a factory block → a network/graph → a
  policy/goal → an information flow → a homeostatic system.
- **The dominant constraint:** my own hands → labour & adjacency → throughput & ratios →
  routing & congestion → specification & emergence → bandwidth & latency → entropy & stability.

## Population: a system resource that evolves
Population is always demand + capacity + scaling pressure, but its FORM changes:
physical workers → managed workforce → machine operators → abstracted capacity/policy →
compute/management → bandwidth/attention → stability-attention. It is never "just a number
that goes up" — each age changes what it *means* to have more people.

## Feasibility doctrine (why this is indie-buildable)
Every age **reuses the same engine primitives** — a grid, nodes, links, buffers, a global
resource meter, and status colours. A new age is mostly **new RULES and new CONSTRAINTS on
the existing substrate**, not a new engine or art-heavy system. "Power," "requests,"
"bandwidth," "entropy" are each *one global/regional value + a rule about how links behave*.
Readability in 2D comes from colour, pulse, and link thickness we already use for belts and
status dots. **Ages are new physics, not new renderers.**

---

# The Age Ladder

## Age 1 — Stone — *"I am the machine."*  (Manual / Adjacency)
- **System logic:** You hand-gather. Collectors need assigned workers. Production is purely
  *local* — a workshop eats only what's belt-fed or in an adjacent tile.
- **Unit:** a single building. **Constraint:** your own time + raw scarcity.
- **Problem type:** hand-to-mouth survival; nothing runs without you.
- **Tools:** gather, place huts, assign your handful of workers.
- **Player tools / population:** a few physical workers you micro.
- **Why you leave:** too much to carry, too many huts to hand-feed. *(All present today.)*

## Age 2 — Tribal/Bronze — *"Set up flows so I don't carry."*  (Workforce & Flow)
- **System logic:** Population becomes a **managed labour pool**; belts + haulers move goods;
  first multi-step chains (A→B→C). Adjacency still rules, so layout matters.
- **Unit:** a production line. **Constraint:** division of labour + feeding chains.
- **Problem type:** who works where, and keeping every link supplied; bottlenecks now
  *cascade* downstream (local production).
- **Tools:** belts, storages, comfort/happiness demand sink, worker assignment.
- **Population:** workforce you allocate across jobs.
- **Why you leave:** hand-laid belts + adjacency get unmanageable as the base sprawls.
  *(Largely present today — this is roughly where the game is now.)*

## Age 3 — Industrial — *"Balance the whole factory, not each machine."*  (Throughput, Ratios & POWER)
**This is the first true mechanic shift — and the one to build first to prove the thesis.**
- **System logic shift:** machines dominate; population stops *doing* the work and shifts to
  **operating/maintaining** it. A new global/regional resource — **POWER** — gates everything.
- **New mechanic: POWER.** Producers draw power; build a grid + plants; over-draw causes
  **brownouts** (everything slows). "Just add another machine" is no longer free — it costs
  power and trips the whole region if you're maxed.
- **Unit:** a factory block + its ratios. **Constraint:** throughput balancing + power budget.
- **Problem type:** **ratios** (how many smelters feed how many toolmakers), shared
  intermediates (coal/steam) creating cross-factory chokepoints, belt **congestion**.
- **Tools:** power plants & grid, splitters/mergers, faster belts, **blueprints/copy-paste**
  (the tedium-killer that makes block-thinking viable).
- **Mindset shift:** from "make this machine work" to "balance the system's flows + power."
- **Why you leave:** spatial sprawl + a tangled power web hit a wall; hand-placing every link
  and balancing every ratio by hand stops scaling.

## Age 4 — Modern — *"I design a system that routes itself."*  (Networks, Demand-Pull & Routing)
- **System logic shift:** physical belts give way (partly) to a **logistics NETWORK** — grids/
  pipes/drones that route on a topology you *configure*, not a path you hand-lay. Flow flips
  from **push** to **demand-pull**: sinks *request*, the network *delivers*. Population becomes
  **abstracted** — "operators/technicians" as a capacity number + policy, not individual NPCs.
- **Unit:** a graph/network. **Constraint:** routing, congestion, priority.
- **Problem type:** which sink gets scarce supply first; request thresholds; avoiding
  network congestion. Simple **circuit-like conditions** ("run pump while tank < X").
- **Tools:** request/provide stations, network priorities, threshold rules, basic logic gates.
- **Mindset shift:** from placing each link to **tuning rules** that make the system self-route.
- **Why you leave:** the web of hand-tuned rules explodes in complexity; you want the system
  to manage *itself*.

## Age 5 — Automated/Cybernetic — *"I'm an architect; I steer, the system executes."*  (Goals, Agents & Emergence)
- **System logic shift:** you stop placing producers and start **declaring targets**.
  Autonomous constructor/logistic **agents** build and rebalance toward goals you set. You
  specify *what* (recipes/output targets + priorities), the system allocates *how/where*.
- **Unit:** a policy/goal. **Constraint:** specification + emergent failure.
- **Problem type:** **debugging emergence** — your goals interact and create unintended
  oscillations, thrashing, starvation loops. Fixing feedback loops replaces placing things.
- **Tools:** target/goal panels, agent fleets, priority policies, monitors & alerts.
- **Population:** fully abstracted into **compute/management capacity** — the ceiling on how
  many goals/agents you can run.
- **Mindset shift:** from operator to **systems designer** — you no longer touch the floor.
- **Why you leave:** even with perfect specs, **moving matter** is the last hard limit. What
  if you didn't have to move it at all?

---

# Post-Modern / Unknown Ages (original, not generic sci-fi)
Rule we set ourselves: **no lasers-and-robots reskins.** Each speculative age is built on a
single *strange but legible* idea borrowed from real systems thinking (distributed computing,
thermodynamics), turned into a factory mechanic. They answer: *"what would production look
like beyond current understanding?"* — by making **information** and **entropy** the resources.

## Age 6 — UNKNOWN #1: The Pattern Age — *"Stop moving matter. Move information."*  (Bandwidth & Caching)
- **The inversion:** goods can be encoded as **patterns** and **reconstructed on demand**
  anywhere there's **substrate** (raw matter) + **energy**. Hauling matter becomes obsolete.
  But moving patterns isn't free either: the **pattern-network has bandwidth and latency**,
  and reconstruction consumes substrate+energy at the destination.
- **System logic shift:** logistics becomes a **memory hierarchy**. You decide what to
  **pre-materialize (cache)** near demand vs **reconstruct on demand**; hot items cause
  **contention**; you build tiers of fabrication like an L1/L2/L3 cache for a civilization.
- **Unit:** an information flow + cache. **Constraint:** **bandwidth, latency, contention** —
  a distributed-systems puzzle, not a spatial one.
- **Problem type:** hotspots, cache invalidation (a recipe changes → caches stale), bandwidth
  saturation, deciding edge vs central fabrication.
- **Mindset shift:** from "where do things go" to "where does computation/fabrication happen,
  and what's worth precomputing."
- **2D-readable & feasible:** it's the existing node+link+queue model with a bandwidth number
  on links and a load colour on nodes. No new engine — a fresh *constraint* on the graph.
- **Why it's novel:** it turns a factory game into a **caching / distributed-systems** game.
  Nobody ships that, and it's legible because everyone intuits "fast local vs slow far."

## Age 7 — UNKNOWN #2: The Entropy Age — *"Sustain the system against its own collapse."*  (Stability & Time)
- **The inversion of the optimization target.** Until now, **more throughput = strictly
  better.** Here you gain the power to run processes **out of sync with time** — buffer output
  into **temporal reservoirs**, pre-run factories ahead, slow zones to stabilize. But bending
  throughput against time accrues **entropy/instability debt**: push harder and the system
  drifts toward **decay** — outputs randomize, sites "age" and fail — unless you spend **order**
  (maintenance/attention) to hold them stable.
- **System logic shift:** a global **stability budget**. Every aggressive optimization raises
  entropy; stabilizers spend resources to lower it. The endgame is **homeostasis**, not
  maximization — the first time in the game where "go faster" can *lose* you the run.
- **Unit:** a homeostatic system. **Constraint:** **stability vs efficiency** (thermodynamic).
- **Problem type:** keeping a sprawling, near-infinite-output civilization from shaking itself
  apart; triaging where to spend order; controlled bursts vs steady-state.
- **Tools:** temporal buffers (store "future" output), stabilizers/anchors, a global entropy
  meter; population/compute becomes **attention** you ration to keep zones stable.
- **Mindset shift:** from maximizer to **steward** — you manage decline and balance, the
  deepest systems-thinking turn in the game.
- **2D-readable & feasible:** one global meter + per-zone "noise" tint as zones destabilize +
  visible stabilizer anchors. It's a global resource + decay timers + a risk/reward dial.
- **Why it's novel:** efficiency finally has a *cost*. It reframes the whole game's reflexes
  (you spent 6 ages learning "more is better"; now that instinct is the threat).

---

# Transition Moments (each retires the old game)
Transitions aren't a button — they're a **designed shock**: a gate to cross, a moment your
current base visibly becomes inefficient, and a new tool that reframes the puzzle.

| From → To | The wall you hit | The reframe |
|---|---|---|
| Stone → Tribal | Can't carry/feed it all by hand | Flows + a managed workforce |
| Tribal → Industrial | Belt/adjacency sprawl unmanageable | **Power** + ratio-balancing + blueprints |
| Industrial → Modern | Power web + sprawl wall | **Demand-pull network**; population abstracts |
| Modern → Automated | Hand-tuned rules explode | **Declare goals**, agents execute |
| Automated → Pattern | Moving matter is the last limit | **Dematerialize** → logistics = information |
| Pattern → Entropy | Infinite cheap output tempts infinite scale | **Entropy** makes "more" the enemy |

Design each transition to: (1) let the old base keep running but feel *creaky*, (2) introduce
the new constraint with a small forced scenario (a tutorial bottleneck only the new tool
solves), (3) reward the mental shift with a power-up moment (the existing age-up toast).

---

# Build order (pragmatic, indie reality)
Don't build 7 ages. **Prove the thesis once, then extend.**
1. **Make Age 3 (Industrial) a real shift first** — add **Power** as a global/regional
   constraint + brisk blueprints. This is the smallest change that proves "an age can
   transform play," and it's the most conventional/safe of the shifts. If Power lands, the
   model is validated.
2. Then **Age 4 (demand-pull networks + abstracted population)** — the first "system routes
   itself" leap.
3. Then **Age 5 (goals/agents)**.
4. The two speculative ages (**Pattern**, **Entropy**) are the long-term identity payoff —
   design them on paper now (this doc), build after 3–5 prove out.

Current state: the game is roughly **Age 1–2** mechanically (manual → flows + local
production + comfort sink), with Age 3 represented only as *content* (more buildings), NOT yet
as a *mechanic shift*. The first concrete task to deliver on this brief is **Power at the
Industrial boundary**.

# Anti-goals (do not do)
- ❌ A linear tech tree where ages are just "+1 building tier."
- ❌ Cosmetic age changes (same play, new sprites).
- ❌ Generic sci-fi upgrades (laser turrets, "nanobots", energy swords).
- ✅ Mechanical transformation, new problem *types*, forced mindset shifts.
