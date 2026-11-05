# Proposal: weapon-fx

## Why

The owner, 2026-11-04: "we also ened high end fx + shaders for both of those things too" (the Concord's and the Krai's
special weapons, change `special-weapons`). Then, while this was being built: "source and destination of phaser and
disruptors must maintain in sync", "this means as they fire, the source and destination of these beams must follow the
ship source and ship destination", "a missile fired can break this rule as it leaves behind a particle trail".

The battle's shots today are a flat glowing cylinder and a glow sprite, and a beam's start stayed where its mount was
when it fired while the ship flew on (the game froze both ends).

## What Changes

- **One effects library**, `docs/mockups/lib/weapon-fx.js`, over the shared damage effects (`lib/ship-fx.js`): an HDR
  pipeline (bloom, heat haze, ACES tone mapping, a dither), camera-facing beam strips with flowing light and strands,
  faction flares, a six-facing shield bubble (the Concord's hex lattice, the Krai's triangles), scars painted by the hull's
  own materials (molten craters, beam burns, a ram's gouge, the quell's lattice that puts the ship's lights out), the
  quell orb, the forge spike, trails, a drive plume with shock diamonds, a bow shock, impact rings and gathering motes.
- **A phone quality**: half-resolution bloom, no haze, no aberration, no MSAA, for the Compatibility renderer (survey T2).
- **Beams and bolts keep their ends on their ships** (the owner's rule): every beam is re-aimed each step from its mount
  as the firing ship now stands to its target as it now stands; a direct-fire bolt (the disruptor, the forge spike) runs
  along that live line; seekers (missiles, torpedoes) fly free and leave their trail. Done now in the battle console
  mockup (v76, the Krai page v15), the game (`Battle.Ends`, two headless tests) and the weapons range.

## Impact

- New: `docs/mockups/lib/weapon-fx.js`; used by `docs/mockups/special-weapons.html` (mockup 16).
- Changed now (the owner's rule): `docs/mockups/battle-console.html` and the Krai page written from it,
  `src/Core/Sim/Battle.cs`, `src/Core/Data/GameData.cs`, `src/Game/View/ViewOverlay.cs`, `tests/Core.Tests` (two tests,
  the golden seed re-pinned because disruptor bolts now land at other moments).
- Later (after approval): the effects in Godot, shaders under `src/Game/View/`.
