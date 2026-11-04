# Design: missiles

## Where the shape comes from

| Manual, printed page | What it shows | What we take |
|---|---|---|
| SFC2, 24 ("Seeking Weapons") | Missiles need no energy to arm but must be reloaded from their rack (half a turn to two, by rack); they track their target for three turns, then run out of fuel; their warheads do the same damage at any range | Racks with rounds and a reload; no reactor energy; a flight time; damage that does not fall with range |
| SFC2, 190 ("Missiles") | Two warhead sizes, the larger taking more damage to destroy and doing twice the damage; speeds by era | A light and a heavy missile |
| SFC2, 191 ("Missile Racks", "Fire One/Fire All") | Racks differ in how many they carry and how fast they fire; fire one at a time or all at once | Rack types; Fire one and Fire all |
| SFC2, 192 ("Guidance & Control") | The launching ship guides its missiles, up to its control rating (usually six); a seventh launch makes one self-destruct; electronic warfare can spoof them | Six in flight a ship; the launcher's sensors and ECM matter |
| SFC3, 28-29 | Weapons bought into hardpoints at a dock | Missile racks as a loadout choice for every faction |

## Grades (provisional, `data/battle/missiles.json`)

The owner, 2026-11-04: "missiles depending on grade will have: red, orange, blue, green", "red being lowest grade, green
being highest". Any faction's racks load any grade; the grade is its colour, in the flame, the ball and the tail.

| Grade | Colour | Speed | Damage | Point defence hits to stop |
|---|---|---|---|---|
| I | Red `#ff3b30` | 900 m/s | 8 | 1 |
| II (today's missile) | Orange `#ff8c1a` | 1,100 m/s | 11 | 1 |
| III | Blue `#2f7bff` | 1,300 m/s | 14 | 2 |
| IV | Green `#2fdc5a` | 1,500 m/s | 18 | 2 |

Every grade: damage the same at any range, 20 s of fuel, then it drifts dead; no reactor energy to fire (own power). A
rack holds 6 rounds and reloads one every 10 s (provisional, D2).

A ship guides six missiles at most; a seventh launch drops the oldest. A launcher whose sensors are disabled loses its
missiles' lock: they fly on straight until their fuel runs out. In a nebula a missile loses lock beyond the nebula's
lock range, as a weapon does (the battle console's `NEBULA.lock_km`).

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
| D5 | 20 s of fuel? | Yes |
