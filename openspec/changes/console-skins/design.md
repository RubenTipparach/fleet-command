# Design: console-skins

Mockup 4. Three faction design languages for one UI kit. Our own designs (CLAUDE.md section 1: take the shape, not the
art); the reference games' skins (SFC2 manual printed pages 108 onward show its console) and LCARS set direction only.

## The owner's rules for every skin

The owner, 2026-10-30, on version 1 of this page: "this ui language thing, it just different colors, not very
interesting". Then:

1. **No skin uses the same button shape.**
2. **Every skin has a palette inspired by its faction's ideology.**
3. **Elements and icons are inspired by how the faction thinks**: each skin uses different shapes, lighting and
   shading, and icon shapes shaped by the faction's ideas, history, traditions and technology. Never by its body
   (owner, 2026-11-04, superseding "their body shapes" of 2026-10-30: "the look of an alien species is not their UI,
   not their spaceship"; CLAUDE.md section 1). The skins below that took anything from a species' body (the "Bodies"
   rows, the Hunt's hide scales, the Brood's flesh) are to be reviewed against this with the owner.

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

## An organic design system (version 24)

The owner, 2026-10-31, of version 23: "Nah that's bad lol. Let's make a new design system for organic", then "The brood
is overly simple. It's just metal with ameobas on top. Completely missing the zerg aesthetic". Version 23's assimilated
tech is **superseded**: it started from a machine and added growth, so the growth read as decoration. This is the
Winamp exercise the owner named, done for organic UI: a study refined one change at a time in Blender
(`tools/blender/organic_study.py`, 14 steps), written up as the **Organic Panel Bible** (`docs/design/organic-bible.html`)
with its skill (`blender-organic-panels`). Reference screenshots of another game's hive interface gave the direction
and are not committed; the shapes, words and colours are our own.

Its rules, in short (the bible has them in full):
- **One organism holding instruments.** Each panel's frame is one body, with no grey metal. (The first round put a
  ribbed wall of tissue behind the panels; that is superseded by the version 25 note below: the console is an overlay
  with no backdrop.)
- **Five materials.** Leathery maroon-brown flesh with a wet coat, near-black chitin plates sunk into it, ivory bone,
  black smoked glass for every screen (with a thin bronze rim and a calm middle), and light.
- **Grown form.** Metaball bodies, voxel remeshed and lumped; a ribcage rhythm; chitin overlapping like vertebrae;
  bone spines and arches growing past the top edge and over neighbours; asymmetric.
- **Crisp tech in a soft frame.** Screens are black with glowing content; keys are square dark caps with glowing amber
  rims and clean icons set into the flesh (a press sinks the cap 3 mm, the flesh stays); the name plate is a tall pocket
  under an arch holding a live portrait; the crest is a big faceted violet crystal.
- **Light and palette.** A warm soft key from above, grazing rims, a dim cool fill; accents light their surroundings.
  A value ladder: fold `#080303`, flesh `#33110f`, lit flesh `#6a2620`, chitin `#140c0e`, bone `#e2d6bb`, screen
  `#050405`, bronze `#9a5a34`; light only in readout green `#b8f27a`, key amber `#ffb03a`, life green `#6fd82a`, hero
  violet `#b35cff`.
- **Fit, measured (G1-G4).** Growth may cover frames and neighbours' frames, never a screen, a key face or the portrait
  window; the study measures it on the evaluated geometry (all clear in the final step).

**Next:** the Vhorr Brood kit is rebuilt on the organic bible once the owner has seen it (a new
`tools/blender/build_ui_vhorr.py` on the skill's method, its kit pieces measured by G1-G4 and the page's fit checks).

## The organic guide reviewed, and no nine-slice for organic skins (version 25)

The owner, 2026-10-31, after study steps 15-25 (the frame sculpted in layers: bone, muscle, leather stretched over both,
veins under the skin, no raw primitives, and the portrait as live pixels under a fine hex-cell filter): "That looks
amazing!", then "Now that looks good. Create the guide and update the console skins documentation. We need a note in
there that due to the nature of this ui, nine slice is not possible and the UI will have to be bocked out using
placeholders then a full detail agent will go in and sculpt components manually".

- **The Organic Panel Bible is reviewed** and its draft hold lifted; it stays a living guide. Its rules: R1 never use raw
  primitives, R2 layered anatomy sculpted as one surface, R3 screens as live pixels under a fine hex filter (2.0 mm
  cells, 20% walls at 45% brightness, label contrast 11.5:1 at 1x), R4 no nine-slice.
- **No nine-slice in an organic skin.** An organic frame is one sculpted body: its ribs change along their length,
  growth crosses the gutters between panels, and no two corners are alike, so it cannot stretch or tile (stretched, the
  sculpt smears; tiled, one vertebra repeats). Every organic kit piece is a **hero piece sculpted at its exact slot
  size**, and each screen shape (desktop, phone landscape, phone portrait) gets its own sculpt. The kit's piece names
  stay the same, so screens still draw only kit pieces by name; what changes is that an organic skin supplies a fixed
  image per slot instead of one stretchable image. A slot without its sculpt is a build error, as for any missing
  piece; nothing falls back to stretching.
- **The build method: block out, then sculpt.**
  1. **Blockout.** The whole console is laid out with plain placeholders: one flat, labelled rectangle per panel at its
     exact slot size and place, with its protected rectangles marked (every screen, key face, label and the portrait
     window). The page's layout and fit checks run on the blockout.
  2. **Full-detail sculpt.** A full-detail agent then sculpts the components one at a time, by hand-directed sculpting on
     the bible's rules: at final size, in place beside its neighbours so growth joins across the gutters, clear of every
     protected rectangle. Live content (readouts, labels, the hex-filtered portrait) is drawn over the piece, never
     sculpted in.
  3. **Swap and measure.** Each sculpt replaces its placeholder; the fit checks run again and its close-up is looked at.
- **Cost.** One sculpt per slot per screen shape instead of one stretchable image per piece: more pieces to make, and a
  layout change to an organic skin means re-sculpting the slots it touches. Rendering is about 5 minutes per piece at
  32 samples on the cloud machine.

- **An overlay, not a scene** (the owner, 2026-10-31: "The one thing about UI is it's overlayed on top of gameplay so that background wouldn't make sense here", then of study step 25: "Only this new iteration is accepted. Minus the background there"). The
  console sits over the live battle, so no wall, cavern or fill stands behind or between the panels; every organic
  piece renders on a transparent film and is judged composited over the game view; only the panels' own bodies join
  them (a tendril). The bible's rule 5, and its study step 26, which is the accepted design; step 14 (placed beads, a
  white spine, a wall) is shown beside it as the owner's rejected iteration.

**Next:** the Vhorr Brood is rebuilt this way, starting with the blockout of the desktop console.

## The Vhorr Brood on the organic bible (page version 25)

The owner, 2026-10-31, of the Organic Panel Bible v2: "Looks dope, now let's see if this new design system can be
applied to the design language/brood skins design system". The Brood is rebuilt on the bible's rules: no raw
primitives (R1), layered anatomy sculpted as one surface (R2), screens as live pixels under a fine hex filter (R3), no
nine-slice, so block out first and then sculpt each component in place (R4), and an overlay over the game with no
backdrop (R5). The owner saw the blockout and said "Go"; the page's Vhorr skin is now the sculpted kit (phase 2, below).

**Phase 1, the blockout (done).** `tools/mockups/vhorr_blockout.mjs` measures the live page (the Vhorr skin at the
1920 x 1080 desktop reference) and writes `assets/ui/vhorr/organic/blockout.json`: every slot the console draws with a
kit piece, its rect in the console's own px, and its protected rectangles (labels, readouts, the inked extent of each
live drawing, icons, bar cells, key and tab labels, the helm pad's nine key faces, the name plate's portrait window, and
child slots). `tools/mockups/vhorr_blockout_draw.py` draws it over the page's own game view:
`docs/screenshots/mockups/vhorr-blockout-desktop.png`.

| Sculpt group | Slots | Size (px) | Notes |
|---|---|---|---|
| `strip.top` | 1 | 1448 x 72 | holds three tracks, three labels and the clock |
| `strip.bottom` | 1 | 1448 x 72 | holds two tracks, labels, the speed readout and the lamp |
| `bar.track` | 3 | 370.5 x 20 | top strip; cells drawn live on top |
| `bar.track` | 2 | 507.5 x 20 | bottom strip |
| `lamp.alert` | 1 slot, 3 states | 40 x 40 | in the bottom strip (shown 40 px, the kit piece is 64) |
| `nameplate` | 1 | 460 x 56 | name field 290 x 26; portrait window 62 x 48 |
| `helm.pad` | 1 | 264 x 264 | nine key faces of 54.5 px (one key cap, placed nine times) |
| `frame.inset` (helm readout) | 1 | 190 x 264 | holds To hit, 83%, a divider and Engage and Stop |
| `divider` | 1 | 154 x 10 | |
| `button` | 2 slots, 3 states | 154 x 48 | Engage, Stop |
| `ssd.bezel` | 1 | 460 x 300 | live drawing 215 x 217 |
| `frame.panel` (weapons) | 1 | 460 x 174 | title, Fire, Multiple and three tabs |
| `button` | 2 slots, 3 states | 192 x 48 | Fire, Multiple |
| `tab` | 3 slots, 2 states | 126.5 x 40 | Offline, Normal, Overload |
| `dial` | 1 | 120 x 120 | live drawing 74 x 74 |
| `frame.inset` (contacts) | 1 | 232 x 120 | label and five icons |
| `crest` | 1 | 96 x 96 | the violet crystal |

24 slots in 17 sculpt groups at the desktop reference. Slots of one group share one sculpt only where they are a
self-contained piece (a key, a tab, a bar track); frames are sculpted once each, in place, because their growth joins
their neighbours'. A phone in landscape shows this same console scaled to fit, so it uses these slots; no mockup lays
out the battle console in portrait yet, so that slot set waits for its layout.

**Awkward for organic slots** (the console rules say panels are fixed rectangles; these come out of the page's flex and
grid layout instead, and must be pinned before sculpting):
- **Track widths come from content**: 370.5 and 507.5 px are what the strips' flex rows leave after the labels and
  gaps; a different label or font would move them. Phase 2 pins them as fixed widths.
- **The weapons panel's 174 px height comes from its content** (a grid with no set height), and the radar row's slots
  sit wherever the column's leftover height centres them (the dial at y 889, the crest at y 901). Phase 2 pins every
  column slot's y and height.
- **Fractional sizes** (tabs 126.5 px from a three-column grid, the tracks): pinned to whole px, so a sculpt maps to
  whole pixels at 2x.
- **No portrait slot.** The bible's tall portrait pocket does not fit the 460 x 56 name plate; its pocket is 62 x 48. A
  taller name plate is a layout change to the console, which needs the owner's approval of a mockup (CLAUDE.md 4).
  Until then the hex-filtered portrait lives in the plate's 62 x 48 pocket.
- **States multiply sculpts.** Keys have three states and tabs two, each a separate render of the same slot, and the
  lamp three; because growth joins across slots, each state is rendered in place in the full scene, not alone.
- **The lamp** shows at 40 px in the strip though its kit piece is 64 px (`--strip-lamp`); it is sculpted at 40.

**Phase 2, the sculpt (built).** Two owner notes shaped it: the blockout's two choices accepted (pin every
content-sized or fractional slot to whole px, in the page too; keep the 460 x 56 name plate and put the hex portrait in
its 62 x 48 pocket), and "If you structured your sculpting tools well. Then we should be able to sculpt panels, buttons
and elements of any size". So the sculpt is a library, not per-slot code:

- **`tools/blender/organic_components.py`, the parametric sculptors** (numpy only, on `organic_anatomy.py`'s
  primitives): a `Field` (bone, muscle and skin height fields over a region, openings, sacs of light, protected
  rectangles, the instruments to place) and one sculptor per kind of piece: `panel` (a body grown round its windows and
  the pieces set into it, a thick rolled lip round every window, a vertebral ridge with horn ends along the edge with
  the most room, long muscle lobes between), `key` (a hard cap sized from its label in a collar of flesh with sinew
  knots each side), `tab`, `strip` (a long body with glass channels, a spine along its outer edge and a knot at each
  end), `bezel` (a deep pocket with a ribcage round it), `round_pocket` (the dial), `crest` (a cradle and the crystal
  heart), `lamp` (a sac of light, green, amber or red), `cord` (the divider) and `helm` (the panel sculptor round nine
  key faces). Every size is laid out in mm from the rectangle and its room (hug thickness from the window's size,
  vertebrae about one per 19 mm, lobe count from the area), seeded from the slot id, so two keys of one size are
  siblings, not clones, and a longer strip has more vertebrae, never stretched ones. `Field.finish` stretches one skin
  over everything (a smooth maximum, then a blur that webs across gaps), raises veins and sacs, creases the hollows,
  sinks every opening, and drops everything outside the bodies to the floor (cut by the renderer: R5).
- **`tools/blender/build_ui_vhorr_organic.py` only reads a layout** (`blockout.json`, or any list of slots with a rect,
  a kit piece and protected rectangles), pins it, and hands each slot to the sculptor its kit piece names. Each
  top-level slot renders as a region (its neighbours' edges sculpted in an 8 mm margin, so light and shadow match),
  through the study's own `field_mesh` and `crystal_heart` and materials (`organic_study.py`: one implementation), lit by
  the bible's key, rims and fill as suns so every region is lit alike, at 2 px per mm on a transparent film. Children
  (keys, tabs, tracks, the lamp, the divider) are cut from their parent's render; each other state is the same region
  re-sculpted with only that state changed and rendered over the child alone.
- **Pinned**: tracks 371 / 370 / 370 and 507 x 20, tabs 127 / 126 / 126 x 40, the weapons panel 460 x 174, the radar row
  where it sits; the page's `organicPin` sets each slot element's size from the manifest (measured: every slot's size
  exact, its position within 1 px of the sculpt, from the strips' and grid's half pixels).
- **The page** (`console-skins.html`): every console element names its slot (`data-slot`); a skin with `organic` in its
  kit draws each slot's own image, in its state, unsliced at its rect (`drawPiece`), keys and tabs swapping images as
  they are hovered, pressed and switched; the portrait is drawn live in the plate's pocket with the hex filter
  (`hexMask`, the same numbers and arithmetic as `hex_portrait.py`); the kit section shows every slot in every state at
  its size. `skins/vhorr.js` lays the organic manifest over the version 23 kit, which still gives the symbol cells, the
  icons and the layout margins the blockout was measured with.

**Numbers.** 24 slots, 23 renders (9 regions, 14 state crops), 228 s of rendering at 24 samples (about 4 minutes for
the whole desktop console on the cloud machine, against the hour estimated: regions are only the slots, not the whole
screen). Rule G1 on the sculpted fields: every protected rectangle clear by 2.83 mm or more (the helm key
faces, the tightest; labels 3.5 mm or more, key labels 6 mm or more). `console_fit` and `dom_fit` pass on all ten skins.
The "any size" proof (`docs/design/organic-bible/any-size.jpg`): one key at 90, 160 and 260 px, one panel at three
aspect ratios, one strip at 300 and 630 px, the same sculptors, each grown at its size.

**Gaps, honestly.** The strips are mostly glass channels (their labels and bars fill the 72 px band, so little flesh
shows); the name plate is glass with a thin rim, its portrait small; the symbol cells and the contacts icons are version
23's renders; the helm pad's keys are one state (the helm's live glyphs do not press); the phone portrait layout has no
slots yet; `vhorr_blockout.mjs` finds the helm by the version 23 hero images, so it needs to read `data-slot` before it
can re-measure the organic page.

## The Brood's picks from the survey (section W, not yet built into the page)

The owner answered survey section W on 2026-11-01, each pick made from a picture of exactly that component in exactly
that style, and said: "survey completed, this is a good start, I still dont want a plain black backround on the ui area
though".

| Id | Part | Pick |
|---|---|---|
| W1 | Top and bottom strips | H, ridge and tentacles |
| W2 | Ship systems display | E, hanging tentacles |
| W3 | Helm pad and the To hit / Engage / Stop panel | D, tentacle wrap |
| W4 | Weapons panel | D, tentacle wrap |
| W5 | Bottom row (radar, contacts, crest) | D, tentacle wrap |
| W6 | Name plate | B, plain heavy leather |
| W7 | Keys | Both: larvae for the big keys, low warts for the small keys and the tabs |
| W8 | The space between the panels | A, tissue: one grown body, a membrane over muscle with ribs across it, so no black shows (answered 2026-11-01, the recommendation) |

- **The picks are data**, not code: `assets/ui/vhorr/organic/styles.json` names each slot's style and each key's body,
  validated when `build_ui_vhorr_organic.py` loads it (an unknown slot key, style or body stops the build). The four
  long keys (Engage, Stop, Fire, Multiple; 154-192 x 48 px) are larvae; the helm's nine keys and the three tabs are
  warts. The owner's pick named Engage and Fire as the big keys; Stop and Multiple are the same size, so they are read
  as big keys too (one line in the styles file to change).
- **The space between the panels** (the owner's note) is a new piece, the column's backing, sculpted over the whole
  left column and drawn under every panel (`organic_components.backing`). Three treatments, all within rule 5 (the
  brood's own body, never a scene): **tissue** (one grown membrane over muscle with ribs across it, sinew cords and
  veins, its free edge rolled into a lumpy lip over the battle), **tendrils** (no body: the battle shows under the
  column, the panels held to each other only by tentacles over the gutters, the bottom row anchored off the screen's
  edge) and **lattice** (a ribcage of bone bars across the column on a spine at each side, the battle seen through its
  holes, the panels hung on it by tentacles). For tendrils and lattice the battle view runs under the whole console.
  The owner picked **tissue** in survey W8 (2026-11-01), from whole-console screenshots. It is graded darker than the
  panels after rendering (`BACKING_GAIN` 0.55 on its colour values: its flesh's median L* 14.8 against the panels'
  19.6-20.6, where the same light gave it 21.9), so the panels stand off it.
- **The ship systems display spans the column** (owner, 2026-11-01: "this ship display should span the whole width"):
  its glass is 400 x 226 px in a 460 x 300 slot (30 px of rim at the sides, 28 at the top, 46 at the bottom for the
  hanging tentacles), the bezel's body fills the slot, and its ribs grow on both sides when the glass is centred
  (`organic_components.SYMMETRIC_MM`). The manifest names each slot's live glass (`live`), and the page centres the
  drawing in it and scales it to its height.
- **Approved** (owner, 2026-11-01, after the whole-console shots: "nice, survey andswered, make changes and publish
  that"): version 26 of the page builds the Brood from these picks, with the tissue backing and the full-width ship
  systems display. A pressed key's cap drops 2 mm and its label rides it down the same 2 px (`--btn-sink` 1 px, padding
  doubled).

## Three more factions (page version 29)

The owner, 2026-11-07: "I have a couple more faction ideas. Id like a faction that is purely mathematical. A faction based
on fractal patterns and designs. Then that faction will merge with the organics creating a new hybrid of fractal organic
faction. Id also like another pirate faction. One more brutal. Decorate their UI with sharp teeth, bullets and blades.
Blood stained buttons and crusty paper with burn marks and drops of blood and grime on them". The hybrid's name is the
owner's ("The Exponent"); the other names and every culture below are ours and provisional (survey N1 to N6). Each is a
first-pass skin module (`docs/mockups/skins/axiom.js`, `exponent.js`, `redtithe.js`); none takes anything from a
species' body (the Tithe's teeth are trophies taken from other crews, a tradition).

| | The Axiom | The Exponent | The Red Tithe |
|---|---|---|---|
| World view | Every true thing is a rule and its iteration; a structure is honoured by how deep its rule has been carried. Three schools: escape-time (screens), tilers (frames), branchers (icons) | The Vhorr Brood after it took the Axiom's rules into its growth: tissue that grows by rule. Phyllotaxis growers, branch-veiners, cell-builders, shell-turners | Raiders who take a tithe of blood from every crew they board; orders on bounty sheets pinned with knives; standing counted in trophies (teeth of beaten crews, spent cartridges, broken blades) |
| Method | Flat and exact, drawn by the rules in canvas: Koch, Sierpinski, Hilbert, Cantor, the dragon curve, a Julia set per pixel, golden-ratio proportions; no randomness | Painted membrane lit from within, its structure computed: Voronoi cells from a seeded Poisson disc, L-system veins (each child its parent over phi), seeds at the golden angle, chambers on a logarithmic spiral. To be sculpted in Blender on the organic bible | Painted crusted, scorched, bloodied bounty sheets on riveted gun steel. To be finished with the paper painted in Krita and the jaws, knives, cartridges and teeth modelled in Blender |
| Button shape | **Koch key**: a long hexagon with Koch-edged ends; hover iterates one level deeper, pressed fills solid in a gold outline | **Chambered key**: a nautilus section at the left end whose open chamber is the face; hover lights veins and septa, pressed fills the chambers with light | **Trap jaw**: a paper slip clamped between sawtoothed steel jaws; hover eases them open, pressed snaps them shut and blood seeps along the bite |
| Frame | Fine indigo border, Sierpinski corners (panel), Hilbert corners (inset) | A Voronoi membrane, a gold vein branching into each corner, a seed node at each turn | Torn scorched sheet on riveted steel, a knife pinning one corner, a cord of trophy teeth at another; grime only in the margins |
| Light | None painted: lines and fills on the void; depth of iteration shown as indigo to cyan | From within: gold veins and cyan seeds through plum membrane, a wet bead on each cell | Hard light from the upper left, deep cast shadows; nothing glows but the alert lamps |
| Icons | Pictures of the job in cyan, strokes cut by Cantor sets, routes branching, comms waves spaced by phi | Tapered grown strokes with seed tips, one gold ink | Cut into the sheet with a blade: a dark cut, a pale rim, scratches |
| Type | Martian Mono over Space Mono | Fraunces over Jura | Stardos Stencil over Special Elite |
| Progress | Sierpinski carpet cells; a Cantor-cut dial ring | One Voronoi cell per step, a cyan heart in a gold wall | A bandolier: empty loop, brass round, red-tipped round on warning |
| Hero pieces | Koch nameplate; eight Koch snowflakes round a gold stop; a Julia screen in Sierpinski corners; snowflake lamps; a dragon-curve crest | Seed-disc nameplate; eight chambered keys on one log spiral round a seed-ring stop; seed-pod lamps; a golden-angle crest in a branching ring | Bounty-sheet nameplate with a burned wax seal; a revolver-cylinder helm with brass chamber keys; a sheet pinned by four knives; a gauge behind cracked, bloodied glass; a tooth over crossed blades on a drop of blood |

| Colour | Axiom | Exponent | Red Tithe |
|---|---|---|---|
| 1 | Void `#05060c`: the unbounded plane | Membrane `#3a1830`: the one body, still growing | Dried blood `#5a1a12`: the tithe already taken |
| 2 | Proof white `#e8ecf6`: stated truth | Cell wall `#1c0a17`: the rule that parts growth | Fresh blood `#c4141c` (alerts only): the tithe being taken |
| 3 | Iteration indigo `#4b3fd8`: a shallow rule | Pith `#f2e6d0`: what growth has proven | Old paper `#c9b48a`: every debt written down |
| 4 | Iteration cyan `#3fe0e6`: a rule carried deep | Rule gold `#f0bf4c`: a rule that settles | Soot `#1b1714`: a sheet closed by fire |
| 5 | Convergence gold `#f2c14e`: true, ready | Seed cyan `#58dce0`: growth counted step by step | Brass `#c8963a`: spent rounds counted as standing |
| 6 | Divergence magenta `#ff3d8b` (alerts only): escape | Divergence `#ff4a7d` (alerts only): growth with no rule | Gun steel `#2e3236`: what holds the sheet holds the crew |

Still rough (first pass): the Axiom's small helm snowflakes read a little like gears; the Exponent's icons need a
stronger language of their own and its inset cells are fine at console size; the Red Tithe has no rendered ground behind
its column (CLAUDE.md section 7), its crest's tooth reads a little like a bone at small size, and on the kit sheet the
narrowest 120 px key trims "Overload" in both painted skins (the console's own keys fit).

## The Exponent its own collective, the Red Tithe in wood (page version 30)

The owner, 2026-11-07, on version 29: "I changed my mind. Exponent is not a mix of two races. It is its own thing.
Inspired by Borg. It's ship ui is a metalic utilitarian look. With veins a and tentacles and barnacles growing. Like am
abandoned rusted ship at sea. Take over by reefs and coral", and "More brown wood on the red tithe". The Exponent column
of the version 29 table above is superseded by this one. We take the idea of an assimilating collective, never its
names, symbols or look (CLAUDE.md section 1: take the shape, not the text).

| | The Exponent (version 30) |
|---|---|
| World view | One collective that grows by taking in ships, stations and crews; nothing decorated, everything function; machines run long past any refit, and what it absorbs keeps growing on them (provisional, survey N2). Three traditions: the assimilators (hatch keys, patch plates, the modular hull, the crest), the conduit-keepers (sheathed veins with a dim pulse), the reef (barnacles, coral, sponge, anemones, tube worms, cabled tendrils, salt) |
| Method | Painted in canvas (first pass): brushed steel and grating, rust blooms and runs, salt crust and verdigris, under a layered reef; every scatter from `FCK.rng`. A second hook, `overgrow(x, rects)`, paints one layer over the laid-out console: cables in the gaps between panels, reef along the column's outer edges, kelp and coral hanging from the top strip. To be modelled in Blender |
| Button shape | **Hatch key**: a steel access hatch on a two-knuckle hinge, a dog latch, four hex bolts and a recessed slot, barnacles on the hinge end; hover lights the slot, pressed sinks it and flares |
| Frame | Riveted chamfered plate round a dark well; a reef mass swelling out of every corner, cabled tendrils along the top and right, a pulsing vein along the foot, rust runs and tide lines |
| Light | A hard work light from the upper left; the only glow is the cold lumen in slots, veins and lenses |
| Icons | Solid stencil marks in lumen, broken by stencil bridges |
| Type | Allerta Stencil over Chivo Mono |
| Progress | Barnacle cells whose apertures glow lumen (breach on warning); fills are a lit vein |
| Hero pieces | A name plate with a porthole lens; a helm of eight square hatch keys round a round valve-hatch stop over grating, its gaps packed with growth; a ship display bezel climbed by reef; a bolted gauge; caged bulkhead lamps; a crest of seven unlike plates bolted into a ring round a branching conduit; a column ground of riveted bulkhead plates mostly overgrown (the page's new optional `column` image, `docs/mockups/skins/README.md`) |
| Palette | Hull steel `#5d686d` (the plate as it was taken), Rust `#8c4522` (running past any refit), Reef coral `#d27a6a` (growth tolerated and built on), Barnacle bone `#e3dbc6` (what has settled and stays), Lumen `#8be6dc` (power carried where needed, no further), Breach `#ff5a36` (alerts only: the hull open to the sea) |

The Red Tithe in version 30: the sheets are nailed to dark, scarred oak (grain, knots, knife gouges, tallies, scorches,
blood soaked into the grain) under strap iron; the column is a plank wall (the `column` image); frames are four boards
with iron corner straps; the strips a two-plank beam; the trap jaws keep steel jaws on oak stakes; tabs are notched tally
sticks; the helm's revolver cylinder sits in an oak wheel rim with an iron tyre; the systems display is pinned by knives
with a hatchet in one corner; the dial in an oak case; the crest's tooth is now a two-rooted molar on an oak roundel.
Palette row 4 is Hall oak `#4a301c` (the boards every sheet is nailed to: the Tithe's hall, where debts are posted and
paid), replacing Soot. Still rough: on the kit sheet's narrowest 120 px key "Overload" still crosses the jaws (a halo of
paper keeps it legible); the Exponent's corner reef is busy at 32 px.

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

**Organic skins are the exception** (version 25): every piece is sculpted at its exact slot size by parametric sculptors (`tools/blender/organic_components.py`), so any size is a re-run; blocked out with placeholders first, so their manifests record each slot's pinned rect, not margins.

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
| U4 | When does the player see another faction's skin? | **Decided** (survey, 2026-11-01): flying a ship of that faction (a captured ship keeps its builder's console) |
| U5 | The Kethran skin's direction? | Answered by the owner: modelled in Blender, "like starcraft" |
| U6 | Does a skin change only images, or also sounds and type? | **Decided** (survey, 2026-11-01): pictures, colours, type and button sounds; the owner adds "Some ships have features that other ships do not" |
| U8 | The six new races (including the Kelvane Institute): names, whether they join the campaign, and whether the combat race is the Kethran Hegemony's own direction? | **Closed** (survey, 2026-11-01): "They are factions. This question is irrelevant." |
| U7 | Are the factions' world views, bodies, technology and ships as the skins read them? | **Decided** (survey, 2026-11-01): yes, as written; the owner: "I'm not gonna let you make any cultural or story decisions without consulting me first" |
| U10 | The hunter race: a new race, and is "Ssarith Hunt" the name? | **Closed** (survey, 2026-11-01): outdated |
| W8 | What fills the space between the Brood's panels (the owner: no plain black)? | **Decided** (survey, 2026-11-01): tissue, one grown body over the column (built in version 26), see "The Brood's picks from the survey" |
