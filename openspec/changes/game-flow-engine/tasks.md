# Tasks: game-flow-engine

## 1. This change (2026-11-05)

- [x] 1.1 `data/campaign/campaign.json` and `CampaignData.cs`, validated against the battle's data
- [x] 1.2 `Campaign.cs`: the map from the mockup's generator, paths and routes by turn, orders, the factions' turn, missions on offer, the shop, saves; tests
- [x] 1.3 `GameFlow` and the screens: menu, factions, skirmish, map and its HUD, briefing, port, mission strip, debrief
- [x] 1.4 The game opens on the menu; the battle's checks still open in battle
- [x] 1.5 `--flow=<screen>` stills (`Main.FlowStill`), looked at for both skins
- [ ] 1.6 A playtest of the flow (menu, new campaign, move, a mission fought and debriefed, the port) at desktop and phone size, recorded

## 2. Still different from mockup 1 v14 (each a line in `docs/design/mockup-sync.md`)

- [ ] 2.1 The faction screen's 3D pedestals, light and dust (the game shows the rendered emblems flat)
- [ ] 2.2 The map's look: the mockup's parallax star shader, the board's reflection of the sky, nebula puffs with an accent colour, cloth banners
- [ ] 2.3 Skirmish fleet building and the loadout screen with a points budget; refits that change the battle (open question E1)
- [ ] 2.4 Officers hired doing something in the battle (change `officers`)
- [ ] 2.5 The phone layouts of the flow screens checked at phone sizes (landscape and portrait)
- [ ] 2.6 Options beyond the display mode (sound levels, quality, UI scale)
