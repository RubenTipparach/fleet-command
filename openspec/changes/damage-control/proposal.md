# Proposal: damage-control

## Why

The owner, 2026-11-04, answering the weapon-targeting change's recommendation "Damage control priorities: a repair queue
the captain orders (which subsystem first), with crew shown working":

- "Do 1, you narrowly escape a battle but damaged the reactor core and warp drive among other things prioritize repairs
  and quickly jump out"

The console had one Priority key per system and repairs that always split the same way. A captain who has just broken
away needs to say what gets mended first, see who is working on it and how long it will take.

## What Changes

- **A repair queue** on the Repair station: nine jobs (reactor core, warp nacelles, engines, sensors, shields, fire
  control, weapon mounts, tractor, transporters) in the order the crews take them, each with its health, the crews on it
  (wrenches that work), the time until it is whole, and Up and Top keys.
- **Crews**: one for each point of repair energy (the energy row at the foot of the queue), at most three on a job, the
  rest on the next job that needs them.
- **Jobs that were not repaired before**: the warp nacelles and the reactor's coolant loops and confinement.
- **The Escape scene**: the Resolute broken away from a lost battle, reactor, nacelles and sensors hit, two Kethran ships
  closing from astern. Order the repairs, charge warp, plot and jump.

## Impact

- `docs/mockups/battle-console.html` (both skins): `REPAIR`, `REPAIR_JOBS`, `repairPlan()`, `mendJob()`, `jobEta()`,
  the Repair station, the Escape scene, named shots `scene_escape` and `repair_expanded`.
- Mockup only. Game code waits for the owner's approval for implementation (CLAUDE.md section 4).
