# Proposal: campaign-space

## Why

The owner, 2026-11-07, on the campaign map:

- "space is kinda random right now. we should have stars, with planets asteriods and nebula clustered around them"
- "rogue planets, dust clouds, and asteroids with no stars are a thing too"
- "need more missions around planets, nebula, and asteroids", "also provide a healthy supply of patrol missions around"
- "we should implement fog of war as well. outside the empire it should be black fully, then revealed when either friendly
  our your ship moves through the fog. Any revealed area updates the tile color (empire controled)"
- "fog of war has three states: unexplored, revealed, and visible"

Until now each sector rolled its own terrain (one in ten a nebula, one in twelve an asteroid field), so clouds and rocks
were scattered evenly with no shape, and the whole map was known from the first turn.

## What Changes

- **Star systems** in `data/campaign/campaign.json` (`map.systems`): each a star, an asteroid belt as a share of the ring a
  set number of sectors out, and a nebula grown beside it. Ten stars: one beside each settled world, and three with only
  belts and clouds (where Redoubt, Rook's Nest and Selene's neighbours orbit).
- **The deep** (`map.deep`): dust clouds and asteroid fields with no star, clear of every star. Ouro and Sabine, with no
  star within three sectors, are rogue planets (`Campaign.IsRogue`).
- **The fog of war**, three states: unexplored (black, nothing shown), revealed (seen once: its terrain, ports and empire as
  last seen, darker), visible (seen now: everything, other fleets included). Visible is your empire's own sectors, your
  fleet's sight (`map.sight.fleet`, 2 sectors) and friendly ships' sight (`map.sight.friendly`, 1: your faction's fleets
  and freighters, your allies'). A mission's sector is revealed by its intel when it is offered.
- **Patrols**: a mission file's `weight` (default 1) sets how often a contract is offered; Patrol weighs 4, and three new
  patrols weigh 2 each: Nebula sweep (in a nebula), Belt patrol (in an asteroid field), Orbital patrol (over a planet).
- The game's map draws the fog; the sector panel names an unexplored sector "Unexplored" and a revealed one's empire as
  last seen.

## Impact

- `src/Core/Campaign/` (`Campaign.MakeSpace`, `Visible`, `Reveal`, `IsRogue`; the map data; mission weight), the game's
  `CampaignMap.cs` and `MapHud.cs`, four mission files, the campaign data.
- Mockup 1 (`docs/mockups/game-flow.html`) still rolls terrain per sector and has no fog: tasks 2.1 and 2.2.
- The borders are unchanged: the generator still takes one draw a sector where the terrain roll was.
