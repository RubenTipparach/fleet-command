# Tasks: hull-texture-designs

## 1. The pitch (2026-11-06)

- [x] 1.1 The designs written up: five for the Concord, five for the Krai, each from a named tradition (design.md)
- [x] 1.2 Mockup 24: the Petrel and the Krai frigate in each design, one at a time and all five side by side, studio and night
- [x] 1.3 Shots looked at, the page published, the tracker's repo copy and the records updated

- [x] 1.4 Version 3 (owner: "they don't bring the same level as the ssarith ships. We need visual story telling no
      repetitive patterns"): every design modelled and baked as the Hunt's (the strake and kit cut styles, windows and features
      from the room plan, story wear), drawn by the shared `docs/mockups/lib/baked-hull.js`

- [ ] 1.5 Version 6 (owner, 2026-11-06: "you have to make drastic changes to improve", with reference sheets): large panels
      and sub panels, the detail kit, bolt wells, marks, wear before the render, 4096 px maps beside the page; C1 and K1
      baked and shown first
- [ ] 1.6 C2-C5 and K2-K5 moved onto version 6, once the owner has seen C1 and K1

## 2. After the owner picks (approved for build)

- [ ] 2.1 The picked designs' stories written for every class of both lines (their room plans), baked at full size
- [ ] 2.2 The emblem on the hull (the Concord's once, to port, on the forward band; the Krai's KC), a hull mask for each

## 3. In the game (approved for implementation)

The owner, 2026-11-06: "Do not put any of these textures in the game until I have enthusiastically approved, they are
kinda lame right now". Nothing here goes into the game until then; the baked maps carry a `.gdignore`.


- [ ] 3.1 The Concord and Krai ships exported with their baked maps (`baked_hull.gdshader`, as the Hunt)

## 4. One copy of the tooling (CLAUDE.md 6.1)

- [ ] 4.1 Change `kelvane-ship-concepts` (rubentipparach/fleet-command#47, another session's) generalised `painted_bake.py` and
      `hull_detail.py` for its own faction (a palette dict per faction, `--faction`, saved passes and `--repaint`) and made
      `docs/mockups/lib/baked-ship.js`, while this change did the same its own way (`--detail`, palettes and liveries from
      data, story wear, the strake and kit cuts, plan windows) and made `docs/mockups/lib/baked-hull.js`. Once #47 merges:
      this change's cut styles, plan windows, sides, liveries, plate colours and wear move onto its structure, this page onto
      `lib/baked-ship.js`, and `lib/baked-hull.js` goes. Until then this branch leaves the Hunt page as main has it.

