# Design: ship-schematics

- **Source**: the exported glb, the same geometry the battle flies (CLAUDE.md section 14: the ships are exported from their
  mockups). No hull rule has a second copy.
- **Quads**: vertices are welded by position. A triangle pairs with its neighbour across a shared edge when the two are
  nearly coplanar and make a convex quad, best pairs first, in a fixed order. The lines left out:
  - edges between faces in one plane, thinned so parallel lines in one plane stay 36 px apart at 1024 px;
  - edges shorter than 3 px;
  - edges with no upward face beside them.

  Creases and open borders are always drawn.
- **Layers**: white RGB, the drawing in alpha. A skin recombines them as hull tint times hull alpha, then wire tint times
  wire alpha over it. The game uses the skin's Signal colour for the hull and Sky for the wire.
- **Scale**: 1024 px square, the longest top-down extent fitted with a 6 % margin, centred on the hull's top-down box. The
  manifest gives metres per pixel and the origin's pixel, so mounts can be placed over the picture.
- **Stand-ins**: hulls not designed yet (the Ssarith Hunt, the Kethran, the Vos) have no schematic. The systems display
  keeps their outline, and the shipyard says "a stand-in hull".

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| H1 | Should each skin draw its own line weight or glow over the layers (the Krai's harsher light)? | No: the same layers, the skin's colours only |
| H2 | Should the mockups (battle console, campaign shipyard) show the schematics too? | Yes, task 2.1 |
