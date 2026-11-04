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
