# Organic Panel Bible

- Link: to be published by the desktop session (CLAUDE.md section 8: the desktop session publishes for cloud sessions).
- Shows: the design system for grown, organic console panels (owner, 2026-10-31, of the Vhorr Brood's version 23: "Nah
  that's bad lol. Let's make a new design system for organic", and "The brood is overly simple. It's just metal with
  ameobas on top. Completely missing the zerg aesthetic"). One organism holding crisp instruments; five materials
  (flesh, chitin, bone, smoked glass, light); growing the form; screens, keys and the portrait; light; the palette on a
  value ladder; the fit rules G1-G4 with the study's measurement; Blender recipes; a side-by-side table of what went
  wrong in version 23; and the worked example: the study told in 14 steps, one fix per step. It goes with the
  `blender-organic-panels` skill.
- Made by: `docs/design/organic-bible.html` (the page) and the study's step renders in `docs/design/organic-bible/`,
  published beside the page, rendered by `tools/blender/organic_study.py` (Blender 4.4.3, Cycles CPU, 32 samples;
  `fit.json` is the final step's fit measurement). The direction came from two reference screenshots of another game's
  hive interface, which are not committed: the renders are our own modelling, words and colours.
- Published: not yet.

## Compiled for publishing (draft)

The guide is a draft that grows with the study (owner, 2026-10-31: "just compile organic guide as html, since it's in
draft and we'll be adding to it as we discover the methodology behind organic ui"). `docs/artifacts/organic-bible/index.html`
is the page compiled into one self-contained file, every study image carried inside it as a data URI, by
`python3 tools/mockups/compile_page.py docs/design/organic-bible.html docs/artifacts/organic-bible/index.html`.
Edit `docs/design/organic-bible.html`, recompile, then republish `index.html` to this artifact's slug.
