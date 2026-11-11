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

## D3. The three forms

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
| FV3 | Which forms make the line? | All three, one per school: the frigate a tree, the cruiser a lattice, the capital a Julia slice | FV3 |
| FV4 | How do phones draw them? | A mesh baked from the same field (marching cubes, a few LODs) on phones; the raymarch on desktop | FV4 |
| FV5 | How does the game hit them? | The C# copy of the field, tied to the shader's by a test (D6) | FV5 |
