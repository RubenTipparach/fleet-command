# Mockup 4: Console Skins

- Link: https://claude.ai/artifact/PngN8P4W8RxrZeeZEda7GW
- Shows: three faction design languages for one UI kit, under the owner's three rules (no two skins share a button
  shape; each palette comes from the faction's ideology; shapes, lighting and icons come from how the faction thinks).
  One battle console drawn only from kit pieces with a live skin switch; the three skins' buttons (idle, hover,
  pressed), icon sets and palettes side by side; each skin's world view, bodies, technology, ships, shapes, light,
  motion, type and palette with roles; the LCARS principles Concord takes and the palette sampled from Starfleet
  Command and Starfleet Academy; the kit table with each skin's own margins, every 9-slice piece at three sizes, and the
  hero pieces.
- Made by: `docs/mockups/console-skins.html` (the page source, one copy), written up in
  `openspec/changes/console-skins/`. Concord's pieces are flat vector painted by the page; the Vos pieces are
  hand-drawn by the page's seeded ink painter; the Kethran pieces are Blender renders from
  `tools/blender/build_ui_kethran.py` (`assets/ui/kethran/`), inlined by `tools/mockups/inline_data.py`. The Concord
  palette was sampled with `tools/mockups/sample_palette.py`. Shots: `docs/screenshots/mockups/console-skins-*.png`.
- Stage: drafted.
- Published: 2026-10-30, version 1 (one kit, four recoloured skins); version 2 (three design languages: Concord after
  LCARS, Kethran rendered in Blender, Vos hand-drawn); version 3 (the Kethran keys plain wedge caps, no lens strip); version 4 (the page made a registry of skin modules, `docs/mockups/skins/`; the Vhorr Brood, the first of six new races); version 5 (the Aelith Conclave); version 6 (the Tarsine Remnant); version 7 (the Krai Warhost); version 8 (the Ombrai, whose bars are rows of eye-rings: a skin may tile its bar images); version 9 (the Kelvane Institute, after the Nucleo NLog Winamp skin: all nine skins in); version 10 (the Vhorr Brood modelled, shaded and rendered in Blender: `tools/blender/build_ui_vhorr.py`, `assets/ui/vhorr/`, on the shared harness `tools/blender/ui_render.py`); version 11 (the Kelvane Institute modelled, lit and rendered in Blender as an old lab dashboard: `tools/blender/build_ui_kelvane.py`, `assets/ui/kelvane/`; shared shapes and shaders in `tools/blender/ui_shapes.py`); version 12 (the Vhorr re-rendered with its carapace horns, tendrils and sinew at their intended height); version 13 (the Ssarith Hunt, a hunter race's jungle kit modelled and rendered in Blender: `tools/blender/build_ui_ssarith.py`, `assets/ui/ssarith/`, module `docs/mockups/skins/ssarith.js`).
