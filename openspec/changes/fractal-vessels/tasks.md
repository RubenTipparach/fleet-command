# Tasks: fractal-vessels

## 1. Mockup

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 Mockup 37, `docs/mockups/fractal-vessels.html`, with:
  - the raymarch on a box proxy that writes depth;
  - the three forms, each its own program;
  - the Petrel at true scale;
  - shape and cost controls;
  - a frame-time readout;
  - damage.
- [x] 1.3 Named shots at desktop size (`SHOOT_SIZES=desktop`, the owner: "dont worry about phone"), looked at
- [x] 1.4 Published. Then recorded in `mockup-work.md`, the tracker's repository copy and `docs/artifacts/fractal-vessels/`
- [x] 1.5 Survey questions FV1-FV5 on the culture framing, the faction, the forms, phones and the game's hits
- [x] 1.7 v2 (owner, 2026-11-10: "Make this thing phone friendly please! Also half resolution ray match if you don't mind"): the vessels marched at half resolution into their own target with the occluders' depth (design D5a), the phone layout, every shot at desktop, phone and portrait sizes in `docs/screenshots/mockups/fractal-vessels/`, republished
- [x] 1.8 v3 (owner, 2026-11-11: the lattice's artifacts, "more interesting ship shapes, triangles?", "Mandelbulb?", Benesi, icefractal.com): the lattice's speckle, grain and stepped edges fixed, the delta lattice, octahedral lattice, Benesi pine and Mandelbulb added (design D3a), shots at all sizes, republished
- [x] 1.9 v4 (owner, 2026-11-11: the KIFS x T Benesi render, "100% should appear more damaged", "a testing scene for me to shoot it with phasers and torpedos"): the ribbed truss, the pine on Benesi's own rule, stronger damage, the firing range, the damage slider fixed (design D3b), shots at all sizes, republished
- [ ] 1.6 The owner's review (approval for build, then for implementation)

- [x] 1.10 v5 (owner, 2026-11-11, of the KIFS x T Benesi It2 render: "go make it, also add some more colors and shading to the ships"): Benesi KIFS replacing the ribbed truss, every form's own palette, accent masks and a fuller light (design D3c), shots at all sizes, republished

- [x] 1.11 v6 (owner, 2026-11-12: "Benesi kifs not animated, can you setup some battle situations mockups with the vessels? and render me a video"): the KIFS's turn on the slider and drift, three battles with a film camera, `tools/mockups/render_video.mjs`, the Ambush rendered to video (design D3d)

- [x] 1.12 v7 (owner, 2026-11-12: the fold lattice, delta lattice and branching cut; the pine "just moves back and forth"): five forms, the battles re-cast with the Mandelbulb, the pine's ring twist (design D3e)

## 2. In the game (once approved for implementation)

- [ ] 2.1 Documented exceptions in CLAUDE.md section 14: no mesh, no UVs and no baked maps for a raymarched vessel
- [ ] 2.2 One distance-function source, `assets/shaders/fractal/<form>.gdshaderinc`. It is included by a Godot spatial
  shader on a box proxy that writes `DEPTH`, lit by the scene's lights.
- [ ] 2.3 The same functions in C# (`src/Core/Sim/Fractal/`), for hits and the damage zones. A test samples both at fixed
  points and fails on any difference over 1e-4.
- [ ] 2.4 Damage from the simulation's zones: fold levels lost and holes cut, passed to the shader as uniforms
- [ ] 2.5 Phones, per survey FV4
- [ ] 2.6 Game stills beside the mockup's shots, `docs/design/mockup-sync.md`, and a playtest on the owner's PC

## 7. Forms still to try

- [ ] 7.1 The Icebox with icefractal.com's own power and fold limit (v3's port, at a fold of 0.8 to 1, came out a solid cube)
- [ ] 7.2 The Abox (Mod 13 x Mod Kali-V3) with the Kali modification's folds and rotations (v4's plain Mandelbox cut came out a block)
- [ ] 7.3 Benesi KIFS's ladder walls and round openings (v5's ball fold and Menger folds both failed; design D3c)
