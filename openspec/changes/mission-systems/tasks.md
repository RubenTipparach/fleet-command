# Tasks: mission-systems

## 1. Battle

- [x] 1.1 `data/battle/systems/field.json` and `SystemsDef.Field`
- [x] 1.2 Sensor reach, `Identified`, scans remembered
- [x] 1.3 Planet kinds and sites, `ScanPlanet`, `BeamDown`, away teams, envoy and waiting
- [x] 1.4 Arrivals, sleepers, runners, truces, fixed hulls, time limit, dead engines
- [x] 1.5 Placeholder classes and the fauna faction; deck plans and crews

## 2. Missions

- [x] 2.1 The format: planets, sites, hulls, arrive/wake, at_planet, hold, cargo, envoy, time limit, dead engines, tags, checks
- [x] 2.2 23 new mission files; station and planet assaults on real stations and planets
- [x] 2.3 Tests (`MissionSystemsTests`, every mission offered, fought and graded for three sides)
- [x] 2.4 `docs/design/mission-files.md`, the mission editor's kind field

## 3. The game

- [ ] 3.1 Stand-ins for the new shapes
- [ ] 3.2 Planets from Falchion and godot-sandbox, sites on them, the close view
- [ ] 3.3 Unknown contacts, Scan planet, Beam down, time left, engines dead in the console
- [ ] 3.4 Headless stills in `docs/screenshots/game/mission-systems/`
- [ ] 3.5 A playtest of every general mission type on the owner's machines (handed to the Mac session)

## 4. Fixes from the first playtest

- [x] 4.1 A withdrawal holds over its risk limit, can be called off, and is called off when the drive cannot charge
- [x] 4.2 A battle that cannot be withdrawn from ends as a stalemate when no ship is hurt for `stalemate_s`
- [x] 4.3 AI captains close in when their fire has not landed; the scripted captain powers its engines for a chase
- [x] 4.4 Regression tests (`StandoffTests`), the golden seed re-pinned
- [x] 4.5 The Mac session re-runs its sweep on these fixes (withdraw and stalemate fixed; a lone corsair still stalled)
- [x] 4.6 Intercept leads at most `intercept_max_lead_deg` off the target; the scripted captain closes the menu after a refused
  Withdraw and boosts engines from the shields too
- [ ] 4.7 The Mac session re-runs the lone-corsair missions (capture, first_blood, scout, relief) from the Krai side

## 5. Records

- [x] 4.1 The Missions guide updated and republished
- [ ] 4.2 Mockups for the placeholders and the planets (each its own change, once the owner asks)
