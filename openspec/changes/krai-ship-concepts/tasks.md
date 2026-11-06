# Tasks: krai-ship-concepts

## 1. Mockup (now)

- [x] 1.1 Proposal and design: the language from the Krai console skin and survey A8
- [x] 1.2 `docs/mockups/krai-ship-concepts.html` v1: three directions at frigate size, direction A as four classes, the Concord line beside them, 40 px silhouettes, three light presets
- [x] 1.3 Named shots looked at; `dom_fit.mjs` passes
- [x] 1.4 Survey A15-A17, the tracker, `mockup-work.md`, `docs/artifacts/krai-ship-concepts/`
- [x] 1.5 v2 (owner: "Those are fighter jets, not spaceships. Think about how we defined culture and story"): spaceships from the culture as written: A Standing blade, B Lance, C Three-edged dagger; no wings or fins; survey A15-A17 rewritten

- [x] 1.6 v4 on the Ship Design Bible: the frigate's rooms as data (`data/ships/krai/frigate_plan.json`), the hull fitted round them by `lib/ship-plan.js`, the Krai's own parts and exterior, Build it up and the deck plans; survey A15-A17 withdrawn
- [x] 1.7 The frigate to the Concord line's standard on the shared `lib/ship-skin.js`: detail and normal maps, the chevron seams as grooves, interior-mapped windows with Krai rooms, six-sided thrusters, warm orange floodlights with shadows, strobes and beacons
- [x] 1.8 The classes: corvette, cruiser and dreadnought plans from `tools/mockups/krai_plans.py`, one builder for all four, a line-up
- [x] 1.9 Damage on the shared `lib/ship-fx.js` (lifted from mockup 6, which now uses it): systems, scorch marks, holes onto the plan's rooms, failures, parts breaking away, a core breach, the Damage tab
- [x] 1.10 The hull breaks apart as the Concord line's (the shared `lib/ship-break.js`, lifted from mockup 6, which uses it and renders as before); a bayonet shape per class
- [x] 1.11 Owner's notes of 2026-11-04: the whole hull no longer stays under its broken pieces; a wide crescent on the cruiser and the dreadnought; a painted livery; nacelle detail; windows flat on one face with level rooms; the thruster throat clear of the stern face

## 2. After the frigate is approved

- [ ] 2.1 The classes finished to the frigate's detail: detail texture, hull lamps, damage (mockup 6's systems)
- [ ] 2.2 The Krai ship display as a wireframe schematic of the chosen hull (console skins)
- [ ] 2.3 The Krai builder moved into `docs/mockups/lib/` beside the Concord line, so the console can fly Krai ships

## 3. After approval for implementation (a separate request)

- [ ] 3.1 Station and blade data in `data/ships/`, a Blender generator per class, then a playtest

## 4. Own unwrap and baked AO (owner, 2026-11-05)

The owner: "every ship needs custom uv Unwraped textures. From NOW on . ALL SHIPS MUST AVOID REPETTIVE TILE FOR MAIN COLOR
MAP", "tile textures are for DETAIL only, we'll probably need a detail mask in the future, same goes for Concord and Krai
ships which will need more work done as well", "dont forget to bake AO as well" (CLAUDE.md section 10; Ship Design Bible
chapter 15). Not started: the Ssarith Hunt goes first.

- [ ] 4.1 Each class unwrapped on its own and its main colour map painted onto its own UVs (`painted-bake`, ship mode), replacing the repeating plating tile as the main colour
- [ ] 4.2 AO baked onto each class's own UVs and kept as its own map
- [ ] 4.3 The plating tile kept as detail only, under a detail mask (to be designed)
- [ ] 4.4 Every hull checked against Ship Design Bible chapter 14 (one grown structure, no placed primitive)
