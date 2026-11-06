# Tasks: missiles

## 1. The write-up and the mockup

- [x] 1.1 Proposal, design and spec delta, citing SFC2 printed pages 24 and 190-192 and SFC3 28-29
- [x] 1.2 The weapons range (mockup 16): salvos of four both ways, the drive flame and trail in the firing faction's
      colour, point defence firing on them by `pd_intercept_chance`
- [x] 1.3 The battle console mockup draws missiles with the new look (v77)
- [x] 1.6 Countermeasures on the range: flares, a decoy shuttle, point defence cannon tracers, fooled by grade
- [ ] 1.4 The owner's review and survey D1-D6
- [ ] 1.5 The battle console mockup: racks and rounds on the weapons panel, Fire one and Fire all, fuel, six in flight

## 2. The game (after approval for implementation, a separate request)

- [ ] 2.1 `weapons.json`: rounds, reload, fuel, point defence hits, the heavy warhead; the loader validates them (done
      for the four grades in `data/battle/missiles.json`: speed, damage, point defence hits, fuel and how often each
      countermeasure fools it; a grade on any weapon but a missile rack is an error. Rounds and reload are not built)
- [x] 2.2 Racks for every faction's classes in `classes.json`; the golden seed re-pinned on purpose
- [ ] 2.3 The battle: magazines, fuel, guidance limit, lock lost with the launcher's sensors (headless tests). Done:
      missiles follow their target, burn out at their fuel, and flares and the decoy shuttle lure them by grade
      (`Battle.Missiles.cs`, `MissileTests`). Not built: magazines, the guidance limit, lock lost with sensors
- [ ] 2.4 The weapons panel and a playtest scenario with missiles from both sides
- [x] 2.5 Missiles drawn in the game's 3D view as on the range: the body, the ball plume and the tail in the grade's
      colour; flares and the decoy shuttle drawn as lures (`src/Game/View/ShotView.cs`)
- [ ] 2.6 Sync (mockup 16 approved for implementation, 2026-11-05): the player's flare and decoy orders and the rack's
      grade on the weapons panel wait for task 1.5 in the battle console mockup; until then every ship's
      countermeasures are automatic
