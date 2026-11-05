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

## Open questions

| # | Question | Provisional |
|---|---|---|
| 1 | The owner's mission scripts | The two-step story chain stands in |
| 2 | Free flight in real time, or move points and turns as now? | Move points and turns, the miniature animating the route |
| 3 | The Ssarith Hunt's rivals and home | None, Calder in the north, until its culture is set |
| 4 | Does a failed story mission come back? | First blood offers itself again; each script decides |
