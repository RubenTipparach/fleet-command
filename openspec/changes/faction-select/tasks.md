# Tasks: faction-select

## 1. Mockup (now)

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 One builder for the emblems (`docs/mockups/lib/emblem-builds.js`), the badge pages moved onto it, their shots unchanged
- [x] 1.3 The faction screen in `docs/mockups/game-flow.html`: platform, pedestals, emblems, lamps, dust, hover turn, the pick card
- [x] 1.4 The campaign in the faction's skin (`docs/mockups/lib/kitcss.js`), Concord, Krai and Ssarith
- [x] 1.5 Shots at desktop and phone sizes, looked at; `dom_fit.mjs` passes
- [x] 1.6 Published; tracker and `mockup-work.md` updated
- [x] 1.7 Disabled keys dark in every skin: the idle key darkened and solid (owner, 2026-11-05: "disabled butons should be like dark")
- [x] 1.8 The walnut table (owner, 2026-11-06): the Material Maker graph `table_wood`, rendered headless; the pedestals and
  slab replaced by the table and brass rings; the badges float and bob; every badge turns toward the pointer; the picked
  faction's lamp full, the hovered one's half (mockup 1 v18)

## 2. After approval for build

- [ ] 2.0 A `button.disabled` piece in each skin's kit, drawn by its own generator, in place of the darkened idle key

- [ ] 2.1 Fold in the owner's notes
- [ ] 2.2 Sound cues for the lamp and the pick
- [ ] 2.3 Every scenario in the spec mapped to a test or a playtest beat
- [x] 2.4 ~~Material Maker graph `plinth_concrete`~~: the slab went with the pedestals; the table's graph `table_wood` is rendered

## 3. After approval for implementation (a separate request)

- [x] 3.1 The screen in Godot (owner, 2026-11-06, asked in the game): the table and emblems exported from the page by
  `tools/mockups/export_faction.mjs`, drawn by `FactionStage` with the lamps, beams, dust, bob and turn; still `docs/screenshots/game/flow/factions.png`
- [ ] 3.4 The Concord pearl's thin-film colour shift in Godot (a small shader in place of the clearcoat stand-in)
- [ ] 3.2 The campaign's panels drawn from the UI kit by faction, as the battle console's are
- [x] 3.5 Every faction at the table in a grid (four, five, four), the nine placeholder emblems as coins (mockup 36), a
  phone's grid of keys; page, export (`export_faction.mjs` writes each seat's x and z) and `FactionScreen` (owner, 2026-11-08)
- [ ] 3.3 Playtest at desktop and phone size
