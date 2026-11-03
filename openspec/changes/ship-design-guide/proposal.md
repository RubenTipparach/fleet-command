# Proposal: ship-design-guide

## Why

The owner, 2026-11-03: "ship designs are terrible. we need to develop a guide based on concord ships. hull lighting,
markings, liveries, windows (this is a must), parallax interiors, parallax damage fx, nacelles (most factions will have
these), decide on discrete components (struts to connect nacelles, primary/secondary hull). Does bridge need a shape?,
map out approximate floor plans out of blocks, and enclose the structure with the hull shape". Then: "Add to the guide:
ship exterior material should compliment the UI, not share materials or colors directly. Ship exterior depending on the
faction should focus on distinct features, color scheme and unique paneling".

Ships so far were drawn hull first (the Krai v1 and v2 most of all), so nothing on their skins was the outside of
anything inside. The panels have their bibles (Analog, Organic); ships need one.

## What Changes

- **The Ship Design Bible**, `docs/design/ship-bible.html`, a guide like the panel bibles, worked on the Concord line:
  - Fourteen chapters:
    - the method (inside out);
    - floor plans from blocks;
    - enclosing the hull;
    - the discrete parts;
    - the bridge's shape;
    - nacelles and struts;
    - windows;
    - parallax interiors;
    - hull lighting;
    - markings and liveries;
    - exterior material against the console;
    - parallax damage;
    - other factions;
    - the checklist.
  - Rules coded M, P, H, B, W, L, X and D.
- **The first ship plan as data**: `data/ships/concord/petrel_plan.json`, the Petrel's 23 rooms as blocks on three decks.
  The guide draws its deck plans from it and fits a hull round it, measuring that every room clears the hull by 1.05 m.
  It measures mockup 6's Petrel the same way.
- `docs/mockups/lib/concord-line.js`: `plainModel` names each mesh's part (primary, secondary, neck, command, strut,
  nacelle, drive, deflector) for the guide's exploded view.
- Survey A18: does the bridge need a shape (recommended: yes, a raised block).

## Impact

- New guide and its captures (`docs/design/ship-bible/`), a new data file, the tracker's Guides, the survey.
- No game code. Later changes refit mockup 6's hulls from plans and redesign the Krai on this method.
