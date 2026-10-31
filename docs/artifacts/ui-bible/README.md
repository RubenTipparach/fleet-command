# Analog Panel Bible

- Link: https://claude.ai/artifact/Gzo38Dh7o1EqmfzfpJr8rF
- Shows: the guide to building 1990s-style instrument panels in Blender (owner, 2026-10-30: "Start writing up a
  guide/bible. UI design Bible for using blender to render UI stuff the way they did in the 90s"). Named and
  numbered parts, depth, light, readability, analog feel, children inside their parents, formed metal (the notch
  and nine more shapes, every one a real Blender render with its fit-check map), the fit rules and their proof, seven
  lighting options rendered side by side, the fine-tuning settings, do and don't pairs, and the worked example: the
  session's Nucleo NLog study told in 19 steps with the owner's notes. It goes with the `blender-ui-panels` skill.
- Made by: `docs/design/ui-bible.html` (the page), its diagrams written into it by `tools/mockups/ui_bible_svgs.py`
  (run it after changing a diagram; `--check` fails if the page is behind), and the study's step renders in
  `docs/design/ui-bible/`, published beside the page. The formed-metal and lighting tiles (`docs/design/ui-bible/formed/`,
  `docs/design/ui-bible/lighting/`) come from `tools/mockups/formed_metal.py` (outlines and the fit check; it refuses a
  failing layout) and `tools/blender/ui_tiles_tail.py` (the Blender renderer, on the study's helpers). The study itself is not committed: it follows someone else's
  layout (CLAUDE.md sections 1 and 10); the renders are our own modelling and text.
- Published: 2026-10-30, version 1; version 2 (2026-10-31: every formed shape and lighting option a real 3D render, fit-checked); version 3 (2026-10-31: rule R, rolled edges never meet, and rule B, layers behind a plate stay inside it, both measured by the fit check with self-tests from the study's own faults; the tiles built on `tools/blender/ui_shapes.py`'s Panel; the Aelith Conclave as the example of rim light, `ui-bible/aelith-rim.jpg`).
- Version 2, 2026-10-31 (owner: "the guide illustrations violates some guidelines. Like the button overlapping with the
  bevels ... implement these as 3d renderings and verify the overlap rule", "make sure all those examples are 3d
  rendered", "Add alternatives like all sides, bottom lit, rim lighting etc"): the drawn catalogue replaced by 3D
  renders that pass the fit check, a fit-rules chapter (9) and a lighting-options chapter (10), close-up renders of a
  well and the notch, the light chapter and settings table brought up to the final study, and step 19.
- Account move: Republished 2026-10-31 on the owner's current account (the earlier links were made from another account and cannot be read from here); every link updated.
