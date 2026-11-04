# Proposal: battle-scenarios

## Why

The owner, 2026-11-04: "we have some test scenarios ready to go. make scenarios for both sides (concord and Krai)". The
battle console mockup (mockup 2) plays a list of test scenes from a selector in the view, and the Krai battle console
(mockup 8) plays the same console from the Krai Warhost's side. The game had one hard-coded skirmish: the Concord against
a Kethran pair and a Vos raider.

## What Changes

- **`data/battle/scenarios.json`** replaces `data/battle/skirmish.json`: a list of scenarios in menu order, each naming
  the side the player commands (a faction with a console skin), its seed, objective, first officer's message and ships.
- **Six scenarios, three for each side**: Battle (the Concord line against the Krai line, and the reverse), Raiders (the
  mockup's skirmish against the Kethran and the Vos, from either side) and Arming (two fast frigates jinking in close,
  the mockup's Arming scene without proximity torpedoes).
- **The Krai Warhost becomes a faction** with its console skin (`krai`), and the Krai line's four classes (corvette,
  frigate, cruiser, dreadnought) get battle figures.
- **Standing orders per ship** in a scenario: stance, fighting range, erratic, hold fire, the hull and shields it starts
  with. The simulation applies them; the first officer's message comes from the scenario, not the code.
- **The scene selector** in the view, as the mockup draws it: a pill naming the scene, a list grouped by side. Picking a
  scene of the other side swaps the console to that side's skin. `--scenario=<id>` starts one from the command line.
- Not yet: the mockup's scenes that need systems the game does not have (mines, tractor, boarding, warp, relief, VIP,
  distress, assault, shuttle, fighters, disable, pinned fire, nebula, escape, cloak, repel boarders). Each is a task that
  waits for its system.
