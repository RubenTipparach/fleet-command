# Proposal: reactor

## Why

The owner, 2026-11-01, on mockup 2's power station: "power generated = blue, systems = green", then "need a config to
allow generation rate to go negative, I'd like to also show how much power is in batteries, which can drain if systems
drain more than is generated, but by default this config will be off, should be in the reactor tab", "reactor tab will
be found under 'power' which will have a button to swap between allocation and reactor. reactor will be a special new
mode for our game specifically. (it didnt exist in sfc)", "reactor has two components: magnetic confinement, and
coolant systems", and "the generator as it takes damage will lose alignment, so the player has to adjust alignment, and
balance coolant systems to keep the reactor from melting down. a meltdown can happen if coolant systems are offline for
a duration, and result in an explosion of the reactor".

## What Changes

- **The power station has two modes**: Allocation (as before) and Reactor, swapped by two tabs at its top.
- **The reactor's output varies**: three magnetic confinement coils lose alignment when the ship is hit and drift while
  confinement is damaged; misalignment cuts the output and adds heat. The captain nudges each coil back.
- **Coolant**: two loops, each with a pump flow of 0-5 boxes (each drawing 1 E, counted in Systems) and a health that
  hits lower. Cooling carries heat away in proportion to the core temperature.
- **Meltdown**: coolant offline, or the core at its limit, counts toward a meltdown; after 20 s the reactor explodes,
  destroying the ship and damaging every ship within 3 km. Cooling down counts back.
- **Draw past output**: a setting in the Reactor mode, off by default. Off, allocation is capped at the output. On, the
  allocation may exceed it; the deficit drains the battery, and with the battery empty every system browns out to the
  share the output can carry. The output falling below the allocation (a damaged reactor) drains the battery either way.
- **The bottom strip**: Power (the output) blue over Systems (the draw) green, on one scale; Systems red when it
  exceeds Power; a Battery bar beside them.
- **Repair**: the reactor (confinement and loops) is a repair priority.
- Every ship runs the same reactor; AI crews nudge their coils back and move flow off a failing loop.

The rules are the mockup's stand-in (`docs/mockups/battle-console.html`); the game's combat design is its own change.
