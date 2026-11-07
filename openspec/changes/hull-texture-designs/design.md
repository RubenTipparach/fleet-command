# Design: hull-texture-designs

## Version 7: the Kelvane Institute's method

The owner, 2026-11-07, of version 6's renders ("this is not good texturing, how do you plan on doing normals with hard
angles?"): "exampline how the ionstitute does its hull, and replicate taht", "lets try a new dirtection". Main's bake tools
(change `kelvane-ship-concepts`) are taken whole, and version 6's own tooling is set aside (it is in the branch's history).
Each design is written in the Institute's data format, `data/ships/<faction>/hull_detail.json`:

| Part | The Institute's method, in the faction's own terms | Keys |
|---|---|---|
| Plating | A calm grid: stations square to the ship, a stepped spine plate, a belt seam, every plate corner cut on the diagonal | `cuts` "grid", `top_station_m`, `flank_station_m`, `spine_half_m`, `corner_m` 0.9 |
| Relief | Armour 0.3 m, sub-armour 0.12 m, plates lifted up to 0.05 m, gaps 0.2-0.32 m, no overlays, no greebles on the outer hull | `armour_t_m`, `lift_m`, `gap_m`, `overlay_min_m2`, `greebles_per_m2` 0 |
| Hatches | Framed hatches on a share of the big plates, with their lamps | `hatch_share` 0.4, `hatch_min_m2` 18 |
| Paint | The faction's palette; light seams; each plate's inset groove and its small marks (red triangles, stencil boxes, dot rows) | `paint` (`armour`, `under`, `plate_marks`) |
| The ship's story | Its own domes, fans, vents, bays, windows and bands, from its room plan | the ship's entry |
| Bake | 2048 px, 32 samples, `painted_bake.py --faction <faction> --ship <slug>`; `mesh` names the hull, `out` the folder | |

The Concord's C1 (mockup 24 v5) is the first. Its paint: off-white plates in seven tones, a pale steel-blue accent, light
grey seams, teal bands, the Concord emblem, red and teal marks.

## Version 6: a clean hull, large panels and sub panels, a kit of details

The owner, of version 5's test bakes: "you have to make drastic changes to improve", then a run of reference sheets
(stylised hard-surface ships, a clean off-white one "ideal for concord style", a Homeworld-like one) with notes: "hull
plating, clean hull", "more detail variety", "notice large panels, and sub panels, liveries, varying colors of armor",
"close up ship details", "vents, lights, escape hatches, missile tubes, bombe tobes", "warning markings too", "tiny holes
or slots for rivets and bolts", then "we dont need those old ww2 style rivets", "I think indents might be better", "you can
see these pill shaped holes here", "those indents I beleve are used for bolts to secrue armor panels in place", "you can
also notice subtle wear and tear, accent colors, lights and small ports", "why is there two armor plates ontop of
eachother?", "are you adding wear and tear textures before you render?". What is taken from the sheets is how such a hull
is built and marked, never their art.

| Part | Version 6 | Where |
|---|---|---|
| Plating | Large panels between the room plan's stations (merged within 7-8 m), decks and walls, each seam "major" (0.1 m Concord, 0.16 m Krai); each large panel split into sub panels at its own places (two levels, a share left whole); cut again at the hull's chines (faces turning over 35 degrees) | `panel_cuts`, `kit_split`, `chine_slabs` |
| Relief | Plates 0.3 m (Concord) and 0.55 m (Krai) thick, each raised by its own amount (up to 0.09 m and 0.16 m), every edge chamfered (0.05 m and 0.1 m), so sub panels stand at their own heights and every edge catches the light. No overlays (the owner: "why is there two armor plates ontop of eachother?") | `build_armour`, `bevel_m` |
| Colour | Plates in several tones of the faction's armour (off-whites and pale greys; the Warhost's steels), a few in an accent (copper; orange), accent modules | `palette` (`ARMOUR`, `ACCENT`, `BRONZE_SHARE`) |
| The kit | Over the plating, mirrored, kept off bends, off one another, out of the opened zones and clear of windows and bays: escape hatches and access panels set into the plating, vents, lit slots (blue; amber and red), small ports, thin grooves, tags, thruster quads, accent modules, missile and bomb tubes, each with its warning marks | `scatter_kit`, `k_*` (`kit` in data) |
| Bays | Ordered machinery in the opened zones, one module a grid cell (blocks, pipe runs, slatted boxes, tanks, lit panels, accent blocks) | `well_modules` (`well_cell_m`) |
| Bolt wells | Pill-shaped wells indented into a plate's corners (diagonal) and across its long edges, on a share of the plates; no rivet rows | `rivets` |
| Marks | Warning corner brackets and arrows by hatches, hazard stripes on launchers, chevrons by vents, bars and boxes on tags, warning bands, the ship's name and number in our own block font | `paint_decals` |
| Wear | Before the render: shallow dents and fore-and-aft scratches in the high-poly's shading (the render light and the normal map carry them). In the paint: chips on the edges and specks on the faces from a lattice hash, grime in the cavities, the story's soot and scorch | `WEAR_BUMP`, `specks`, `paint_render`, `wear` |
| Resolution | 4096 px a ship (about 2.9 cm a texel; 1536 px was 7.6 cm, too coarse for a bolt well or a mark), the maps beside the page as WebP files, published with it | `--px 4096`, `maps_dir` |
| Checking | The high-poly rendered in its palette colours before every bake (`--preview`); any piece reaching past the hull is named and left out | `preview_render`, `within_hull` |

C1 and K1 are made first on version 6, as the test of the look. The other designs (C2-C5, K2-K5) keep their traditions
and liveries and move onto it once the owner has seen C1 and K1; until then the page shows them as to come.

## How every design is made (version 3)

Version 2 drew the designs in a shader over the models. The owner: "None of this look good, they don't bring the same
level as the ssarith ships. We need visual story telling no repetitive patterns". So every design is now made as the
Ssarith Hunt's ships are (change `ssarith-ships-engine`, the `ship-detailing` skill):

| Step | What | Where |
|---|---|---|
| The hull | The game's own model's hull pieces, in its export frame (the Petrel: main hull, command block, pylons, nacelles; the Krai frigate: hull and nacelles) | `tools/mockups/glb_hull_obj.py` writes `tools/materials/raw/painted/ships/<mesh>.obj` from the glb |
| The unwrap | Each ship unwrapped on its own (Smart UV Project, islands packed again) | `painted_bake.py --ship` |
| The detail layer | Primary armour cut into plates by the faction's own cut style, each plate raised by its own amount, overlays on the big plates, a sub-armour layer, machinery in the gaps, then the story's features | `hull_detail.py`, laid out in `data/ships/<faction>/hull_detail.json` |
| The bake | Data (kind, plate id, edges), glow, position, normals (object and tangent), AO, a low sun's cast shadows, the hull lights | `painted_bake.py --detail data/ships/<faction>/hull_detail.json --ship ...` |
| The paint | The design's palette by plate, its plate colours, its liveries and markings, and its wear. Painted into a painted map and a PBR set (base colour, ORM, normals, emission) and a hull-light map | `painted_bake.py` (`plate_colours`, `livery_paint`, `wear`) |
| The page | The baked hull, its windows and bays as panes with rooms and hangars behind, its running lights, and the glb's other parts | `docs/mockups/lib/baked-hull.js`, the Hunt page's code (to fold into `lib/baked-ship.js`, tasks 4.1) |

**Cut styles (versions 3-5, since removed):** `strake` for the Concord (staggered fore-and-aft strakes) and `kit` for the
Krai (frames split and split again), then `plan` (seams only at the plan's bulkheads and decks). Version 6 replaces them
with large panels and sub panels (above).

**The story, from the room plan** (`data/ships/<faction>/<ship>_plan.json`, shared by every design of one hull):
- **Windows.** A window wherever people live or work against the hull: one row a deck at eye height, 1.6 m apart, clear
  of the bulkheads, on the side the room is on, never mirrored blindly (Ship Bible W1).
- **The Concord's features.**
  - Sensor domes over the sensor bay.
  - Cooling fans and reactor slots over the engine spaces.
  - Life support's vents.
  - The shuttle bay's door on the side the plan puts it.
- **The Krai's features.** Raked slots in the keel over the reactor, and the boarding bay's door to starboard. Nothing is
  round.
- **The paint's wear.**
  - Soot streaking aft from every vent and fan, and round the drives.
  - Scorch at the gun's muzzle.
  - The paint worn bright round each bay, and grime under it.
  - It is broken up by noise, so no two streaks match.

## Recipes from culture (version 5)

The owner, of version 4: "AGAIN all of these are BAD ... there's so much mess and mistakes, you also used a pure white on
black ... It looks like you just sketched random BLACK lines", "greebles dont look like greebles at all!", "the process is:
render high poly model, and squash that down into a texture", "these empiures SHOULD BE SYMMETRIC", and "YOU CANT JUST USE
THE CULTURE DEVELOPED FOR ONE FACTION AND SLAP THE SAME SKILLS ON TO OTHERS", "THE TECHNIQUES are a pallete that you can
reuse between different ships". Version 4 laid the Hunt's recipe on both lines. Version 5 writes each faction's recipe
from its own culture (the `ship-detailing` skill's method), and every design of a faction is a variation on its recipe.

### The Concord

The culture (survey U7, accepted): "A federation of many species that decides by consent; explores, mediates, defends";
"Clean energy and light; screens are glass lit from behind"; the Ship Bible's "straight strakes and ring bands", "pearl
armour, blue bands, warm-lit windows"; the Concord shows its broad face where the Krai show their edge.

| Principle | What it means for a hull |
|---|---|
| CP1 Open and calm | A ship that arrives to talk shows one continuous face: flush plating, not armour piled on armour |
| CP2 Order by consent | Everything on a shared grid: plates on the frames and decks, bands on the bulkheads, all mirrored |
| CP3 Light as material | The ship shows itself by light: lit windows, glass, lit lines, floods |
| CP4 Many makers, one standard | Plates differ by a tone, never by a patchwork; one standard of finish |
| CP5 Kept with care | Machinery behind flush hatches, shown only where it is serviced (the service band, the keel); wear light and clean |

| Technique | Setting | From |
|---|---|---|
| Hull form | The Concord line's lofted hull | |
| Plating | Strakes on frames: butts on a 2.5 m frame, plates two to five frames long, strakes 1.8-2.8 m, ring seams at the bands | CP2 |
| Relief | Flush skin: plates 0.12 m thick, barely proud (0.02 m), seams 6-8 cm, no trenches | CP1 |
| Exposed machinery | The service band low on each flank and a keel trench only | CP5 |
| Fasteners and frames | None seen | CP1 |
| Features | Glass domes over the sensors, fans with white lamps, plain slots; windows wherever people live, mirrored | CP3, CP2 |
| Paint | Pearl, a tone apart plate to plate; the design's bands, sash or sections | CP4 |
| Wear | Light: soot only near the drives and vents, scuffs at the bays | CP5 |
| Light | The render (sky, key and bounce); the gaps a pale grey-blue, never black | rule 3, 4 |

### The Krai Warhost

The culture (the owner's words and U7): "a combat focused race ... strong metal framing, almost blade like"; "rough gun
metal, maybe sort of akin to volcanic magma type metal"; "this faction believes strongly in tradition"; "an emperor cheif
... a star ruling over the 3 great houses"; "War is the only honest test; rank is won in blood"; tradition "kept, carried
and added to, never replaced"; the Krai show their edge.

| Principle | What it means for a hull |
|---|---|
| KP1 Forged, not assembled | Few, massive plates, thick, their edges chamfered wide, a hammered skin |
| KP2 Edge first | The ship reads by its edges: chines and ridges ground bright, every plate edged |
| KP3 Ranked and heraldic | Strict mirror symmetry; frames at a martial cadence down the flanks; the crest on the keel line |
| KP4 Kept and added to | Marks added over time (tallies, honours), never painted over |
| KP5 The heart is guarded | Machinery shows only at the engines and the reactor, in deep vented trenches |

| Technique | Setting | From |
|---|---|---|
| Hull form | The Krai line's lofted hull | |
| Plating | Split panels in big blocks (frames 9-13 m, panels at least 2.4 m), corners cut at 45 degrees | KP1 |
| Relief | Layered and heavy: plates 0.5 m thick, proud up to 0.3 m, overlays on the big plates | KP1 |
| Exposed machinery | The reactor's keel trench and the engine trenches aft only | KP5 |
| Fasteners and frames | Raised frames at a fixed cadence down the flanks, bolt rows along them (to build) | KP3 |
| Features | Raked slots and chevron vents, no domes or fans (nothing round) | KP1 |
| Paint | The design's steel, edges picked out bright | KP2 |
| Marks | Tallies in fives, honours | KP4 |
| Light | The render; the gaps a dark steel, never black | rule 3, 4 |

## Rules every design keeps

All of these are already decided:
- **Tradition, not the body.** Each design names the tradition it comes from (CLAUDE.md section 1; Ship Bible C1-C5).
  None takes a shape, pattern or texture from what a species looks like.
- **The hull complements the console; it does not copy it** (Ship Bible X1-X3). Each design shows all three of X3:
  distinct features, a colour scheme (field, stripe, light) and its own paneling.
- **Markings** (Ship Bible chapter 10):
  - one field colour per faction;
  - stripes on bulkhead lines, never across a window row;
  - the registry floodlit;
  - one crest a ship;
  - red only for danger or rescue on the Concord.
- **The Concord's emblem** (B-E27) appears once, on the port side, upright on the forward band, never rotated or
  outlined (owner, faction-emblems).
- **The Krai's tallies** count in fives, aft (mockup 7).
- **Nothing repeats** (T1, T7). The joints never line up (T4).

## Concord: five designs

The culture, as accepted (survey U7, change `console-skins`): "A federation of many species that decides by consent;
explores, mediates, defends"; "Clean energy and light; screens are glass lit from behind". The palette's meanings are
provisional:

| Colour | Hex | Meaning |
|---|---|---|
| Deep | `#1b2a6b` | the shared night sky |
| Signal | `#2f62e0` | trust, the flag |
| Sky | `#3fa9f0` | |
| Accord | `#2fbf5a` | "agreed" |
| Lilac | `#b7a6f2` | the many species, mixed |
| Alert | `#e8352b` | red alert only |
| Pearl | `#e4e2dc` | the hull |

Each design takes one of the three things the Concord does, or the technology it is proud of.

| # | Design | Tradition it comes from | What the hull shows | Palette |
|---|---|---|---|---|
| C1 | **Survey white** | The explorers, the Survey Service: a ship that goes first and must be seen and known | Pearl armour in long fore-and-aft strakes (5-7 m by 1.4-2 m, each a shade of its own); a grey subhull below the chine; Signal ring bands on bulkhead lines with Deep pin seams; the registry big and floodlit; small access hatches on every few strakes. Working density: only what a survey ship needs | Pearl `#e4e2dc`, subhull `#8a9098`, Signal `#2f62e0`, Deep `#1b2a6b` |
| C2 | **Many yards** | The member worlds' yards: a federation that decides by consent builds a ship by consent, a section from each world | The hull in four sections, bow to stern, each in its own yard's white and its own plating (long strakes; square panels; staggered short plates; ring-by-ring). The sections are joined by narrow Accord-green treaty seams (green is "agreed"). A small yard mark in each section's corner | Pearl `#e4e2dc`, bone `#ddd6c6`, cool white `#dfe5ea`, sand `#e2d9c8`, Accord `#2fbf5a`, Deep seams |
| C3 | **Envoy sash** | The mediators: the Concord's envoys, whose ships arrive to talk, ceremonial density | Broad, quiet plates (fewer seams, larger panels); a wide sash along the waterline, lilac fading into sky (the many species, mixed), edged with a Deep pin line; the emblem's band a little broader. Nothing aggressive, no hazard marks on the flanks | Pearl `#e4e2dc`, Lilac `#b7a6f2`, Sky `#3fa9f0`, Deep `#1b2a6b` |
| C4 | **Lightwork** | The lightwrights: clean energy and light, their screens glass lit from behind | Pearl plates divided by recessed light channels running with the strakes. By day the channels are fine pale-blue inlays; at night they glow faint Sky, and the registry is back-lit, not floodlit. The ship lit from within like its own consoles, in the hull's own terms (lines in the plating, not the console's sweeps) | Pearl `#e4e2dc`, inlay `#a9c9e8`, glow Sky `#3fa9f0`, Deep seams |
| C5 | **Home Guard** | The defenders, the Home Guard's yards: ships that patrol, take hits and are patched in the field | Two-tone: a darker grey subhull rising to the chine, armour a warmer, duller pearl. Field repairs as plates of other ages (whiter new plates, greyer old ones). Wear follows use: soot aft of the drives, scuffs round the hatches. Service markings only where needed: KEEP CLEAR in red at the hatches (red for danger only), a big unit number | Armour `#d3d2cc`, subhull `#6f7782`, Signal `#2f62e0`, Alert `#e8352b` (danger marks only) |

## Krai: five designs

The culture, from the owner and as accepted (survey U7, changes `console-skins`, `faction-emblems`):
- "a combat focused race ... angry red colors and orange lit buttons ... strong metal framing, almost blade like";
- "rough gun metal, maybe sort of akin to volcanic magma type metal";
- "this faction believes strongly in tradition";
- "an emperor cheif ... a star ruling over the 3 great houses";
- "War is the only honest test; rank is won in blood";
- tradition "kept, carried and added to, never replaced".

The hull today:

| Colour | Hex |
|---|---|
| Blued steel | `#56637a`, `#68768d` |
| Bronze | `#b98a4e` |
| Crimson war stripe | `#7a1e2c` |
| Floods (warm orange) | `#ff8a3c` |
| Blood | `#b3121b` |
| Bone | `#e8dccb` |

| # | Design | Tradition it comes from | What the hull shows | Palette |
|---|---|---|---|---|
| K1 | **Tempered blade** | The smiths: a Krai ship is forged and quenched like a blade, and wears the colours the temper leaves | Blued steel on the broad faces running to straw and bronze toward the chines and edges, as a quenched blade's colours run; plates square to the hull with 45 degree corners (the kit style the owner kept); a fine hammer mottle; the crimson war stripe and tallies kept | Blue `#3a4a6a`, steel `#56637a`, straw `#c9a25a`, bronze `#b98a4e`, crimson `#7a1e2c` |
| K2 | **House banner** | The three great houses under the emperor: a ship flies its house | Blackened steel flanks; the dorsal crest, the keel and the bow edges in the house's field; the emperor's eight-point forge star (from the KC emblem) once, on the crest. Three provisional house fields (blood, bone, bronze) until the owner names the houses and their colours | Blackened `#22201f`, house blood `#b3121b` / bone `#e8dccb` / bronze `#b98a4e`, steel `#c9d0d8` |
| K3 | **Blood record** | Rank won in blood, and tradition added to: a ship's record is cut into its plates | Blued steel plates; every seam an engraved channel inlaid with blood enamel, so the plate pattern itself is drawn in red; the tally rows along the war stripe; plates that have earned an honour edged in bronze. At night the inlays catch the orange floods | Steel `#56637a`, plate `#68768d`, blood `#b3121b`, bronze `#b98a4e` |
| K4 | **Blackened war plate** | The war forge, edge first: the owner's "rough gun metal"; the edge is what a blade shows | Near-black oxidised steel, rough; every chine, ridge and edge ground bright, so the ship reads as its edges; the seams show a dim ember line at night. Closest to the console (its forge glow), so the exterior risk is X1's: it complements the console only if the glow stays in the seams | Black steel `#1c1a1b`, ground edge `#c9d0d8`, ember `#ff5a1a` (night), crimson `#7a1e2c` |
| K5 | **Kept plate** | "Kept, carried and added to, never replaced": a ship carries the plates of the ships before it | A patchwork of generations. The oldest plates, bronze gone brown-green, sit at the core (keel, bridge, crest); blued steel round them; the newest bright steel at the edges and the bow, where a ship grows. Each generation its own plate size and seam width; the tallies on the oldest plate | Old bronze `#8a6a3e`, verdigris `#5f6b4a`, blued `#4c5a72`, new steel `#9aa4b0`, crimson `#7a1e2c` |

## The page

| What | How |
|---|---|
| Ships | The two glbs inlined (`tools/mockups/inline_data.py`) and parsed by three.js's GLTFLoader, their hull pieces hidden; each design's baked hull, its panes and its running lights drawn by `lib/baked-hull.js` from the inlined bake manifests (`assets/ships/<faction>/baked/manifest.json`), lined up on the glb's hull by their bounding boxes |
| Views | One design at a time (orbit, quarter, side, top, ahead, below, close on the bay), or all five side by side (one camera); Painted and PBR looks; studio light, and night (the ship lit by its own floods, lamps and windows) |
| Notes | Each design's card: its tradition, why, what it shows, its palette swatches |
| Shots | Each design in studio and night, each faction's five side by side, at desktop and phone sizes |

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| 1 | Which design for each faction, or a mix (for example C1's strakes with C4's light channels) | None picked: the owner chooses from the page |
| 2 | One design for every class, or by role (C3 for flagships, C5 for patrol, K2 by house) | One per faction, with the class's marking density varying (the superseded `concord-ship-look`'s ceremonial, working, minimal) |
| 3 | C2's member yards: is a ship really built section by section by different worlds? | A reading of "decides by consent", not set. The owner's (U7) |
| 4 | K2: the three great houses' names and colours | Unnamed; blood, bone and bronze as placeholders |
| 5 | K3: what earns an honour plate | Placeholder; marks of kills are the owner's (mockup 7) |
| 6 | K4 shares the console's forge glow (Ship Bible X1) | Kept to the seams at night only; drop it if it reads as the console |
