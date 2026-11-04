# Design: cloaking

## Rules

`s.cloak.k` runs from 0 (seen) to 1 (cloaked) over `fade_s` toward the order. From the moment it starts to cloak until
it has fully decloaked a ship draws `cost_e`, its shields neither hold nor regenerate, and `fireMount` refuses it.

`detected(s, t)` is the one rule for whether s sees t: its own side, not fully cloaked, revealed to s's side, or within
`detect_km`. `canLock` builds on it (with the nebula's rule), and the contacts, the markers, the radar, the map, the 3D
model, the AI's choice of target and every lock read it. A ship that stops being seen is unlocked, with the reason.

| Number | Value | Meaning |
|---|---|---|
| `cost_e` | 5 E | Reactor draw while cloaking or cloaked |
| `fade_s` | 3 s | To cloak, and to decloak |
| `detect_km` | 1.5 km | Within this a cloaked ship is seen anyway |
| `sweep_e` | 2 E | Battery spent by a sweep |
| `base_km`, `per_eccm_km` | 4 km, +3 km a point | The sweep's reach; the science officer adds 1 km a level |
| `reveal_s` | 8 s | How long a sweep reveals what it found, to the sweeper's side |
| `sweep_cool_s` | 10 s | Between sweeps |
| `hit_penalty` | 0.25 | Off the chance to hit a revealed cloaked ship |

The AI with `ai.cloak` stays cloaked closing in, decloaks inside 6 km with 60 % of its guns charged, fights for 12 s
(`AI_CLOAK`), then cloaks again. Any AI that sees no enemy sweeps when it can, and heads for where it last saw one.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| C1 | Which classes carry a cloak? | Concord CA and DD, Kethran KDD and KFF; the Vos none |
| C2 | Should a cloaked ship leave a trace (a shimmer, a sensor ghost) at some range? | Only inside 1.5 km |
| C3 | Should cloaked ships be able to beam, launch or lay mines? | Not yet: no fire only, the rest unchecked |
| C4 | Should a sweep also show the sweeper to the other side? | Not yet |
