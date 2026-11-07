# Kelvane Institute Ships (mockup 23)

- Link: https://claude.ai/artifact/LfkkemDKPhan6Y7ZSBfgmx
- Source: `docs/mockups/kelvane-ship-concepts.html` (one copy, not duplicated here). The Concord line yardstick
  (`docs/mockups/lib/concord-line.js`) and the shared grown hull (`docs/mockups/lib/hull-grow.js`) are inlined by
  `tools/mockups/inline_data.py`; re-run it before publishing.
- OpenSpec change: `openspec/changes/kelvane-ship-concepts/`.
- Shots: `docs/screenshots/mockups/kelvane-ship-concepts/` (`tools/mockups/shoot.mjs`): the board views and blockout 1 at
  desktop, phone landscape and portrait, and every blockout from its bow quarter and its side at desktop.

## Versions

- 2026-11-06, cloud session (owner: "begin designs on the institute ships"): version 1, page written and shot. A blockout
  board of twelve frigate-sized hulls from five traditions drafted inside the Institute (the Calibrators, the Open
  Notebook, the Long Baseline, the Field Stations, the Collider Halls). Each is one closed grown structure on the shared
  `docs/mockups/lib/hull-grow.js`, in the console's champagne, dark glass and amber or in clay, beside the Concord Petrel.
  Survey section I added (I1-I4). Published from the cloud session (no desktop session was reachable; publishing is
  permanently approved), version id `1793953149-7d10`.
- 2026-11-06, cloud session: version 2, version id `1793953202-7263`: the survey link points at the survey copy that
  carries section I (`https://claude.ai/artifact/YHX4u5fPn5hQ1CMr3HhEbt`).
- To do from the desktop session: republish the tracker (`docs/mockups/tracker.html` already lists M23 with this link) and
  the survey (section I); the cloud session's account cannot open either.
- 2026-11-06, cloud session (owner: "l1 loop looks cool, can you make me some more variations of ships with loops?
  horizontal, vertical, both?"): version 3, the loop family. Seven more loop ships (13-19: Long loop, Twin loops, Wide ring;
  Tall loop, Keel loop; Crossed loops, Quartered loop), each closed, grown by the page's `loopOf()`; an All ships / Loop
  family switch; every loop ship shot from its quarter, side, above and ahead. Survey I2 lists them. Published, version id
  `1793957210-7877`; survey cloud copy version 9, version id `1793957212-1424`.
- 2026-11-06, cloud session (owner: "12,11,18.19,17 all really good shapes, start detailing these", "12 is no go use 5",
  "Mind the clean and sterile and almost scientific aesthetics. They would have decorated their ships with smooth round
  pieces rather than industrial greebles"): versions 4 and 5 together. The line (frigate from 5, destroyer from 11,
  cruiser from 17, battlecruiser from 18, carrier from 19), each class unwrapped on its own and baked by
  `tools/blender/painted_bake.py --faction kelvane` from `data/ships/kelvane/hull_detail.json` into
  `assets/ships/kelvane/`; Painted, PBR, Champagne and Clay looks; Studio and Night; the loops' inner walls dark glass lit
  at their rims. Published as artifact version 4, version id `1793969381-6d19`; survey cloud copy version 10, version id
  `1793969382-5dd1`.
- 2026-11-06, cloud session (owner: "we need greebles on the inside", "missing texture on the nose", "also where are
  windows to give scale", "do we got normal maps for the insitue too?"): version 6. Machinery on every face that looks
  across an opening, the outer hull still clean; a lit aperture's open middle and the frigate's glass nose an instrument's
  array of cells; rows of windows on every flank with rooms behind them, drawn by the pane code now shared with the Hunt
  (`docs/mockups/lib/baked-ship.js`). All five rebaked. Page 11.3 MB. Published as artifact version 6, version id
  `1793981351-4965`.
- 2026-11-07, cloud session (owner, 2026-11-06 and 07: "greebles look too random", "large vents, fuel pods, coils and
  pipes", "carrier should have landing pads and fighters", "diagonals on corners", "these lines are quire dark", windows
  "in awkward spotrs", the flood "light dots", warm rooms, then reference images of greebles, hull armour and details, "I
  hate those pods", "i dont like this light line, get rid"): version 7. Panelled machinery in rows (no fuel pods), plate
  grooves and marks, inspection hatches, diagonal corners, the carrier's pads with a craft on each, no light lines, lighter
  seams, windows clear of strips and openings, no flood dots, warm rooms. All five rebaked. Page 10.7 MB. Published as
  artifact version 7, version id `1794016956-ca38`.
