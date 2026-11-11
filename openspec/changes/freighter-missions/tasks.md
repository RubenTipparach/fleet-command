# Tasks: freighter-missions

## 1. Data

- [x] 1.1 Freighter hulls for the Concord, Krai and Hunt (neutral, never for sale), each faction's `freighter`, the Vos on the stand-in
- [x] 1.2 `escort.json` and `raid.json`, kind `encounter`; `hull: "freighter"` on Convoy defense, Convoy ambush and Distress
- [x] 1.3 `rules.freighter_refit_turns`; `docs/design/mission-files.md` and the mission editor's kind field

## 2. Rules and game

- [x] 2.1 `Campaign.Meet`, `FreightersAt`, `EncounterOf`; encounters never random, gone at End turn
- [x] 2.2 Freighter groups field `FreighterOf(faction)` and hold their course; boarded ships count as taken
- [x] 2.3 Haul outcomes, hauls out of battles and unsellable, `Campaign.CashOut`, the met freighter off her route
- [x] 2.4 The map draws each freighter as her approved ship with her containers, anyone's where your ships see
- [x] 2.5 The sector panel's Escort and Raid keys and Cash out
- [x] 2.6 Tests (`CampaignTests`) and flow stills (`--flow=freighter`, `raid`, `haul`; `docs/screenshots/game/freighters/`)
- [x] 2.7 A playtest of an escort, a raid and a cash out from every side (`data/playtests/freighters-<side>.json`, beats `meet`
      and `cash_out`; `docs/playtests/2026-11-08-freighter-missions.md`), and what it found: a prize strikes on a critical core,
      locks on her drop, her core is shut down (survey V6); a cash out that empties the fleet recommissions a ship

## 3. Mockup sync (mockup 1, game-flow)

- [ ] 3.1 The page's freighters drawn as the approved freighters (it draws its `freighterModel` placeholder)
- [ ] 3.2 The page's sector panel offers Escort and Raid on a shared sector, and Cash out
