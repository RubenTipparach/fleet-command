# Fractal vessels (mockup 37)

- Link: https://claude.ai/artifact/2mUTtEaxfqSAnX94REdZHj
- Shows: raymarched fractal vessels with no mesh (seven forms since v3), first one per school of the Axiom's draft culture:
  - a 4D quaternion Julia set sliced into 3D;
  - a chain of folded Menger blocks;
  - a branching tree.
- They sit beside the Concord's Petrel at true scale, with shape and cost controls and a frame-time readout. On desktop and phones (v2), marched at half resolution.
- Made by: `docs/mockups/fractal-vessels.html`, the very file published. Its data comes from `data/space/starfield.json`
  and `assets/ships/concord/petrel_level.glb`, inlined by `tools/mockups/inline_data.py`.
- Change: `openspec/changes/fractal-vessels`. Shots: `docs/screenshots/mockups/fractal-vessels/` (desktop, phone and portrait).
- Published: 2026-11-10, version 1, from the cloud session (branch `claude/gallant-brown-1vddu9`).
- Version id `1794339810-88ef`.
- 2026-11-10, version 2 (owner: "Make this thing phone friendly please! Also half resolution ray match if you don't mind"):
  phone friendly (a folding sheet upright, a narrow column sideways, 44 px targets) and marched at half resolution with
  the Petrel's depth kept. Published from the cloud session on `claude/vigilant-keller-s93pth`, artifact version 2,
  version id `1794341490-ee6b`.
- 2026-11-11, version 3 (owner: "theres someartifacts with the square one", "more interesting ship shapes, triangles?",
  "Mandelbulb?", Benesi, icefractal.com): the lattice's speckle, grain and stepped edges fixed; the delta lattice,
  octahedral lattice, Benesi pine and Mandelbulb added (the Icebox tried and left out). Published from the cloud session on
  `claude/vigilant-keller-s93pth`, artifact version 3, version id `1794380439-435c`.
- 2026-11-11, version 4 (owner, of the KIFS x T Benesi render: "this is what I wwas referring to"; "100% should appear more
  damaged, make a testing scene for me to shoot it with phasers and torpedos!"): the ribbed truss, the pine on Benesi's own
  rule, stronger damage, a firing range for phasers and torpedoes, the damage slider fixed; the Abox tried and left out.
  Published from the cloud session on `claude/vigilant-keller-s93pth`, artifact version 4, version id `1794384206-9c47`.
- 2026-11-11, version 5 (owner, of the KIFS x T Benesi It2 render: "go make it, also add some more colors and shading to
  the ships"; then "I liek the blue, purple green solors"): Benesi KIFS replaces the ribbed truss (Mandelbulber's octahedral
  KIFS with Benesi's turn before the third fold, matched to the render's silhouette at its own camera); every form in its
  own blues, purples and greens, with accent masks and a fuller light. Published from the cloud session on
  `claude/vigilant-keller-s93pth`, artifact version 5, version id `1794391519-5368`.
- 2026-11-12, version 6 (owner: "Benesi kifs not animated, can you setup some battle situations mockups with the vessels?
  and render me a video of what a battle might look like?"): the KIFS breathes (its slider turns Benesi's turn); three
  battles under Battle (Ambush, Skirmish, The line) with a film camera; the three rendered to MP4 by
  `tools/mockups/render_video.mjs` (1280 x 720 copies in `docs/screenshots/mockups/fractal-vessels/battles/`, the
  1920 x 1080 originals sent to the owner). Published from the cloud session on `claude/vigilant-keller-s93pth`, artifact
  version 6, version id `1794448148-9fd3`.
- 2026-11-12, artifact version 7 (the page's v6 with a phone fix: the battle caption under the title on an upright phone,
  name tags under the panels). Version id `1794448516-5228`.
