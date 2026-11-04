# Design: battle-scenarios

The battle console mockup's test scenes (`docs/mockups/battle-console.html`, `SCENES`) in the game, playable from both
the Concord's side and the Krai Warhost's (owner, 2026-11-04: "make scenarios for both sides (concord and Krai)").

## 1. The data

`data/battle/scenarios.json` holds `scenarios`, a list in menu order. Each scenario:

| Field | Meaning |
|---|---|
| `id` | unique; `--scenario=<id>` starts it |
| `name` | the selector's label (the same names on both sides) |
| `side` | the faction the player commands; it must have a `skin` in `factions.json` |
| `seed` | the battle's seed (CLAUDE.md section 6.3) |
| `objective` | shown under the selector |
| `say` | `[who, text]`, the first line in the log |
| `ships` | the first is the player's: side 0 and of the scenario's side |

A ship may carry its captain's standing orders, each optional with the default in brackets: `stance` (attack; attack,
escort or hold), `range` (mid; close, mid or long, the tuning's `ai_range_*_km`), `erratic` (false), `hold_fire` (false),
`hull_frac` (1) and `shields_frac` (1). An unknown value is a data error naming the field (CLAUDE.md section 6.4).

The first scenario is the one the game opens in. The golden seed's battle is `concord_raiders`, the old skirmish
unchanged (seed 20261101, the same six ships), so the pinned hulls hold.

## 2. The scenarios

| Scene | Concord side | Krai side | From the mockup |
|---|---|---|---|
| Battle | Resolute (Albatross), Tallis (Swift), Meridian (Petrel) against Kharz (cruiser), Vekk (frigate), Ruhn (corvette) | the same fleets, sides swapped | the Battle scene, with the Krai as the foe |
| Raiders | the Concord three against Vorrak, Ashkar (Kethran) and Rattlejaw (Vos) | the Krai three against the same | the Battle scene as it is; the Krai names are mockup 8's (`docs/mockups/lib/krai.js`, `player.ships`) |
| Arming | Resolute against two Krai frigates at 300 m/s, erratic, fighting close | Kharz against two Swifts the same way | the Arming scene; the game has no proximity torpedoes yet, so the objective asks for overloads |

Positions follow the mockup's skirmish: the player's three at the origin heading north, the foe's three 6 to 11 km
ahead (the mockup's units times 100 m).

## 3. The Krai line's battle figures

Placeholders until the combat change, like every class's, written to the Krai's culture
(`openspec/changes/krai-ship-concepts/design.md`: knife hulls, the hull is the weapon, and its class table's weapons):

| Class | Code | Battle length | Hull | Reactor | Weapons |
|---|---|---|---|---|---|
| Corvette | CV | 140 m | 55 | 11 | twin bow guns (disruptors, 70 degree arcs), point defence |
| Frigate | FF | 230 m | 85 | 15 | one spinal cannon (a disruptor, 40 degrees), two flank disruptors, point defence |
| Cruiser | CA | 340 m | 135 | 21 | two spinal cannons, a bow torpedo, two flank disruptors, a stern lance, two point defence |
| Dreadnought | DN | 570 m | 220 | 26 | three spinal cannons, a bow torpedo, four broadside lances, three point defence |

A battle length is the length the battle draws a ship at, about 2.7 times its true length, as the battle console mockup
frames its ships; `ship-models-engine` scales the approved model to it.

## 4. The selector

Drawn as the mockup draws it (`docs/mockups/battle-console.html`, "The scene selector"): a 230 x 30 px pill at 110 px
from the view's left, `SCENE` and the scene's name, an arrow; the objective under it. Open, a black sheet lists the
scenarios grouped by side under the faction's name, one 32 px key each, the one in play lit; a click elsewhere shuts it.
Picking one rebuilds the battle and the 3D view, and sets the console's kit to the side's skin (`Kits.For`, one `IKit`
per skin). It is one more control in the view's immediate-mode input, like the Target key.

## 5. The scripted captain

The playtest skill's bot, built here because the owner asked for it with these scenarios (2026-11-04: "develop a bot to
play through these test scenarios for each faction"). It plays fair: InputMap actions (`next_target`, `fire`, `back`)
and pointer clicks pushed into the viewport at a control's centre, the control found by id where it was last drawn
(`IControlSource`, which every panel and the view implement). Its `fight` beat is what a captain at the console does:
lock with Tab, order Intercept on the helm, fire when the weapons panel would fire. `data/playtests/suite.json` lists
every run, each scenario from both sides; `scripts/playtest.sh` plays them headless at a fixed 30 frames a second, so a
four-minute battle takes seconds and gives the same verdict. Its first run found a real bug: every hover card threw,
because the card layer has no size and its clamp's bounds crossed.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| 1 | Which of the mockup's other scenes come next, and from which side first? | Each waits for its system; both sides together, as these three |
| 2 | Which scenario should the game open in? | The Concord's Battle: the line against the line |
