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

Then, on version 1 of the page (2026-10-30): "this ui language thing, it just different colors, not very interesting.
1. do research on LCARS design language ... 2. extract colorpallete from starfleet command and starfleet academy, build ONE
design language based on that 3. build the other design language using 3d models in blender like starcraft 4. design a
hand drawn aesthetic of the mirak ui", with three rules: "NO skin uses the same button shape", "every ui has a color
palette inspired by that factions ideologies", "ui elements and iconography is inspired by how that faction thinks. each
ui skin uses different shapes, lighting, and shading on their UI and different icon shapes shaped by their world view,
their body shapes, their technology, the shape of their ships".

Every screen is built from interface pieces, so the pieces must be designed once as a kit with slots, and each
faction's look must be a skin for that kit. This is mockup 4 in `mockup-work.md`.

## What Changes

- **The UI kit**: one list of named pieces, each a 9-slice piece (frames, bars, buttons, tabs, strips) or a hero piece
  (helm pad, systems display bezel, dial, alert lamp, crest, name plate), with its slot size and slice margins.
- **Three design languages, one per faction**, each with its own method, button shape, palette from its ideology, icon
  set, lighting, type and motion: Concord built on LCARS's principles with a palette sampled from Starfleet Command and
  Starfleet Academy (flat, back-lit vector); Kethran Hegemony modelled and rendered in Blender, StarCraft-like hard
  surface; Vos Syndicate hand-drawn after the reference game's pirate skin.
- **One console, swapped live**: the same battle console layout drawn from kit pieces in each skin, with a skin switch,
  and every 9-slice piece shown stretched to three sizes to prove the slicing.
- **Which skin you see**: your faction's in your own ships; a captured ship keeps its builder's console; skirmish lets
  you pick a faction; Options can pin a favourite (survey U4, provisional).
- The page shows the real methods: Concord's vector pieces and Vos's drawn pieces are painted by the page's code, and
  Kethran's are Blender renders from a committed generator.

## Capabilities

### New Capabilities

- `ui-kit`: the named kit of 9-slice and hero pieces, faction skins, and how screens draw only kit pieces.

### Modified Capabilities

None.

## Impact

- New mockup page `docs/mockups/console-skins.html`, shots, record in `docs/artifacts/console-skins/`, tracker entry M4.
- Later (after approval): a Blender kit generator (`panel-textures`), baked images per skin with margins in a manifest,
  and the game's theme built from it.
