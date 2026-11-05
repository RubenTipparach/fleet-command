# Design: campaign-missions

## A mission script

`FCMissions.define(script)`, in `docs/mockups/lib/campaign-missions.js` (its header lists every field):

| Field | Meaning |
|---|---|
| `id`, `name`, `glyph`, `kind` | `kind` is `contract` (expires after `turns`, default 3, unless accepted) or `story` (never expires) |
| `reward` | Base credits (contracts add 0 to 200 at random, in 50s) |
| `where(ctx)` | The sectors it may be offered in |
| `brief(ctx, m)`, `objective(ctx, m)` | The briefing and the one-line goal |
| `needs(ctx, m)` | Why the fleet cannot take it yet, or nothing |
| `battle(ctx, m)` | `{ enemy, count, name? }`, or nothing for a mission done on arrival |
| `result(ctx, m, win)` | `{ credits, standing, prize, next, log }` |

`ctx` is the campaign as a script may see it: the side and its rival, the sectors, the fleet's sector, the home port, tests
(own, hostile, plain, port, the group in a sector), the fleet and its marines, the campaign's seeded random numbers, and
distances. A script never changes the campaign itself; the page applies what `result` returns. That keeps every rule of
credits, standing, prizes and the fleet's size in one place (CLAUDE.md section 6.1) and makes a script safe to drop in.

## The flow

1. Missions show as markers: amber a contract, pale blue a story mission, green one you have accepted.
2. A marker opens the briefing: objective, opposition (from the script's battle), reward, expiry. **Accept** puts it in
   the mission log (the top bar's Missions key) and draws the route there.
3. The player moves the fleet. Its miniature flies at 2.6 sectors a second, lifting up to 0.35 board units off its stand in
   the middle of the route and turning to each leg. The camera is not moved (CLAUDE.md section 8).
4. Arriving in an accepted mission's sector opens its briefing. **Deploy** starts the battle (the stand-in until the battle
   console takes it), **Complete** a mission without one.
5. The result is applied and shown, with the script's log line and any missions it offered next.

## A new campaign

| Side | Home | First ship | Rival |
|---|---|---|---|
| Concord | Halcyon (4, 10) | CNS Resolute, Swift frigate (tier 1) | Krai Warhost |
| Krai Warhost | Vahr (24, 10) | Kharz, Krai corvette (tier 1) | Concord |
| Ssarith Hunt | Calder (17, 2) | SH-1, Hunt corvette (stand-in, tier 1) | none yet (survey H1) |

4000 credits, two marines, standing 40 with your own side, -40 with your rival, 0 with everyone else. Shakedown cruise is
offered two to three sectors from home, and eight contracts across the map.

Hulls are five tiers (the first draft's slots, costs and hull points); each side names its own: the Concord line (Swift,
Petrel, Albatross, Pelican, Condor, mockup 6), the Krai line (corvette, frigate, cruiser, dreadnought, mockup 7), and three
Hunt stand-ins. A shipyard sells only its own side's hulls.

## Test maps in skirmish

The owner, 2026-11-05: "Add test maps to scenarios". The skirmish lists the game's own scenarios (`data/battle/scenarios.json`,
inlined: Battle, Duel, Arming and Wrecks for each side) under **Test maps**, after the presets. A test map shows its ships at
their starts on a 2 km grid and its roster, and launches straight into battle with its fixed fleets on its own side.

## Mission types and scenarios

The catalogue of mission types, the outcome grades, the first scenarios and story chains, and what the script shape needs
next (grades, objectives, teams, lines per side, hails, gates, branches) are in the Fleet Command Missions guide,
`docs/design/mission-guide.html` (https://claude.ai/artifact/LRgd2iRm8gdTZ84i7htXhH), written from the reference game's
mission files (structure only, our own words).

## Mission types in the mockup

The owner, 2026-11-05: "let's implement all \"ready\" mission types, amke sure to keep this artifact updated. I'd also like
to add \"capture the prize\" mission to capture a designated target ship for research". Every type the Missions guide
marked ready is a script: Patrol, Fleet action, Ambushed (offered as a Dispatch run whose briefing expects no one),
Overwhelming fleet (Stem the tide), Capture, Distress call (a trap four times in ten). Capture the prize is new: a named
rival frigate or cruiser under escort, taken and not destroyed, then towed home by a story mission (Deliver the prize) that
pays the research bounty. The guide's "In the mockup now" section has each one's battle, objectives and grading.

The script shape gains:
- `battle` returns groups (`fac`, `count`, `role`: enemy, ally or neutral, an optional `label` and `tag`) and objectives
  (`id`, `text`, `primary`, `byWithdraw`), and may give `intel` (what the briefing believes). The older `{ enemy, count }`
  is one enemy group with the script's objective as primary and "lose no ships" as bonus.
- `setup(ctx, m)` fills `m.data` when offered (a target's name, whether a call is a trap); a chain's `next` may carry data.
- `result(ctx, m, o)` receives the outcome `{ grade, win, met, how, pay }`; `grade(ctx, m, met, how)` may replace the
  default rule.

**Grades** (the guide's five): every primary met is a victory, every bonus too outstanding, withdrawing short of the primary
a draw, failing it a defeat, losing the fleet a crushing defeat. Pay: victory 1, outstanding 1.5, draw 0.25, defeat and
crushing 0, of the reward, to the nearest 10 cr. Hull lost per ship, by grade (base plus a random spread, percent):
outstanding 5 + 15, victory 10 + 35, draw 20 + 30, defeat 35 + 50, crushing 60 + 30 with every ship but the flagship lost.

The stand-in battle has no rules to decide an objective, so the tester ticks each one; the bar shows the grade the script
gives. Asteroid sectors put rocks in the stand-in (the battle console's rocks are the real ones, mockup 2; owner, 2026-11-05:
"we do have asteroids").

## Open questions

| # | Question | Provisional |
|---|---|---|
| 1 | The owner's mission scripts | The two-step story chain stands in |
| 2 | Free flight in real time, or move points and turns as now? | Move points and turns, the miniature animating the route |
| 3 | The Ssarith Hunt's rivals and home | None, Calder in the north, until its culture is set |
| 4 | Does a failed story mission come back? | First blood offers itself again; each script decides |
| 5 | What does research give for a delivered prize? | A bounty and standing; an unlock later |
| 6 | Five grades or three? (guide question 2) | Five |
