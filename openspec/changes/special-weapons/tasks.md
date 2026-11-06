# Tasks: special-weapons

## 1. The write-up and the mockup (this change)

- [x] 1.1 Proposal, design and spec delta, citing the reference manuals' printed pages
- [x] 1.2 The four weapons' numbers in `data/battle/special_weapons.json`, units in every key
- [x] 1.3 Mockup 16, the special weapons range (`docs/mockups/special-weapons.html`): the approved ships, every weapon's
      run, the card's readouts from `RULE` and the data; on mockup-work.md and the tracker
- [ ] 1.4 The owner's review of mockup 16 (approved for build), and survey X1-X5, X8
- [ ] 1.5 The specials in the battle console mockup (both skins): the mount rows, the Krai Charge key, the four icons on
      the icon guide, the wingman behaviour, a scene for each

## 2. The game (after approval for implementation, a separate request)

- [x] 2.1 `DataLoader` reads `data/battle/special_weapons.json` and validates it; a class's `fits` are checked against
      its mounts (data test: an unknown key and a missing class are errors)
- [x] 2.2 `Rules`: the pierce hit, the chorus's joined damage, the ram's damage (headless tests: "A chorus joined by two
      ships burns 11.9 a second", "40 % of a forge spike goes through a full shield", "A ram at 660 m/s does 33")
- [x] 2.3 The battle: the chorus's burn and its joiners, the quell's flight, shield drop and stun, the forge's burn, the
      surge and the ram with recoil (golden seed re-pinned on purpose)
- [ ] 2.4 The weapons panel rows, the Charge key and the icons in both skins; the layout and skin parity tests
- [ ] 2.5 The AI and the wingman behaviour (the AI's blood charge is done, `ThinkCharge`: a Krai captain charges a
      target within 3 km and 25 degrees of its bow; the wingman behaviour "Join the chorus" waits for survey X2)
- [ ] 2.6 A playtest scenario for each weapon in `data/playtests/suite.json`, from both sides
- [x] 2.7 The four specials drawn in the game's 3D view as on the range (`src/Game/View/ShotView.cs`): the chorus as a
      Sky core with Lilac strands, a strand for each joiner and a joining beam from each joiner's lance; the quell as a
      deep blue six-armed star over a ball with bead trail; the forge slug as an orange streak; the surge as a bow flare
      and a drive ball
- [ ] 2.8 Sync (mockup 16 approved for implementation, 2026-11-05): the player's Charge order (an input action and the
      Krai helm's key) and the specials' rows on the weapons panel wait for task 1.5 in the battle console mockup; until
      then the specials fire from the weapons panel as ordinary mounts and only the AI charges
- [ ] 2.9 Balance: with the specials in, the scripted Concord wins both duels with full hull and the scripted Krai lose
      the full battle and the duel (playtest suite, 2026-11-05); a look at the numbers once survey X1-X5 is answered
