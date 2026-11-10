## Why

The owner, 2026-11-09: "I also want to opt to have more damage fx. If your ship is low health it should be on fire,
smoking, particles falling off and a bar that flashes Low Hull Integrity". Then, mid-design: "I think the reactor
destabilizing also needs a warning as well".

Today a ship shows its damage only as hits land (scorch marks, torpedo holes, a broken-off piece burning for about
20 s, `ShipDamage.cs`) and, on the console, as a hull bar that turns red below 35 %. A ship at a fifth of its hull looks
much like one at four fifths between hits, and nothing on the screen calls out a hull about to fail or a reactor
counting to a meltdown, though the second kills the ship and everything within 30 km (`REACTOR.blast_km` x 10). The Engineering
officer says it once in the log, where it is easy to miss under fire.

## What Changes

- **A ship in trouble shows it, all the time, in the view.** Below half its hull a ship trails smoke from its worst-hit
  places; below 35 % those places burn, throw sparks and shed glowing scraps of hull that tumble away; below 20 % all of it
  twice as hard, from a third place. Every ship, yours and theirs, so a burning target reads as nearly done.
- **One warning band on the console, for both warnings.** A band at the top of the live view, in the skin's own inset
  frame and alert colour, flashing: **Low hull integrity** with the hull's share, below 35 % (the same share that turns
  the hull bar red, whose label then reads Low hull integrity too), and **Reactor unstable** with the seconds to meltdown,
  counted from the same numbers the simulation counts with, flashing faster under 10 s, reading **Reactor cooling** in
  Caution as the core counts back, gone once it is safe. Both at once sit side by side. A tap opens the page that deals
  with it (Damage control, the Reactor page).
- **The Concord's console first, every skin with it** (CLAUDE.md section 7, the master UI): the same band in the same place
  in every skin, each drawn in its own frame, colour and type.
- **Options: Damage effects, Full or Reduced** (the owner's "opt to have"), default Full. Reduced keeps the smoke and the
  warnings and drops the fire, sparks and scraps to a quarter, for a slow phone.
- Mockup first: the battle console page (mockup 2) and the 12 faction pages, shots `low_hull`, `reactor_unstable`,
  `damage_warnings`. Game code waits for approval (CLAUDE.md section 4).

## Capabilities

### New Capabilities
- `damage-warnings`: the low-hull effects on every ship, and the console's warning band (Low hull integrity, Reactor
  unstable).

### Modified Capabilities
- (none: `ship-damage-visuals` keeps its hit marks, breakup and core breach; this adds a state shown between hits)

## Impact

- Mockup: `docs/mockups/battle-console.html` (`lowHullStep`, `warnings`, `warnBands`, `reactorCritical`), the 12
  `battle-console-<skin>.html` pages regenerated.
- Game, once approved: `src/Game/View/ShipDamage.cs` (the burning places), `src/Game/View/Fx.cs` (a pooled particle
  emitter), `src/Game/Ui/Panels.cs` (the band, beside `StripMeter` and `Bar`), `src/Core/Sim/Battle.Reactor.cs` (a
  `MeltdownLeftS` read, no rule change), `data/battle/tuning.json` (`low_hull`, `warnings`), the Options sheet, a sound cue.
