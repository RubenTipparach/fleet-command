# Proposal: console-skins

## Why

The owner, 2026-10-30, with two screenshots of the reference game's battle screens:

- "I want the ui to look really nice, like modeled and lit in blender then baked into a texture, also it should be
  deisnged for 9slicing for some elements, and others are like hero elements."
- "I really like espeically the mirak ui design, its so playful and pirate like."
- "I also enjoy the sleek, utilitarian, LCARS blue version of the federation, very sci-fi very minimal."
- "whatever we do, we gotta design it for like UI kit that we can sweap out between factions."
- "before I approve this start another artifact so we can design the design language of the ui, which changes based
  on what faction origin we are."

Every screen is built from interface pieces, so the pieces must be designed once as a kit with slots, and each
faction's look must be a skin for that kit. This is mockup 4 in `mockup-work.md`.

## What Changes

- **The UI kit**: one list of named pieces, each a 9-slice piece (frames, bars, buttons, tabs, strips) or a hero piece
  (helm pad, systems display bezel, dial, alert lamp, crest, name plate), with its slot size and slice margins.
- **The design language of each faction skin**: palette, type, shape, ornament, materials and lighting, and how a
  piece is built in Blender for that skin. Concord: sleek, utilitarian, minimal, blue. Vos Syndicate: playful,
  organic, pirate-like. Kethran Hegemony: options to choose from (survey U5).
- **One console, swapped live**: the same battle console layout drawn from kit pieces in each skin, with a skin switch,
  and every 9-slice piece shown stretched to three sizes to prove the slicing.
- **Which skin you see**: your faction's in your own ships; a captured ship keeps its builder's console; skirmish lets
  you pick a faction; Options can pin a favourite (survey U4, provisional).
- The page draws its pieces in code as stand-ins for the Blender bakes: this change sets the language, and the bakes
  follow once a skin is approved for build.

## Capabilities

### New Capabilities

- `ui-kit`: the named kit of 9-slice and hero pieces, faction skins, and how screens draw only kit pieces.

### Modified Capabilities

None.

## Impact

- New mockup page `docs/mockups/console-skins.html`, shots, record in `docs/artifacts/console-skins/`, tracker entry M4.
- Later (after approval): a Blender kit generator (`panel-textures`), baked images per skin with margins in a manifest,
  and the game's theme built from it.
