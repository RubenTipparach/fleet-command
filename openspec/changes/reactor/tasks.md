# Tasks: reactor

## 1. Mockup (now)

- [x] 1.1 Proposal and design with the model's numbers
- [x] 1.2 In `docs/mockups/battle-console.html`: the Reactor mode, coils, loops, meltdown, draw past output, brownout, Power blue over Systems green, the battery
- [x] 1.3 Checked headless (`window.MOCKUP_SIM`): equilibrium, meltdown timing, the cap, the brownout
- [x] 1.4 Shots `reactor` and `reactor_expanded`, looked at
- [x] 1.5 Survey questions R1-R4

## 2. After approval for build

- [ ] 2.1 Fold in the owner's notes and the R answers
- [ ] 2.2 Every scenario mapped to a headless test

## 3. After approval for implementation (a separate request)

- [x] 3.1 `REACTOR` in `data/battle/systems/reactor.json`, validated on load (`SystemsDef.Reactor.cs`); every class's rating leaves room for its pumps
- [x] 3.2 The reactor in `src/Core/Sim` (`Battle.Reactor.cs`, `Rules.Reactor.cs`), with tests for heat, drift and meltdown (`ReactorTests`), the golden seed re-pinned
