# Tasks: console-skins

## 1. Mockup (now)

- [x] 1.1 Proposal, design (kit piece list, skin languages, how a skin is made), spec deltas
- [x] 1.2 `docs/mockups/console-skins.html`: the kit list; each skin's palette, type, shape and ornament; one console
  drawn from kit pieces with a live skin switch; 9-slice pieces at three sizes; hero pieces; Kethran options A and B
- [x] 1.3 Shots at desktop and phone sizes, looked at; published; recorded; in the tracker and `mockup-work.md`
- [x] 1.4 Survey: U4, U5 (open), U6

## 1b. Version 2: three design languages (owner, 2026-10-30)

- [x] 1.5 Research LCARS (the owner's four links); sample the reference games' palettes (`tools/mockups/sample_palette.py`)
- [x] 1.6 Design: the factions as the skins read them, three languages, the owner's three rules, icons in the kit
- [x] 1.7 Kethran pieces modelled and rendered in Blender (`tools/blender/build_ui_kethran.py`, `assets/ui/kethran/`)
- [x] 1.8 Page v2: Concord vector pieces, Vos hand-drawn pieces, Kethran renders, three button shapes and icon sets side
  by side; shots looked at; published; recorded; tracker and `mockup-work.md`
- [x] 1.9 Survey: U5 answered, U7 added

## 1c. Versions 4-9: six more races, one at a time (owner, 2026-10-30)

- [x] 1.10 The page made a registry of skin modules (`docs/mockups/skins/`, contract in its README)
- [x] 1.11 Vhorr Brood (v4), Aelith Conclave (v5), Tarsine Remnant (v6), Krai Warhost (v7), Ombrai (v8), Kelvane
  Institute (v9), each shot, looked at and published
- [x] 1.12 Survey U8 (answered: skins only for now; the races are civilisations met in pockets across the galaxy)

## 1d. Version 13: the Ssarith Hunt (owner, 2026-10-30)

- [x] 1.13 The Ssarith Hunt, a hunter race's jungle kit, modelled and rendered in Blender (`tools/blender/build_ui_ssarith.py`,
  `assets/ui/ssarith/`), its module `docs/mockups/skins/ssarith.js`, shot, looked at and published
- [x] 1.14 Survey U10 added: a new race, and its name

## 1e. Version 15: the Kelvane Institute on the NLog construction (owner, 2026-10-31)

- [x] 1.15 The Kelvane kit rebuilt as the Nucleo NLog study taught (owner: "They were inspired by the nucleo look. So.lets
  update that"): fit-checked shapes (`tools/mockups/kelvane_shapes.py`, `assets/ui/kelvane/shapes.json`), modelled and
  rendered on the shared panel helpers (`tools/blender/build_ui_kelvane.py`), every piece passing K, L, R, P and B and
  the lit-pixel check, its module `docs/mockups/skins/kelvane.js` updated, shot, looked at and published

## 1e. Version 14: the Aelith Conclave on the analog panel bible (owner, 2026-10-31)

- [x] 1.15 The study's panel helpers moved into `tools/blender/ui_shapes.py` (`Panel`, `offset_poly`, `notched`, `metal`,
  `satin`, `film_mat`, `film_ink`, `link_layers`); the bible's tile renderer runs on them alone (a re-rendered tile matches
  the old one within 1 level of 255)
- [x] 1.16 The fit check gains rule R (rolled edges never meet, measured) and rule B (layers behind a plate stay inside it,
  clamped when built and checked on the render), with self-tests from the study's own faults
- [x] 1.17 The Aelith Conclave rebuilt: shapes fit-checked (`tools/mockups/aelith_shapes.py`), shell rendered
  (`tools/blender/build_ui_aelith.py`, `assets/ui/aelith/`), rule B verified on the renders, holograms drawn from the same
  shapes (`docs/mockups/skins/aelith.js`); contact sheet and shots looked at; published
- [x] 1.18 The Concord top strip's bars kept inside the strip, clear of its thin bar (optional strip padding tokens,
  measured on the page for every skin by `tools/mockups/strip_fit.mjs`)

## 1f. Version 16: children inside parents, measured (owner, 2026-10-31)

- [x] 1.19 `tools/mockups/console_fit.mjs` (pixels, every skin's console) and `tools/mockups/dom_fit.mjs` (geometry, any
  page): the children-inside-parents rule measured; both catch the faults on the old page and seeded faults
- [x] 1.20 Every fault fixed at its cause: strip bands and bar layout per skin, strip lamps, titles, the helm column's
  widths, Kethran's groove and prow block, Vhorr's keys and tabs, Aelith's tab lens (re-rendered in Blender, fit rules
  re-checked); mockup 1's top bar beside an open port panel
- [x] 1.21 Shots looked at, every artifact republished

## 1g. Version 17: symbol bars, the Remnant's dust, Krai, Ombrai and the Hunt on the bible (owner, 2026-10-31)

- [x] 1.22 The Remnant's keys and tabs: one dust for every state, only the light changes
- [x] 1.23 Symbol bars in the kit (`bar.cell.off`, `.on`, `.warn`, the harness's `cell` kind) for the Vhorr, Aelith, Tarsine,
  Krai, Ombrai, Kelvane and Hunt; the fit check measures them
- [x] 1.24 Krai, Ombrai and the Hunt rebuilt from fit-checked shapes in Blender, each with its own lighting option
- [x] 1.25 One `lip_runs` helper for every well's lamps; the fit check fails a title that shows no pixels; dom_fit
  scales border widths with the console's transform
- [x] 1.26 Shots looked at; published

## 1h. Version 18: taller strips (owner, 2026-10-31)

- [x] 1.27 Strips 72 px in every skin (art re-made, shapes re-checked), padding from each measured band, labels over their
  bars; console fit and dom fit pass; published

## 2. After approval for build

- [ ] 2.1 Blender kit generator per skin (`panel-textures`), bakes, manifest with margins
- [ ] 2.2 Mockups 1 and 2 redrawn from the kit, skin switch on both

## 3. After approval for implementation (a separate request)

- [ ] 3.1 The game's theme built from the manifest; a validation that fails on a missing piece
