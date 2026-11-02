# Tasks: concord-ship-look

## 1. Mockup (now)

- [x] 1.1 Proposal, design and spec delta from the owner's notes (2026-11-02)
- [x] 1.2 Survey questions A7-A9 in `docs/design/survey.html`
- [x] 1.3 `docs/mockups/concord-ships.html`: three classes on one builder, livery, markings by density, plating by class, windows at true scale with a figure, baked floodlights and hull floods, navigation lights, three light presets, line-up at true scale
- [x] 1.4 Named shots at desktop and phone sizes, looked at; `dom_fit.mjs` passes
- [x] 1.5 Published (https://claude.ai/artifact/J4baTFsXR3CR51zFvTnRj4), recorded in `docs/artifacts/concord-ships/`, listed in the tracker and `mockup-work.md`

## 2. After approval for build

- [ ] 2.1 Fold the owner's notes and survey A answers into the design and the page
- [ ] 2.2 Blender generators for the three classes (`blender-hard-surface`), Material Maker graphs for the three plating patterns, the hull lights baked (`light-baking`), each with a manifest and a budget

## 3. After approval for implementation (a separate request)

- [ ] 3.1 Ship classes and liveries as data in `data/ships/`, validated on load
- [ ] 3.2 The ships in the game's 3D view, then a playtest at desktop and phone size (scenarios: a window is person sized, the registry reads in shadow, marking density follows the class)
