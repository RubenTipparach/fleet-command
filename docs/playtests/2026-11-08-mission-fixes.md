# Playtest: the mission sweep again, on the standoff fixes (2026-11-08)

The cloud session's request after PR #66 (`claude/missions-fixes`, task 4.5 of `mission-systems`): run the Mac mission
sweep again on its fixes for the three standoffs in `2026-11-08-mission-types.md`, especially Krai against Vos on belt
patrol, patrol and capture, Krai First blood, and Concord Crossroads with a failed jump.

| | |
|---|---|
| Commit | `26fe4b9a` (PR #66, checked out detached), with the bot changes below built locally and not committed |
| Machine | the owner's Mac (Apple silicon), Godot 4.7.1 .NET |
| How | As before: one headless run per mission and side at a fixed 30 fps, a new campaign, the `offer` beat, then `contract`. 80 runs (the 40 missions the first sweep played, from both sides) |

## The three fixes

| Fix | Seen |
|---|---|
| 1. A withdrawal holds over its risk limit | **Works.** Concord Crossroads: Withdraw at 422 s, then "Holding the jump: risk 55 %, over 30 %. Jump from the Warp page, or Withdraw again to call it off." at 529 s; no jump, no wrecked nacelles, and the battle reached its debrief. The failed jump itself did not recur on this run, so calling off a withdrawal on a wrecked drive was not seen |
| 2. A story battle that cannot be withdrawn from ends as a stalemate | **Works, once the bot let the battle run** (below). Krai First blood ended a draw |
| 3. AI captains close in; the bot powers its engines for a chase | **Half.** Krai against three corsairs (belt patrol, patrol, distress, nebula sweep, orbital patrol) now fight and lose instead of stalling. Krai against a lone corsair (capture, First blood, scout, relief) still stalls: below |

## Found

1. **The bot paused its own battle.** On a mission with no withdrawal, the bot opened the menu, found Withdraw disabled
   and left the menu open. The battle does not step while the menu is open (`Main.cs`, the `MenuOpen` check), so the
   clock stopped at 62 s and the stalemate never came. Closing the menu with Resume (`m.resume`) after a refused
   Withdraw fixed it. This is a one-line bot fix for PR #66, sent to its session. A player closes the menu, so the game
   was not at fault.
2. **The lone-corsair chase still stalls, on arc, not range.** At 62 s in Krai capture and First blood: the corsair is
   at 47-52 km (an eighth of its fighting range, the AI's closest), both ships at 1,200 m/s, the corvette's two
   disruptors charged and in range (140 km) but **out of arc**, with the helm on intercept. So the Krai forward guns never
   bear on a ship that circles at their own speed.
   - The bot's engine boost never runs: the corvette starts with 2 boxes in weapons and the boost needs more than 2.
     It sits at 6 boxes in engines (1,200 m/s) against a class top speed of 3,800 m/s.
   - The AI's closest range, an eighth, still leaves the corsair circling at about 50 km, where only a ship facing it
     can shoot.
3. **The Concord now loses belt patrol, nebula sweep and orbital patrol**, which it won in the first sweep: the Vos
   corsairs closing in hit harder. Every offer is still Heavy against one ship (the fixed seed), so this says the
   corsairs got more dangerous, not that the missions are unbalanced.

## The sweep

No run crashed and none stuck. Changes from the first sweep:

| Mission | Concord | Krai |
|---|---|---|
| belt_patrol | lost (was won) | lost (was a stall) |
| capture | won | still a stall: arc |
| crossroads | withdrew, held over risk; lost (was stuck) | lost |
| distress | won | lost (was a stall) |
| first_blood | won | stalemate, a draw (was stuck) |
| nebula_sweep | lost (was won) | lost (was a stall) |
| orbital_patrol | lost (was won) | lost (was a stall) |
| patrol | lost | lost (was a stall) |
| relief | lost | stall: arc, at 58 km |
| scout | withdrew: out of time | stall: arc, at 41 km |
| starbase_construction | time, outstanding | lost |

Everything else came out as in the first sweep: the same wins (shakedown and stop the scout from both sides; Concord
fleet action and monster hunt), the same losses, homeworld assault and sabotage refused by their needs, and the non-combat
operations (away team, extraction, find the ship, salvage, survey) still beyond the bot.

No take was recorded: this was a headless rerun, and the first sweep's take is still the recording of these missions.
