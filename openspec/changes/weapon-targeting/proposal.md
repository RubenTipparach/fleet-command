# Proposal: weapon-targeting

## Why

The owner, 2026-11-04, for every skin's battle console ("[General for all UI]"), with a crop of the Tactical station's
Aim at row:

- "this aim at should be icon based - not all weapons can precisely aim, so recommend shift + click to see weapon hit
  stats in details."
- "in the original SFC, most weapons like torpedos hit based on arc receiving the hit. So based on where wapons are placed
  on the ship, thats the current set of items getting hit on that arc. a small bracket would show above those subsystems
  to show they were on the chopping block. where as EMPs and phasers and disruptors had a better chance of delivering the
  direct hit on stuff, but would damage other systems on rthe current arc as well (like a 50/50 spred)"
- "this aim at window needs more details, there should be an expand button to view viable targets to shoow with various
  weapons."
- "create a scenario that requires you to disable certain subsystems like thrusters, weapoons etc."
- "create another scenario where you can manually target at multiple ships at once. the: Pin weapon fire system, should
  allow you to lock a weapon on a specific target even if your main sensors changed targets. if that target is destroyed
  (ship or susbsystem) then the pinning stops."
- "We also need a nebula scenario to test out the ECM/ECCM systems."
- "I'm not sure what other game mechanics I dont have yet, if you think of anything recommend some stuff"

Then, 2026-11-04, with crops of the Krai's Tactical station and Target box:

- "aim at should show status of subsystems"
- "I should be able to click to aim at specific systems here too" (the Target box's systems display)
- "in this expanded view, I expect to be able to target individual weapons as well, weapons control system dont affect
  PDCs or missiles actually so disabling that wouldnt disable independently powered weapons" (the expanded Tactical page)

## What Changes

- **Where a hit lands (the chopping block).** Every subsystem has a place in the hull, as the mounts do. A hit through a
  facing reaches what that side of the ship exposes. Torpedoes, missiles, mines and collisions land anywhere on that
  block; lances and disruptors (precise weapons) put the hit on the aimed subsystem half the time when it is on the
  block and spread the rest over it.
- **Aim at as icons**: the hull and the seven subsystems, named on hover, in every skin's icon set, each key lit in the
  locked target's status for that subsystem (the target display's status boxes), the aim ringed and a bracket over each
  one on the chopping block. On the Target box, clicking a subsystem aims at it.
- **Aim at a weapon**: the expanded Tactical page lists the target's weapons as aim keys (and its mounts on the Target box
  aim on a click); a precise hit aimed at a mount lands on that mount when it is on the block.
- **Independently powered weapons**: point defence and missiles charge from their own generators, so weapons control
  (damaged, disabled or destroyed) never stops them; every other mount stops charging once weapons control is disabled.
- **Hit details**: shift+click an aim or a weapon for a sheet of the target's exposed facing, what is on the block, and
  each weapon's way of aiming, arc, range, chance to hit and chance to put the hit on the aimed subsystem.
- **The chopping block shown**: on the target's systems display, a small red bracket over every subsystem and mount the
  facing turned toward you exposes.
- **Expanded Tactical**: a grid of weapons against contacts, each cell the chance to hit now; a click pins that weapon
  to that contact.
- **Pinned fire**: a pinned weapon fires at its contact with the aim it was pinned with, whatever the locked target is;
  the pin stops when the ship, or the subsystem it aims at, is destroyed. Pin weapons (Tactical) pins the selected ones
  to the locked target; a pinned mount carries a small flag.
- **Three test scenes**: Disable (the Vorrak's engines and weapons, without destroying it; fails if it is destroyed),
  Pinned fire (three raiders from three sides), Nebula (a jamming raider inside a nebula: a hidden contact until ECCM
  beats its ECM or you close to 6 km; every shot worse inside, ECM and ECCM count for more).
- **Labels fit their keys** (rule T, Analog Panel Bible): a pointed key's label shrinks to its open field, down to 11 px,
  and a key may give a short form of its label.

## Capabilities

- `weapon-targeting` (new): the chopping block, precise and by-arc weapons, aim as icons, hit details, pinned fire, the
  expanded Tactical grid, nebulae, and the three scenes.

## Impact

- `docs/mockups/battle-console.html` (both skins; the Krai page is written from it), its Tactical station, systems
  display, weapons and hit model; `HITS`-like tuning in one table (`STRIKE`), `SYS_POS`, `NEBULA`.
- No game code (CLAUDE.md section 4). The rules are the mockup's; the game's simulation takes them when this is approved
  for implementation.
