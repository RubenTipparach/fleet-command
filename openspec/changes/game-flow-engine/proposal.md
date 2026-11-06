# Proposal: game-flow-engine

## Why

The owner, 2026-11-05: "implement game flow into the game, but I want you to implmenent missions as well". This is mockup 1
(`docs/mockups/game-flow.html`, v14; change `game-flow`, with `faction-select` and `campaign-missions`), approved for
implementation in the owner's words. Until now the game opened straight into a battle.

## What Changes

- **The campaign's rules in Core** (`src/Core/Campaign/`), ported from the mockup:
  - the map grown from the data, with the mockup's own random generator, so a seed draws the same territory in the page
    and the game;
  - the cheapest path and routes split into legs by turn, with orders flown on after End turn;
  - the factions' fleets and freighters moving every turn;
  - mission offers from the mission files (change `mission-scripting`);
  - standing, the shipyard, selling, refits, supplies, repairs and recruiting;
  - save and load as JSON.
- **The campaign's data** in `data/campaign/campaign.json`: factions, terrain, tiers, hulls (each with the battle class it
  fights as and its schematic), systems, supplies, the map's seeds, ports, fleets and freight routes, and the rules'
  numbers, including each grade's pay and prestige.
- **The screens in Godot** (`src/Game/Flow/`), drawn only from the console skin's kit pieces:
  - the main menu;
  - the faction screen (the rendered emblems; New campaign and Continue);
  - the skirmish list (the game's scenarios);
  - the campaign map in 3D: hex tiles by holder, borders, stars, planets, stations, asteroids, nebula puffs, your fleet
    on a gold stand, the factions' fleets with strength flags and headings, freighters and routes, mission banners, and
    the route preview by turn;
  - the top bar, the sector panel with the mission row and its Briefing key, the briefing, and the port (shipyard, fleet,
    outfitter, supplies, recruit);
  - the mission strip over the battle, with Withdraw;
  - the debrief (verdict, grade, credits, prestige, reputation, objectives, ships).
- **The game opens on the main menu.** The battle's own checks (layout test, parity test, stills, playtests,
  `--scenario`) still start straight into a battle.
