# Design: weapon-arming

A mode is a set of overrides on a weapon's numbers, kept beside the weapon (`WEAPONS.<w>.overload`, `.proximity`);
`spec(m)` merges the mount's mode in, and every rule and readout reads `spec(m)`, so the to-hit, the range ring and the
shot agree (CLAUDE.md 6.1). `canArm(w, mode)` says which modes a weapon has.

| Weapon, mode | Range | Charge | Cost | Damage | Other |
|---|---|---|---|---|---|
| Lance, normal | 12 km | 3.5 s | 2 E | 7 | precise |
| Lance, overload | 4 km | 5 s | 4 E | 12 | precise, short range only |
| Pulse torpedo, normal | 18 km | 8 s | 5 E | 14 | by arc |
| Pulse torpedo, overload | 7 km | 12 s | 9 E | 26 | by arc |
| Pulse torpedo, proximity | 18 km | 8 s | 5 E | 8 | +20 % to hit, target speed ignored, block reach 120 degrees |

The chopping block's reach (`STRIKE.reach_deg`, 60 degrees either side of the facing struck) becomes a parameter of
`exposure()` and `damage()`, so a proximity burst reaches the subsystems on the neighbouring facings too.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| A1 | Should proximity torpedoes also be better against fighters and missiles (point defence duty)? | Not yet |
| A2 | Should overloads risk damaging the mount, as in the reference game? | Not yet |
| A3 | Disruptors: a proximity or a burst mode? | None; their overload only |
