# Tasks: concord-hull-concepts

## 1. Mockup (now)

- [x] 1.1 v1: three directions (Halo, Lantern spine, Light sail), published; the owner rejected it (2026-11-03)
- [x] 1.2 v2: proposal and design rewritten around Star Crew Bridge Simulator's Tern family, with the data cited
- [x] 1.3 `docs/mockups/concord-hull-concepts.html` v2: Swift, Petrel, Albatross and the Tern at true scale, three pylon arrangements, four liveries, 40 px silhouettes, measured facts
- [x] 1.4 Survey A11 (pylons) and A12 (sizes) rewritten
- [x] 1.5 Named shots, looked at; `dom_fit.mjs` passes
- [x] 1.6 v4: the Swift and Petrel as three ships each (one below, above and below, level), the Albatross shortened into two hulls
- [x] 1.7 v5: the one-below Petrel removed; mockup 5's lighting and damage on every ship; detail and normal maps; windows with rooms
- [x] 1.8 v6: furnished rooms behind the windows, phaser burn-throughs show a room, 90 degree spotlights along the hull, upright pylons 5.5 m out, turrets clear of the command block, the command block cap off its floor
- [x] 1.9 v7: the Tern from its latest model's geometry in our paint, a second Albatross with main-hull pylons, detailed thrusters, lights that settle on or off
- [x] 1.10 The Condor battleship and the Pelican carrier
- [x] 1.11 Republished to the same link, recorded in `docs/artifacts/concord-hull-concepts/`, the tracker and `mockup-work.md`

- [x] 1.12 The Concord line is the standard for Concord ships and mockup 5 is retired (owner, 2026-11-03: "Concord line (hull concepts) - new standard, get rid of the old concord ships")
- [x] 1.13 The ship table and hull geometry are one module, `docs/mockups/lib/concord-line.js`, inlined into mockup 6 and, through `lib/space.js`, into the battle console and the game flow, whose Concord ships are now the line's (owner: "start applying these spaceships into the ui mockup stuff"); console pairings CA Albatross, DD Petrel, FF Swift, TR Pelican, provisional
- [ ] 1.14 The battle console's ship systems display drawn from the active ship's own plan (its hull stations, pylons and nacelles) instead of the stand-in outline
- [x] 1.15 The generic damage code (particles, effects, weapon light, beams and torpedoes, the hull shader's hole and scorch rules, light flicker, emitter programmes) and the detail tile, tri-planar GLSL, room ray and thruster are shared modules, `docs/mockups/lib/ship-fx.js` and `docs/mockups/lib/ship-skin.js`, and the breakup (fracture cells, cut faces, caps, pieces breaking away, the breach sequence, wires) `docs/mockups/lib/ship-break.js`, used by the Krai line too; mockup 6 renders as before (compared shot for shot)

## 2. After approval for build

- [ ] 2.1 The chosen line carried into mockup 5 (ships, damage pieces along their decks)
- [ ] 2.2 Station data in `data/ships/` and a Blender generator per class

## 3. After approval for implementation (a separate request)

- [ ] 3.1 The hulls in the game, then a playtest (scenarios: the family reads at 40 px; the pylon arrangement shows the class)
