# Mockup 7: Battle Console, Krai Warhost

- Link: not yet published (the desktop session publishes; CLAUDE.md section 8). Publish
  `docs/mockups/battle-console-krai.html` as a new artifact titled "Krai Battle Console", icon `console`, then record
  the link here, in `mockup-work.md`, `docs/design/README.md` and the mockup tracker.
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
- Version 2 (the joined column and strips, no clamps): not yet published.
