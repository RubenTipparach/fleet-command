# Proposal: kelvane-ship-concepts

## Why

The owner, 2026-11-06: "begin designs on the institute ships". The Kelvane Institute has a battle console (mockup 20) and
an emblem (mockup 21), but no ships: in the battle console it flies a stand-in hull in its colours (kelvane-battle-console,
D7).

Two lessons from the Ssarith Hunt shape how this starts:
- **Breadth before depth.** The Hunt's first eight versions refined one hull until the owner deleted them ("We need a
  variety of designs. Not just 3 lame ones that look like coffins"). Version 9, a board of twelve massings to pick from,
  is what worked. The Institute starts there.
- **Culture, not anatomy, and the culture is the owner's** (Ship Design Bible C1-C5). The Institute's culture is one line
  ("truth is measured, never assumed"). Where a culture is too thin to design from, the rule is to offer traditions to
  choose from, never to decide them (C4). So the board's massings come from five traditions drafted inside the Institute,
  each marked provisional, and the survey asks which go on.

## What Changes

- **Mockup 23 version 1**, `docs/mockups/kelvane-ship-concepts.html`: a blockout board of twelve frigate-sized hulls, about
  104-122 m, in five drafted traditions (three are those drafted for the emblem, two new). Each is one grown structure. Each
  is shown in the Institute's champagne, dark glass and amber, or in plain clay, at true scale, with the Concord Petrel to
  hand. Each can be seen on its own from the bow quarter, the side and above.
- **The grown hull is shared**: `docs/mockups/lib/hull-grow.js` is lifted from the Ssarith Hunt page's own copy, which now
  inlines it. The Hunt's six ships export to the same OBJ, byte for byte. It gains what the Institute's hulls need, each off
  by default:
  - smooth normals across the whole skin;
  - glass as a face of the skin;
  - branches that meet again round an opening;
  - caps that meet their bands edge to edge.
- **Survey section I**: which traditions the Institute keeps, which blockouts go on, and the first class.

## Impact

- New: `docs/mockups/kelvane-ship-concepts.html`, `docs/mockups/lib/hull-grow.js`,
  `docs/screenshots/mockups/kelvane-ship-concepts/`, `docs/artifacts/kelvane-ship-concepts/`, this change.
- Changed:
  - `docs/mockups/ssarith-ship-concepts.html`: the shared hull grower inlined; geometry unchanged;
  - `docs/design/survey.html`: section I;
  - `docs/mockups/tracker.html`: the new mockup in the Kelvane faction's modal;
  - `mockup-work.md` and `docs/design/README.md`.
- No game code.
