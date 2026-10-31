# Mockups

Interactive three.js pages, one per subject, made before game code (CLAUDE.md section 4, the
`threejs-mockups` skill). The rolling list is [`mockup-work.md`](../../mockup-work.md); the tracker
(`tracker.html`, the `mockup-tracker` skill) holds every link and approval. Each published page is
recorded in `docs/artifacts/<name>/`.

| Page | Mockup |
|---|---|
| `game-flow.html` | 1: primary game flow |
| `tracker.html` | the mockup tracker |
| `lib/template.html` | the starting skeleton for a new page |

Shots: `NODE_PATH=$(npm root -g) node tools/mockups/shoot.mjs docs/mockups/<page>.html docs/screenshots/mockups`,
then `python3 tools/mockups/shrink_png.py docs/screenshots/mockups`.
