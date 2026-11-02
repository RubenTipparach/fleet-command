# Proposal: concord-ship-look

## Why

The owner, 2026-11-02, with five screenshots of fan-made starship models (not committed: someone else's art): "let's
develop the concord ships in 3 js artifact". Their notes, in their words:

- "take notes on how: windows communicate scale."
- "ships have liveries and markings that make it feel lived in"
- "branding is super important, from sillhoute to colors of features, lighting, which emphasizes registry numbers and
  ship features in low lighting conditions"
- "some ships have more markings than ohters, depends on how much they emphasize it"
- "ship textures vary from differnt metallic patterns to show ship construction processes, and what kinds of features
  they want to emphasize"
- "federation emphasizes nacelles like legs and disc like head or brain"
- "romulan and klingon emphasizes bird like head with bird wings"
- "gorn emphasizes claw like features, since they enjoy hunting"

The Concord is the player's faction. Before any ship model is built in Blender, the owner sees the look as an
orbitable three.js page (CLAUDE.md section 4): mockup 5, `docs/mockups/concord-ships.html`, which also presents the
companion change `ship-damage-visuals`. It is the first part of mockup 3 (look dev) for one faction.

## What Changes

- **Three Concord classes**, our own designs on one parametric hull: the Bastion-class cruiser (CS Meridian, 312 m),
  the Lantern-class frigate (CS Wayfarer, 196 m) and the Swift-class corvette (CS Kestrel, 118 m). The class names are
  the hulls of mockup 1.
- **The Concord silhouette**: a disc for a head (the bridge crown on top is the brain), nacelles carried on bent
  pylons like legs.
- **Windows as the scale cue**: every window is person sized and every row is one deck, so a captain counts decks.
- **Livery and markings with a density per class**: ceremonial on the cruiser, working markings on the frigate, a bare
  racing stripe on the corvette.
- **Plating patterns per class** that say how the hull was built: radial ring plating, mixed patchwork plates,
  long strakes.
- **Branding lit for the dark**: registry, name and crest floodlit; hull floods that graze the plating; navigation
  lights after an airliner's (mockup 3's table). All baked into the emission map, so a ship in shadow still reads.
- Notes on how other factions read: bird heads and wings, claws for the hunters (survey A8 maps them to our factions).

## Impact

- New: `docs/mockups/concord-ships.html`, `docs/artifacts/concord-ships/`, a row in `mockup-work.md` and the tracker,
  survey section A (A2-A9).
- No game code. Ships are built in Blender only after the mockup is approved for build (`blender-hard-surface`,
  `light-baking`).
