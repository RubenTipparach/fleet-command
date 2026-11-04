# Design: damage-control

## The queue

`s.repairQ` is the order of the nine jobs. Every step `repairPlan(s)` walks it: a job already whole is skipped, a job
that needs work takes up to `max_crews` of the crews left. The crews are the ship's repair energy, whole points, so the
Power station and the queue's own energy row set the same number (one rule, CLAUDE.md 6.1).

| Number | Value | Meaning |
|---|---|---|
| `per_crew_s` | 0.004 | Health a crew mends a second (0.4 % of a job), browned out with the ship |
| `max_crews` | 3 | Most crews on one job; the rest go down the queue |
| `mount_per_crew_s` | 0.003 | The weapon mounts' rate |
| chief engineer | +20 % a skill level | The repair officer's ability (change `officers`) |

A job's health is its worst part: the reactor is the lower of its confinement and its coolant loops (the worst loop is
mended first, then the confinement); the warp job is its worst nacelle; the mounts job its worst mount. The time to
whole (`jobEta`) sums what every part still needs at today's crews, so the readout and the rule are one calculation.

The old rule mended the priority system and every mount at once; the queue replaces it. `A.priority(k)` (Top) and
`A.repairUp(k)` (Up) are the only ways to change the order.

## The Escape scene

The numbers are set so the captain must act: left as it is (engines first) the reactor melts down in about 40 s; with
the reactor first and the nacelles second the ship can jump in about two minutes, before the pursuers close. Hull 35 %,
shields 20 %, reactor confinement 0.4 with coils out of line and loop A at 0.25, nacelles 0.15 and 0.35, sensors 0.3
(the plot runs at 30 % speed), banks 22 of 40.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| D1 | Should spare parts in the hold speed repairs, and be used up by them? | Not yet: crews only |
| D2 | Should a destroyed system (0 %) be repairable in battle, or only at a dock? | Repairable, slowly, as now |
| D3 | Three crews at most on a job? | Yes |
| D4 | Should the AI order its own queue? | Not yet: its default order |
