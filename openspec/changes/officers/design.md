# Design: officers

An officer is `{ id, name, rank (0-3), spec (a station), skill (1-4) }`. `officerSkill(s, station)` is the one rule:
the officer in that slot counts their whole skill in their specialty, half (rounded down) elsewhere, 0 for an empty slot
or a ship without officers (every AI ship, for now). Each ability is one number in `OFFICER`, read by its rule:

| Station | Ability, per skill level | Rule that reads it |
|---|---|---|
| Helm | +4 % high energy turn chance | `hetOdds` (the turn and the helm readout) |
| Power | -15 % reactor coil drift | `reactorStep` |
| Tactical | +2 % chance to hit | `hitChance` |
| Point defence | +5 % intercept chance | `pdStep` |
| Science | +25 % scan speed, +1 km sweep reach | the scan, `sweepKm` |
| Comms | 0.25 of the target's ECM countered | `hitChance` |
| Repair | +20 % every crew's rate | `repairPlan` and `jobEta` |
| Transport (security chief) | +10 % ambush first volley, +4 % hit aboard, +10 % pad recharge | `secStep`, `padPower` |
| Fleet | +1.5 % wingmen's chance to hit | `hitChance` |

The player has thirteen officers: one per station and four in reserve. Names, ranks and skills are ours and provisional.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| O1 | Should officers gain experience, be wounded or killed (the reference game's officers can)? | Not yet |
| O2 | Should enemy ships have officers? | Not yet |
| O3 | Should a slot be changed only in port (the campaign's officers page), or in battle too? | In battle too, for the mockup |
| O4 | Portraits on the slot and the card? | Not yet: rank pips and name |
