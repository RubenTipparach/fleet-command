# Tasks: engagement-zone

## 1. The rules (src/Core)

- [ ] 1.1 `data/battle/systems/zone.json` (`side_km`, `return_s`, `warn_km`), a scenario's own `zone`, validated
- [ ] 1.2 `Rules.InZone`, the countdown on the commanded ship, called off by returning or by taking command of a ship inside
- [ ] 1.3 Abandoning: `Battle.WarpFunctional` (shared with the Withdraw's check), the fleet's warp-out and `Over = "abandoned"`
- [ ] 1.4 Self-destruct without a working drive; command passes on, or the battle is lost
- [ ] 1.5 AI captains keep inside the zone; an enemy that leaves has fled
- [ ] 1.6 Tests: the countdown, the call-off, the warp-out, the self-destruct, a replay from the seed

## 2. Mockup first (mockup 38)

- [ ] 2.1 The warning banner (retreat and self-destruct), the zone on the radar and the map, the border in the 3D view,
  in `docs/mockups/battle-console.html` (the Concord master), then every faction's page
- [ ] 2.2 The owner's approval for build, then for implementation

## 3. The game (after approval)

- [ ] 3.1 The banner, the radar and map square, the 3D border, in every skin
- [ ] 3.2 The scripted captain: a beat that flies out and back, and one that abandons; the suite
- [ ] 3.3 Campaign: an abandoned mission's consequences (survey EZ2)

## 4. The owner's review

- [ ] 4.1 Survey EZ1-EZ5
