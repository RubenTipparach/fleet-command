# Design: fractal-vessels

## D1. What the owner asked for, and how the culture rule reads it

The owner (2026-11-10) wants "a fractal type of life form. and interdimensional being", and asks whether its ships can
be raymarched fractals. CLAUDE.md section 1 forbids deriving a design from a species' body. There are two framings, and
the owner chooses between them (survey FV2):

1. **The vessel is the being.** The people have no ships. What a captain fights is the part of a being from a higher
   dimension that crosses our space: a 3D slice of a 4D shape. Here the shape is not a design taken from a body. It is
   the being itself, as our space can hold it, and the being has nothing to design. Its makers' culture (if any) shows in
   its console, emblem and conduct, not in its hull.
2. **The vessels are made, and fractals are how the makers think.** This is the Axiom's draft (survey N1): "every true
   thing is a rule and its iteration", kept by three schools. Each form in the mockup comes from one school:
   - the **escape-time school** (it marks a point by how long it takes to escape): the quaternion Julia slice;
   - the **tilers** (exact plates, folds and repeats): the Menger chain;
   - the **branchers** (a rule for one branch, applied again): the tree.

The mockup labels each form with its school, which suits either framing. A blank answer to FV2 takes framing 2, the
recommendation, because it keeps the Axiom's console (mockup 25) and its culture whole.

## D2. Raymarching on a box proxy

Each vessel is a box mesh of 12 triangles, drawn with its back faces, so it still draws when the camera is inside it.
Its fragment shader works like this:

- It takes the camera position into the box's object space, which is the fractal's own space. A uniform scale sets the
  length in metres.
- It finds where the pixel's ray enters and leaves the box.
- It marches from the entry: at each step it moves 0.85 of the distance the function returns. It stops when that
  distance is under one pixel's width at that range (`t * uPix`, with `uPix = 2 tan(fov / 2) / height` times 1.2). It
  also stops when it leaves the box or after N steps.
- On a hit:
  - the normal is the gradient of the function, from four extra samples on a tetrahedron;
  - occlusion comes from five samples along the normal, and the step count is a second cue;
  - colour comes from the orbit trap (how close the iterate came to the axes and the origin);
  - lighting is one key light and a sky term, shared with the scene;
  - the folds glow from inside, and a rim light adds the edge.
  - It writes the hit point's depth through the camera's projection.
- On a miss, it draws a halo from the nearest the ray came to the surface, written at the far plane. The box has a
  margin of 0.22 units round the vessel, so the halo is not cut off.

Each form is its own program (a `FORM` define), so no step branches on the form.

Three numbers are set per form, because the forms differ in how open they are:
- **the trap's scale** (Julia 0.8, lattice 1.0, tree 1.2);
- **how much the folds glow** (1.0, 0.5, 0.8);
- **how strong the occlusion is** (1.0, 0.6, 1.0). A Menger sponge is porous at every level, so full-strength occlusion reads
  it as dark everywhere.

## D3. The first three forms

| Form | Length | Function | The slider |
|---|---|---|---|
| Julia slice | 132 m | `z <- z^2 + c` in quaternions; `c = (-0.62, 0.42, 0.18, 0.12)` (a non-zero `w` part, so the slices at +w and -w differ); the 3D point is `(z, x, y)` with `w` the slice; distance `0.25 sqrt(|z|^2 / |dz|^2) log |z|^2` | the 4th coordinate, `w = 0.55 x slider` |
| Fold lattice | 112 m | Knighty's Menger fold (mirror, sort, turn, scale 3, step out) in five blocks, each 0.7 the size of the one behind; iterations minus 5 fold levels (4 at the defaults, so the finest tunnel is about 0.7 m and never under a pixel at battle range); a block more than 0.04 away is skipped | the turn per fold, `0.1 + 0.07 m + 0.32 x slider` rad |
| Branching | 128 m | a tapered segment (0.92 long, radius 0.15 to 0.09), then mirror, tilt and roll 90 degrees (so every split is mirror-symmetric about the centreline), scaled 1/1.52 a level; rooted at the stern | the branch angle, `0.42 + 0.22 x slider` rad |

- **No stretching.** A non-uniform stretch of a fold smears it (the first lattice, which was stretched to the hull's
  length, came out streaked). The lattice gets its length from the chain of blocks instead.
- **No envelope.** Cutting a dense fractal to an ellipsoid hides it behind the ellipsoid's own surface. The first
  lattice and the first Mandelbox both drew as speckled eggs. So every form's outline is its own.
- **The palette is provisional**, the Axiom placeholder line's: hull `#e8ecf6`, deep `#2f2696`, trim `#4b3fd8`, glow
  `#3fe0e6`, plus a rim `#b99cff`. It waits for FV1 and FV2.

## D3a. Version 3: the lattice's artifacts, and four more forms

The owner, 2026-11-11: "I like the square one and the julia slice, but theres someartifacts with the square one, I'd like
more interesting ship shapes, triangles? more distinct looking shapes, the fold latice looks most like a spaceship to me";
then "Mandelbulb?"; then two Benesi fractals from Wikimedia Commons ("benisi is most structurally sound tome"); then
icefractal.com.

**The lattice's artifacts** were the shader's, not half resolution's (a full-resolution shot showed the same):
- **Purple speckle.** It came from real tunnels two or three pixels wide, coloured deep by the orbit trap. Two fixes:
  - the trap now colours from the first `trapN` iterations only (2 for the lattices), so colour comes in broad panels;
  - the tilers' folds stop once a cell is under 4 pixels (`LOD_PX`). The marcher sets a pixel's width at each sample
    (`gPix`), so a far block folds less than a near one.
- **Grain.** The normal is now read over 2.5 pixels (it was 1.5), and the occlusion samples at least 2 pixels a step.
- **Stair-stepped edges.** A ray that passes within 1.5 pixels of the surface without touching it now shades the surface
  at its nearest point, faded by how near it came. Silhouettes are soft, and a half-resolution march scales up without
  steps.
- Each form has its own step share (`uStepK`): 0.85 for the exact folds, 0.8 for the Mandelbulb and 0.5 for the Benesi
  pine, whose estimate overshoots.

**The new forms** (v3), each in its own program as before:

| Form | Length | Function | The slider |
|---|---|---|---|
| Delta lattice | 140 m | The fold lattice's Menger blocks laid out as an arrowhead: a spine of five (half sides 0.42 to 0.14) and two swept wings of three (0.28 to 0.14), mirrored about the centreline. From above it is a narrow nose on swept wings | the turn per fold |
| Octahedral lattice | 130 m | A Sierpinski octahedron: mirror the axes, sort toward the nearest corner, scale by 2 toward it (six copies at the corners); three blocks (half sizes 0.8, 0.56, 0.4) overlapping into one spear. Its faces open into triangles of triangles | the turn per fold (0 is clean) |
| Benesi pine | 150 m | After Benesi's power 2 rule (his "pine tree", posted on fractalforums.com): `(x, r) -> (x^2 - r^2, 2 x r)` along and away from the axis, the angle round the axis doubled, plus the point's own seed. In any plane through the axis it is the Mandelbrot set: the heavy bulb aft, the needle at the bow. The estimate `0.5 log r r / dr` overshoots, so it steps at 0.5 | shifts the seed along the axis |
| Mandelbulb | 120 m | The power P bulb, `z -> r^P (sin P theta cos P phi, sin P theta sin P phi, cos P theta) + c`, its pole along the hull | the power, 5 to 11 |

- **The Benesi turn.** The Commons hybrid ("KIFS x T Benesi", a Sierpinski octahedron crossed with Benesi's transform) is
  CC BY-SA 4.0, so only its idea is taken. Its idea is to turn space so the diagonal lies along an axis, fold there, and
  turn back.
  - Tried on the octahedron, the turn collapsed the triangles into round blobs: its second mirror plane, x + y = 2z, is
    not one of the octahedron's symmetries. So the octahedral lattice is the plain Sierpinski fold, and Benesi's own form
    is the pine.
  - The CC0 "Benesi - Pwr2" image is the pine's kind of shape. Nothing of either image is copied.
- **The Icebox** (icefractal.com's article, its code MIT): a Mandelbulb with a box radius, then a box fold and a ball fold.
  It was ported and left out, because it came out a solid cube: its folds keep every point within the box from escaping.
  The article gives no values for its power and fold limit, and those are what decide whether it opens up. A port with
  the article's own values is a task (7.1).

## D3b. Version 4: the Benesi truss, damage, and a firing range

The owner, 2026-11-11:
- "benisi pine doesnt look like thereference", then, with the "KIFS x T Benesi Mag Forward" render: "this is what I wwas
  referring to";
- "I love the damage amount, 100% should appear more damaged, make a testing scene for me to shoot it with phasers and
  torpedos!";
- with an "Abox - Mod Kali" render: "this is also another cool one we can try out too".

**The ribbed truss.** Mandelbulber's Benesi Mag Forward turn, read in its source (GPLv3, so only the maths is used), is a
rotation and nothing else: `t = x sqrt(2/3) - z sqrt(1/3)`, then `((t - y) sqrt(1/2), (t + y) sqrt(1/2), x sqrt(1/3) +
z sqrt(2/3))`. It turns space onto the diagonal. v3's mirror-and-turn-back was wrong, and that is why it collapsed.

Running the hybrid in a test harness (the turn, then the kaleidoscopic fold) got the render's silhouette when the turn ran
on the first two iterations only: a crown, three arms round a triangular centre, and a keel. It did not get the walls.
The render's look is a structure, so the truss builds that structure on purpose:
- a Sierpinski octahedron drawn as a frame, every level's edges ribbed beams (40 rings an edge, fading before they are
  finer than three pixels) with rounded caps at the corners;
- every level is kept, so each face is a triangle of beams round a triangular opening with the next level nested in it;
- the Benesi turn on the third fold twists the inner levels;
- three trusses overlap into a spear;
- the slider is the beam weight.

**The pine** is now Benesi's own power 2 rule, as Mandelbulber writes it:
- the square's sign is flipped where the seed is forward of the waist and the point is farther along the axis than from
  it;
- round the axis the turn is -2 |x| / r;
- it steps at 0.3 with twice the steps.

Its broadside (`pine_side`) is the CC0 "Benesi - Pwr2" render's kind of shape: a needle, flaring into a body.

**Damage at 100%:**
- folds lost: 55% (v3's 70% melted the Julia slice);
- the hull is charred by three octaves of value noise on a PCG hash, in streaked patches up to about half its area, with
  flickering ember lines along their edges;
- each damage hole is mirrored across the centreline and half again as large, with a dimmer glow round it.

v3 had also commented out the line that hands the slider's damage to the shader. That is fixed, so the slider works again.

**The firing range.**
- **Firing.** Under Fire, pick Phaser or Torpedo, then click or tap a vessel; a drag still orbits.
- **Where a shot lands.**
  - Each visible vessel is drawn with its own march in a `PICK` program into a 1 x 1 float target at the clicked pixel.
    The target holds the hit point in the vessel's space and which vessel it was, so no shape has a second copy.
  - The pick takes the screen's pixel size. Its own 1 x 1 size first made every ray stop at the box.
- **Phaser:** a beam from the Petrel's bow (or just off the camera when the Petrel is hidden) burns a 2.6 m groove with
  an ember line that cools over 20 s.
- **Torpedo:** it flies for 0.7 s, bursts, and blows a 6.5 m hole with a glowing rim and a ring of char.
- **Hits kept:** each vessel keeps its last 24.
- **Repair all** clears the hits and the damage.

**Left out:**
- **The Abox.** A Mandelbox at scale -1.5 cut to a hull showed only its solid inside, a plain block. The render's twisted
  beams come from the Kali modification's own folds and rotations, which are not in hand. That is task 7.2.

## D4. Damage

- **Fold levels lost**: `iterations - floor(damage x iterations x 0.45)`, never fewer than 2. A hurt Julia slice swells
  and smooths. A hurt tree loses its finest branches. A hurt lattice loses its smallest tunnels.
- **Holes**: three named points per form, each a sphere of radius `r x damage` cut from the field (`max(d, -sphere)`),
  so a hole has real depth.
- **Light leaks round every rim**: `exp(-40 x distance to the rim)` in the glow colour, and the rest dims by up to 30%.
- In the game, the holes would be the simulation's damage zones. The field can be cut anywhere, and nothing needs to be
  modelled (D6).

## D5. Cost

- The page shows frame rate, frame time and the pixels drawn. Iterations (3-14), march steps (32-320) and render scale
  (25-150%) are on sliders.
- **Measured in a cloud session**, Chromium's software renderer on 4 CPU cores at 1920 x 1080: the Julia slice 76 ms, the
  Mandelbox (since dropped) 156 ms, the first lattice 430 ms. These numbers only rank the forms. A cloud session
  measures no real frame time (CLAUDE.md section 9). The owner's reading on the RTX 3070 is the one that counts.
- **Per pixel the vessel covers**: up to `steps x iterations` evaluations of the function, plus 9 more for the normal and
  occlusion. That is about 160 x 9 = 1,440 at the defaults. Pixels the box covers but the vessel misses stop when they
  leave the box.
- **Phones** are out of scope for this mockup (owner, 2026-11-10). In the game they would need a cheaper path (FV4).

## D5a. Half resolution, and phones (v2)

The owner, 2026-11-10: "Make this thing phone friendly please! Also half resolution ray match if you don't mind" (this
replaces the day's earlier "dont worry about phone" for the mockup; survey FV4, how the game draws them on a phone, stays
open).

- **Marching at half resolution.** The vessels are on a layer of their own and are marched into a target half the drawing
  buffer's width and height, so a quarter of the pixels march. The target keeps linear light (half floats) and alpha, and
  has its own depth:
  - first the occluders' depth goes in (the Petrel and the figure, with a material that writes no colour), so the depth
    test still hides a vessel behind the Petrel;
  - then the vessels, which write their hit depth as before;
  - one full-screen triangle scales the target up with linear filtering, tone maps and encodes it as the vessels' shader
    would on screen, and lays it over the full-resolution scene.
  - A pixel's size in the march (`uPix`) is taken from the target's height, so the stopping distance and the normal's
    sample step grow with the pixel.
- **The cost.** In the cloud's software renderer (no frame time is claimed from it) the close Julia view took 542 ms full
  size and 136 ms at half: about the quarter the pixels predict. Silhouettes against the Petrel are half-resolution
  steps; at a phone's pixel density (2 to 3 device pixels a point, capped at 2) the target is still a pixel a point.
- **A switch**, March at half resolution under Cost, draws them full size, for comparing.
- **Phones.**
  - On a narrow screen held upright, the controls are a sheet along the bottom that starts folded to its header (Show,
    Hide), with the view buttons in a strip just above it that scrolls sideways.
  - On a phone held sideways, the controls are a 270 px column on the right.
  - The title folds to its name and the tracker link, with an About button.
  - Every control is a 44 px target on a touch screen, and the page keeps clear of the notch and the home bar.
  - A view button steps the camera back along its own line on a narrow screen, so the same width fits. This happens only
    when a view is picked: the camera is never moved for the player otherwise.
  - On a touch screen the march starts at 128 steps, not 160.

## D6. If it goes in the game

- **One source for each function.** A `.gdshaderinc` holds it for the Godot shader. A C# copy serves the simulation's
  hits, because the simulation has no GPU. Two copies of one rule break CLAUDE.md section 6.1 unless a check ties them,
  so a test samples both at fixed points and fails on any difference over 1e-4.
- **Godot**: a `ShaderMaterial` on a `BoxMesh` with `cull_front`, `render_mode depth_draw_always`, `DEPTH` written from
  the hit, and `LIGHT()` taking the scene's lights. The same structure as D2.
- **Exceptions to CLAUDE.md section 10**, to be listed in section 14: a raymarched vessel has no mesh from a Blender
  generator, no UVs and no baked maps. Its hull lights are glow terms in the field. The rules about not deriving designs
  from bodies, no z-fighting and symmetry still hold: the forms are mirror-symmetric by their folds.

## Open questions

| # | Question | Recommendation (provisional until answered) | Survey |
|---|---|---|---|
| FV1 | Whose vessels are these? | The Axiom's. They replace the placeholder line once approved | FV1 |
| FV2 | Is the vessel the being (framing 1), or made by a people who think in fractals (framing 2)? | Framing 2, which keeps the Axiom's draft culture and console | FV2 |
| FV3 | Which forms make the line? | Since v3, from the owner's notes (the lattice "looks most like a spaceship", the Julia slice liked): the frigate the octahedral lattice, the cruiser the fold or delta lattice, the capital the Julia slice; the Benesi pine and the Mandelbulb as other options, the tree dropped unless asked for | FV3 |
| FV4 | How do phones draw them? | A mesh baked from the same field (marching cubes, a few LODs) on phones; the raymarch on desktop | FV4 |
| FV5 | How does the game hit them? | The C# copy of the field, tied to the shader's by a test (D6) | FV5 |
