# Tasks: game-flow

## 1. Mockup (now)

- [x] 1.1 Proposal, design and spec deltas, citing the SFC2 and SFC3 manuals' pages
- [x] 1.2 Survey questions F1-F6 in `docs/design/survey.html`
- [x] 1.3 `docs/mockups/game-flow.html`: main menu, options, quit; skirmish scenarios, fleet, loadout, stand-in battle, results; campaign map (hex grid, perspective, pan and zoom, territory, terrain, standees, movement, end turn), ports (shipyard, outfitter with supplies, recruiting), missions (markers, briefing, deploy, results, back to the map); desktop and phone, landscape and portrait
- [x] 1.4 Named shots for every screen, shot at desktop and phone size and looked at
- [x] 1.5 Published (https://claude.ai/artifact/UBbAouwAU3JvcbGf6MhzLt); recorded in `docs/artifacts/game-flow/`; listed in the mockup tracker and `mockup-work.md`
- [x] 1.8 Version 14 (2026-11-05): 3D banners; pieces centred when alone; multi-turn routes and orders; the sector panel's Briefing key; the debrief (change `campaign-missions`)
- [x] 1.7 Version 13 (2026-11-05): factions move their own fleets every few turns; mission standees; the player's fleet on gold; ships face their last move; strength flags; freighters and routes (own, and allies' routes); nebulae in the owner's palette, a main and an accent colour each
- [x] 1.6 Version 8 (2026-11-05): the campaign in the faction skins and the faction screen (change `faction-select`), missions as scripts and the first ship (change `campaign-missions`), the game's scenarios as skirmish test maps

## 2. After approval for build

- [ ] 2.1 Fold the owner's notes and survey F answers into the design and the page
- [ ] 2.2 Every scenario above mapped to a test or a playtest beat

## 3. After approval for implementation (a separate request)

Approved for implementation by the owner, 2026-11-05: "implement game flow into the game, but I want you to implmenent
missions as well". The work is change `game-flow-engine` (and `mission-scripting`, `ship-schematics`).

- [x] 3.1 Data: `data/campaign/campaign.json` (factions, terrain, tiers, hulls, systems, supplies, the map) and `data/missions/`, validated on load
- [x] 3.2 Simulation: campaign state, movement and routes by turn, the factions' turn, mission offers, battles and results, the shop, saves, with tests (`tests/Core.Tests/CampaignTests.cs`); loadout budgets for skirmish still to do (`game-flow-engine` 2.3)
- [ ] 3.3 Screens in Godot (done: `src/Game/Flow/`), then a playtest at desktop and phone size (`game-flow-engine` 1.6)
