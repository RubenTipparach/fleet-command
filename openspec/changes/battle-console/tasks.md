# Tasks: battle-console

## 1. Mockup (now)

- [x] 1.1 Proposal, design and spec delta, citing the SFC2 manual's printed pages
- [x] 1.2 Shared code: `docs/mockups/lib/kitdraw.js` and `docs/mockups/lib/concord.js` taken out of `console-skins.html`, `docs/mockups/lib/space.js` out of `game-flow.html`; both pages inline them again and still load without errors
- [x] 1.3 `docs/mockups/battle-console.html`: the console in the Concord skin around a stand-in battle; every panel interactive; every panel expands; desktop first (no mobile mockup yet, owner 2026-11-01)
- [x] 1.4 Named shots (battle, power, fleet, power expanded, ship expanded, weapons expanded, map expanded, menu) at 1920 x 1080, looked at
- [x] 1.5 Survey questions B1-B6 in `docs/design/survey.html`
- [x] 1.5a Owner's first notes (2026-11-01): icon tabs in narrow columns named on hover; the ship below the centre; a speed handle; Power over Systems; desktop only
- [x] 1.6 Listed in `mockup-work.md`, the mockup tracker and `docs/artifacts/battle-console/`
- [ ] 1.7 Published by the desktop session (CLAUDE.md section 8), the link recorded in the tracker, `mockup-work.md` and `docs/artifacts/battle-console/README.md`

## 2. Iteration with the owner (before approval for build)

- [ ] 2.1 Fold the owner's notes, panel by panel, into the design and the page
- [ ] 2.2 Move `console-skins.html`'s kit framework (`VARS`, `setSkinVars`, the `.k-*` CSS) into `docs/mockups/lib/` so this page and the skins page share one copy, and the skin switch works here
- [ ] 2.3 Replace `FCConcord.live.ssd` (a fixed drawing) with this page's data-driven systems display, so both pages draw one

## 3. After approval for build

- [ ] 3.1 Every other skin on this page, each checked by hand at all three sizes for function and legibility (the owner's rule)
- [ ] 3.2 The kit's piece list grows by the cells and icons, with every skin supplying them
- [ ] 3.3 Every scenario in the spec delta mapped to a headless test, the layout test or a playtest beat

## 4. After approval for implementation (a separate request)

- [ ] 4.1 Data: weapons, classes, systems and scenarios in `data/`, validated on load
- [ ] 4.2 Simulation in `src/sim/` (movement, energy, shields, weapons, damage, AI behaviours), headless tests and golden seeds
- [ ] 4.3 The console in the engine chosen by survey T4a, the layout test, then a playtest at desktop and phone sizes
