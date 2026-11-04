# Tasks: special-weapons

## 1. The write-up and the mockup (this change)

- [x] 1.1 Proposal, design and spec delta, citing the reference manuals' printed pages
- [x] 1.2 The four weapons' numbers in `data/battle/special_weapons.json`, units in every key
- [x] 1.3 Mockup 12, the special weapons range (`docs/mockups/special-weapons.html`): the approved ships, every weapon's
      run, the card's readouts from `RULE` and the data; on mockup-work.md and the tracker
- [ ] 1.4 The owner's review of mockup 12 (approved for build), and survey X1-X5, X8
- [ ] 1.5 The specials in the battle console mockup (both skins): the mount rows, the Krai Charge key, the four icons on
      the icon guide, the wingman behaviour, a scene for each

## 2. The game (after approval for implementation, a separate request)

- [ ] 2.1 `DataLoader` reads `data/battle/special_weapons.json` and validates it; a class's `fits` are checked against
      its mounts (data test: an unknown key and a missing class are errors)
- [ ] 2.2 `Rules`: the pierce hit, the chorus's joined damage, the ram's damage (headless tests: "A chorus joined by two
      ships burns 11.9 a second", "40 % of a forge spike goes through a full shield", "A ram at 660 m/s does 33")
- [ ] 2.3 The battle: the chorus's burn and its joiners, the quell's flight, shield drop and stun, the forge's burn, the
      surge and the ram with recoil (golden seed re-pinned on purpose)
- [ ] 2.4 The weapons panel rows, the Charge key and the icons in both skins; the layout and skin parity tests
- [ ] 2.5 The AI and the wingman behaviour
- [ ] 2.6 A playtest scenario for each weapon in `data/playtests/suite.json`, from both sides
