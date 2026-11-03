# Tasks: warp

## 1. Mockup (now)

- [x] 1.1 Proposal and design with the model's numbers
- [x] 1.2 In `docs/mockups/battle-console.html`: Power, Warp mode; nacelles, banks, plot, coolant reserve, stability, Q-G field, override, jump, mishap; gravity wells; the Warp scene
- [x] 1.3 Played through headless (`window.MOCKUP_SIM`): a planned jump succeeds; a forced one is refused, then fails and breaches with the override
- [x] 1.4 Shots `warp`, `warp_expanded`, `warp_jumped`, looked at
- [x] 1.5 Survey questions J1-J5

## 2. After approval for build

- [ ] 2.1 Fold in the owner's notes and the J answers
- [ ] 2.2 Every scenario mapped to a headless test

## 3. After approval for implementation (a separate request)

- [ ] 3.1 `WARP`, nacelles per class and gravity wells in `data/`, validated on load
- [ ] 3.2 The warp drive in `src/sim/`, with golden-seed tests for charging, plotting, risk and the jump roll
