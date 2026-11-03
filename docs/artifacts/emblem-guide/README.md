# Emblem Design Guide (guide)

- Link: https://claude.ai/artifact/1ELwhue4vzFPjySnVc475V
- Shows: how faction emblems are designed and the owner's notes on them (newest first, in their words); the method
  (ideology, then a flat SVG sketch with every choice labelled, then the owner's pick, then 3D); what the reference
  badges teach; what the STARFLEET Visual Standards Manual 2026 teaches (cited by printed page); sixteen rules compiled
  from all three; the references; and the Concord's four annotated sketches (A Many Hands, recommended; B The Saucer;
  C The Open Door; D Beacon and Wings), each one image with labels on its elements, small sizes, a note box and a pick.
  The rejected first Concord render (a ring of five domes) is kept with the owner's words.
- Made by: `docs/design/emblem-guide.html` (the render inlined from `docs/design/emblem-guide/renders.json` by
  `tools/mockups/inline_data.py`), published with the `db` and `user` capabilities. The owner's notes and picks are in
  its database, collection `notes`: one document per design id (`concord-a` ... `concord-d`, `general`) holding
  `{text, updatedAt}`, and `concord-pick` holding `{design, updatedAt}`.
- Change: `openspec/changes/faction-emblems/`.

## Publishes

- Version 1 published 2026-11-03 (cloud session, faction emblems thread), version id `1793743795-415c`. Its `notes`
  collection read back empty after publishing.
