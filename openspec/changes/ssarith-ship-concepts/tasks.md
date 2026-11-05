# Tasks: ssarith-ship-concepts

## 1. Versions 1 to 8 (deleted, 2026-11-05)

- [x] 1.1 Versions 1 to 8 drafted, published and reviewed; deleted on the owner's word (plans, tiles, generator and shots removed; git keeps them)

## 2. Version 9: the blockout board (now)

- [x] 2.1 Twelve frigate-sized massings in grey clay, at true scale with the Concord Petrel, board views from the front, above and the side, each one focusable
- [x] 2.2 Named shots looked at; survey H4; `mockup-work.md`; the artifact record
- [x] 2.3 Version 10: the board redone in the examples' shape language (shape, not design), painted by the new `painted-bake` skill (`tools/blender/painted_bake.py`)
- [x] 2.4 The owner picks blockouts (H4): 1 Hooded lance, 3 Forked prow, 7 Harpoon, 9 Trident; Spearhead withdrawn
- [x] 2.5 Version 11: each pick unwrapped on its own, AO baked, its own painted colour map (`painted_bake.py --ship`), the tile as detail only
- [x] 2.6 Version 12: each pick rebuilt as one grown structure (Ship Design Bible chapter 14), re-exported and re-baked
- [x] 2.8 Version 13: the unpicked blockouts discarded; each pick's hull detail modelled (`tools/blender/hull_detail.py`, `data/ships/ssarith/hull_detail.json`: layered armour, sub-armour, machinery, domes, fans, louvres, lamps, liveries), mirrored, nothing repeated, baked with a low sun and deep AO
- [x] 2.9 Version 14: the PBR set (base colour, ORM, tangent normals, emission), the hull lights baked into a light map, fixtures (floods, position lights, beacons, strobes), provisional windows, the Hunt emblem; Look and Light on the page; the `ship-detailing` skill with image samples
- [ ] 2.10 The window and flood colours (survey H5)
- [ ] 2.7 A detail mask for the tile (owner: "we'll probably need a detail mask in the future")

## 3. After blockouts are picked

- [ ] 3.1 Each picked blockout designed rooms first (and its windows then placed by its rooms, W1) (plans in `data/ships/ssarith/`), its tradition from the culture as the owner sets it, its parts
- [ ] 3.2 Its painted map re-baked on its final hull (`painted-bake`, ship mode)
- [ ] 3.3 Damage, lights and the classes, on the shared ship modules

## 4. After approval for implementation (a separate request)

- [ ] 4.1 Exported by `tools/mockups/export_ships.mjs`, then a playtest
