# Tasks: faction-emblems

## 1. Mockup (now)

- [x] 1.1 Proposal and design: the rules, the ten emblems, sizes with their numbers, open questions
- [x] 1.2 `docs/mockups/lib/emblems.js`: every faction's shapes, drawn as crest, mark and hull paint from one source
- [x] 1.3 `docs/mockups/faction-emblems.html`: every emblem at 96 px, 40 px and 20 px, in colour and one colour, beside
      today's console crest, as hull paint, beside the console's icons, and on a stand-in hull flank in 3D at true scale
- [x] 1.4 `dom_fit.mjs` passes; shots at desktop, phone and portrait looked at
- [x] 1.5 Survey questions E1-E12
- [x] 1.6 On the tracker and `mockup-work.md` (mockup 9)
- [ ] 1.7 Published from the desktop session, and the link recorded in the tracker, `mockup-work.md` and
      `docs/artifacts/faction-emblems/README.md`

## 2. After approval for build

- [ ] 2.1 Fold in the E answers and the owner's notes; redraw any emblem whose meaning changed
- [ ] 2.2 A check that measures rule E3 (every device's window at least 5 units, every rim at least 5 units) and E4
      (no two outlines overlap by more than a set share at 20 px), run on `emblems.js`
- [ ] 2.3 Each skin's `crest` piece re-rendered from the emblem by its own generator (Blender skins through
      `tools/blender/ui_render.py`, Concord flat in `docs/mockups/lib/concord.js`, the Vos inked)
- [ ] 2.4 Ask the Krai ships thread to replace the frigate's placeholder flank marking; the ship bible's chapter 10
      updated if E12 puts the emblem on the flanks

## 3. After approval for implementation (a separate request)

- [ ] 3.1 The shapes as SVG files in `assets/emblems/<faction>.svg`, written by a committed script from one source
- [ ] 3.2 The mark in the game's contacts, fleet list, hover cards and target tag; the crest in each skin's kit
