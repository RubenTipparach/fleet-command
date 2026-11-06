# Playtest 2026-11-04: ship damage, every ship type, recorded

The owner asked: "do ships actually break? add in explosions when they get hit, and if a part gets hit enough break it
appart like in the damage demo, also add the damage scars stuff too!", then "re-record a couple battle videos to show me
you fixed each ship type".

| | |
|---|---|
| Commit | the branch `claude/ships-export-mac` after `8e9f574`, with the damage work (this report's commit) |
| Machine | The owner's Mac: Apple M4 Max, macOS 15.6.1, Godot 4.7.1 .NET (Forward+, Metal), .NET 8.0.427 |
| How | The scripted captain plays each scenario through input actions in a 1920 x 1080 window in real time; OBS records the window from `PLAYTEST_START` to `PLAYTEST_END` (ScreenCaptureKit window capture, scale 1.0, 60 fps) |
| Takes | H.264 MP4, 1920 x 1136, 60 fps, at the take's own size; copied to Drive as they are and confirmed with the Google Drive connector at the same byte size |

## What was built

- **Hits.** The simulation records every hit that lands (`Battle.Hits`: the ship, where the shot came from, the weapon's
  kind, what the shields took, what reached the hull, whether it killed). The view turns each into a fireball, sparks
  and a flash of light where it strikes the plating facing the shooter, or a blue flare when the shield stops it.
- **Scars.** Every hull hit leaves a scorch mark (a lance a clean burn; a torpedo or bolt a burn with an ember ring that
  cools over 20 s), and a torpedo a hole showing the decks and rooms behind it by interior mapping. The rules are mockup
  6's (`lib/ship-fx.js`), ported to `src/Game/View/damage.gdshaderinc` for `hull.gdshader`.
- **Parts blown off.** On a hull below 60 %, a part that has taken 14 % of the hull's points comes off with a blast and
  burns: an outer cell of the main hull, a pylon, a nacelle or the command block.
- **Break-up.** A destroyed ship goes through the mockup's core breach: for 3.6 s its outer pieces blow off one by one,
  then the main blast throws the rest, each piece tumbling and burning, its broken faces closed by cut faces with decks
  and rooms (`cutface.gdshader`), charred and glowing toward the break and cooling over 30 s. The pieces are the mockup's
  own fracture cells, exported into the manifest (`break`) by `tools/mockups/export_ships.mjs`; each is a copy of its
  body that the hull shader trims to its cell, so no mesh is cut in the game.
- **The Wrecks scene**, both sides: every class of the other side, crippled (30 % hull), shields down, holding fire.
  Target view now stays on a destroyed target's wreck until the next lock, so it can be watched coming apart.

## The runs

| Run | Scenario | Result | Take | Drive |
|---|---|---|---|---|
| `wrecks` | Concord, Wrecks: the Krai corvette, frigate, cruiser and dreadnought | ok: won, all four broken up | 110 MB | [concord-wrecks-damage](https://drive.google.com/file/d/1hr1_tNGh68EHbZ-fetv0yLKh6EJYUU6l/view?usp=drivesdk) |
| `wrecks` | Krai, Wrecks: the Concord Swift frigate, Petrel destroyer and Albatross cruiser | ok: won, all three broken up | 101 MB | [krai-wrecks-damage](https://drive.google.com/file/d/1vbFhc0KL13nIeto2LcDmf1ekHXH2t_Y9/view?usp=drivesdk) |
| `battle` | Concord, Battle | ok: won, hull 100 % | 139 MB | [concord-battle-damage](https://drive.google.com/file/d/1aiE-9WA9H1rMWQbViYM3ORblmpwFOZTO/view?usp=drivesdk) |
| `battle` | Krai, Battle | ok: lost, 1 foe left | 326 MB | [krai-battle-damage](https://drive.google.com/file/d/1AlFRmWXAeCnb8fLut9_j76ihqbccsJfU/view?usp=drivesdk) |

Headless, the whole suite: `PLAYTEST_SUITE 8/8 ok` (the six scenarios and both Wrecks).

## What the contact sheets show

Looked at (`docs/playtests/2026-11-04-ship-damage/`):

- Every ship type comes apart: the Krai corvette, frigate and cruiser into burning cells with lit decks in their cut
  faces; the dreadnought in the last seconds (Target view puts a 570 m ship low in the frame, so mostly its stern shows
  while it is hit); the Concord Swift and Petrel; the Albatross loses a burning nacelle before its break-up.
- Scorch marks and torpedo holes on the plating as the hits land (the Albatross in Krai Wrecks, the Kharz in Concord
  Wrecks); shockwave rings on each break.
- No menu, dialog or blank frame over a take.

## Findings

- **The bot turned its own Intercept off.** Pressing a lit helm order again returns to free heading; the fight beat
  pressed Intercept again after the script's own Intercept beat (and again after each new lock), so the ship flew on
  straight. This was the stall in this afternoon's Krai Battle and Concord Duel takes. Fixed: the captain presses
  Intercept only while its key is unlit. With it the Krai won a run for the first time (Arming, headless).
- First tries had the effects far too big (a fireball sized in metres where the view wanted scene units, and a flame
  a hundred times too large). Fixed and looked at again before these takes.
- OBS once failed to find the game window (an invalid window id) and timed out; the take was retried and recorded. One
  OBS throughout, closed after every take.
- Not built yet: subsystem signatures (coolant, plasma, venting fire per system), wires, modelled holes, and a part blown
  off changing the simulation (a lost nacelle does not slow the ship): `ship-damage-visuals` 3.1, 3.2b, 3.2c.

## The core breach (later the same day)

The owner, on these takes: "aww theres no big super nova", "we had a big explosion ewarlier, but it needs better shaders
and aprticles", then, with a picture of the 1999 game's warp core breach: "center is white with pink edges, and a blue
ring glowing outward. this explosion was so iconic to me as a child", "I really wahnt it to have this exact look and
colors, and the debris should have trails of flame". (The "big explosion earlier" was a bug: a burning piece's flame drawn
a hundred times its size.)

- A ship's end is now a star of white light with pink-violet edges and soft spikes (`novastar.gdshader`), a flat disc of
  blue light racing out through the battle plane, brightest at its rim and streaked from the centre
  (`novadisc.gdshader`), eighteen chunks of debris flung out each trailing flame, white sparks, and a blue-white flash over
  the ships round it (`Fx.Nova`). Our own shaders and code; only the look is the reference's.
- A part blown off and every hit get a ball of fire that billows, rolls and cools from white through orange to red as its
  edge burns away (`fireball.gdshader`), and a ragged shock ring (`shockring.gdshader`).
- Bloom was tried and taken out: the white Concord hulls under their floods bloomed into blobs.
- First look: `docs/playtests/2026-11-04-ship-damage/core-breach-first-look.jpg` (the Krai Wrecks scene, the Swift and the
  Petrel going up).
- Recorded with the breach, the same way as above, both confirmed on Drive at the local byte size:
  [concord-wrecks-nova](https://drive.google.com/file/d/1bJYt7CS2ZLLCEwP-84nDynU2bLZFwdVb/view?usp=drivesdk) (131 MB,
  won, the Krai corvette, frigate, cruiser and dreadnought each going up) and
  [krai-wrecks-nova](https://drive.google.com/file/d/1D8ktHh1qR_tRCOqL8hNAAjoLgfk6Nv_H/view?usp=drivesdk) (101 MB, won,
  the Concord Swift, Petrel and Albatross). Looked at: `core-breach-each-krai-class.jpg`, three moments of each breach.
