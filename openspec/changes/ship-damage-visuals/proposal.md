# Proposal: ship-damage-visuals

## Why

The owner, 2026-11-02, with the Concord ships brief: "make some mockups, make sure they are destryuctible, the more
detailed the descrtuction the better, emphasize how we can communicate subsuustem destruction with ship destruction".
And:

- "damage is very purposeful, ships break into pieces, damage texture has burnmarks towards the dges"
- The question to ask: "how can I understand ship subsystem damage by looking at it?", including "coolant leaks",
  "ship systems on fire (venting fire)", "scorch marks", "paralx shader that shows holes in ships, or actual holes in
  ships modeled?" and "chunks of ships being shot off - nacelles can be lost in the game, you only really need 1
  nacelle to limp away".

A captain under fire looks at the 3D view more than at the console. If each subsystem fails in a way of its own, the
ship itself becomes a damage readout. This applies to every faction; the Concord ships show it first (mockup 5,
`docs/mockups/concord-ships.html`, with the change `concord-ship-look`).

## What Changes

- **Every subsystem has a signature** in four states (healthy, damaged, critical, destroyed), so its state is read
  from the ship: nacelles flicker, vent plasma and are shot off; the reactor leaks coolant, then pulses red, then
  breaches; the bridge crown smokes, goes dark, burns, is blown off; weapons spark and crater; the deflector dims;
  impulse vents sputter and smoke; saucer sections go dark section by section.
- **Scorch marks** at every hit, charred toward the centre with a hot rim while fresh.
- **Holes** at heavy hits, shown both ways for the owner to choose: modelled holes (the skin cut away, decks and ribs
  inside, lit red by emergency light) and a parallax shader (the skin intact, an interior drawn in it).
- **Venting fire, coolant and plasma**, each its own colour and motion, from the hole or vent of the system that
  failed.
- **Chunks shot off**: a nacelle snaps at its pylon's knee and tumbles away burning; the ship limps on the other.
- **Breaking apart** at a core breach or zero hull: the ship comes apart along its build lines (saucer wedges,
  engineering hull halves, nacelles), every broken edge charred toward the break with a cooling ember line, with a
  flash, a shockwave, debris, fires and a coolant cloud.
- One damage function, used by clicking the hull, the system buttons and the scenarios alike.

## Impact

- New: the damage half of `docs/mockups/concord-ships.html`; survey A2-A6.
- No game code. In the game the damage state lives in the simulation (`src/sim/`), and the look reads it.
