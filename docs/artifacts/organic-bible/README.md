# Organic Panel Bible

- Link: https://claude.ai/artifact/RNyM5ka91uporfETHjAQcD (slug `c56cabee-e37f-4451-a6dd-bf03afee1b3e`).
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

## Versions

- Version 1, 2026-10-31, published from the cloud session (owner: "Now that looks good. Create the guide", "Just publish it"; commit 60b7e2e): reviewed, rules 1-4 (never raw primitives; layered anatomy sculpted; screens as pixels under a fine hex filter; no nine-slice, block out then sculpt), study steps 1-25; the compiled `index.html`, one self-contained file; version id `1793479604-bb1a`.
- Version 2, 2026-10-31, published from the cloud session (owner: "The one thing about UI is it's overlayed on top of gameplay so that background wouldn't make sense here", "the guide should include my rejected iteration with the Pebbles. Only this new iteration is accepted. Minus the background there"; commit 1b59614): Rule 5 (an overlay, not a scene), study step 26 (step 25 without the backdrop, over a game view), step 14 marked Rejected and step 26 Accepted; version id `1793481192-afd1`.
- Version 3, 2026-10-31, published from the desktop session (cloud session request, commit d3c949c): rule 4 is no nine-slice and no fixed sizes (parametric sculptors), the any-size sheet, the Brood's build; version id `1793486570-f931`.
