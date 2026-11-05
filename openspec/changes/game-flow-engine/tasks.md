# Tasks: game-flow-engine

## 1. This change (2026-11-05)

- [x] 1.1 `data/campaign/campaign.json` and `CampaignData.cs`, validated against the battle's data
- [x] 1.2 `Campaign.cs`: the map from the mockup's generator, paths and routes by turn, orders, the factions' turn, missions on offer, the shop, saves; tests
- [x] 1.3 `GameFlow` and the screens: menu, factions, skirmish, map and its HUD, briefing, port, mission strip, debrief
- [x] 1.4 The game opens on the menu; the battle's checks still open in battle
- [x] 1.5 `--flow=<screen>` stills (`Main.FlowStill`), looked at for both skins
- [x] 1.7 Accepting a mission sets course for its sector, and the briefing reopens on arrival (owner, 2026-11-05: "if you accept a
  mission your ship auto navigates there"); the same in mockup 1 v15
- [x] 1.8 The debrief stands over the battle as it ended, dimmed, or over the map (owner: "background shouldnt disappear on mission win screen")
- [x] 1.9 Screens fade through black; a loading screen with a progress bar at boot (ships and pictures on the loader's threads,
  then every ship's hull shader, the effect shaders and the map's materials drawn once off screen to compile them), before a
  campaign's map and before a battle (owner: "make transitions between menus smoother, implement loading progress bar if its
  kinda slow, implement loading if you have shaders you need to prewarm"). Boot on the Mac: 2.2 s cold, 0.8 s warm
- [x] 1.10 The port redesigned in the game and mockup 1 v16 (owner: "improve the "shop" expeirience as well, AND improve the
  loadout, crew recruitment and supplies shopping"): Shipyard, Loadout, Supplies and Crew tabs with the fleet down the left;
  every price the rules' own quote (`RefitQuote`, `StockQuote`, `SupplyQuote`, `RestockQuote`, `RepairCost`, `SaleQuote`);
  one officer per station (`Hire` replaces, `Dismiss`); tests in `ShopTests`
- [ ] 1.6 A playtest of the flow (menu, new campaign, move, a mission fought and debriefed, the port) at desktop and phone size, recorded

## 2. Still different from mockup 1 v14 (each a line in `docs/design/mockup-sync.md`)

- [ ] 2.1 The faction screen's 3D pedestals, light and dust (the game shows the rendered emblems flat)
- [x] 2.2 The map's look and HUD at the mockup's (2026-11-05, owner: "map in godot needs to reach freature parity"): the sky and
  parallax stars, glass hexes and border ribbons, two-colour nebula puffs, asteroid belts, planets, stations, flight stands,
  cloth banners and strength flags, freighters and routes, fleets flying their moves, the route with turn badges, a fixed
  camera; the top bar, mission log, zoom keys and sector panel. Stills `docs/screenshots/game/flow/map*.png`, `route.png`,
  `accept.png`, `log.png`
- [ ] 2.2b Still different on the map: a planar mirror under the glass (the game's glass reflects the sky panorama only), and
  stars that streak as the view pans
- [ ] 2.3 Skirmish fleet building and the loadout screen with a points budget; refits that change the battle (open question E1)
- [ ] 2.4 Officers hired doing something in the battle (change `officers`)
- [ ] 2.5 The phone layouts of the flow screens checked at phone sizes (landscape and portrait)
- [ ] 2.7 The mockup's screens fade and show the same loading screen (the game has them first)
- [ ] 2.6 Options beyond the display mode (sound levels, quality, UI scale)
