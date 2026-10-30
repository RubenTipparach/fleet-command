# Design: console-skins

Mockup 4. Our own designs after the qualities the owner named (CLAUDE.md section 1: take the shape, not the art). The
reference game's two skins (SFC2 manual printed pages 108 onward show its console) set direction only.

## The kit

Every screen draws only these pieces, by name (CLAUDE.md section 7). A skin supplies one image (or image set) per piece;
a missing piece is a build error.

| Piece | Kind | Slot at 1920 x 1080 (px) | Slice margins (px) | Used by |
|---|---|---|---|---|
| `frame.panel` | 9-slice | any, min 96 x 96 | 24 | every console panel, port panels, sheets |
| `frame.inset` | 9-slice | any, min 48 x 48 | 12 | readout wells, lists, the schematic's well |
| `bar.track` | 9-slice | any width x 14 | 7 left, 7 right | hull, capacitor, speed, power bars |
| `bar.fill` | 9-slice | any width x 10 | 5 left, 5 right | the bars' fills, tinted by state |
| `button` | 9-slice, 3 states | min 44 x 44 | 12 | every key: idle, hover, pressed, plus a lamp |
| `tab` | 9-slice, 2 states | min 40 high | 10 | tab rows, the arming states |
| `strip.top` | 9-slice | full width x 48 | 64 left, 64 right, 8 | the top strip |
| `strip.bottom` | 9-slice | full width x 48 | 64 left, 64 right, 8 | the bottom strip |
| `divider` | 9-slice | any x 6 | 12 | between stacked panels |
| `nameplate` | hero | 460 x 56 | none | the captain's plate |
| `helm.pad` | hero | 300 x 300 | none | the helm's 3 x 3 pad, live arrows drawn over |
| `ssd.bezel` | hero | 460 x 300 | none | the ship systems display's frame |
| `dial` | hero | 120 x 120 | none | radar and gauges, needle drawn live |
| `lamp.alert` | hero, 3 states | 64 x 64 | none | condition green, yellow, red |
| `crest` | hero | 96 x 96 | none | the faction's mark |

## The skins

| | Concord | Vos Syndicate | Kethran Hegemony (options, U5) |
|---|---|---|---|
| Owner's words | "sleek, utilitarian ... very sci-fi very minimal" | "so playful and pirate like" | to choose |
| Shape | Thin frames with small chamfers, long straight runs, segmented bar caps | Bulging rounded corners, hand-built plates, cabling along edges, uneven rivets | A: hard angular plates, notches. B: ornate filigree on stone |
| Palette | Deep navy glass, steel, cyan-blue light, white text | Rust, brass, bone, verdigris lenses, amber text | A: gunmetal, blood red, bone white. B: black stone, gold |
| Type | A clean technical face | A playful display face for headings, a sturdy face for numbers | A: stencil. B: engraved serif |
| Light | Edge-lit glass, soft glow lines | Warm lamp light, glossy brass highlights, worn paint | A: harsh top light. B: candle-warm gold |
| Ornament | Almost none: hairlines and ticks | Rivets, pipes, patches, painted markings | A: hazard notches, armour bolts. B: scrollwork |

## How a skin is made (after approval)

Each piece is modelled and lit in Blender (`panel-textures`, its kit `tools/blender/panel_kit.py`), rendered face on
to one image per state, and a 9-slice piece is modelled at a reference size with all its detail in the corners and
edges that stretch or repeat cleanly. The bake writes a manifest of each piece's size and margins; the game builds its
theme from the manifest. Budget per skin, provisional: about 30 images, 2048 x 2048 of atlas at most, a few megabytes
compressed, the same on phones.

## The page

Stand-ins for the bakes are drawn in canvas with the same slot sizes and margins, so the page proves the layout and the
slicing; their look is the design language, not the final art.

## Open questions

| Id | Question | Recommendation (built in the mockup) |
|---|---|---|
| U4 | When does the player see another faction's skin? | Both: a captured ship keeps its builder's console, and Options can pin one (provisional) |
| U5 | The Kethran skin's direction? | Show options here and choose (the page shows A and B) |
| U6 | Does a skin change only images, or also sounds and type? | Images, colours, type and button sounds (provisional) |
