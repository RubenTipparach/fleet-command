# Tasks: boarding-defence

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 The interior graph and routes from the Petrel's plan
- [x] 1.3 Teams, boarders, fights, ambushes, sabotage and the bridge (`secStep`, `boardShip`, `volley`)
- [x] 1.4 The Security page: teams, Walk or Beam, the 3D interior with moving figures, docked and expanded
- [x] 1.5 The security chief's abilities; the Repel boarders scene and shots; both skins
- [ ] 1.6 The owner's review; B1-B5

## 2. In the game (owner, 2026-11-06: "please add ALL systems that are IN the mockup")

- [x] 2.1 Deck plans as walkable graphs (`src/Core/Data/DeckPlan.cs`; per class in `data/battle/systems/cloak.json`), routes, rooms
- [x] 2.2 Teams, boarders, fights, ambushes, sabotage and the bridge (`Battle.Security.cs`), `SecurityTests`
- [x] 2.3 The Security page on its own entry of the ship panel's rail (the Transport station is the comms package's), the deck plan in 3D by projection, docked and expanded
- [x] 2.4 The Repel boarders scene from both sides and its playtest (`data/playtests/repel.json`)
- [ ] 2.5 Pads for beams and boarding from the transporter (hook `SecurityUsePad`), the security chief from the officers (hook `CloakOfficerSkill`)
