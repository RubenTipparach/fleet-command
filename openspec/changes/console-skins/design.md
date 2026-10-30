# Design: console-skins

Mockup 4. Three faction design languages for one UI kit. Our own designs (CLAUDE.md section 1: take the shape, not the
art); the reference games' skins (SFC2 manual printed pages 108 onward show its console) and LCARS set direction only.

## The owner's rules for every skin

The owner, 2026-10-30, on version 1 of this page: "this ui language thing, it just different colors, not very
interesting". Then:

1. **No skin uses the same button shape.**
2. **Every skin has a palette inspired by its faction's ideology.**
3. **Elements and icons are inspired by how the faction thinks**: each skin uses different shapes, lighting and
   shading, and icon shapes shaped by the faction's world view, body shape, technology and ships.

And three methods, one per skin: Concord from research on LCARS with a palette sampled from Starfleet Command and
Starfleet Academy; Kethran modelled in Blender "like starcraft"; Vos "a hand drawn aesthetic of the mirak ui".

## The factions, as the skins read them (provisional, survey U7)

The game has no written lore yet, so the skins need a short, working account of each faction. These are ours, and
the survey asks the owner to correct them.

| | Concord | Kethran Hegemony | Vos Syndicate |
|---|---|---|---|
| World view | A federation of many species that decides by consent; explores, mediates, defends | An empire of rank and conquest: order is strength, the strong lead | Free clans of raiders: luck, loot, loyalty to the crew, nothing else |
| Bodies | Every shape of hand, claw and tentacle: controls must suit all of them | Heavy, armoured, thick-clawed: big keys with a long, firm travel | Lanky, long-fingered, many-jointed, tattooed: quick flicks and gestures |
| Technology | Clean energy and light; screens are glass lit from behind | Fusion, foundries, armour plate; machines are proud of their mass | Salvage, patched and grown over; nothing matches, everything works |
| Ships | Saucers and slender nacelles, long smooth sweeps | Armoured wedges and blades, stepped plates, forward prows | Scavenged hulls under spikes, mandible prows, swirling paint |

## The three languages

| | Concord | Kethran Hegemony | Vos Syndicate |
|---|---|---|---|
| **Method** | Flat vector, back-lit, built on LCARS's principles | Hard-surface models, lit and rendered in Blender | Hand-drawn: ink, wash and hatching, drawn by a script |
| **Button shape** | **Pill**: a round-capped bar segment; where a frame ends, its cap is the button | **Trapezoid wedge keycap**: raised, chamfered, wider at the top, a plain face (owner: "buttons should be more plain"); lighter steel on hover, sinks and darkens when pressed | **Claw tag**: a lopsided hand-cut tag with one hooked claw corner and an uneven ink edge |
| **Frame** | The sweep: an elbow joining a thick vertical bar to a thin horizontal one; never the same thickness after a turn | A heavy bolted bezel with chamfered corners around recessed dark glass, conduits along the edges | A wobbly inked border over a stained wash, a spiral or a knot in each corner |
| **Light and shading** | None painted: flat colour that glows, on black | Harsh key light from the top left, rim light, deep occlusion, spec on bevels, emissive lenses | Drawn: hatching on the shadow side, a dry-brush highlight, a little ink bleed |
| **Icons** | Open circles and arcs in an even thin line; nothing pointed (from the saucer and the circle of species) | Solid wedges, chevrons and blades pointing forward (from the prow); stencilled | Brush-drawn spirals, claws, eyes and teeth, a little crooked (from tattoos and trophies) |
| **Type** | A tall condensed sans in capitals, right-aligned labels (Antonio) | A squared stencil face (Saira Stencil One) | A hand-lettered marker face (Permanent Marker) over a hand-written body (Patrick Hand) |
| **Motion** | Snappy fades, at most 150 ms; nothing moves that a hand was reaching for | A mechanical press: the key travels 2 px with a hard stop | A wobble: a pressed tag tilts and springs back |
| **Sound (U6)** | Soft chirps | Heavy clunks and servo whines | Rattles, a bell, a laugh |

### Concord: LCARS's principles, Starfleet's palette

**The research** (the owner's four links). LCARS was designed by Michael Okuda for Star Trek's ship interfaces; Gene
Roddenberry asked that panels not be busy, so the look is minimal to read as far beyond our own technology
(Wikipedia). Bracer Jack's *LCARS Manifesto* and *Creating a Coherent LCARS Interface* (lcars-terminal.de) set out the
principles we adopt, in our words:

- **The sweep is the language.** One shape, an elbow that joins a thick bar to a thin one, frames every group of
  controls. Frames always change thickness at a turn, thick to thin or thin to thick; never the same.
- **The rounded cap ends a bar**, and because it ends something it reads naturally as a button. Buttons are
  round-ended rectangles; when a label is too long the button grows a flat side and keeps one cap.
- **Break the frame into buttons** rather than adding buttons inside it, unless a segment is too thin to touch. A
  frame that only frames is waste. Buttons in one frame all look alike.
- **Flat and vector.** No bevels or emboss on the frames; gradients only where they do not distract.
- **Colour with meaning, five at most.** Each colour is assigned a role. Two text colours: normal and highlight.
- **Three type sizes**: title, sub-header, data. One typeface.
- **Spacing on two constants**: the gap between frames and the gap inside a frame.
- **The interface never rearranges itself under the hand.** A new task is a new screen; transitions are quick fades
  under about 150 ms, not cross-fades.

These match rules we already have (CLAUDE.md section 7: fixed panels, no layout shifting). LCARS itself is a
studio's copyrighted design (Wikipedia: CBS claims copyright and has sent takedowns), so Concord takes the principles
and none of its artwork, its typeface, its numbers decor or its colour set.

**The palette**, sampled by k-means from the reference games' screens (saturated pixels of the console columns and
strips of Starfleet Command's Federation console, and of Starfleet Academy's HUD; `tools/mockups/` script in the
change's tasks; the screenshots are not committed). Sampled values, then the Concord colour each one became:

| Sampled | Source | Concord colour | Role (one meaning each) | Ideology |
|---|---|---|---|---|
| `#141d50`, `#273579` | SFC frames, idle | **Deep** `#1b2a6b` | Idle frames, unlit segments | Calm, the night sky shared by every homeworld |
| `#064ed3`, `#2d26f4` | SFC frames, SFA HUD | **Signal** `#2f62e0` | Live frames and buttons | Trust; the colour of the Concord's flag |
| `#1797e0`, `#0570ac` | SFC readouts | **Sky** `#3fa9f0` | Readouts, data, real-time values | Clarity, open information |
| `#0d9c3c`, `#03a004` | SFC and SFA ready lights | **Accord** `#2fbf5a` | Ready, armed, consent given | Consent: green is "agreed" |
| `#ae97ec` | SFC highlights | **Lilac** `#b7a6f2` | Highlighted text | The many species, mixed |
| `#97160f`, `#f40e0e` | SFC and SFA alerts | **Alert** `#e8352b` | Red alert only, never decoration | Danger, used sparingly |

Text: white `#eef3ff` normal, Lilac highlight. Background: true black.

### Kethran Hegemony: modelled in Blender

Every piece is a hard-surface model rendered face on in Cycles (`tools/blender/build_ui_kethran.py`, writing
`assets/ui/kethran/` and its `manifest.json` of sizes and margins). Gunmetal plates bolted in layers, chamfers that
catch a harsh top light, recessed dark glass, conduits along the edges, plain trapezoid wedge keycaps that sink when pressed (owner, 2026-10-30, on version 2:
"buttons should be more plain. BUT YES YOU ARE GETTINGIT"), a caged alert lamp, and an iron crest of a prow between two blade chevrons with a red
enamel inlay. Hazard notches are red and black, not yellow and black.

| Colour | Hex | Role | Ideology |
|---|---|---|---|
| Gunmetal | `#2b2f33` | Plate | Iron: the empire's material and its patience |
| Iron | `#4a4f55` | Raised plate, bezels | Mass and rank |
| Oxide | `#9e2a1e` | Painted plates | Old blood, old wars |
| Signal red | `#d9483b` | Rank stripes, alerts, the faction | Blood and command |
| Ember | `#ffb547` | Lenses, readouts | The foundry's fire |
| Bone | `#d8cfbf` | Stencilled text | Trophies of the defeated |

### Vos Syndicate: hand-drawn

After the reference game's pirate skin (the owner's favourite: "so playful and pirate like"), as if each console were
inked and painted by its crew: wobbly ink outlines, a stained wash fill, hatching on the shadow side, a dry-brush
highlight, spirals and knots in the corners, lopsided claw-tag buttons, a gauge drawn as an eye. Every piece is drawn
by a committed script with a seeded wobble, so it is the same every time and never hand-painted.

| Colour | Hex | Role | Ideology |
|---|---|---|---|
| Ink | `#2a1410` | Outlines, hatching | The crew's tattoos |
| Rust | `#a8482a` | Frames, plates | Salvage: everything they own was someone's |
| Brass | `#d6a043` | Buttons, highlights | Loot and luck |
| Verdigris | `#3fb7a3` | Lenses, readouts | The sea-green of old coin; the spoils kept |
| Bone | `#efe0bd` | Paper, text | Trophies and the charts they steal |
| Blood | `#d23a4a` | Alerts | A warning, once |

## Five more races (version 4, first pass)

The owner, 2026-10-30, with screenshots of StarCraft II's Zerg console, StarCraft's Protoss console and Starfleet
Command's Klingon, Romulan and Gorn skins: "Some more UI design ideas: an organic alien race, lots of leather shining
through light, slimy carapace, tendrils; an wise and advanced alien race. holographic buttons, with organic shaped metal
armor peices surrounding the edges; a powerful ancient race. rusty, with back lit buttons clogged with dirt. their ships
havent been upgraded in ages, rough around the edges; a combat focused race, lots of triangular motifs, angry red colors
and orange lit buttons that are activated. strong metal framing, almost blade like; an enigmatic alien race with cool
dark purple colors and pink buttons, progress bars are curcular, almost organic like eyes. this is all first pass stuff,
really love how you did the hand drawn one".

Each is a skin module (`docs/mockups/skins/<id>.js`, the contract in `docs/mockups/skins/README.md`) on the same kit,
under the same three rules. Names are ours and provisional (survey U8). Whether these races join the campaign map, and
whether the combat race is the Kethran Hegemony's own direction, is U8 too.

| | Vhorr Brood | Aelith Conclave | Tarsine Remnant | Krai Warhost | Ombrai |
|---|---|---|---|---|---|
| Owner's words | "organic ... leather shining through light, slimy carapace, tendrils" | "wise and advanced ... holographic buttons, organic shaped metal armor" | "powerful ancient ... rusty, back lit buttons clogged with dirt ... rough around the edges" | "combat focused ... triangular motifs, angry red ... orange lit buttons ... blade like" | "enigmatic ... dark purple ... pink buttons, progress bars are circular ... like eyes" |
| World view | The brood is one body; it grows, consumes and adapts | Patient knowledge kept over aeons; act rarely, act exactly | A fallen empire that remembers being first and will not change | War is the only honest test; rank is won in blood | Hidden purpose; they watch, and are seldom seen |
| Bodies and technology | Grown, not built: hide, chitin, membranes, nerve tendrils | Graceful bodies, psionic minds; light shaped by thought in sculpted alloy shells | Huge, slow, long-lived; machines older than the crew, patched and never replaced | Lean predators; forged steel, edged everything | Shadowy, many-eyed; technology that looks back at you |
| Ships | Living hulls of carapace plates over glowing flesh | Smooth golden shells around crystal cores | Massive, scarred, rusted dreadnoughts | Knife-blade hulls, swept triangles | Dark, curved, petal-like hulls with glowing eyes |
| Method | Painted organic material: leather lit from behind, wet carapace, tendrils | Hologram light inside sculpted pearl and gold armour | Corroded iron, pitted and stained; buttons lit through grime | Black blade steel with red, lit orange when engaged | Dark violet glass, pink light, concentric rings |
| Button shape | **Blister pod**: a swollen, lopsided membrane lit from within | **Holo lozenge**: a pointed-ended light shape held between two armour claws | **Arched tablet**: round top, flat foot, a backlit window caked in dirt | **Blade**: a parallelogram that ends in a point | **Crescent bar**: one end bitten concave, the other round |
| Progress and gauges | Veins that fill | Thin light lines | Needle gauges behind cracked glass | Segmented triangles | Circular, like eyes (the owner's words) |

## The kit

Every screen draws only these pieces, by name (CLAUDE.md section 7). A skin supplies one image (or image set) per
piece and its own slice margins; a missing piece is a build error. Icons join the kit, one set per skin.

| Piece | Kind | Slot at 1920 x 1080 (px) | Used by |
|---|---|---|---|
| `frame.panel` | 9-slice | any, min 96 x 96 | every console panel, port panels, sheets |
| `frame.inset` | 9-slice | any, min 48 x 48 | readout wells, lists, the schematic's well |
| `bar.track` | 9-slice | any width x 16 | hull, capacitor, speed, power bars |
| `bar.fill` | 9-slice | any width x 10 | the bars' fills |
| `button` | 9-slice, 3 states | min 44 x 44 | every key: idle, hover, pressed |
| `tab` | 9-slice, 2 states | min 40 high | tab rows, the arming states |
| `strip.top`, `strip.bottom` | 9-slice | full width x 56 | the top and bottom strips |
| `divider` | 9-slice | any x 10 | between stacked panels |
| `nameplate` | hero | 460 x 56 | the captain's plate |
| `helm.pad` | hero | 300 x 300 | the helm's 3 x 3 pad |
| `ssd.bezel` | hero | 460 x 300 | the ship systems display's frame |
| `dial` | hero | 120 x 120 | radar and gauges, needle drawn live |
| `lamp.alert` | hero, 3 states | 64 x 64 | condition green, yellow, red |
| `crest` | hero | 96 x 96 | the faction's mark |
| `icon.*` | hero, per skin | 48 x 48 | helm, fire, shields, tractor, transport, science, comms, repair, map, settings |

The margins differ by skin (a pill's caps, a wedge's slants and a claw tag's hook take different widths), so each skin's
manifest records its own; the layout never depends on them.

## How each skin is made

- **Concord**: vector shapes in code (canvas), rendered at 2x to images with their margins. No Blender: LCARS is flat.
- **Kethran**: `tools/blender/build_ui_kethran.py`, the CSG way (`blender-hard-surface`, `panel-textures`), Cycles,
  face on, 2x; a contact sheet shows each 9-slice piece stretched three ways.
- **Vos**: a seeded hand-drawn painter (ink line with wobble and pressure, wash, hatching) rendering each piece at 2x.

Budget per skin, provisional: about 40 images, at most 2048 x 2048 of atlas, a few megabytes, the same on phones.

## The page

One console (mockup 2's layout) drawn from kit pieces, with a live skin switch; each skin's sheet (its world view,
palette with roles, button in three states beside the other skins' buttons, icon set, type, frames stretched three
ways, hero pieces); a side-by-side of the three buttons and icon sets to prove rule 1 and rule 3.

## Open questions

| Id | Question | Recommendation (built in the mockup) |
|---|---|---|
| U4 | When does the player see another faction's skin? | Both: a captured ship keeps its builder's console, and Options can pin one (provisional) |
| U5 | The Kethran skin's direction? | Answered by the owner: modelled in Blender, "like starcraft" |
| U6 | Does a skin change only images, or also sounds and type? | Images, colours, type, icons, motion and button sounds (provisional) |
| U8 | The five new races: names, whether they join the campaign, and whether the combat race is the Kethran Hegemony's own direction? | New, minor factions with the names above; the combat race stays its own race and the Kethran keep their look (provisional) |
| U7 | Are the factions' world views, bodies, technology and ships as the skins read them? | As written above (provisional) |
