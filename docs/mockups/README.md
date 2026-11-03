# Mockups

Interactive three.js pages, one per subject, made before game code (CLAUDE.md section 4, the
`threejs-mockups` skill). The rolling list is [`mockup-work.md`](../../mockup-work.md); the tracker
(`tracker.html`, the `mockup-tracker` skill) holds every link and approval. Each published page is
recorded in `docs/artifacts/<name>/`.

| Page | Mockup |
|---|---|
| `game-flow.html` | 1: primary game flow |
| `battle-console.html` | 2: the battle console (in-game UI), Concord skin |
| `console-skins.html` | 4: console skins |
| `battle-console-krai.html` | 7: the battle console in the Krai Warhost skin, written from `battle-console.html` by `tools/mockups/skin_page.py krai` (never edit it) |
| `lib/kitdraw.js`, `lib/concord.js` | the kit's drawing helpers and the Concord skin, shared by mockups 2 and 4 |
| `lib/krai.js` | the Krai Warhost skin (palette, icons, live readouts, kit loader), shared by mockups 4 and 7 |
| `lib/space.js` | the nebula sky, streaking stars and stand-in ship, shared by mockups 1 and 2 |
| `concord-ships.html` | 5: Concord ships and damage |
| `concord-hull-concepts.html` | 6: Concord hull concepts |
| `tracker.html` | the mockup tracker |
| `lib/template.html` | the starting skeleton for a new page |

Shots: `NODE_PATH=$(npm root -g) node tools/mockups/shoot.mjs docs/mockups/<page>.html docs/screenshots/mockups`,
then `python3 tools/mockups/shrink_png.py docs/screenshots/mockups`.

A faction's battle console: `python3 tools/mockups/skin_page.py <skin>` writes it, and
`NODE_PATH=$(npm root -g) node tools/mockups/skin_parity.mjs docs/mockups/battle-console-<skin>.html` presses every control
of every named shot in it and in the Concord, and fails on any difference.
