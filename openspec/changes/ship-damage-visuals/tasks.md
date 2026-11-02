# Tasks: ship-damage-visuals

## 1. Mockup (now)

- [x] 1.1 Proposal, design and spec delta from the owner's brief (2026-11-02)
- [x] 1.2 Survey questions A2-A6 in `docs/design/survey.html`
- [x] 1.3 In `docs/mockups/concord-ships.html`: a signature per subsystem in four states, scorch marks, modelled and parallax holes, venting fire, coolant and plasma, a nacelle shot off, the break-up; one damage function for clicks, buttons and scenarios
- [x] 1.4 Named shots for each signature, looked at
- [ ] 1.5 Published with mockup 5

## 2. After approval for build

- [ ] 2.1 Fold survey A2-A6 into the design
- [ ] 2.2 Fracture lines and pre-cut pieces in the Blender generators; char and ember maps

## 3. After approval for implementation (a separate request)

- [ ] 3.1 Subsystem health and states in `src/sim/`, with tests (a nacelle at 0 detaches and halves top speed; a core at 0 breaks the ship)
- [ ] 3.2 The damage look in Godot reading the simulation, then a playtest that shows each signature
