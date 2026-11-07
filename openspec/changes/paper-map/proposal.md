# Proposal: paper-map

## Why

The owner, 2026-11-07: "lets do something for fun. make the space background a crusty print on old piece of paper with
hexagon grid drawn on it. The hex grid cells can have borders of different colors based control. The edges of the map is
crusty paper with a white border thats starting to tear, the big map poster sidts ona large wooden table, of which sits in a
room with red carpedting, and light red-orange floral print".

Then (2026-11-07): "The main map should be like 2d print ed star map. With a cool nebula background", "The walls in that
room should be like a rustic dark green wall with wood trimmings", and "The map is a bit bright, pull in main they should be
e a fog of war change".

The campaign map has been a board of dark glass hexes over a nebula sky. This change makes it a printed chart: an old paper
poster, its hexes outlined in the colours of whoever holds them, lying on a walnut table in a carpeted, papered room, with
the same miniatures standing on it.

## What changes

- Mockup 1 (`docs/mockups/game-flow.html`): the campaign map's look becomes the paper chart (`buildRoom`, `paperSheet`,
  `paperMesh`); the glass board stays behind `?look=space` and the shot `map_space`, for comparison.
- Three Material Maker graphs: `map_paper`, `carpet_red`, `wall_green_rustic` (`tools/materials/build_ptex.py`); the first
  version's rose wallpaper (`wallpaper_floral`) is gone with the owner's green walls.
- The chart is a printed star map (a nebula and stars printed in night-blue ink), and it carries the fog of war the game
  draws (change `campaign-space`, its task 2.2).
- The faction screen's walnut table and the map room's table are one builder (`walnutTable`).
- The game keeps the glass board until the owner asks for the paper map in the game (a sync task, `game-flow-engine`).

## Impact

The map's pieces, routes, picking, HUD and rules are unchanged. The page grows by the room's maps (about 0.9 MB).
