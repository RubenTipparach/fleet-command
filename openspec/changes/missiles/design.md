# Design: missiles

## Where the shape comes from

| Manual, printed page | What it shows | What we take |
|---|---|---|
| SFC2, 24 ("Seeking Weapons") | Missiles need no energy to arm but must be reloaded from their rack (half a turn to two, by rack); they track their target for three turns, then run out of fuel; their warheads do the same damage at any range | Racks with rounds and a reload; no reactor energy; a flight time; damage that does not fall with range |
| SFC2, 190 ("Missiles") | Two warhead sizes, the larger taking more damage to destroy and doing twice the damage; speeds by era | A light and a heavy missile |
| SFC2, 191 ("Missile Racks", "Fire One/Fire All") | Racks differ in how many they carry and how fast they fire; fire one at a time or all at once | Rack types; Fire one and Fire all |
| SFC2, 192 ("Guidance & Control") | The launching ship guides its missiles, up to its control rating (usually six); a seventh launch makes one self-destruct; electronic warfare can spoof them | Six in flight a ship; the launcher's sensors and ECM matter |
| SFC2, 33 and 136-137 ("Sensor Decoy Launch Panel", "Wild Weasels") | A decoy shuttle draws seeking weapons to itself until it is destroyed or voided (by its ship firing weapons, moving fast, or straying too far); when it is gone, the seeking weapons reacquire the ship | The decoy shuttle, and missiles that go back to the ship when it dies |
| SFC3, 28-29 | Weapons bought into hardpoints at a dock | Missile racks as a loadout choice for every faction |

## Grades (provisional, `data/battle/missiles.json`)

The owner, 2026-11-04: "missiles depending on grade will have: red, orange, blue, green", "red being lowest grade, green
being highest". Any faction's racks load any grade; the grade is its colour, in the flame, the ball and the tail.

| Grade | Colour | Speed | Damage | Point defence hits to stop |
|---|---|---|---|---|
| I | Red `#ff3b30` | 240 m/s | 8 | 1 |
| II (today's missile) | Orange `#ff8c1a` | 320 m/s | 11 | 1 |
| III | Blue `#2f7bff` | 400 m/s | 14 | 2 |
| IV | Green `#2fdc5a` | 480 m/s | 18 | 2 |

Speeds near a ship's own (owner: "the missiles should move slower. Different grades have different speeds and damage
values"): a ship at its 300-380 m/s top speed outruns a red or an orange missile and is caught by a blue or a green one,
as the reference game's missile speeds sat among its ships' (SFC2, printed page 190). Every grade: damage the same at any
range, 30 s of fuel (7.2 to 14.4 km), then it drifts dead; today's missile's range in `weapons.json` becomes 9.6 km, the
orange grade's reach; no reactor energy to fire (own power). A
rack holds 6 rounds and reloads one every 10 s (provisional, D2).

A ship guides six missiles at most; a seventh launch drops the oldest. A launcher whose sensors are disabled loses its
missiles' lock: they fly on straight until their fuel runs out. In a nebula a missile loses lock beyond the nebula's
lock range, as a weapon does (the battle console's `NEBULA.lock_km`).

## Countermeasures (owner, 2026-11-04: "Missiles follow ships around. Can be countered with counter measures like flares, decoy shuttles, or pdc")

A missile follows its target wherever it goes. Three things beat it, each in `data/battle/missiles.json`:

| | What it is | What it does |
|---|---|---|
| Flares | Four burning lures thrown from the hull, burning 4 s, 20 s to ready again | Each missile closing on the ship is fooled with its grade's `fooled_by_flare_x` (red 60 %, orange 45 %, blue 30 %, green 15 %), rolled once, and goes for a flare; one that loses its flare flies on, lost |
| Decoy shuttle | A small unmanned craft that flies off slowly, showing the ship's own sensor picture | Each missile closing on the ship is fooled with its grade's `fooled_by_decoy_x` (red 85 % to green 40 %) and chases it; the first to reach it destroys it, and the rest reacquire the ship; voided if the ship fires its main weapons (provisional, D6) |
| Point defence cannons | `weapons.json`'s `pd`, drawn as tracer streams | A burst every 1.5 s at the nearest missile still coming; it stops one `pd_intercept_chance` (45 %) of the time; blue and green need two |

The higher the grade, the harder it is to fool: a captain spends point defence on green missiles and flares on red.

## The look (`lib/weapon-fx.js`, the weapons range)

The weapon is everyone's, so the body is plain gunmetal (a slim dart with four fins); behind it a big boiling ball of
drive light and a long tail, both in its grade's colour, so a captain sees at a glance how dangerous a salvo is. No weapon
is drawn white (owner: "please avoid the color white for weapons"). It leaves the rack slowly, lights its drive a quarter of a second later, then weaves as it homes. Point defence fires
a thin white beam from the hull to the missile, both ends where they now stand; a missile it stops bursts small.

## Cost

Rules: a magazine count and a reload timer a rack, a fuel timer a missile. Look: a dart (about 120 triangles), a flare
and a 70-point trail a missile (139 triangles, 3 draws); six in flight a ship bounds it.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| D1 | Four grades, red to green, as in the table? | Yes |
| D2 | Missiles cost rounds, not reactor energy? | Rounds; restocked at a supply dock in the campaign |
| D3 | Six in flight a ship, a seventh drops the oldest? | Yes |
| D4 | "Missile" or "drone" on the console? | Missile |
| D5 | 30 s of fuel? | Yes |
| D6 | Countermeasures: flares, a decoy shuttle voided when its ship fires main weapons, point defence cannons; chances by grade as in the table? | Yes |
