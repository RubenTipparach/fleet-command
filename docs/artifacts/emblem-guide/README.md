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
- Version 2 published 2026-11-03, version id `1793744559-d16f`: the joins fixed (owner: "you can see gaps in the shape,
  this is low quality work"): B's nacelles and keel grow out of the saucer as one web of hull, C's walls end in its sill
  and its founders stand on it, A's tips blend out of their arms; every sketch's joins measured on the page
  (`window.EMBLEM_CHECK`, run headless by `tools/emblems/sketch_check.mjs`); rule 17 added; pressing a picked design
  again removes the pick. The stray pick of B in the `notes` collection was deleted at the owner's word ("unpick that,
  I didnt pick anything yet").
- Version 3 published 2026-11-03: B restored to its first shape (owner: "that changed the jellyfish design to much, the
  jelly fish has a tiny small tail"): three separate legs and the tiny tail, only rooted under the saucer so no wedge
  gaps show; the web of hull from version 2 removed.
- Version 4 published 2026-11-03: every feature on a sketch has an id (A1, A2 ... D5; owner: "can you label each
  feature A1, A2, A3 etc"), in its label and in a badge on the element; anchors moved onto the features they name.
- Version 5 published 2026-11-03: B reworked on the owner's notes ("i dont like the way its colored", the thin lines in the
  legs, "some sort of shape encompassing that" for a patch, B1 one solid colour, the star impressed): the saucer one
  solid Signal blue, no lines in the legs, the star pressed in (a shaded floor with a lit lower-right rim), the
  nacelles standing on a patch frame; three patch shapes shown with reasons (B-E1 the sweep frame, in the sketch; B-E2
  the mission arch; B-E3 the sector hex); B's joins measured on the emblem and frame without the field.
- Version 6 published 2026-11-03: B's frames and colours (owner: "dont like any of those shapes, though b-e2 is a good
  direction", "those colors are no good either, hard to read from far away"): the sweep frame and the hex dropped; four
  arches grown from B-E2 (B-E4 the thickening arch, in the sketch; B-E5 the hangar door; B-E6 the flared arch; B-E7 the
  saucer arch), each one's joins measured; colourways measured for contrast (at least 7:1, rule 18): B-K1 pearl on
  night (11.9:1, in the sketch), B-K2 pearl on flag blue (4.8:1, fails), B-K3 night on pearl (11.9:1), each at 12 px.
- Version 7 published 2026-11-03: B-E4 to B-E7 retired (owner: "dont like any of those shapes, though b-e2 is a good
  direction (but I still DONT LIKE IT)"); B-E2 back in the sketch; six variations on it (B-E8 to B-E13), each changing
  one thing. Every emblem now has a version for black and one for white (owner: "the colors are bad, they dont read on
  the white background", "need different versions for white and black background"), each measured: B pearl on night on
  black (ship on field 11.9:1, outline 17.8:1), night on pearl on white (11.9:1, 13.2:1); A, C and D pearl on black
  (17.8:1) and night blue on white (13.2:1).
- Version 8 published 2026-11-03: B-E8 to B-E13 retired (owner: "the shape on e-9 is still the exact same, you just
  changed teh style. Keep in mind the shape must be closed line beacuse it needs to be printed as a pin or a patch on a
  uniform"). Rule 19, one closed outline, measured on every patch (the printed patch one piece, the background one
  region). Three different outlines beside B-E2: B-E14 the capsule, B-E15 the keyhole, B-E16 the five-sided house.
- Version 9 published 2026-11-03: two rounded triangles for B (owner: "can we try a triangle shape with cruved ends?"):
  B-E17 (corners rounded 10) and B-E18 (rounded 20), each one closed outline, the nacelles standing on the base.
- Version 10 published 2026-11-04: the trapezoid (B-E19) and the chevron (B-E20) for B (owner: "What about a more
  trapazpidal shape and another one more Chevron"), both one closed outline, joins measured.
- Version 11 published 2026-11-04: B-E21 the upside-down chevron and B-E22 the curved-top trapezoid (owner: "upside down
  chevron, another alternate shape maybe a more curved top trapezoid?"), both one closed outline, joins measured.
- Version 12 published 2026-11-04: B-E23 the rounded peak, drawn from the owner's red line over B-E22 ("something more
  like this?"): the trapezoid's sides rise from their shoulders in one curve to a rounded peak; now B's main sketch.
- Version 13 published 2026-11-04: B-E23 links to its 3D badge (mockup 10); the owner's notes on 3D and iridescence.
- Version 14 published 2026-11-04: the pressed star is satin Signal blue (owner: "make the start like a satin in blue");
  B-E24 the arrow-foot peak ("what if it was a slightly arrow shaped at the bottom") and B-E25 the pointed peak ("Can we
  make the to point too?"), B-E25 now B's main sketch.
- 2026-11-04, version 15 (`1793776102-a400`): the owner's note on the saucer's double bevel and the leg segment marks; `lensTop` and `legSegments` added to `window.EMBLEM_PARTS`.
- 2026-11-04, version 16 (`1793776690-c54f`): the owner's note on the corners; `inset` clips exactly, `polys` samples segment by segment so each path corner is a vertex.
- 2026-11-04, version 17 (`1793779365-6ec3`): the owner's notes on the hull marking (one emblem, half size, port side).
- 2026-11-04, version 18 (`1793779848-702d`): B-E26, the beacon, from the owner's sketch ("can we try a shape like this?"), measured: one closed outline, parts 5.0 units apart.
- 2026-11-04, version 19 (`1793781463-2f51`): B-E26 tightened (about 6 units of field above and below the ship, was 17 to 20); B-E15 marked as one of the owner's candidates.
- 2026-11-04, version 20 (`1793786521-e569`): B-E26 rejected (owner: "Beacon sucks"); B-E27, the smooth keyhole ("Keyhole but smoother"), measured: one closed outline, parts 5.3 units apart.
- 2026-11-04, version 21 (`1793787092-93c4`): B-E27 accepted and made B's main sketch ("Yea ok. I'll accept that"); its parts exported to `assets/emblems/concord/b-e27.parts.json`.
- 2026-11-04, version 22 (`1793795704-e08e`): the Krai Warhost section (owner: "begin krai emblem design, this faction believes strongly in tradition. give them some sort of asymmetric logo"): KA the heirloom blade (recommended), KB the lineage standard, KC the fang torc; feature ids KA1, KB1 and so on; measured joins and outline contrast.
