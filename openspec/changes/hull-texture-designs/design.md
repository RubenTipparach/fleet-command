# Design: hull-texture-designs

## The pitch, and how a picked design gets made

Mockup 23 (`docs/mockups/hull-texture-designs.html`) shows two frigates, the very models the game flies:
- the Concord's Petrel (`assets/ships/concord/petrel_level.glb`, 88.8 m);
- the Krai frigate (`assets/ships/krai/frigate.glb`, 110.4 m).

Each is shown five ways. A design is drawn by a shader over the ship's own geometry, in ship space (+x port, +y up, +z
bow, metres). That lets ten designs share one page, and nothing is baked yet. Each design works out:
- which plate a point is on;
- the seams, panel lines and hatches as normals (so they catch the light) and as paint;
- the plate's own colour;
- the liveries and markings;
- the night glow.

Plate layouts are laid per row with their own jitter, so no two rows' joints line up into brickwork (T4) and no stretch
repeats (T1, T7). Everything is mirrored about the centreline (T7).

A picked design is then made the binding way, as the Ssarith Hunt was (change `ssarith-ships-engine`):
1. The ship is unwrapped on its own.
2. A modelled detail layer is laid out in `data/ships/<faction>/hull_detail.json` (primary plates cut to the design's
   layout, sub-armour, machinery in the trenches, vents, lamps).
3. It is baked selected-to-active by `tools/blender/painted_bake.py --ship` and painted into the design's palette:
   base colour, ORM, tangent normals, emission and a hull-light map.
4. It is exported to the game.

The shader in this page is a pitch instrument, not a second implementation. Its layouts and palettes become the
`hull_detail.json` and paint settings of the bake.

The Concord's "too much like primitives" (owner, 2026-11-06) is answered twice:
- now, by each design's plates, seams and hatches, which break up the smooth octagon faces;
- after the pick, by the modelled detail layer (T5, T6): plates standing proud by their own amounts, access hatches,
  vents in holes cut for them, the service band's pipe and clamps as real geometry.

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
| Ships | The two glbs inlined (`tools/mockups/inline_data.py`) and parsed by three.js's GLTFLoader. The hull materials are swapped for the design's (Concord `paint_*`, Krai `hull`); everything else (glows, bells, metals, registry) as exported |
| Views | One design at a time (orbit, quarter, side, top, ahead), or all five side by side (five views of the same ship, one per design, the same camera). Studio light, and night (a dim key, the design's own glows) |
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
