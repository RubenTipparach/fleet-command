# Fleet Command Freighters (mockup 24)

- Link: https://claude.ai/artifact/Gkv9jzwEZd258noT7WGtv4
- Source: `docs/mockups/freighter-concepts.html` (one copy, not duplicated here). The Concord line yardstick
  (`docs/mockups/lib/concord-line.js`) and the shared grown hull (`docs/mockups/lib/hull-grow.js`) are inlined by
  `tools/mockups/inline_data.py`; re-run it before publishing.
- OpenSpec change: `openspec/changes/freighters/`.
- Shots: `docs/screenshots/mockups/freighter-concepts/` (`tools/mockups/shoot.mjs`): the board, the board from above, the
  board in clay, each faction's two and each concept from its bow quarter, at desktop, phone landscape and portrait.

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
- 2026-11-07, cloud session: version 4, version id `1794051997-b796` (owner: "Freighter should only focus on approved
  factions", "Narrow down freighter options to ones where cargo is visible. And make sure all cargo is the same size",
  "The containers I mean", "The cargo hull shape it self can be the way it was in the draft"): eight concepts, two for each
  approved faction (the Concord, the Krai Warhost, the Ssarith Hunt, the Kelvane Institute), every one carrying the same
  6 by 6 by 12 m container in sight; the hulls as drafted. Survey section V is now V1-V5.
- 2026-11-07, cloud session: version 5, version id `1794052249-66bc` (owner, on a phone: "Nothing renders", the labels and
  meter drawn but the 3D view black): no flatShading on any material (the hulls are unindexed, so each face carries its
  own normal; flatShading's screen-space derivatives can come out zero on a phone's GPU), the canvas resized whenever its
  stage changes size, and a notice if the 3D view loses its graphics context. A phone emulation (Pixel 7) renders it;
  the owner's phone is to confirm.
- 2026-11-07, cloud session: version 6, version id `1794053733-a050` (owner: "1,4,5,8, but with 8 having really skinny arms
  and link.tbe arms together with a thin bridge at the end to keep the loop design ethos", "All freighters should be 2x
  bigger. Along with the containers too", "The institute freighter should be rounder at the tip", "Show m artifact with
  just the four"): the page opens on the four picks, every freighter and container twice the drafted size, the Twin
  ledger a loop curving in to a thin bridge with a domed nose; the board of eight beside them. Survey V2 and V5 decided.
- 2026-11-07, cloud session: version 7, version id `1794053937-a0c7` (owner: "Fix the institute", with an outline of the
  shape wanted, "I made an outline of what I want"): the Twin ledger's stern case at full width, its two outer strips
  running straight on as the really skinny arms, curving in at the bow to a thin bridge with a domed nose.
- 2026-11-07, cloud session: version 8, version id `1794054085-fef1` (owner: "Fix the institute again.  No nipple at the
  end", "Flatter bow"): the Twin ledger's arms turn in on a squared quarter to a flat bow, closed flush with the hull.
- 2026-11-07, cloud session: version 9, version id `1794054377-452d` (owner: "The institute ship will be shorter than is
  cargo. That is the cargo is taller in height"): the Twin ledger's hull 16 m tall as shown (8 m drafted), its arms one
  3.2 m roll wide, under container stacks two high (25 m).
- 2026-11-07, cloud session: version 10, version id `1794054480-2c51` (owner: "Adjust institute ship len to fit cargo
  better .After that change. Now I approve. Detail them"): the Twin ledger's straight run is its eight rows of containers
  with 1.5 m clear at each end (274 m shown). Approved for build.
