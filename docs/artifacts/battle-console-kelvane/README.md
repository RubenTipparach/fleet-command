# Battle console: Kelvane Institute (mockup 20)

- Page: `docs/mockups/battle-console-kelvane.html`, written by `tools/mockups/skin_page.py kelvane` from
  `docs/mockups/battle-console.html` (never edit it by hand).
- OpenSpec: `openspec/changes/kelvane-battle-console`.
- Kit: `assets/ui/kelvane/` rendered by `tools/blender/build_ui_kelvane.py` from `tools/mockups/kelvane_shapes.py`;
  skin library `docs/mockups/lib/kelvane.js`.
- Shots: `docs/screenshots/mockups/battle-console-kelvane-*.png`.

## Publishes
- Link: https://claude.ai/artifact/FNK2Q5RKWRVN7t4yET16ty
- Version 1 (2026-11-06): the Kelvane Institute skin of source v83, with the live strip meters; parity 70 shots, 4,087 presses of 396 controls, every press matched the Concord's. Version id `1793930210-1831`.
- Version 2 (2026-11-06): the emblem KID (the hydrogen eigen plot) on the crest, the name plates and the live emblem, as the oval 2D logo in KID's colours. Version id `1793934554-99a8`.
- Version 3 (2026-11-06): the live emblem and 2D logo in KID's revised palette (dark green fill, green lines, velvet red). Version id `1793937966-4030`.
- Version 4 (2026-11-06, committed from a cloud session, to publish from the desktop session): placing a mine no longer stops the page ("Uncaught ReferenceError: Rm is not defined": the mine preview read the transport ring's own copy of the reach); the helm keys' glyphs and turn angles sit inside the key's glass, measured from the kit's own key renders, with more room round the glyphs (owner: "the numbers arent in the buttons for turn angle, also these icons need a little more space on these buttons"); the glass measured out from the key's centre, so STOP and every glyph sit centred and a starboard turn's angle clears the rim (owner: "Institute 90degree to the right is busted. Stop isnt centered. The ssarith tol has same issue").
  Published by the desktop session with longer missile tails, 2.4 s of battle time and 0.45 units wide, a point kept every 1/30 s (owner: "your missiles don't have much of a tail"). Version id `1793980433-08fc`.
- 2026-11-06, committed from the desktop session, not yet published (the artifact tools stopped answering in this session): a shield hit flares the struck facing's bands toward white and 3.5 times as bright (`shield_flash_white` 0.7, `shield_flash_boost` 2.5; owner: "missing shield reaction when weapon impacts"), as the game now does. The next desktop session publishes it to the existing slug.
