# Tasks: cloaking

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 The cloak (`CLOAK`, `cloakStep`, `setCloak`), its draw, no shields, no fire
- [x] 1.3 `detected` and `seenFoes` in every list, marker, radar, map, model and lock
- [x] 1.4 The sweep (`sweep`, `sweepKm`), the Science keys, the rings
- [x] 1.5 The AI's cloak and sweep; the Cloak scene and shots; both skins
- [ ] 1.6 The owner's review; C1-C4

## 2. In the game (owner, 2026-11-06: "please add ALL systems that are IN the mockup")

- [x] 2.1 The rules in `src/Core`: `Battle.Cloak.cs` (fade, draw, no shields, no fire, `Detected`, `CanLock`, `SeenFoes`, the sweep, the AI's cloak and sweep), the numbers in `data/battle/systems/cloak.json`, `CloakTests`
- [x] 2.2 The Science station's Cloak and Sweep keys; contacts, radar, markers, picking and the 3D model read `Detected`
- [x] 2.3 The cloak's fade in 3D (`BattleView.Cloak.cs`) and the sweep rings over the view (`ViewOverlay.Cloak.cs`)
- [x] 2.4 The Cloak scene from both sides (`data/battle/scenes/cloak.json`) and its playtest (`data/playtests/cloak.json`)
- [ ] 2.5 The science officer's sweep reach, once the officers are in the game (hook `CloakOfficerSkill`)
