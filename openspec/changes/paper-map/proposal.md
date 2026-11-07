# Proposal: paper-map

## Why

The owner, 2026-11-07: "lets do something for fun. make the space background a crusty print on old piece of paper with
hexagon grid drawn on it. The hex grid cells can have borders of different colors based control. The edges of the map is
crusty paper with a white border thats starting to tear, the big map poster sidts ona large wooden table, of which sits in a
room with red carpedting, and light red-orange floral print".

The campaign map has been a board of dark glass hexes over a nebula sky. This change makes it a printed chart: an old paper
poster, its hexes outlined in the colours of whoever holds them, lying on a walnut table in a carpeted, papered room, with
the same miniatures standing on it.

## What changes

- Mockup 1 (`docs/mockups/game-flow.html`): the campaign map's look becomes the paper chart (`buildRoom`, `paperSheet`,
  `paperMesh`); the glass board stays behind `?look=space` and the shot `map_space`, for comparison.
- Three Material Maker graphs: `map_paper`, `carpet_red`, `wallpaper_floral` (`tools/materials/build_ptex.py`).
- The faction screen's walnut table and the map room's table are one builder (`walnutTable`).
- The game keeps the glass board until the owner asks for the paper map in the game (a sync task, `game-flow-engine`).

## Impact

The map's pieces, routes, picking, HUD and rules are unchanged. The page grows by the room's maps (about 0.9 MB).
