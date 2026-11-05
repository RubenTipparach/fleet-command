# Mockup 1: Game Flow

- Link: https://claude.ai/artifact/UBbAouwAU3JvcbGf6MhzLt
- Shows: the main menu (options, quit), skirmish setup (scenarios, point budget, fleet, refit by
  hardpoint with arcs, supplies, wingman behaviour, variants), a stand-in battle and its results, and the
  campaign: the hex map in perspective with pan, zoom and standees, movement, End turn, the shipyard,
  outfitter and recruiting center at friendly ports, mission markers, briefings, Deploy and the return to
  the map. Desktop, phone landscape and phone portrait.
- Made by: `docs/mockups/game-flow.html` (the page source, one copy), written up in
  `openspec/changes/game-flow/`. Shots: `docs/screenshots/mockups/game-flow-*.png`, from
  `tools/mockups/shoot.mjs` then `tools/mockups/shrink_png.py`.
- Stage: drafted. Approvals are given in the mockup tracker.
- Published: 2026-10-30, version 1; version 2 (UMD three.js so it renders on the artifact host); version 3 (bordered hexes, 3D miniatures, belts and nebulae, sector numbers, market and standing); version 4 (Star Crew nebula sky and streaking parallax stars, a 30 x 22 map, routes traced as lines, faction-space tooltip, no edge tilt, nothing hidden at far zoom); version 5 (no sector numbers, port sectors lit, a glass board with a planar mirror and a sky reflection probe, puff nebulae in several colours, the top bar docked to the screen with standing named by faction, click a ship to go there, every stand's hex ringed the board's way round).
- Account move: Republished 2026-10-31 on the owner's current account (the earlier links were made from another account and cannot be read from here); every link updated.
- 2026-10-31, republished from the desktop session (owner: "Make sure all published pages link back to the master", commit 7200539): the page links back to the mockup tracker; version id `1793462716-81fa`.
- Version 6 published 2026-11-03, version id `1793700035-b9e5` (from the Concord ships thread, on top of live version `1793462716-81fa`): the Concord ships on the menu, the campaign map's standees and the stand-in battle are the Concord line's (mockup 6).
- Version 7 published 2026-11-03, version id `1793702168-f28d` (Concord ships thread): the Albatross standees and ships hang their pylons from the main hull.
- Version 8 published 2026-11-05, version id `1793863638-eec3` (desktop session, changes `faction-select` and `campaign-missions`): the faction screen (the three finished emblems on wooden pedestals, lamp, dust and hover turn, continue or new campaign); the campaign drawn in the chosen faction's skin by `docs/mockups/lib/kitcss.js`; one smallest-tier ship to start; the fleet flying its route; missions as scripts (`docs/mockups/lib/campaign-missions.js`) with accept, a mission log and a story chain; the Krai as the Concord's rival and the Ssarith Hunt's space; the game's scenarios as skirmish test maps.
