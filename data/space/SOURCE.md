# Space backdrop data

Copied from Star Crew Bridge Simulator (`RubenTipparach/Star-Crew-Bridge-Simulator` at `9a35f08`, 2026-10-30), at
the owner's request (2026-10-30: "Star crew bridge simulator! also borrow its star shader from there too, so when we
scroll around the stars trace an outline"). Unchanged; never edit them by hand.

| File | What it is |
| --- | --- |
| `skies/quiet.json` | A nebula sky: a pale galaxy band with brown dust lanes, one faint rust cloud, mostly black. The campaign map's sky. |
| `skies/verdigris.json` | Thin teal and sea-green veils over a faint blue band. The main menu's sky. |
| `skies/ember.json` | A red and amber emission cloud torn by black dust. The stand-in battle's sky. |
| `starfield.json` | The vector stars' classes and colours, and the streak rule (FreeSpace's lines). |

Each sky is a closed sphere of directions with a colour and a star-hiding share per vertex (8 bytes a vertex, base64),
and its vector stars, written in Star Crew by `tools/space/nebula_skies.py` from `data/space/skies/recipes.json`
(its `openspec/changes/nebula-skies`). New skies for Fleet Command are made the same way, by porting that tool when a
change asks for them. The mockups inline these files with `tools/mockups/inline_data.py`.
