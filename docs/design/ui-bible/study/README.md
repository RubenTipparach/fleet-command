# The Nucleo NLog study

A learning study, not game art. It rebuilds the two-layer construction of the Nucleo NLog Winamp skin (by its own
author) in Blender: a pillowed metal plate over a recessed, backlit display film. It was made to learn the
construction that the Analog Panel Bible (`docs/design/ui-bible.html`) and the `blender-ui-panels` skill teach, and
that the Kelvane Institute skin now uses. It follows the original's layout. All the text, track names and labels are
our own, and nothing of the original's logo, name or art is in it. Committed at the owner's request (2026-10-31:
"Make pr push everything").

| File | What it is |
|---|---|
| `nlog_study.py` | The Blender script. Run `/opt/blender/blender --background --factory-startup --python nlog_study.py -- out.png 64`. Environment: `NLOG_ANIM=<dir>` with `NLOG_FRAMES=96` renders the animated loop, `NLOG_SCALE=<percent>` sets the size, `NLOG_TOP=1` renders only the top display, and `NLOG_GEOM=<file>` writes the geometry and stops. |
| `fitcheck.py` | Rule B, measured on a render: every film lies inside its plate, and no lit pixel lies outside the plates. |
| `edgecheck.py` | Rule R, measured on the geometry: no two rolled edges touch (openings 10.6 mm, dimples 8.2 mm, raised plates 6.8 mm). |
| `nlog_study.jpg` | The final still, 1100 x 1400 px, after the edge fixes: 111 of 111 edge distances pass, 0 lit pixels outside the plates. |
| `nlog_study_4k.jpg` | The 4K still, 3014 x 3836 px, rendered before the edge fixes and the scrolling title, so it shows the old faults. |
| `nlog_anim.mp4`, `nlog_anim_small.gif` | The 8 s loop, 96 frames at 12 fps, after the edge fixes; every frame passes `fitcheck.py`. |
| `nlog_study_fit.json`, `nlog_study_geometry.json` | The plates, films, openings and dimples the two checks read. |

The two checks are this study's own. The skins use the shared checker, `tools/mockups/formed_metal.py`, which
measures the same rules R and B.
