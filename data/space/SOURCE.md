# Space backdrop data

Copied from Star Crew Bridge Simulator (`RubenTipparach/Star-Crew-Bridge-Simulator` at `9a35f08`, 2026-10-30), at
the owner's request (2026-10-30: "Star crew bridge simulator! also borrow its star shader from there too, so when we
scroll around the stars trace an outline"). Unchanged; never edit them by hand.

| File | What it is |
| --- | --- |
| `skies/quiet.json` | A nebula sky: a pale galaxy band with brown dust lanes, one faint rust cloud, mostly black. The campaign map's sky. |
| `skies/verdigris.json` | Thin teal and sea-green veils over a faint blue band. The main menu's sky. |
| `skies/ember.json` | A red and amber emission cloud torn by black dust. The battle console mockups' sky. |
| `starfield.json` | The vector stars' classes and colours, and the streak rule (FreeSpace's lines). |

Each sky is a closed sphere of directions with a colour and a star-hiding share per vertex (8 bytes a vertex, base64),
and its vector stars, written in Star Crew by `tools/space/nebula_skies.py` from `data/space/skies/recipes.json`
(its `openspec/changes/nebula-skies`). The mockups inline these files with `tools/mockups/inline_data.py`.

## The composed skies (2026-11-04)

The owner, 2026-11-04: "make sure to generate lots of cool nebulas to be used for our demos, I'm getting sick of the same
one being used all the time". The painter is now here, `tools/space/nebula_skies.py`, copied unchanged from Star Crew at
`95a49b2` with its `skies/recipes.json` (whose four painted recipes are Star Crew's). `tools/space/nebula_recipes.py`
(ours) composes fourteen more from themed palettes and seeds into the same recipes file, and the painter paints them:

| Sky | Look |
| --- | --- |
| `crimson_reach` | A wide blood-red cloud with ember edges and a white-hot core |
| `abyssal_teal` | Deep sea-green veils over near black, a cold cyan core |
| `violet_storm` | Storm-purple clouds, magenta at their hearts, a pale lilac core |
| `golden_veil` | A thin gold veil, warm amber knots, a sun-coloured core |
| `emerald_rift` | A green cloud torn by a dark rift, a yellow-green core |
| `ice_halo` | Pale blue-white wisps round a brilliant blue star |
| `rose_quartz` | Soft pink clouds with peach edges, a gentle white core |
| `rust_lanes` | Brown and rust dust thick with dark lanes, a dim orange glow |
| `twin_lights` | A cyan cloud and a magenta cloud meeting, each round its own core |
| `ashen_void` | Almost empty: grey-blue drifts and one faint smudge |
| `aurora` | Green and violet ribbons along the horizon, a teal core |
| `solar_furnace` | A blazing yellow-white heart in orange fire |
| `deep_indigo` | Indigo clouds lit blue from within, crossing black lanes |
| `coral_drift` | Coral and turquoise side by side, a warm core |
| `tactical_slate` | Cold slate-blue haze, steel-grey edges, a silver core (the Concord Disable scene, 2026-11-06) |
| `tactical_amber` | Smouldering amber and copper, brown lanes, a gold core (the Concord Pinned fire scene) |
| `tactical_jade` | Jade and moss veils over green-black, a mint core (the Krai Disable scene) |
| `tactical_magenta` | Hot magenta and plum folds round a pink-white core (the Krai Pinned fire scene) |

Eight more (2026-11-06), one for each of the tractor, mines and craft test scenes (`data/battle/scenes/tractor.json`):

| Sky | Look |
| --- | --- |
| `iron_shoal` | Cold steel-blue banks of dust, a pale silver core |
| `amber_tide` | A long amber swell over deep brown, a soft gold core |
| `cobalt_drift` | Cobalt clouds over black, electric blue hearts |
| `saffron_haze` | A saffron haze with rose edges, a cream core |
| `jade_veil` | A thin jade veil, sea-green and grey |
| `orchid_deep` | Orchid and plum clouds, violet lanes, a pink-white core |
| `slate_ember` | Slate-grey dust with embers glowing through it |
| `copper_mist` | Copper beside verdigris, a bright brass core |

`docs/screenshots/skies/contact.png` (`tools/space/sky_sheet.py`) shows every sky unwrapped. `tools/space/sky_panorama.py`
bakes each to a small panorama, `assets/skies/<id>.jpg` (1024 x 512), which the game uses as its environment's sky, so the
hulls reflect the nebula and take half their ambient light from it (owner, 2026-11-04: "reflection probes ... but dont
crank it up super high": a 128 radiance map, made once per scenario). Each game scenario names its
own (`data/battle/scenarios.json` "sky"; a test checks the file exists and no two scenarios share one).

**Not byte-reproducible across machines.** On the owner's Mac (NumPy 2.0.2 with Apple's Accelerate) the painter does not
reproduce Star Crew's three files byte for byte, so those three are kept as copied and only the composed ones were
painted here; `--check` is only meaningful on the machine that painted a file.

## The faction skins package's skies (2026-11-06)

Nine more composed by `tools/space/nebula_recipes.py` for the scenarios the faction skins package added (each scenario
under a sky of its own; named `skins_` to keep apart from the other packages' skies):

| Sky | Look | Scenario |
| --- | --- | --- |
| `skins_moss_haze` | An olive-green haze with amber knots, a dim gold core | `ssarith_battle` |
| `skins_tidal_glass` | Clear sea-blue glass with one green current, a cool white core | `ssarith_duel` |
| `skins_copper_tide` | Copper and teal clouds rolling together, a hot copper core | `ssarith_arming` |
| `skins_saffron_ring` | A saffron cloud round a hot white core, rose at its edge | `concord_allied` |
| `skins_cobalt_crown` | A ring of cobalt clouds round a violet-white core | `krai_allied` |
| `skins_jade_lantern` | Jade-green veils lit by a warm yellow core | `ssarith_allied` |
| `skins_slate_drift` | Slate-blue drifts, a faint lavender knot | `concord_four_races` |
| `skins_amber_lull` | Still amber clouds low on the horizon, a pale honey core | `krai_four_races` |
| `skins_pearl_mist` | Pale pearl-grey mist with faint rose and blue, a soft white core | `ssarith_four_races` |
