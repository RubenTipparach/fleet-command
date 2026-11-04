# Tasks: ssarith-ship-concepts

## 1. Concepts (now)

- [x] 1.1 Proposal and design: the Hunt's culture turned into ship parts, provisional rules, three directions, survey section H
- [x] 1.2 The frigate's rooms as data (`data/ships/ssarith/frigate_plan.json`) from `tools/mockups/ssarith_plans.py` on the Krai plan layer (its writer takes the classes and folder)
- [x] 1.3 `docs/mockups/ssarith-ship-concepts.html` v1: the fitted hull and its shovel prow, directions A Grasp, B Jaw and C Crouch, Stalk and Strike, deck plans, 40 px silhouettes, the Petrel yardstick, the page's checks
- [x] 1.4 Named shots looked at; `dom_fit.mjs` passes
- [x] 1.5 `mockup-work.md` (mockup 12), `docs/artifacts/ssarith-ship-concepts/`, survey section H
- [x] 1.6 Published: https://claude.ai/artifact/UbsDVv6EvKbt3wkCDb6Ris
- [ ] 1.7 On the tracker, and the survey republished with section H (from the desktop session: the cloud session's account cannot open either)

## 1b. Withdrawn (owner, 2026-11-04)

- [x] 1b.1 Version 1 withdrawn in the design, the page, `mockup-work.md` and the survey; the bible's chapter 13 (C1-C5) and CLAUDE.md section 1 hold the rule
- [x] 1b.2 Version 3: three traditions drafted inside the Hunt (H1 as recommended), each choice naming its source (C3), the stereotype test on the page (C5)
- [x] 1b.3 Version 4: smooth chamfers on every direction; C Course withdrawn; C Field Hull (a secondary hull in place of nacelles, rooms first, three layouts)
- [x] 1b.4 Version 5: a fair hull (smooth outlines from above and in profile, one chamfer, no polygons added)
- [x] 1b.5 Version 6: the plating laid on each face, true size on the chamfers
- [ ] 1b.6 The owner picks, renames or replaces the directions (survey H2) and C's layout (H3)

## 2. After a tradition is chosen (H2)

- [ ] 2.1 The chosen direction to the Concord line's standard on the shared `lib/ship-skin.js`: a scute detail tile, interior-mapped windows with Hunt rooms, hooded floods as spotlights with shadows, the drive
- [ ] 2.2 The classes (H5), their plans from `tools/mockups/ssarith_plans.py`, a line-up
- [ ] 2.3 Damage and breakup on `lib/ship-fx.js` and `lib/ship-break.js`; a claw breaks away as a nacelle does
- [ ] 2.4 The hull marking once E11 and E12 are decided; kill notches (H4)
- [ ] 2.5 The Hunt builder moved into `docs/mockups/lib/` so the console can fly Hunt ships

## 3. After approval for implementation (a separate request)

- [ ] 3.1 Exported by `tools/mockups/export_ships.mjs` with named nodes (claws among them), then a playtest
