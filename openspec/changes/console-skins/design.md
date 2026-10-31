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
| **Motion** | Snappy fades, at most 150 ms; nothing moves that a hand was reaching for | A mechanical press: the cap sinks into its socket; the frame never moves | A struck tag stays put; its ink darkens and its letters dip (no key moves, owner: "Vos buttons are still being rotated when I click") |
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

**More pirate factions may come** (owner, 2026-10-31: "Yea we might have more pirate factions. That's cool", on
Krita painting the Vos skin). So the hand-drawn method is built to be reused, not tied to the Vos: Krita's brush
engines driven by script (`krita-brushwork`, `tools/krita/brush.py`) are the shared painter, and each pirate faction
would be its own skin module and design language on it (its own brushes, palette, button shape and icons; the three
rules still hold: no two skins share a button shape). None is designed yet.

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


A sixth (owner, 2026-10-30, with the Nucleo NLog Winamp skin: "the old science, nucleo nlog skin from winamp. UI love this
old osciloscope aesthetic from science labs. Some race will value thier anolog physics instruments and design their ui
vbased on this"):

| | Kelvane Institute |
|---|---|
| World view | Truth is measured, never assumed; every reading is shown on an instrument a scientist can check by eye |
| Bodies and technology | Careful, many-handed experimenters; cream-enamelled lab instruments, chrome, brass, valves |
| Ships | Rounded research hulls studded with sensor dishes and gauge clusters |
| Method | Rebuilt 2026-10-31 on the Nucleo NLog construction (owner: "They were inspired by the nucleo look. So.lets update that"), in our own design: a pillowed champagne plate whose every edge rolls over, over recessed displays lit from behind. Two kinds of light for two kinds of information: **amber backlit film** with flat ink for readouts (the name plate, every key's name, the tabs, the strips' meters, the icons) and **green phosphor glass** with a faint graticule for the scopes (the helm, the ship display, the radar). Soft lamps tucked under each amber window's top lip (10 mm, 180 degrees, the study's settings) and a soft main light from the upper left shade into the wells; light linking keeps the metal and the displays apart |
| Formed metal | The helm's eight keys sit in one pressed cradle (an annular channel) round its round phosphor scope; the ship display's round scope is held at its sides by two ears of the plate, a screw on each; each strip ends in a bay that holds a needle meter on amber film; the bar is an EQ-style slot with a lit streak; keys sit in pressed dimples; jewel lamps glow in dimples; the crest's tuning fork over a sine wave is inlaid in brass inside a pressed ring |
| Button shape | **Instrument key**: a domed key in a pressed dimple beside a small amber window that holds the key's name; a pilot ring round the key lights on hover and glows when pressed, and the pressed key sinks 1 mm |
| Progress and gauges | Needle meters in bays; the bars' lit streak in a slot; oscilloscope traces on a graticule |
| Fit | Every piece passes the bible's rules K, L, R, P and B before it renders (`tools/mockups/kelvane_shapes.py`), and no render has a lit pixel outside its plate (`--verify`) |

A seventh (owner, 2026-10-30, with a screenshot of a Starfleet Command battle console: "A gurilla style faction, gun
metal, beveled corner, industrial retro CRT style buttons", then "that faction is from the gorn, which are more hunter
like faction, they enjoy the sport, so their ui is reminiscent of a dense jungle akin to the Predator"). We take the
shape only: an original reptilian race that hunts for sport, its console a hunter's kit carried through a dense jungle.
No name, ship, glyph or art from that game or from those films. The name is ours and provisional (survey U10).

| | Ssarith Hunt |
|---|---|
| World view | The hunt is the only honest sport: the quarry must be worthy, the stalk patient, the kill earned and counted |
| Bodies and technology | Big-boned reptiles with heavy clawed hands, slow and silent until they strike; a hunter's field kit: worn gunmetal, camouflage, tracking tubes, grown over by the jungle it hides in |
| Ships | Squat armoured hunting ships with broad prows, dark and mottled, built to wait unseen |
| Method | Modelled in Blender: worn gunmetal with an oiled sheen and 45 degree chamfers, hex bolts in counterbores, canopy camouflage on the hero pieces, vines and frond leaves over the hardware, tally marks and tracking reticles in bone white, green phosphor tubes behind curved glass |
| Button shape | **Tube key**: a small curved CRT screen with scanlines, set into a thick cut-cornered gunmetal bezel; dark at rest, lit green on hover, flaring and sunk 1.5 mm when pressed |
| Frame | Heavy chamfered gunmetal with a canopy-green inner lip round a dark tube well; a vine curls over one corner and fronds cover another |
| Light and shading | A dim light falling through the canopy from above, a weak cool fill; the tubes spill green onto the steel |
| Icons | Hunt glyphs drawn as glowing vector strokes on tiny tubes: a track mark, closing brackets, a scale plate, a snare, a scent trace, a call, a skinning blade, a trail to an X, a tally |
| Progress and gauges | Segmented phosphor cells; a tracker that sweeps and marks the quarry |
| Type | A stencilled military face for titles (Black Ops One) over a phosphor terminal face for readouts (VT323) |
| Motion | A key's tube flares and settles; the tracker sweeps; nothing moves until it strikes |
| Distinct from | The Vhorr Brood (a grown hive: hide, membranes, no metal); the Krai Warhost (open war: black blades, angry red, triangles); the Vos (flamboyant pirates, hand-drawn) |

| Colour | Hex | Role | Ideology |
|---|---|---|---|
| Gunmetal | `#3a4044` | Plates, bezels, keys | The hunter's gear: plain, made to outlast the hunt |
| Canopy | `#2f4a2a` | Camouflage, field paint | The hunting ground; staying unseen |
| Moss | `#5b6340` | Camouflage mottling | Patience: the hunter becomes the jungle |
| Phosphor | `#6dff8a` | Tubes, readouts, traces | The tracker: the quarry in sight |
| Bone | `#d8cfb4` | Tally marks, trophies, titles | The count of worthy kills |
| Kill red | `#c2412b` | Alerts only | The strike, given once |

## Rendered, not drawn (version 10 onward)

The owner, 2026-10-30, on version 9: "all kinda get the point across, but I think svgs should be reserved for flat stuff
like the cocnord. Voorr, conclave, Krai ,Ombrai and Kelvane could all use rendering in blender. Vhor needs special
treatment with organic materials and shaders, and organic looking modeling, things likeslimey leather, tendrils, glaws,
carapaces, fibers, could help sell the alien hive mind aesthetic, ive it a try again. Kelvane 100% can be modeled, let and
rendered just take the osciloscope dash board for inspiration, old school electircal dashboards, dials, switches etc";
then "the conclave is one exception. They have a hybrid of both vector graphics for holographic buttons and icons, but 3d
rendered shell around the UI panels and elements".

| Skin | Method | Generator |
|---|---|---|
| Concord | Flat vector (page) | |
| Vos Syndicate | Hand-drawn by a seeded painter (page) | |
| Kethran Hegemony | Hard surface in Blender | `tools/blender/build_ui_kethran.py` |
| Vhorr Brood | Organic in Blender: hide with light through it (Voronoi veins, subsurface, clear coat), iridescent carapace (thin film), flesh tendrils (subsurface), sinew fibres, blister pods glowing from their cores | `tools/blender/build_ui_vhorr.py` |
| Kelvane Institute | The NLog construction (2026-10-31): pillowed champagne plates over amber backlit readout windows and green phosphor scopes, keys in pressed dimples, the helm's keys in a pressed cradle, meters in bays, lamps under the lips, light linking; from fit-checked shapes | `tools/mockups/kelvane_shapes.py`, `tools/blender/build_ui_kelvane.py` |
| Ssarith Hunt | Hunter's jungle kit in Blender: worn gunmetal with chamfers and sunk hex bolts, canopy camouflage, vines and fronds, bone tally marks and reticles, CRT tube keys with scanlines | `tools/blender/build_ui_ssarith.py` |
| Krai Warhost, Ombrai | In Blender | to come |
| Aelith Conclave | Hybrid on the analog panel bible: pillowed satin-gold shells with formed wells (lozenges drawn to a point, knuckled corners, a scalloped rim, embracing arms), pearls on flat metal, each well a void rim-lit in cyan; the holograms drawn as vectors into the wells from the same shapes | `tools/blender/build_ui_aelith.py` from `assets/ui/aelith/shapes.json` (`tools/mockups/aelith_shapes.py`) |
| Tarsine Remnant | On the bible since version 22: corroded iron, worn gold, grimy backlit glass, dim aged spots | `tools/mockups/tarsine_shapes.py`, `tools/blender/build_ui_tarsine.py` |

### The Aelith Conclave, rebuilt on the analog panel bible (version 14)

The owner, 2026-10-31: "Using these design principles. Update conclave UI" (the analog panel bible,
`docs/design/ui-bible.html`, and the `blender-ui-panels` skill), still under the hybrid rule of 2026-10-30 ("vector graphics
for holographic buttons and icons, but 3d rendered shell around the UI panels and elements").

- **The shell** is the bible's two-layer panel in the Conclave's own materials: satin gold (`#c9a646`, metallic 0.95,
  roughness 0.24, a fine grain under a thin coat) whose every edge rolls over, footprint-true, and pearl inlays (thin-film
  sheen) set on flat metal. Its wells are formed with the shell, never added on top: keys are lozenges drawn to a point
  between two gold claws, each with a pearl; panel frames have a knuckle of armour grown into each corner of the well,
  each holding a pearl (the bible's ear); the helm's rim is scalloped by eight lobes between the eight keys (the bible's
  scallop); the ship display is embraced by two arms from the sides; the strips are a long channel drawn to a point at
  each end, where a swept wing holds a pearl.
- **The light**: a soft main light from the upper left on the gold and into the wells (strength 0.8, 60 degrees across, so
  the lip's shadow fades out softly); each well a void of dark teal glass (`#0c3340`, glowing at 0.12) **rim-lit**: cyan
  strip lamps tucked under the lip all the way round each well, 0.0008 W per mm of edge, spread 120 degrees. Rim light
  is the bible's option chosen for the Conclave because the Conclave's light is thought made visible, held by the armour
  rather than thrown at it: it rises from the shell's own inner edge on every side at once (patient, balanced, no single
  source), and it leaves the middle of each well dark, where the holograms are drawn and must read.
- **The holograms** (keys, fields, the helm's eight keys and crystal, the crest's triangle, the alert crystals, icons) stay
  vector, drawn by `docs/mockups/skins/aelith.js`, clipped to their wells so no glow spills onto the gold. A kit image is
  the render and its hologram layered in one SVG, so the page's 9-slice and hero slots take it unchanged; the two bar fills
  are light alone and ride inside the track's channel.
- **One source of shape**: `tools/mockups/aelith_shapes.py` builds every piece's plate, wells, pearls, films and hologram
  outlines (shapely), checks them against the bible's fit rules (K, L, R, P, B), and writes `assets/ui/aelith/shapes.json`
  only if all 20 pieces pass; the Blender generator models exactly those outlines and the page draws its holograms from
  them. After the render, `aelith_shapes.py --verify` measures rule B on the images: no lit pixel outside any shell (all
  19 rendered pieces: 0).
- Panel titles sit inside their well, right of the corner knuckle, in holo light.

Every generator shares `tools/blender/ui_render.py` (render settings, lights, the camera, byte-reproducible PNGs, the
manifest), and the page loads any rendered kit with `FCK.fromManifest`; a kit may render its own icons (`icon.<name>`).

## Children inside parents, measured on every skin (version 16)

The owner, 2026-10-31, with a shot of the Kethran console's top strip: "remember the overlap rule? can you fix this
too...and every other ui systems too, to be sure that you understand how to UI stuff". The rule (the analog panel
bible, chapter 7; the `blender-ui-panels` skill): every child sits wholly inside its parent's open area with an even
margin, and a panel never grows with its content (CLAUDE.md section 7). It is now measured on the page, not judged:

- `tools/mockups/console_fit.mjs` shoots every leaf of every skin's console (a title, label, number, bar, fill, key,
  tab, icon, the plate's name, the strip lamp) on its parent's art with and without it, takes the leaf's own pixels from
  the difference, grows them by 3 px and looks under them for frame lines: a run of strong luminance steps (over 34 of
  255) at least 14 px long under the leaf and 36 px long in the parent, that is not one of three or more parallel lines
  at one pitch (scanlines, ribs). A fill may fill its groove but not cover its rim. Every column piece stays inside the
  460 px column and no key, label or panel is clipped. On the page as it was it found 90 faults in ten skins; it passes
  now. `tools/mockups/dom_fit.mjs` checks flat CSS pages (mockup 1, this page's proof rows) the same way on geometry.

What it found, and the fix for each (one fix per cause, in the shared layout where the cause was shared):

| Fault | Skins | Fix |
|---|---|---|
| A label over a bar (35 px) in a strip whose open band is 26-41 px, so labels touched the rail and bars sat on the lip | Kethran, Vos, Vhorr, Aelith, Tarsine, Ombrai | A skin may lay a label beside its bar (`--bar-flow: column`); each skin's strip padding is its art's open band, measured (Kethran's top strip: rows 17-43) |
| The alert lamp (48 px) drawn over the bottom strip's rails | every skin but Ssarith and Krai | `--strip-lamp`: the lamp sized to the band less 6 px |
| A panel title set on the frame's bevel or band | Kethran, Vos, Tarsine, Ombrai, Vhorr | Kethran and Vos title the open field; Tarsine and Ombrai move theirs clear of the band's edge; Vhorr's tag grows to hold its letters |
| The helm readouts' 154 px panel grew 7-106 px into the 3D view: its frame and the key caps needed 161-260 px | every skin | The helm pad drawn at 264 px (its 2x render stays sharp), the readouts in a 190 px inset frame, fixed; radar icons 32 px so the row fits 460 px |
| Kethran's bar groove (9 px) narrower than its 10 px fill | Kethran | Groove 11 px (2.5-13.5), re-rendered |
| Kethran's prow block 2 px from the plate's chamfer | Kethran | Moved in to 8 px of land, re-rendered |
| Vhorr keys: a vein texture and 24 px top and bottom margins left 9-slice seams across the face; the pressed pod creased; the face too thin for a label with descenders | Vhorr | A plain membrane on keys, vertical margins 0, one crown for every state, pod radius 21, re-rendered |
| Vhorr's tab ridge and vein crossed its label | Vhorr | Moved to the shell's top and bottom edges, re-rendered |
| Aelith's tab lens too thin for its label | Aelith | The lens as tall as rule R allows (land 6.2 mm), re-rendered and re-checked (K, R, L, P, B) |
| Labels a few px low or wide in their key or tab | Aelith, Tarsine, Kelvane | `--btn-pb`, `--tab-pb` and smaller key type, per skin |

Mockup 1's port panel covered the map's top bar and keys on desktop; the top bar now starts beside an open panel and
the keys step aside, as they already did on a phone.

## Symbol bars, the Remnant's dust, and three skins on the bible (version 17)

The owner, 2026-10-31, with four screenshots of the reference game's skins (not committed; they set direction only, in
order the Remnant, the Krai, the Ombrai and the Hunt): "Remnant: need to sync up dust with out and unlit button to give it
that illusion. Id fix up health and power sliders for a lot of these factions: have different repeated symbols for them.
Please use that instead of the same boring progress bar. The concord having them makes sense and sodoes kethran.and voss.
Everybody else could use a little more character. In addition we need to update krai, onbrai, and hunt to use the new
guildine standards".

**The Remnant's dust.** Each tablet key drew its grime from a seed that included its light level, and its backlight drew a
random number only when lit, so the idle, lit and pressed keys each carried different dirt and the tabs two different
plates. Now one seed per piece draws the same dust at every level; only the light behind it changes, which is the
illusion of a lamp coming on behind old glass. The rule holds for every skin: a piece's states share their texture, wear
and dust, and differ only in light (and a pressed key's travel).

**Symbol bars.** A skin may supply three optional kit pieces, `bar.cell.off`, `bar.cell.on` and `bar.cell.warn` (a
rendered kit: kind `cell`, with the gap between cells); its bars then become a row of its own symbol in the track's
groove, as many whole cells as fit at the groove's height, centred, the first `round(n x value)` lit. The track may be
taller than 16 px (its image height is its slot height). Concord, the Kethran and the Vos keep the plain fill, as the
owner said. The fit check measures a row of cells like a fill: it may fill its groove, never cover its rim.

| Skin | Symbol | Unlit | Lit | Warning |
|---|---|---|---|---|
| Vhorr Brood | Spore egg, a swollen membrane | Dim olive | Bile glowing from its core | Blood |
| Aelith Conclave | Lozenge of light, the key's own shape | Hollow outline in scanlines | Filled with light | Coral |
| Tarsine Remnant | Arched window, the tablet key's in miniature, caked in the same dust | Dark grimy glass | Gold light behind the dirt | Ember |
| Kelvane Institute | Jewel lamp in a brass bezel, an instrument's indicator row | Dark amber glass | Amber | Red |
| Krai Warhost | Chevron, a blade's point | Dark glass | Forge orange | Red |
| Ombrai | Eye | Dim lidded eye | Open, glowing pink | Crimson |
| Ssarith Hunt | Claw mark on a phosphor tube | Dark tube | Green phosphor | Kill red |

**Krai: cast volcanic gunmetal (version 23).** Owner, 2026-10-31: "edges of panels look kinda flat here. i also
expected something like a rough gun metal, maybe sort of akin to volcanic magma type metal", and on keys "I actually prefer
the more animated versions on click". The blade steel becomes cast volcanic gunmetal (`steel()` in
`tools/blender/build_ui_krai.py`): near-black basalt-grey metal with a cooled-lava crust split by fissures, coarse pits,
roughness from 0.5 to 0.9, and a dim ember glow (`MAGMA_ES` 0.7) in some fissures, deepest in the crevices. The roll
grows from 1.6 mm to 2.0 mm (the land shrinks from 3.0 mm to 2.3 mm so rule R still holds), and the top of every roll is
worn bright, picked out by the surface's own tilt. An engaged key's glass is pushed in 3 mm down and back while its frame
stays put (`PRESS_MM`); krai.js sinks the label to match (`--btn-sink` 1.5 px, `--btn-motion` 0.06 s, `--btn-pb` 2 px).

**Krai, Ombrai and the Hunt on the bible.** Each is now built the way the Aelith and Kelvane were: a shapes file of fit-
checked outlines (rules K, L, R, P and B on every piece, written only if all pass, and `--verify` for rule B on the
renders: 0 lit pixels outside any plate), a Blender generator on the shared panel kit (pillowed plates whose every edge
rolls over, formed wells, a film layer behind the metal, light linking) and the skin module loading the rendered kit.
Each takes a different one of the bible's lighting options, chosen from how the faction thinks:

| Skin | Shapes, generator | The look | Light, and why |
|---|---|---|---|
| Krai Warhost | `tools/mockups/krai_shapes.py`, `tools/blender/build_ui_krai.py` | Blackened blade steel with a tight 1.6 mm honed roll, red enamel bezels and blades, a fang of metal grown into every well's corner; Blade keys dark ember at rest, red on hover, forge orange engaged; an octagonal war-shield helm round a red heart triangle; spikes driven into the ship display; a hexagonal radar | **Bottom**: forge lamps on each well's lower lips only, so the glow rises like faces round a forge, never from a calm sky |
| Ombrai | `tools/mockups/ombrai_shapes.py`, `tools/blender/build_ui_ombrai.py` | Pillowed plum with a thin iridescent sheen and lilac rims; crescent-bitten keys with a moon jewel in the bite; an iris of eight petal wells round a slit-pupil eye; an eye-shaped ship display under two lids; a watching eye at each strip's end; moons for alert lamps | **Side**: lamps under each well's left lip, so every well is lit like a crescent moon with its far side in shadow, for a race that watches and is seldom seen |
| Ssarith Hunt | `tools/mockups/ssarith_shapes.py`, `tools/blender/build_ui_ssarith.py` (rewritten) | Cut-cornered olive gunmetal, every edge rolled, the heroes pressed with a hide-scale texture; every well a small CRT tube with scanlines on its glass; hex bolts in counterbores, bone tally marks and reticles engraved, frond leaves on flat metal; caged jewel alert lamps | **Backlight only**: the tubes light themselves and no lamp hides under any lip, because a hunter shows nothing the quarry could see |

All three share their states' texture (one model per key, tab and cell; only the light changes). Every well's lamps are
placed by one helper, `ui_shapes.lip_runs` (every edge for rim light, the lower lips for bottom light, the left lips for
side light). It replaced three copies; the Aelith's copy had been aiming its long lips' lamps out of the well, so its rim
light now runs all the way round as designed, at half the power (0.0004 W per mm).

Learnt on the way (now in the `blender-ui-panels` skill): a plate or well corner must be rounder than about twice the roll,
or offsetting by the roll and bevelling by the roll turns it inside out into a spike; outlines are cleaned of
near-duplicate points before they are offset.

## Taller strips (version 18)

The owner, 2026-10-31, with a shot of the Kethran top strip as it was before version 16: "text and progress bar
overlapping with parent bevels, we need to make parent taller". Version 16 had laid each label beside its bar to fit the
26-41 px open bands of 56 px strips; the owner's fix is the better one. The strips are now **72 px** (the 3D view's inset
grows from 60 to 76 px top and bottom). Every skin's strip keeps its rails, lips and end ornaments at their size, centred
on row 36, and its open band grows 16 px; the shaped skins' strips pass rules K, L, R, P and B again (the Krai's end blades
moved back clear of its longer pointed channel). Each skin's strip padding is its measured band, the alert lamp is sized to
it, and every label stands over its bar again, 3 px clear of the art (console fit: all ten pass).

| Skin | Open band (top strip) | Was |
|---|---|---|
| Concord | 57 px beside the thin bar | 44 |
| Kethran | 43 px, rows 17-59 | 27 |
| Vos | 55 px | 41 |
| Vhorr | 52 px | 36 |
| Aelith | 47 px | 33 |
| Tarsine | 46 px above the beam | 30 |
| Krai | 45 px | 30 |
| Ombrai | 49 px | 33 |
| Kelvane, Ssarith | 66 px | 50, 55 |

## The Kelvane's keys and toggles light up (version 19)

The owner, 2026-10-31: "these need to switch back light on off based on hover/click state", then "some of the togle
buttons for instutues having lights on would be cool". The Kelvane key's amber window was backlit in every state, so only
its small pilot ring changed. Now the window is the state: unlit film and no lamp at rest, backlit amber on hover, brighter
film and a stronger lamp when pressed, the pilot ring lit with it. Each tab, the console's toggles (Offline, Normal,
Overload), carries a jewel pilot lamp in a pressed dimple at its left, inside the slice margin: dark glass when off,
glowing when that setting is on. The plate, glass and dimple are the same in every state; only the light changes.

## Keys never move (version 20)

The owner, 2026-10-31: "Krai buttons shouldnt move position when I press them", then of the Kethran keys "these buttons
should not move, the button should have a 3d animation where the inner part is pushed in and the frame stays put in the
same spot". No pressed key is moved or squashed by the page any more (the Kethran and Hunt slid down, the Vhorr squashed,
the Krai slid sideways), the Vos tag included (owner: "Vos buttons are still being rotated when I click", which corrected an earlier reading of
their note on the Vos keys as asking for the tilt). Every skin's pressed image keeps its frame exactly where its idle
image has it (measured: identical footprints in all seven rendered kits) with the cap sunk in its socket, so a press swaps
to that image and the label rides the cap down by the skin's `--btn-sink` (2 px on the Kethran's long key travel, 1 px
elsewhere).

## Drawn on canvas, and live (version 21)

The owner, 2026-10-31: "can you animate all the toggle and sliders? for every ui kit? that would be valuable to see the
interaction. if you can, get rid of the slice lines, and use canvas rendering, or a game engine like phaser js or three js".

- **No more slice lines.** The browser's CSS border-image samples across the lines between a 9-slice piece's corners,
  edges and centre, and leaves faint seams. The page now paints every kit piece itself: each image is cut once into nine
  canvases (so no slice ever samples its neighbour) and each element gets a canvas behind its content, sized to its box at
  the console's scale and the screen's pixel ratio, its slices drawn on whole device pixels. Plain canvas is enough: no
  engine is needed for 2D pieces, and the page stays one self-contained file. The borders stay only to lay content out.
- **Live controls, in every skin.** A key lights on hover and is pushed in while held (its art cross-fades to the sunk cap
  in 70 ms, its label rides down; the frame never moves); a toggle (the arming tabs) switches
  with a 150 ms fade; every bar is a slider (drag along it and its fill slides, or its symbols light and go dark one by
  one). For people, the bars fill from empty when a skin is shown and the weapon capacitor keeps charging and
  discharging; headless checks see still frames.
- The fit checks measure content without the art canvases, which cover the border box by design.
- The Krai ship display is a wireframe schematic (owner: "The krai ship display should be a wire frame schematic"): the
  blade hull top-down in glowing hairlines, a node at every vertex, its spine and frame ribs, the wing spars, the lower
  hull in dashed hidden lines, the three weapon mounts boxed and called out, the engines as nozzle rings.

## The Remnant on the bible, grime kept (version 22)

The owner, 2026-10-31: "I love the Remnant ui, i htink it could use some polish from our guide, but please dont remove the
grimey industrial feel, I love that story telling so much". The Tarsine Remnant is now built like the other rendered
skins: fit-checked shapes (`tools/mockups/tarsine_shapes.py`, rules K, L, R, P and B on all 19 shaped pieces, `--verify`
0 lit pixels outside any plate) rendered in Blender (`tools/blender/build_ui_tarsine.py`). The grime lives in the
materials: corroded iron with rust blooms, verdigris and pitting, soot pooled where the plate's own occlusion collects it,
great domed rivets with rust streaks running down the flat, worn gold on bezels, brackets and the crest, and one caked
dirt layer laid over every window's glass, the same in every state, so only the backlight behind it changes. Keys stay
arched tablets on a wider foot; tabs chipped lintels with a slot of glass; the strips a 72 px beam with a grimy glass
channel; the helm nine tablets, the centre lit like a hearth; cracked glass over the ship display and dial; caged bulbs
for the alert lamps; little arched windows for the bar's cells.

**Light**: the bible's spots, dimmed and uneven: one or two old lamps under each window's lip, each with a fixed age
between 35% and 100% power, because the Remnant never replaced its fixtures and not all of them still burn. The name
plate's letters are pale and outlined dark, so they read over the lit gold and the stains alike.

With ten rendered kits inlined as PNG the page reached 19 MB, past the artifact host's 16 MiB limit. The page now carries
each kit image as high-quality WebP (quality 92, alpha kept; `tools/mockups/inline_data.py`), 2.4 MB in all, measured
indistinguishable on the page (mean 0.33 of 255); the committed renders stay PNG.

## The Vhorr Brood: assimilated tech (version 23)

The owner, 2026-10-31, with two screenshots of StarCraft II's Zerg interface (not committed; direction only, take the
shape, not the art): "The brood still needs work. Analyze elements in this UI system carefully", then "I like that some of
the tech in zerg is human tech with an organic layer on top", then "Send it. If it's good, it's good; if not we're gonna
repeat the same exercise we did with winamp" (the Nucleo NLog study: a reference studied element by element, then the
skin rebuilt and compared pixel by pixel, the `blender-ui-panels` skill's section 8).

**What the reference does that the Brood does not**:
- **One organism.** Its panels share one continuous carapace and are joined by sinew cables; ours are isolated boxes.
- **A silhouette that breaks the rectangle.** Domes, horns and ribs overhang the frames and cast shadow on the playfield;
  ours are even bands with corner claws.
- **Bone and chitin ridges.** Vertebral ridges and glossy bone against wet flesh; ours is mottled hide that reads as camo.
- **Membrane windows.** Smoked glass with a thin bright inner rim, calm behind text; ours are flat dark interiors.
- **Glowing nodules.** Pustules at the joints act as status lights; ours has none.
- **A portrait pocket and a focal object.** A fleshy pocket holds a live portrait, and a large crystal is the heartbeat;
  our name plate is a band and our crest is quiet.
- **Crisp keys in a soft frame, and state by glow colour.** Our tabs are low contrast.
- **A thick-and-thin rhythm.** Swollen knots alternate with thin tendrils.

**The rebuild: assimilated tech.** The brood grows, consumes and adapts; it does not build consoles, it takes them. Two layers:
1. **A captured machine**, built on the panel bible (plates with rolled edges, rivets, screens, real keys), fully legible.
2. **The growth over it**:
   - Creep and membrane spreading from the corners and seams, thinning toward the controls.
   - Conduits wrapped in sinew, and tendrils bridging the gutters between panels.
   - Bolts swallowed into glowing pustules that act as status lights.
   - Bezels fogged by membrane, but each screen's middle clear.
   - Keys with membrane over their edges.
   - Bone and chitin ridges growing out of the metal over the frame tops, breaking the rectangle.
   - A fleshy portrait pocket on the name plate.
   - A large glowing egg or crystal as the focal hero piece.
   - Contacts glowing by threat.

**The fit rules extend naturally**: growth may cover frames and bezels, never a readout, a label or a key's face, and an
overhang never covers another panel's content. Measured, like every other rule. A shared "growth overlay" kit idea
(tendrils across the gutters between panels) is new and needs its fit rule.

If the result is not right, the next step is the Winamp exercise: study the reference element by element, rebuild, and
compare pixels.

**Built (version 23, 2026-10-31)**: `tools/mockups/vhorr_shapes.py` writes and checks the shapes, `tools/blender/build_ui_vhorr.py`
renders them, `docs/mockups/skins/vhorr.js` draws the live readouts.
- **The captured machine**: gunmetal steel with a 3 mm roll (keys 2.4, tabs 2.0), machined wells (the frames' corners
  chamfered 22 px to make land for a bolt), dark glass lit bile by a lamp under each top lip. Keys and tabs are backlit only,
  because a lamp's hot spot under the lip crossed their labels. Its plates, wells and bolts pass rules K, L, R, P and B.
- **The growth**: creep is a thick core of dark wet hide whose light runs through a web of thin veins (patches read as
  camouflage), inside a translucent membrane that thins onto the steel; nodules swell on the larger creeps. Pustules sit
  over the bolts, chitin spines with vertebral knobs and horns run along the helm's and the ship display's top edges and
  out past the rounded corners, captured conduits carry sinew bands, the name plate's left end is a fleshy pocket holding
  the brood's eye, and the crest is a glowing egg cradled by chitin ribs.
- **Rule G**, new and measured: growth stays inside the piece's image (G1), at least 3 px from every live readout, label
  and key face (G2), and on a 9-slice piece only in the cells the slicing never stretches (G3). Creep is grown from an
  anchor and fitted by sliding and shrinking, never clipped, so it has no straight cut edge.
- **Pressed keys move** (owner, 2026-10-31: "I actually prefer the more animated versions on click"): the glass is
  pushed 3 mm in and 3 px down, the frame and growth unchanged; `--btn-sink` 1.5px moves the label with it.
- Contacts on the dial glow by threat: bile far, amber nearer, blood nearest.
- Not built yet: tendrils bridging the gutters between panels. They need a page-level growth overlay, a new kit idea
  with its own fit rule (never over another panel's content).

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
| `strip.top`, `strip.bottom` | 9-slice | full width x 72 | the top and bottom strips |
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
| U8 | The six new races (including the Kelvane Institute): names, whether they join the campaign, and whether the combat race is the Kethran Hegemony's own direction? | Skins only for now, with the names above; factions decided later (provisional) |
| U7 | Are the factions' world views, bodies, technology and ships as the skins read them? | As written above (provisional) |
| U10 | The hunter race: a new race, and is "Ssarith Hunt" the name? | A new race named the Ssarith Hunt: a skin now, a faction decided with the others under U8 (provisional) |
