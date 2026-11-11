# Tasks: freighters

## 1. The write-up and the board (this change)

- [x] 1.1 Proposal, design and spec delta, citing the SFC3 manual's printed pages 37 and 42
- [x] 1.2 Mockup 24, the freighter board (`docs/mockups/freighter-concepts.html`): eight grown hulls, two for each approved
      faction, each naming its tradition, every one carrying the same container in sight, at true scale beside the Petrel
      and a person, measured; on `mockup-work.md` and the tracker
- [x] 1.3 Survey section V (V1-V5) in `docs/design/survey.html`
- [x] 1.4 The owner's picks (survey V2: 1, 4, 5 and 8, 8 made a loop) and the size (V5: twice the draft)
- [x] 1.5 Mockup 24 v6: the four picks as the line, twice the size, the Twin ledger's loop; the board beside them
- [ ] 1.6 The traditions kept (survey V1)

## 2. The picks (approved for build, owner 2026-11-07: "Now I approve. Detail them")

- [ ] 2.1 Each pick grown again at its size on its faction's own section (the Institute's `ksec`, the Hunt's `hsec`, the
      Concord's octagonal loft); the container's size moved into `data/` for the page and the game
- [x] 2.2 Detailed and baked onto its own UVs (`ship-detailing`, `painted-bake`): plates, windows a person tall on the
      lived-in decks, the faction emblem, hull lights (v12: 2048 px, 32 samples, `tools/blender/painted_bake.py --faction
      freighters`, layout `data/ships/freighters/hull_detail.json`, maps in `assets/ships/freighters/`)
- [ ] 2.2b The Twin ledger's bow: a thin seam shows down its centre where the two arms meet
- [x] 2.3 Exported by `tools/mockups/export_ships.mjs` (approved for implementation, owner 2026-11-07: "implement in game"):
      the page builds its picks on `docs/mockups/lib/baked-fleet.js`, each glb with its drive, reactor, lamps and breakup, its
      containers written as `cargo` in `assets/ships/manifest.json` (`window.MOCKUP_CARGO`); the container's low box by
      `tools/blender/build_container.py --glb`
- [ ] 2.3b The holds as damage zones (each container stack a breakable piece)
- [x] 2.2a The baker's loader: a ship names its faction's look (`load_detail` in `tools/blender/painted_bake.py`), a look
      `from` a faction's own hull_detail.json, so the Hunt's and the Institute's looks have one copy; the Concord's and
      Krai's written in the freighters' file until their lines move onto the standard
- [x] 2.5 The container as an asset (owner: "lookem up online for cc0 options, back ao and get diffuse and normals and
      specular for each container"): `tools/textures/fetch_cc0.py` fetches Poly Haven's CC0 "Container Side" set,
      `tools/blender/build_container.py` models the container (frame, castings, door bars, hinges) and bakes diffuse,
      normals, AO, roughness and specular onto its box (`assets/containers/`, sources in `SOURCES.md`); the page draws it
- [ ] 2.4 One cradle: the board's `cradle` and the Institute page's `loopOf` become one function in
      `docs/mockups/lib/hull-grow.js`, inlined by both pages (design, "One copy")

## 3. The game (after approval for implementation, a separate request)

- [x] 3.1 A freighter battle class for each approved faction in `data/battle/classes.json` (`fr`, `krai_fr`, `hunt_fr`,
      `kelvane_fr`), with its model and its containers (`src/Game/View/Cargo.cs`), on every damage range; each campaign
      faction names its freighter hull (`factions.<id>.freighter`), the Vos keeping the `kff` stand-in
- [x] 3.2 The campaign map draws each faction's freighter in place of the placeholder (`CampaignMap.cs`; the mockup's own
      `game-flow.html` is a sync task, `openspec/changes/freighter-missions` 3.1)
- [x] 3.3 Convoy defense, Convoy ambush and Distress field the faction's own freighters (`hull: "freighter"`)
- [ ] 3.3b A playtest of each from both sides
- [x] 3.4 The Kelvane freighter in the campaign: the Twin ledger (`freighter_kelvane`, battle class `kelvane_fr`) as the
      Institute's `freighter`, on its Lyceum to Lyceum Depot route
