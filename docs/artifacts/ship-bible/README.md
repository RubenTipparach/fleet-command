# Ship Design Bible (guide)

- Link (the cloud session's account, published 2026-11-04 with the Culture, not anatomy rules): https://claude.ai/artifact/4nhxxoGusHfkXX63Xt25bm
- Link (the main account, not reachable from cloud sessions): https://claude.ai/artifact/QxFV2qGabbN2chE3zd8km7
- Source: `docs/design/ship-bible.html` (one copy, not duplicated here), its captures `docs/design/ship-bible/*.jpg`
  (published as supporting files; rendered from mockup 6's named shots, cropped to the 3D view and saved at 960 px),
  the Petrel plan `data/ships/concord/petrel_plan.json` and the Concord line `docs/mockups/lib/concord-line.js`, both
  inlined by `tools/mockups/inline_data.py`.
- OpenSpec change: `openspec/changes/ship-design-guide/`.

## Versions

- 2026-11-03, cloud session (Concord ships thread; owner: "ship designs are terrible. we need to develop a guide based on
  concord ships ...", then "Add to the guide: ship exterior material should compliment the UI, not share materials or
  colors directly ..."): version 1, version id `1793722593-ea42`.
- 2026-11-03, cloud session (Concord ships thread): version 2, version id `1793739161-a2e8`. Chapter 5 takes the A18 answer (a bridge block only on super capital ships); X1 is a guide, not a hard rule, and X2 (no shared colours) is withdrawn (owner: "I said compliment, so not a hard rule"); plans from the shared `lib/ship-plan.js`.
- 2026-11-04, cloud session (owner: "the look of an alien species is not their UI, not their spaceship", "make sure these
  notes are added to the cultural guides of designing spaceships"): chapter 13, Culture, not anatomy (rules C1-C5), added
  in `docs/design/ship-bible.html`, with a checklist line and a row in Other factions (now chapter 14). **To republish from
  the desktop session**: the cloud session's account cannot open this artifact.
- 2026-11-04, cloud session (owner: "publish new sets", "dont publish updates to the amster artifact mockup tracker on this account"): published as a new artifact on the cloud session's account, https://claude.ai/artifact/4nhxxoGusHfkXX63Xt25bm, version id `1793801590-d689`. Not on the tracker (the tracker stays on the main account).
- 2026-11-05, cloud session (owner: "every ship needs custom uv Unwraped textures ...", "shapes must be made with purpose and
  flow into the grater structures"): version 2 on the cloud account, version id `1793849492-26ef`. Chapter 14, One
  structure, grown (F1-F5), and chapter 15, Every ship its own texture (T1-T4); Other factions is now chapter 16, the
  checklist chapter 17.
- 2026-11-05, cloud session (owner: "vents, lights, and sub armor pieces, and primary armor peices", "liveries, smooth
  sphereical portions of armor, and specifically shaped vents", "no repeating elemnts, single axis symmetry, and lots of
  shading"): version 3, version id `1793873085-5bf7`. Chapter 15 gains T5-T8 (armour in layers, distinct features,
  nothing repeats with one axis of symmetry, shading for depth) and a checklist line.
- 2026-11-07, cloud session (owner: "update the institute ship guide"): version 4 on the cloud account, version id
  `1794023426-75c0`. Chapter 17, The Kelvane Institute (the line, its traditions, rules KI1-KI9, four captures from mockup 23
  v8: `kelvane_lineup`, `kelvane_inside`, `kelvane_carrier`, `kelvane_craft`, published as supporting files); the checklist
  is now chapter 18 with a KI line.
- 2026-11-11, cloud session (Vos ships thread; owner: "the ship guide should clearly state that there should be noface that is completely devoid of detail"): version 3 at the main account's link (the tracker's), version id `1794390029-4d38`, which had stood at version 2; brought up to date with chapters 13 to 18 and the four Institute captures as supporting files, and T9 (no face without detail), T10 (ordered machinery panels), T11 (masts with mass) and T12 (colour fields and pale edges, not built yet) added from the owner's reference ships, in our own words. The cloud account's copy (4nhxxo...) is no longer reachable from this account.
- 2026-11-11, cloud session: version 4, version id `1794400281-6daa`: T13 (no two antennas the same shape, a mirrored pair being one design) and dishes under T11 (owner: "No two antenna cam be the same shape. Also need dishes as well").
- 2026-11-11, cloud session: version 5, version id `1794401940-2f75`: T13 corrected, a port and starboard pair of antennas differ too, overriding T7's symmetry for antennas and dishes.
- 2026-11-11, cloud session: version 6, version id `1794403496-09be`: T13 adds no two antennas the same length (at least 12% apart, measured whole).
- 2026-11-11, cloud session: version 7, version id `1794409572-bbd0`: T14, glow is baked light or a shader, never flat paint.
