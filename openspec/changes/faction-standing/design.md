# Design: faction-standing

## Privileges

`Campaign.GrantsOf(fac)`: everything for your own faction; nothing for neutral, your rival, or a faction below 0; otherwise each
`rules.privileges` grant whose standing you have. `Campaign.Allows(grant)` asks it of the port where the fleet stands, and
every shop rule asks `Allows` (CLAUDE.md 6.1: the key's state and the purchase are the same rule):

| Grant | Standing | Opens | Rules that ask |
|---|---|---|---|
| parts | 0 | repair kits, hull repairs | `Resupply` of `parts`, `Restock`, `Repair` |
| supplies | 15 | marines, shuttles, mines, missile reloads | `Resupply` of the rest |
| weapons | 30 | buying and selling systems, fitting stock | `BuySystem`, `SellSystem`, `FitStock` |
| ships | 50 | that faction's yard (`YardSide`) | `Buy` |
| officers | 70 | hiring | `Hire` |

A refusal at a friendly port names the standing needed and yours. The Market (selling ships) stays open at 0 or better, as
before.

## Pursuit

State: `CampaignState.Heat` (alarm by faction), `FactionFleet.Pursuit` and `Recalled`. At End turn, before the factions move:

1. For each faction but neutral and your own: alarm + `heat_per_turn` + max(0, -standing) / `hostility_step` if the fleet
   ends the turn in its hostile sector; otherwise alarm - `cool_per_turn` (not below 0).
2. Level = the number of `levels` the alarm has reached. Hunters wanted = `size_base` + `size_per_level` x (level - 1), at
   most `size_max`.
3. None out and some wanted: a group "<faction> hunters" leaves its port nearest the fleet. Out and fewer than wanted: it is
   reinforced. Out and none wanted: recalled home, where it disbands.

Hunters move `speed_sectors` a turn on the cheapest path to the fleet. In the fleet's sector, `Campaign.Caught` offers
`pursuit.json` (an `event`: never random, may be withdrawn from, gone at End turn) handed the group's name, faction and size;
the game opens its briefing. Won, its outcome's `disband` breaks the hunters up; the alarm stays, so new hunters of the same
size leave the next turn the fleet is still in hostile space. No randomness is used.

## Open questions

| # | Question | Provisional |
|---|---|---|
| 1 | The standings of each grant | 0, 15, 30, 50, 70 |
| 2 | Do foreign ships cost more than your own? | Same price |
| 3 | Do the pirates (Vos) hunt too? | Yes, in their own sectors |
| 4 | Should beating hunters lower the alarm? | No: "larger and larger fleets" |
| 5 | Should a caught fleet be forced to fight? | No: it may withdraw, or move on |
