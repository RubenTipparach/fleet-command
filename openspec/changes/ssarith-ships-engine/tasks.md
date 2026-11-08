# Tasks: ssarith-ships-engine

## 1. This change (2026-11-06)

- [x] 1.1 The exporter's Hunt branch: glbs, manifest entries naming the baked maps, the fields as `.bin`
- [x] 1.2 `baked_hull.gdshader`; `ShipSkin` and `ShipDamage` take it; `cutface.gdshader` by a signed distance field
- [x] 1.3 Six Hunt classes, The Hunt from each side, the Hunt on both damage ranges; the scenario test widened to every faction with approved ships
- [x] 1.4 Stills of The Hunt, Hunt wrecks and the breakup in the running game (docs/playtests/2026-11-06-ssarith-hunt.md), and the suite 14 of 14
- [x] 1.5 Every general scene from the Hunt's side (owner, 2026-11-07: "Ensure spaceships for ssarith are also implemented"): 20 scenes with skies of their own, its classes' mines and the Disable objective, the bot suite and the scene tests widened (docs/playtests/2026-11-07-ssarith-e2e.md)

## 2. Still different from mockup 12 (docs/design/mockup-sync.md)

- [x] 2.1 Rooms behind the windows and hangars behind the launch bays (2026-11-06, owner: "get main, implement these ships in game"): `pane.gdshader` on quads built from the window_* and bay_* empties, `ship_rooms.gdshaderinc` (a port of lib/ship-rooms.js)
- [x] 2.2 The Hunt as a side the player commands: done by change `ssarith-console-engine` (its console skin, mockup 15, in the game; its campaign flies the Hunt's own classes)
- [ ] 2.3 The Hunt's own weapons, once its culture is set (survey H1)
- [x] 2.4 Strobe, beacon and position-light glows, as the page draws them over its lamps (`lamp_glow.gdshader`, from each lamp's `glow_m`); none at the floods (v26)
- [ ] 2.5 `*.bin` in the export presets once there are any
