# Tasks: officers

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 The roster, skills and `officerSkill`; every ability read by its rule
- [x] 1.3 The slot, its hover card and click; both skins, the Krai's names and ranks
- [ ] 1.4 The owner's review; O1-O4
- [ ] 1.5 Cite the SFC3 manual's officer pages (28-29) in the design once read (CLAUDE.md section 1)

## 2. Game

- [x] 2.1 The roster, faction names and ranks in `data/battle/systems/reactor.json`; `Rules.OfficerSkill` and `Battle.OfficerSkill`, `OfficerNext` (`Battle.Officers.cs`), tests (`OfficerTests`)
- [x] 2.2 The abilities of the systems built: the helm's HET chance (`Rules.HetChanceNow`), the power officer's coil drift (`ReactorStep`), the chief engineer's repair crews (`Battle.Repair`)
- [ ] 2.3 The other abilities, each in its own rule as it comes into the game: tactical and fleet in the chance to hit, comms against ECM, point defence's intercepts, science's scan and sweep, the security chief's (their numbers are in `Data.Systems.Officers.Ability`)
- [x] 2.4 The slot on every station's frame with its hover card and click (`StationPanel.Reactor.cs`), both skins
- [ ] 2.5 Officers hired in the campaign (`Campaign.Officer`) sit in the battle's slots
