# Tasks: ship-damage-visuals

## 1. Mockup (now)

- [x] 1.1 Proposal, design and spec delta from the owner's brief (2026-11-02)
- [x] 1.2 Survey questions A2-A6 in `docs/design/survey.html`
- [x] 1.3 In `docs/mockups/concord-ships.html`: a signature per subsystem in four states, scorch marks, modelled and parallax holes, venting fire, coolant and plasma, a nacelle shot off, the break-up; one damage function for clicks, buttons and scenarios
- [x] 1.4 Named shots for each signature, looked at
- [x] 1.5 Published with mockup 5 (https://claude.ai/artifact/J4baTFsXR3CR51zFvTnRj4)

## 2. After approval for build

- [ ] 2.1 Fold survey A2-A6 into the design
- [ ] 2.2 Fracture lines and pre-cut pieces in the Blender generators; char and ember maps

## 3. After approval for implementation (a separate request)

- [ ] 3.1 Subsystem health and states in `src/sim/`, with tests (a nacelle at 0 detaches and halves top speed; a core at 0 breaks the ship)
- [ ] 3.2 The damage look in Godot reading the simulation, then a playtest that shows each signature
  - [x] 3.2a (2026-11-04, owner: "add in explosions when they get hit, and if a part gets hit enough break it appart like in
    the damage demo, also add the damage scars stuff too") Hits, scars and the break-up, from the hull damage the simulation
    already deals: the simulation records each hit that lands (`Battle.Hits`, tested); `src/Game/View/ShipDamage.cs` makes a
    fireball where it strikes the plating facing the shooter (a blue flare when the shield stops it), a scorch mark with a
    cooling ember ring, a hole with the decks behind it for a torpedo (`hull.gdshader` with `damage.gdshaderinc`, ported from
    `lib/ship-fx.js`); a part hit hard on a hull below 60 % is blown off; a destroyed ship goes through the mockup's 3.6 s core
    breach and main blast. The cells are the mockup's own fracture, exported by `tools/mockups/export_ships.mjs` (manifest
    `break`), each drawn as a copy of its body trimmed by the hull shader, closed by cut faces (`cutface.gdshader`). The
    Wrecks scene from both sides (every class of the other side, crippled) and its playtest show every ship type coming apart.
  - [x] 3.2d (2026-11-04, owner: "aww theres no big super nova", "center is white with pink edges, and a blue ring glowing
    outward", "the debris should have trails of flame") The core breach's main blast: a white star with pink edges
    (`novastar.gdshader`), a flat blue disc racing out through the battle plane (`novadisc.gdshader`), debris trailing flame,
    white sparks, a blue-white flash; part blasts and hits a rolling ball of fire (`fireball.gdshader`) and a shock ring
    (`shockring.gdshader`)
  - [ ] 3.2e The mockups' mainBlast() to match (lib/ship-break.js and mockup 6's own copy)
  - [ ] 3.2b Subsystem signatures (coolant, plasma, venting fire per system), wires, modelled holes: they wait for 3.1
  - [ ] 3.2c A part blown off changes nothing in the simulation yet (a lost nacelle does not slow the ship): 3.1
