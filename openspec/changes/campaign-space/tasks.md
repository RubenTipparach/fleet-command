# Tasks: campaign-space

## 1. Built (2026-11-07)

- [x] 1.1 `map.systems`, `map.deep` and `map.sight` in `data/campaign/campaign.json`, validated on load
- [x] 1.2 `Campaign.MakeSpace`: stars, belts and nebulae by system, dust clouds and asteroid fields in the deep; `IsRogue`
- [x] 1.3 The fog: `Sector.Known` and `KnownOwner`, `Campaign.Visible`, `Reveal` (start, load, every sector flown, the
  factions' turn, an offered mission's sector)
- [x] 1.4 The game's map: unexplored black and unlit, revealed in the empire last seen and darker, fleets only where seen;
  the board rebuilt when the fog changes; the sector panel's "Unexplored" and "(last seen)"
- [x] 1.5 Mission `weight`; Patrol 4; Nebula sweep, Belt patrol and Orbital patrol
- [x] 1.6 Tests: space is systems with belts and nebulae and a deep between; the fog's three states; patrols offered often
  and in their terrain

## 2. Still to do

- [ ] 2.1 Mockup 1: the same space from the same data (its `buildCampaign` still rolls terrain per sector)
- [x] 2.2 Mockup 1: the fog of war, as the game draws it (2026-11-07, on the paper chart, change `paper-map`; mockup 1 v22)
- [ ] 2.3 Rocks on the battlefield in an asteroid sector's battle (open question F2)
- [ ] 2.4 Look at the map from the owner's machine and tune the counts (dust clouds, fields, belts)
