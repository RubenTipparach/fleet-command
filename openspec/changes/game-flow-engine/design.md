# Design: game-flow-engine

## Where things go

| Part | Where | Why |
|---|---|---|
| Campaign state, map, moves, factions' turn, shop, saves | `src/Core/Campaign/Campaign.cs`, `CampaignState.cs` | plain C#, tested headless (CLAUDE.md 6.2) |
| Campaign data | `data/campaign/campaign.json`, `CampaignData.cs` | tuning in data, validated on load (6.4) |
| Missions | `data/missions/`, `MissionScript.cs`, `MissionRun.cs` | change `mission-scripting` |
| Which screen is up, saving, starting battles | `src/Game/Flow/GameFlow.cs` | the flow's one controller; Main wires it (the composition root) |
| The 3D map | `src/Game/Flow/CampaignMap.cs`, in its own world in a SubViewport | it never touches the battle's world, sky or camera |
| The screens | `src/Game/Flow/FlowScreens.cs`, `MapHud.cs` | `ConsolePanel`s: the kit's pieces and the console's immediate-mode controls |

The battle is the console the game already has. A mission's battle is a Scenario built by `MissionRun.Plan`, started with
Main's own `Start`. While a flow screen is up, the battle is hidden and does not step.

## Matching the mockup

The map's random numbers are the mockup's `rng()` (mulberry32), drawn in the same order, so a new campaign's territory
and terrain match the page's (a test pins one row against the page's generator). Where the game is simpler than the page,
the difference is in `docs/design/mockup-sync.md` with a task here.

## Saves

`user://campaign-<side>.json`: the whole `CampaignState`, the random generator's state included, so a loaded game draws on
exactly as it would have (a test saves, loads and compares the next turn). It is written after every move, End turn,
mission and purchase.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| E1 | Should refits change the battle (the fittings, not only the class)? | Not yet: a hull fights as its battle class; the fittings and supplies are the campaign's (task 2.3) |
| E2 | Should the Ssarith Hunt be playable before its console skin is built? | No: its card says "Not in the game yet" |
| E3 | Should a lost fleet end the campaign? | No: a new smallest hull is commissioned at home |
