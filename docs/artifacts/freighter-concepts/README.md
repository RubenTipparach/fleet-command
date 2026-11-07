# Fleet Command Freighters (mockup 24)

- Link: https://claude.ai/artifact/Gkv9jzwEZd258noT7WGtv4
- Source: `docs/mockups/freighter-concepts.html` (one copy, not duplicated here). The Concord line yardstick
  (`docs/mockups/lib/concord-line.js`) and the shared grown hull (`docs/mockups/lib/hull-grow.js`) are inlined by
  `tools/mockups/inline_data.py`; re-run it before publishing.
- OpenSpec change: `openspec/changes/freighters/`.
- Shots: `docs/screenshots/mockups/freighter-concepts/` (`tools/mockups/shoot.mjs`): the board, the board from above, the
  board in clay, each faction's three and each concept from its bow quarter, at desktop, phone landscape and portrait.

## Versions

- 2026-11-07, cloud session (owner: "we need to design freighter for every faction in the game currently"): version 1,
  version id `1794040560-c1e9`. A board of eighteen freighter blockouts, three for each faction in the game (Concord,
  Kethran Hegemony, Vos Syndicate, Krai Warhost, Ssarith Hunt, Kelvane Institute), 140-210 m, each a spine, a hold or a
  cradle, each from a tradition inside its faction (drafted where the owner has written none). Each hull is one closed
  grown structure on the shared hull-grow code, in its faction's colours or clay, beside the Concord Petrel and a person;
  only the cargo is placed. Survey section V added (V1-V6), in the survey's source only: this account does not open the
  tracker or the survey, so the desktop session puts M24 on the tracker and republishes the survey.
- 2026-11-07, cloud session: version 2, version id `1794040612-0a4e`: the notes name survey section V (R was the reactor's,
  and ids are never reused). Its renaming also touched names inside the inlined libraries.
- 2026-11-07, cloud session: version 3, version id `1794040627-3914`: the libraries re-inlined exactly
  (`inline_data.py --check` passes), the page loading with no errors.
