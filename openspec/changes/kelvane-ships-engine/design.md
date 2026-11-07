# Design: kelvane-ships-engine

## One copy of every ship rule

The Institute's ships are made as the Hunt's were (change `ssarith-ships-engine`): the low-poly hull the bake unwrapped
(`tools/blender/painted_bake.py --faction kelvane --ship`, the manifest's `mesh` in `assets/ships/kelvane/manifest.json`)
is drawn by the page and exported to the game, and the maps stay where the bake wrote them, named by the manifest.

The Hunt's page held the only copy of the code that makes a baked ship take damage and come apart. It moves, unchanged,
into `docs/mockups/lib/baked-fleet.js` (`window.FCBakedFleet(THREE, ctx)`), which both pages inline; the page passes what
differs (its scene, its look and light, its normals rule, a glow scale for craft, a hook after each ship is built). The
Hunt's ship data (cores, lamps, pieces, seeds, geometry, normals) and its static shots were checked identical after the
move, and the Kelvane blockout OBJs byte for byte. The exporter's Hunt branch becomes a branch for every baked faction
(`baked` in its faction list), so the Hunt's glbs came out byte-identical.

| What | How | Numbers |
|---|---|---|
| Hull look, lamps, glows, windows and bays, breakup | As the Hunt's (ssarith-ships-engine design), by the same shader and code | 9, 11 or 13 cells by length |
| Craft glows | The page scales a craft's lamp glows; the exporter writes each lamp's own `glow_m`, so the game's are the same | 0.22 of a ship's |
| Landing pads | `pad_<i>` empties from the bake's pads: centre, normal, along, radius, the craft that parks there and how far up the normal its origin stands | 24 pads, 4 m, stand 2.06 m |
| Parked craft | The fighter's glb dressed once, lights removed (its glows kept), copied onto each pad, outside the hull node so the breakup does not copy them and the damage never reads them; one shown per fighter in the bay, spread over the pads; none once the ship comes apart | 12 fighters, 24 pads |
| Classes | Battle lengths 2.7 times the true length, as every line; figures and mounts placeholders at the Hunt's sizes | 340, 367, 448, 529, 578 m |

The Kelvane cruiser keeps the id `kelvane_rv`: it is the Patience-class research cruiser, and the Four races scene's
Patience (the battle console mockup's, mockup 2) is one, so the scene now flies the approved ship.

## Open questions

| Question | Provisional answer |
|---|---|
| The Institute's weapons and figures | The generic ones (lances, torpedoes, point defence, missiles) at the Hunt's sizes, until its culture sets them |
| Its ships' names and captains | The class names (Parallax, Candour, Patience, Rigour, Curiosity) and "Institute captain", the mockup's Dr. Orrel of the Patience (culture is the owner's, C4) |
| How many fighters the carrier carries | 12, half its pads; every pad is filled on the page |
| Its campaign | None yet: the campaign's sides are the Concord, the Krai and the Hunt |
