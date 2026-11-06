# Mockup 8: Battle Console, Krai Warhost

- Link: https://claude.ai/artifact/D6PacxkhuY6Q2n8TGHKDTv (slug `D6PacxkhuY6Q2n8TGHKDTv`), on the owner's current account; M8 on the
  mockup tracker.
- Shows: the battle console of mockup 2 in the Krai Warhost skin, every control working against the same seeded
  stand-in battle and test scenes. The Krai kit rendered in Blender (frames, keys, tabs, strips, name plate, dial, lamps,
  crest, symbol bars, allocation cells), the column as one plate with the panels formed into it as joined wells and a
  channel of red light crossing them, 80 px strips whose metal runs into the column, every label and title on a metal
  placard, the full icon set in the Krai's language, Teko and Russo One type,
  blade-cut drawn keys, and the player's side named as the Warhost.
- Made by: `docs/mockups/battle-console-krai.html`, written by `tools/mockups/skin_page.py krai` from
  `docs/mockups/battle-console.html` (never edit the written page). The Krai look is `docs/mockups/lib/krai.js`; the
  kit is `assets/ui/krai/` (`tools/mockups/krai_shapes.py`, `tools/blender/build_ui_krai.py`). Written up in
  `openspec/changes/krai-battle-console/`.
- Verified: `tools/mockups/skin_parity.mjs` pressed every control of the 40 named shots in the Concord and the Krai
  pages (2,175 presses of 270 controls): identical console and battle state a second after each press.
- Shots: `docs/screenshots/mockups/battle-console-krai-*-desktop.png` (14 of the 40 named shots; `SHOOT_SIZES=desktop node tools/mockups/shoot.mjs docs/mockups/battle-console-krai.html <dir>` makes all of them).
- Stage: drafted.

## Publishes

- Version 1 (first pass, clamps and a backplate): not published.
- Version 2 (the joined column and strips, no clamps): published 2026-11-03 from the cloud session at the owner's request
  ("artifact so I can play this mockup?"; no desktop session was reachable), version id `1793715999-c961`, then
  republished as mockup 8 (another session's branch holds mockup 7), version id `1793716070-4027`.
- Version 3 (the gaps closed to a rib; icon keys and status boxes rendered in the rail tabs' style): published 2026-11-03, version id `1793717243-818b`.
- Version 4 (one unbroken 20 px divider between panels with the title on it; the backlight red, the whole kit re-rendered): published 2026-11-03, version id `1793722302-b4c0`.
- Version 5 (states in the Krai palette, no green; glyphs orange on dark glass; no white marks; panel tab icons red): published 2026-11-03, version id `1793736690-394d`.
- Version 6 (nothing white on a key: orange ink on dark keys, red mode tab names): published 2026-11-03, version id `1793737624-3c64`.
- Version 7 (the top strip re-rendered at 60 px): published 2026-11-03, version id `1793738755-e645`.
- Version 8 (a bloom experiment on the column's channel of red light, with a Bloom key to compare): published 2026-11-03, version id `1793739594-563e`.
- Version 9 (hover popups and pointed keys' labels sized by the key's own end caps; rule T): published 2026-11-04, version id `1793761414-3503`.
- Version 10 (the shared weapon targeting of change `weapon-targeting`; parity 2,378 presses of 313 controls): published 2026-11-04, version id `1793763262-429a`.
- Version 11 (the five new mechanics and the aim changes of version 72 of the source page; Krai officer names and ranks; bloom always on; parity 3,187 presses of 389 controls): published 2026-11-04, version id `1793777628-6b78`.
- Version 12 (drag-and-drop repairs, version 73 of the source page; parity 3,225 presses of 380 controls): published 2026-11-04, version id `1793778538-858d`.
- Version 13 (wide keys `keyw.*` rendered in Blender and nine-sliced for the Tactical grid; weapon aims and independently powered weapons from version 74 of the source page; parity 3,261 presses of 391 controls): published 2026-11-04, version id `1793780737-78bf`.
- Version 14 (the boarding party of version 75 of the source page; parity 3,432 presses of 394 controls): published 2026-11-04, version id `1793782743-4feb`.
- Version 15 (one console for every skin, version 76 of the source page; the Allied fleet and Three races scenes; parity 3,917 presses of 396 controls): published 2026-11-04, version id `1793801244-fa5c`.
- Version 16 (expanded panels on a rendered backing of blackened steel, never black; version 77 of the source page; parity 3,917 presses of 396 controls): published 2026-11-04, version id `1793804657-82d3`.
- Version 16 rebuilt after merging main (2026-11-04; parity 3,917 presses of 396 controls): version id `1793806624-cff7`.
- Version 17 (version 78 of the source page, no visible Krai change; parity 3,917 presses of 396 controls): published 2026-11-04, version id `1793818997-51ff`.
- Version 18 (version 79 of the source page; merged onto another session's live `1793822395-b7e4`, whose black under the strips is carried; parity 3,917 presses of 396 controls): published 2026-11-04, version id `1793823168-2879`.
- 2026-11-04, desktop session (Mac), owner: "this top part needs a blcak background, same with bottom stuff too": version 18, version id `1793822395-b7e4`, a text edit on the live version: the strips drawn on black (no change where the Krai metal covers them).
- 2026-11-04, desktop session (Mac), owner: "add shield display to all ships as well": version 20, version id `1793828141-484d`, a text edit on the live version: every ship shows its 3D shield display.
- Version 21 (2026-11-05): the Krai page's own fleet and every Krai ship fly as the Krai line's exported glbs; published over the desktop session's live `1793828141-484d`; parity 3,917 presses of 396 controls. Version id `1793850935-983c`.
- Version 22 (2026-11-06): the Patience in the allied and swap scenes; parity 70 shots, 4,087 presses of 396 controls, every press matched. Version id `1793930948-97b6`.
- Version 23 (2026-11-06): the Kelvane emblem KID in its colours. Version id `1793934556-1828`.
- Version 24 (2026-11-06): the Kelvane emblem in KID's revised palette. Version id `1793937970-b670`.
- Version 25 (2026-11-06, committed from a cloud session, to publish from the desktop session): placing a mine no longer stops the page ("Uncaught ReferenceError: Rm is not defined": the mine preview read the transport ring's own copy of the reach); the helm keys' glyphs and turn angles sit inside the key's glass, measured from the kit's own key renders, with more room round the glyphs (owner: "the numbers arent in the buttons for turn angle, also these icons need a little more space on these buttons"); the glass measured out from the key's centre, so STOP and every glyph sit centred and a starboard turn's angle clears the rim (owner: "Institute 90degree to the right is busted. Stop isnt centered. The ssarith tol has same issue").
