# Tasks: kelvane-ships-engine

## 1. This change (2026-11-07)

- [x] 1.1 The Hunt's damageable baked ships lifted into `docs/mockups/lib/baked-fleet.js`; the Hunt page on it, checked identical
- [x] 1.2 The Kelvane page on it: the five classes and the fighter damageable, the carrier's fighters parked through its hook; the blockout OBJs unchanged
- [x] 1.3 The exporter's baked branch for any baked faction; the Institute's six glbs, their fields, the carrier's pad empties
- [x] 1.4 `ParkedCraft`: the fighter on the carrier's pads, one a fighter in the bay
- [x] 1.5 Five classes, the carrier's fighters, The Institute and Institute wrecks from each side, the Institute as a side (Battle, Duel, Arming, Four races), its five on both damage ranges; the scenario test widened
- [x] 1.6 Stills of the Institute's ships in the running game, side by side with the mockup's shots, and the suite (docs/playtests/2026-11-07-kelvane-institute.md)
- [x] 1.7 Every general scene from the Institute's side (owner, 2026-11-07: "test all kelvan general mission scenarios"): 18 scenes with skies of their own, its classes' cloak, marines and craft, the bot suite and the scene tests widened, its console's every station played and stilled (docs/playtests/2026-11-07-kelvane-e2e.md)
- [x] 1.7 The top-down schematics (owner, 2026-11-08: "Top down shop sillohuet needs improvement. Let's add some details so the top down view of these kelvane ships don't look so bad"): `tools/mockups/ship_schematics.py` draws a Kelvane ship's wire from its bake (plate bevels and gaps from its normal map and occlusion, looked up through its UVs where each pixel's top surface is), its mesh only by its outline and real creases; the mounts moved onto the slim hulls, read from the schematics; game-flow and space-chart re-inlined

## 2. Still different from mockup 23 (docs/design/mockup-sync.md)

- [ ] 2.1 The Institute's own weapons and figures, once its culture is set
- [ ] 2.2 Its ships' names and captains, once its culture is set (survey C4)
- [ ] 2.3 Every pad filled: the page parks a fighter on all 24 pads, the game one a fighter in the bay (12)
- [x] 2.4 The Institute in the campaign (owner, 2026-11-07: "Implement kelvane in the game please", "We should have emblem, UI, and spaceships"): its badge on the faction screen (mockup 1 v20, `tools/mockups/export_faction.mjs`, the four badges at a 2.5 m pitch), its territory, home world, depot, fleets and freight route on the map, its five classes as the campaign's tiers, its skin on every campaign screen; its names provisional (survey I10)
- [ ] 2.5 Whether the Institute cloaks (survey I9): today its cruiser and destroyer do, provisionally
