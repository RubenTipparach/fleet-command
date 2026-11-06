# Tasks: damage-control

## 1. Mockup (now)

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 The queue rule (`REPAIR`, `REPAIR_JOBS`, `repairPlan`, `mendJob`, `jobEta`), nacelles and reactor included
- [x] 1.3 The Repair station: the queue, crews at work, time to whole, the energy row; docked and expanded
- [x] 1.3a Drag and drop in place of the Up and Top keys (`repairMove`, the pointer layer's drop); shot `repair_drag`
- [x] 1.4 The Escape scene and its named shots; both skins
- [ ] 1.5 The owner's review; D1-D4

## 2. Game (after approval for implementation)

- [x] 2.1 The queue in `src/Core/Sim` (`Battle.Repair.cs`; the crews replace repair energy mending the worst system), numbers in `data/battle/systems/reactor.json`, with tests that read as sentences (`RepairTests`)
- [x] 2.2 The Repair station in the game (`StationPanel.Repair.cs`): the queue by drag and drop, crews at work, time to whole, the energy row; docked and expanded, both skins (skin parity)
- [x] 2.3 The Escape scene from both sides (`data/battle/scenes/reactor.json`, `concord_escape`, `krai_escape`) and its scripted captain (`data/playtests/escape.json`)
