# Design: vos-ships-engine

## One copy of every ship rule

The Vos's ships are made as the Institute's were (change `kelvane-ships-engine`): the low-poly hull the bake unwrapped
(`tools/blender/painted_bake.py --faction vos --ship`, the manifest's `mesh` in `assets/ships/vos/manifest.json`) is drawn
by the page and exported to the game, and the maps stay where the bake wrote them, named by the manifest. The bakes moved
from `assets/ships/vos/baked/` (kept out of Godot while the mockup was a draft) into `assets/ships/vos/`, where the game
reads them (`data/ships/vos/hull_detail.json` `out`).

The page had its own small builder (`BS.looks`, panes, fixtures), which the exporter could not read. Its ships are now
built by `FCBakedFleet.load`, so the page's hull look, lamps, panes and breakup are the shared code's; the page's own
additions go in through `afterBuild`. A craft parked on the Deck runner keeps the plain baked look, as it is not a ship of
its own on the page.

| What | How | Numbers |
|---|---|---|
| Hull look, lamps, windows and bays, breakup | As the Hunt's and the Institute's, by the same shader and code | 9 or 11 cells by length |
| The field | `tools/blender/hull_sdf.py` from each bake's mesh, 1 m cells | Cross 1.3 million cells |
| Antennas and dishes | `lib/baked-ship.js` `masts` from the manifest, built into the hull piece, exported as meshes; their tip lamps a black glow | 4 to 7 a ship |
| Lenses | `lib/baked-ship.js` `lenses` records each lens's centre, normal, radius, phase and colour; the exporter writes `lens_<i>`; `lens.gdshader` is the page's shader | colour 0.25, 0.72, 0.64 |
| The Deck runner's Cutters | Their places on the deck written as the bake's pads would be (`pad_<i>`, normal up, nosed to the bow, stand 0 m), so `ParkedCraft` parks the Cutter's own glb there | 6 pads, 6 fighters |
| Containers | `MOCKUP_CARGO` on the page, the freighters' format; the game's `Cargo` draws them | the one container at scale 2 (12 x 12 x 24 m) |
| Classes | `data/ships/vos/line.json`; templates the Concord cruiser (`ca`) and, for the Deck runner, the Kelvane carrier | 329, 362, 413, 459, 405 m |

## The classes

Every Vos ship is about cruiser size (mockup 37 design D11), so every class copies a cruiser's battle numbers and the
Deck runner a carrier's, at three quarters of its hull points. Each has its trade in what the game already plays:

| Ship | Trade | In the game |
|---|---|---|
| Drum | Mine laying | 12 mines (a cruiser carries 4) |
| Cross | Tractor beams | 2 energy on its tractor |
| Boarder | Boarding | 2 energy on its transporters, 24 marines |
| Broadside | Passing as a freighter | A cruiser's guns; the disguise is vos-ship-concepts 2.4 |
| Deck runner | Fighters | 6 Cutters, parked on its deck |

## Open questions

| Question | Provisional answer |
|---|---|
| The Vos's weapons and figures | A cruiser's, until their culture sets them |
| Their ships' names and captains | The mockup's ship names and "Vos captain" (culture is the owner's, U7) |
| The Twin boom and the Stack in battle | Exported, unused: the Deck runner launches Cutters (survey Y14 asks the Stack's job) |
| The deck plans for boarding | The Petrel's, as every placeholder line's, until the Vos room plans become deck plans |
| The old `vcr` corsair in the Allied fleet scenes | Drawn as the Boarder, at its own 200 m battle length |
