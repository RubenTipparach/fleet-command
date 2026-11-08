# Playtest: freighter missions from every side (2026-11-08)

The owner, 2026-11-08: "start implenting and playtesting frieghter missions", and "frieghter missions are availible from any
sector with a fregher that your ship is on". Change `freighter-missions` (task 2.7) and `freighters` (3.4).

| | |
|---|---|
| Branch | `claude/freighter-missions` |
| Machine | the owner's Mac (Apple silicon), Godot 4.7.1 .NET |
| How | `data/playtests/freighters-<side>.json`: the scripted captain starts a new campaign, sails into another faction's freighter's sector and presses her Raid key, fights the raid, cashes out any cargo taken at the nearest port that buys it, then sails to one of its own freighters and plays her Escort (beats `meet` and `cash_out`, `src/Game/Playtest/ScriptedCaptain.Freighters.cs`). Headless at a fixed 30 fps for the four sides; the Concord's run again in a 1920 x 1080 window, recorded with OBS |
| Suite | the four runs are in `data/playtests/suite.json` |

## Results (headless, the final build)

| Side | Raid | Cash out | Escort | Result |
|---|---|---|---|---|
| Concord | the Kelvane Long Acre, struck at 138 s; won, outstanding | 1,050 cr at Meridian | the Patience; won, outstanding | ok |
| Krai | the Hunt Wayfarer, struck at 108 s; then the bot stalled on her escorts (no hull changed for 60 s) and withdrew; primary met, victory | 1,350 cr at Ironmouth | the Tidewell, against three Concord raiders: she and the carrier melted down; lost, crushing | ok |
| Ssarith | the Krai Ember Reach; won, outstanding | 1,350 cr at Pell Reach | the Wayfarer; won, outstanding | ok |
| Kelvane | the Concord Hollow Bell with three escorts (a heavy offer) against one frigate: the frigate's core melted down at 69 s; lost, crushing | no cargo aboard | the Long Acre against three Vos cruisers; won, outstanding | ok |

## Found and fixed

1. **A raided freighter could not be taken.** Hits through her shields knocked out both coolant loops while her hull was at
   34 %, above the capture line (25 %), so her core went critical and she melted down 20 s later. With that fixed, she
   struck at 25 % and the ship that had her locked kept firing until she broke up. Now a prize strikes when her core goes
   critical, every lock on her drops as she strikes, and a struck ship's core is shut down by the boarding party
   (`Battle.cs`, `Battle.Reactor.cs`; test `A_prize_whose_core_goes_critical_strikes_and_her_shut_down_core_never_melts_down`).
   Provisional: survey V6.
2. **Cashing out crashed the map when the freighters were the whole fleet.** The Krai carrier was lost after taking the
   Wayfarer, so the freighter was all the fleet had; selling her left no ship and `CampaignMap.Rebuild` read ship 0 of an
   empty list. `Campaign.Recommission` is now the one replacement (a new smallest hull at home), after a lost battle and
   after a cash out (test `Cashing_out_freighters_that_were_all_the_fleet_had_commissions_a_new_ship_at_home`).
3. **The Institute had no freighter.** The Twin ledger (`freighter_kelvane`, class `kelvane_fr`) now runs its Lyceum route.

## Seen, not changed

- An encounter's escorts or raiders come at the offer's strength, one to three ships, as a contract's do, and the briefing
  shows it. A new campaign's single ship against a heavy offer loses (the Kelvane raid, the Krai escort); a player can turn
  it down, the bot takes the nearest.
- Both losses ended with the captain's own core melting down: the bot never tends its reactor (the player's job on the
  Reactor page). A bot limit, not a game fault.
- The Krai raid stalled on the Hunt frigates after the freighter struck; the bot withdrew with the primary met.
- The freighter names are one shared list (`map.freighter_names`), and Patience is also a provisional Kelvane warship name
  (survey I10).

## The take

The Concord run, recorded: see the Drive link in the PR and below once uploaded.
