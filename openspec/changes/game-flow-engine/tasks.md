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
- [ ] 1.6 A playtest of the flow (menu, new campaign, move, a mission fought and debriefed, the port) at desktop and phone size, recorded.
  Desktop done (2026-11-05, `data/playtests/campaign.json`, `docs/playtests/2026-11-05-campaign.md`): menus, a new Concord
  campaign, moves over two turns, three contracts start to end. Still to do: the port in the script, and phone size
- [x] 1.11 Found by that playtest and fixed: the shut port panel took every press meant for the map's HUD (Move here, End
  turn, Briefing) and passed it to the map; the map's drag moved it about seven times as far as the pointer (it now keeps
  the point grabbed under the pointer); a headless boot waited forever for a frame to warm the shaders
- [x] 1.13 The owner's port and map notes (2026-11-06), in the game and mockup 1 v18 together:
  - fitting is free (owner: "fitting stuff on a ship should be free, have the store below"): systems are bought into the
    fleet's stores at a port and fitted from them at no cost, and a system taken off goes back to them; the store below the
    fitting list buys and sells (`Campaign.BuySystem`, `SellSystem`, `Refit`, `FitStock`, `CampaignState.Stores`); tests
    `Fitting_is_free_and_takes_from_the_stores`, `Removing_a_system_stows_it_and_selling_it_pays_the_yard_share`,
    `Empty_hardpoints_take_stock_from_the_stores_and_buy_the_rest`
  - every crew station opens its list, kept full after a hire (owner: "for each crew slot, I should be able to click on it
    and always geta list, even when I hire someone. Always 3 experienced of various levels and 5 cadets"): per station three
    experienced officers at different levels and five cadets (`data/campaign/campaign.json` officers: `experienced`, `cadets`,
    `given` and `family` names; the lowest level is now Cadet); the hired one's place goes to someone new of the same kind and
    whoever served before goes back on the list (`RollOfficers`, `Hire`, `Dismiss`, `OnOffer`); tests
    `Every_station_offers_three_experienced_officers_at_different_levels_and_five_cadets`,
    `A_station_keeps_its_list_after_a_hire_and_one_officer_serves_it`
  - the course stays drawn (owner: "future turns should be dotted lines, also when I click move show a line to my
    destination, dont just remove it when I set course"): the course set with Move here is drawn from where it was set until
    End turn, then from the fleet's place each turn after; this turn's leg solid, later turns dotted; the picked sector's
    route over it in the sky colour when it goes elsewhere (`CampaignMap.ShowRoute`, `GameFlow.Course`)
  - Options (owner: "need an options menu with audio controls too", "also this display button should be in options"):
    master, music and effects volumes in ten boxes each and the window mode, from the main menu and the battle's menu, which
    lose their Display keys; remembered in `user://settings.cfg` (`AudioSettings`, `OptionsCard`); the score plays on the
    Music bus (`MusicPlayer`: the main theme on the menus and map, the battle cue in a battle)
  - the ship schematics drawn smooth at thumbnail size (owner: "use AA or something, sampling kinda sucks on these ships"):
    imported with mipmaps and sampled trilinear (`Schematics.Smooth`)
  - stills `docs/screenshots/game/flow/outfit.png`, `crew.png`, `course.png`, `options.png`, `factions.png`
- [ ] 1.12 Also found: a mission that goes with a moving fleet (Assassination) leaves the fleet's course where it was set;
  the player must set course again each turn from the briefing. Whether an accepted mission's course should follow it is
  the owner's call; mockup 1 does the same (its `CAMP.order` is the sector the course was set for), so both would change together

- [x] 1.13 (2026-11-06) Every mission flown: marker beacons, drones and battles that end on their objectives; the
  Shakedown and Deliver the prize rewritten; the mission strip without its fill, objectives ticked live, Withdraw in the
  battle's menu; ships slide on the map (no hop), in the game and mockup 1 v18

## 2. Still different from mockup 1 v14 (each a line in `docs/design/mockup-sync.md`)

- [x] 2.1 The faction screen's 3D scene, light and dust (2026-11-06: the walnut table, change `faction-select` 3.1)
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
- [ ] 2.6 Options beyond the display mode and the volumes (2026-11-06): graphics quality and interface scale, which mockup 1 shows
- [ ] 2.8 Mockup 1: the mission strip without its fill and with live ticks, Withdraw in the battle menu
- [ ] 2.9 Mockup 1: the Skirmish screen by scenario type, then the faction to play it as, with a scroll bar
- [ ] 2.10 Mockup 1: sync to the game's 2026-11-06 changes: the goals' close key and their list in Comms (a battle opens on Comms), the campaign map's Menu opening the pause menu, the debrief on black
