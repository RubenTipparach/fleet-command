# Design: mission-systems

## Sensors

`Battle.SensorKm(ship)` = (`sensors.base_km` 70 + `sensors.per_e_km` 14 x energy boxes in sensors) x the sensors' health, x
`passive_x` 0.6 on passive sensors. `Battle.Identified(t)`: your own side; within any friendly ship's reach; or scanned. The
view shows an unidentified ship as "Unknown contact" (no name, class or captain). The AI's own detection (`Battle.Detected`,
the cloak rules) is unchanged, except that a ship not yet arrived is seen by no one. A finished scan (the existing scan of the
locked target, `Battle.Sensors`) adds the ship to `ScannedIds` and says so.

## Planets, sites and away teams

- `Battle.ScanPlanet()` scans the selected planet at the ship-scan rate x `planet_scan_x` 0.6, only while the commanded ship is
  within `planet_scan_km` 60 km of its surface. Done, every site is `Known`.
- `Battle.BeamDown(planet, site)` sends up to `away.team_marines` 4 marines; it needs the site known and not held, no team
  there, the planet within the transporter's reach (15 km, comms.json), a pad ready and a marine aboard
  (`CannotBeamDown` says which is missing, for the key's tip). After `away.away_s` 18 s the team rolls once per marine
  (`win_p` 0.55 each); a hostile site is held when the wins reach its garrison, and each lost roll costs a marine at
  `loss_p` 0.35. Done, it beams back as soon as its ship is in reach with a pad ready. The randomness is the "away" stream.
- The envoy (`Transfer("vip", +1)`) lands on a planet with `takes_envoy`; `Transfer("vip", -1)` beams up whoever is
  `waiting` there.

## Timing, sleepers, runners and truces

| Field | Rule |
|---|---|
| `arrive_s` | waits `stage_km` 2500 km off the field, unseen and untouchable, and warps in at its place at the time |
| `wake_s` | no orders, holds fire, still, until the time or until its hull drops |
| tag `scout` | a runner: course away from the fleet, full speed, never fights |
| tag `truce` | holds fire and place until anyone fires (`Battle.FirstFire`), then attacks |
| fixed hulls | `Ship.Fixed`: never moves (stations, yards, bases, outposts, batteries, pods) |
| `time_limit_s` | at the limit `Over = "time"` (graded as an end), `TimeUp` true |
| `dead_engines` | from `every_s`, for `for_s` of every `every_s`, every ship's ordered speed is 0 |

## Placeholders

| Class | Shape | Length | Hull | Weapons | Notes |
|---|---|---|---|---|---|
| `sb` Starbase (existing) | station | 420 m | 260 | 6 lances, 2 PD | already in the transport scenes |
| `yd` Shipyard | yard | 560 m | 300 | 4 lances, 2 PD | |
| `ab` Asteroid base | rock_base | 380 m | 320 | 4 disruptors, missiles, PD | |
| `op` Outpost | outpost | 200 m | 140 | 2 lances, PD | also a listening post or a station frame |
| `pb` Planetary battery | battery | 120 m | 90 | torpedo, lance | placed in orbit by `at_planet` |
| `pod` Cargo pod | pod | 40 m | 20 | none | holds salvage |
| `mon` Void leviathan | monster | 620 m | 420 | 3 disruptors, lance | the fauna; moves at 1600 m/s |

Figures are provisional until each has its own design; every one uses the Petrel's deck plan meanwhile.

## The catalogue as files

| Type | File | Primary | Uses |
|---|---|---|---|
| Base assault | station | destroy the station | station hull |
| Planetary assault | planet | silence the orbital batteries | planet, batteries, an away team bonus |
| Shipyard strike | shipyard_strike | destroy the yard | yard, sleepers in dock |
| Asteroid base assault | asteroid_base | destroy the base | base hull |
| Homeworld assault | homeworld_assault | station and home fleet | station, batteries, planet |
| Defence | defend_base | the station stands | allied station, a late wave |
| Holding action | holding_action | hold the post to the time | time limit, three waves |
| Starbase construction | starbase_construction | the frame stands to the time | time limit, waves |
| Drain the yard | drain_yard | reach the yard and hold it to the time | marker, time limit |
| Ambush | ambush_lie | stop every runner | runners, time limit |
| Sleeping giants | sleeping_giants | destroy the capital ships | sleepers |
| Monster | monster_hunt | kill the leviathan | the monster, a scan bonus |
| Dead engines | dead_engines | destroy the raiders | engine outages |
| Crossroads | crossroads | stop the raiders, harm no traffic | neutral freighters |
| Scout | scout | scan two ships and reach the jump point | scans, marker |
| Scout (stop one) | stop_scout | stop the runner in time | runner, time limit |
| Survey | survey | survey the planet | planet scan |
| The secret below | secret_below | survey, take the lab, team home | planet, hostile site |
| Find the ship | find_ship | scan the smuggler | quarry among traders |
| Diplomat run | diplomat_run | land the envoy | envoy, truce |
| Relief (transporter) | relief | relieve the planet | cargo beamed down |
| Extraction (transporter) | extraction | beam up the scientist | waiting |
| Away team (transporter) | away_team | survey, hold a site, team home | sites |
| Sabotage (transporter) | sabotage | take the depot | hostile site |
| Salvage (transporter) | salvage | 8 goods aboard | pods with cargo |
| Hunted (event) | pursuit | beat the hunters | faction-standing |

The new contracts weigh 0.4 each, so the patrols (weight 10 of the contracts' 30.2) stay about a third of what is offered.

## Fixes from the first playtest

The Mac session's sweep of every mission type (2026-11-08, `docs/playtests/2026-11-08-mission-types.md`; owner: "yes fix all
three"):

| Found | Fix | Numbers |
|---|---|---|
| A failed jump wrecked both nacelles; the withdrawal waited for a charge that never came, the Withdraw key locked, until the ship died | A withdrawal jumps by itself only at or under a risk limit (over it the drive holds, says why, and the captain may jump by hand); Withdraw pressed again calls it off; a withdrawal whose drive can no longer charge is called off by itself | `warp.withdraw_max_risk` 0.3 (`data/battle/systems/reactor.json`) |
| A story mission refuses Withdraw, so a fight neither side can win never ends | A battle that cannot be withdrawn from, where no ship has been hurt for a while, ends as a stalemate, graded as a withdrawal (a draw) | `stalemate_s` 180 (`data/battle/tuning.json`) |
| A Krai corvette never caught a Vos corsair circling at the same 1200 m/s, and no hull changed | An AI captain whose fire has not landed closes in: half its fighting range for every span without a hit, to an eighth; the scripted captain moves two boxes from weapons to engines when a chase goes nowhere | `ai_close_after_s` 20 (`data/battle/tuning.json`) |

The golden seed was re-pinned for the AI rule (`tests/Core.Tests/GoldenSeed.cs`); `tests/Core.Tests/StandoffTests.cs` pins all
three, the AI rule's test failing with the rule switched off.

## Open questions

| # | Question | Provisional |
|---|---|---|
| 1 | The placeholders' looks and figures | Stand-ins and the numbers above until each has a mockup |
| 2 | Should an unknown contact hide its faction colour too? | No: only its name, class and captain |
| 3 | Away team size and odds | 4 marines, 18 s, 0.55 a roll |
| 4 | A monster's culture (none: it is fauna), and whether more than one kind | One, the void leviathan |
| 5 | Should scanning a ship reveal its cargo in the readout? | Not yet; the mission counts the scan |
