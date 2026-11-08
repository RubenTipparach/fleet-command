# Playtest: every mission type, ports by standing, hunters (2026-11-08)

The cloud session's handoff after PR #65 (owner, 2026-11-08: "Once you have completed a first pass. Commit changes and
merge to main, and hand off to Mac session to playtest all general mission types"). Changes `mission-systems` and
`faction-standing`.

| | |
|---|---|
| Commit | main at `e5fd63cd` merged into `claude/freighter-missions` (PR #64) |
| Machine | the owner's Mac (Apple silicon), Godot 4.7.1 .NET |
| How | **The sweep:** for each of the 42 mission files other than the two freighter encounters (played on 2026-11-08, `2026-11-08-freighter-missions.md`), from the Concord and from the Krai, a new campaign, the mission put on offer with the new `offer` beat (the tests' `OfferMission`, the one input no player gives), then the `contract` beat played it as a player does: its sector, its briefing, Accept, sail, Launch, the fight (Fight; a stall of 60 s with no hull changed is withdrawn from), the debrief. Headless at a fixed 30 fps, one Godot run per mission, 84 runs. **Ports and hunters:** `data/playtests/standing-concord.json` and `standing-krai.json` (new, in the suite) sail to a Kelvane port at standing 0 and at 70, then into the nearest hostile space until the hunters come (beats `port` and `loiter`); the Concord's run recorded in a 1920 x 1080 window |

## Ports and hunters

| Side | Kelvane port, standing 0 | Standing 70 | Hostile space | Pursuit |
|---|---|---|---|---|
| Concord | Lyceum Depot opens parts (Supplies key and Market lit; Shipyard, Loadout, Crew dark) | parts, supplies, weapons, ships, officers (every key lit) | Vos space at 11,16: alarm 0 for two turns, 1 on the third with one hunter | caught on turn 7 by the Vos hunters (1 ship); won, outstanding |
| Krai | Lyceum opens parts | all five | Vos space at 13,16: alarm 0 two turns | caught on turn 7 (1 ship); won, outstanding |

The sector panel shows the alarm ("Vos Syndicate alarm 0 of 4 · no hunters yet"), and the hunters' briefing opens itself
when they catch you. The Supplies key opens at standing 0 because the Supplies page sells parts, the first grant.

## The sweep

Every new campaign starts from the same seed, so every mission the sweep put on offer rolled **Heavy** strength, and the
fleet was one starting ship. Most fights against a heavy offer were lost; that is the sweep's bias, not each mission's
balance. Whether each mission is winnable by a player is not answered here (below).

| Mission | Concord | Krai |
|---|---|---|
| ambush_lie (Ambush) | time ran out, defeat | time ran out, defeat |
| ambush (Convoy ambush) | lost | lost |
| ambushed (Dispatch run) | lost | lost |
| assassinate | lost | lost |
| asteroid_base | lost | lost |
| away_team | withdrew: stall (a planet to survey) | withdrew: stall |
| belt_patrol | **won** | withdrew: stall (see 3) |
| capture | **won** | withdrew: stall (see 3) |
| crossroads | **stuck** (see 1) | lost |
| dead_engines | lost | lost |
| defend_base | lost | lost |
| defend (Convoy defense) | lost | lost |
| diplomat_run | withdrew: out of time | lost |
| distress | **won** | withdrew: stall (see 3) |
| drain_yard | lost | lost |
| extraction | withdrew: stall (beam up Dr. Hale) | withdrew: stall |
| find_ship | withdrew: stall (scan the smuggler) | withdrew: stall |
| first_blood (story) | **won** | **stuck** (see 2) |
| fleet_action | **won** | lost |
| holding_action | lost | lost |
| homeworld_assault | refused: "bring a fleet of two or more" | refused, the same |
| monster_hunt | **won** (victory) | lost |
| nebula_sweep | **won** | withdrew: stall (see 3) |
| orbital_patrol | **won** | withdrew: stall (see 3) |
| overwhelming (Stem the tide) | lost | lost |
| patrol | withdrew: out of time | withdrew: stall (see 3) |
| planet (Planetary assault) | lost | lost |
| prize_home (story) | not playable alone: it needs the `prize_name` a won Capture the prize hands it | the same |
| prize | lost | lost |
| pursuit (event) | not offered this way: played by the hunters above | the same |
| relief | withdrew: out of time, draw | withdrew: stall |
| sabotage | refused: "carry at least 3 marines" | refused, the same |
| salvage | withdrew: stall (recover 8 units) | withdrew: stall |
| scout | withdrew: out of time, draw | withdrew: stall (scan two ships) |
| secret_below | withdrew: stall at 347 s | lost |
| shakedown (story) | **won** | **won** |
| shipyard_strike | lost | lost |
| sleeping_giants | lost | lost |
| starbase_construction | lost | lost |
| station (Enemy station assault) | lost | lost |
| stop_scout | **won** (victory) | **won** (victory) |
| survey | withdrew: stall (survey a planet) | withdrew: stall |

Every mission loaded, briefed, launched and graded except the four the table says; no run crashed.

## Found

1. **A failed jump leaves a withdrawal that can never finish** (Concord, crossroads). After 420 s of fighting, the bot
   pressed Withdraw. The drive charged and plotted, jumped at **52 % risk**, and the jump failed: "Warp field collapsed
   ... nacelles damaged, banks drained, course lost". Both nacelles were left at 0.00, so the banks can never charge
   again ("not enough energy stored: 0 of 40 E"). The battle stays "Withdrawing" with the key disabled, and it only ends
   if the enemy finishes the ship. Two minutes later two Krai ships (one at 100 %, one at 62 %) still had not. The
   withdrawal jumps the moment the drive is ready, whatever the risk. A player is trapped once both nacelles are gone:
   no withdrawal and no way to cancel it. This needs a rule: refuse an auto-jump over some risk, let Withdraw be
   cancelled, or end the withdrawal when the drive is wrecked. Reproduced with the bot's new message, which now gives the
   drive's state when a withdrawal never ends (`ScriptedCaptain.LaunchAndFight`). In the simulation alone the same
   withdrawal jumps at 21 s, at the start or after 300 s.
2. **A story mission refuses Withdraw, so a stalemate never ends** (Krai, First blood). The Krai corvette and a Vos
   corsair never closed (point 3), neither took damage, and the menu says "No withdrawal" on a story mission. A player
   could close the range; the bot could not, and nothing ends the battle.
3. **The Krai corvette cannot catch a Vos corsair.** Every Krai stall against the Vos (belt patrol, capture, distress,
   nebula sweep, orbital patrol, patrol) is the same picture: "Varn at 49.0 km, speeds 1200/1200 m/s, 0 mounts would
   fire, helm intercept". The corvette's class can make 3,800 m/s, but at its starting power the bot's intercept holds
   1,200, and the corsair keeps away at the same speed, laying decoys and flares. The Concord frigate wins all the same
   missions. A player can put more power to the engines; the bot does not, so this is a bot limit first. It is also worth
   a look in the Vos AI's kiting against a ship of exactly its speed.
4. **The non-combat operations are beyond the bot.** Away team, extraction, find the ship, salvage, survey and scout need
   Scan, Beam down or the transport keys; the bot has no beat for them yet, so each stalled and withdrew with a draw. They
   loaded, briefed and launched; whether their flows feel right is still for a player, as the handoff said.

## Not checked

- Each mission at Light or Moderate strength, or with a fleet: the sweep's fixed seed rolled Heavy every time.
- Frame time with the planet shader, the planet view key, the Beam down flow and the lava planet's glow spots: no run here
  went into a planet's battle at a window size with a player's eye on it.
- A phone shape with a panel expanded during these missions (the battle suite covers the expanded panels in battle).

## The take

The Concord's standing and hunters run in a 1920 x 1080 window, recorded with OBS from `PLAYTEST_START` to `PLAYTEST_END`:
2 min 2 s, H.264 at 1920 x 1136, 60 fps, at the take's own size:
https://drive.google.com/file/d/13gHFAzCs5hXN95Lybp_bLkXYJDiJj2FS/view (`fleet-command-2026-11-08-standing-hunters.mp4`). The contact sheet showed the menu, Lyceum Depot's panel at
standing 70 with the Market key, and the Hunted battle against the Vos corsair Grist with its objective "Beat the
hunters". Stills are in `docs/screenshots/game/standing-concord/` (both ports, the alarm, caught, the battle and the
debrief).
