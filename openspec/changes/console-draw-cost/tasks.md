# Tasks: console-draw-cost

- [x] 1.1 Capped panel redraws (`panel_redraw_hz`, `panel_redraw_hz_lowspec`), at once when touched (PR #84)
- [x] 1.2 Rounded rectangles as cached meshes and fills from cached triangles (an interim step; 1.4 replaces the meshes)
- [x] 1.3 Stills on main and the branch, diffed; any difference fixed or shown
- [x] 2.1 The console's panels in a `SubViewport` rendered only when a panel redraws; overlay and hover cards stay live
- [ ] 2.2 A test: `Controls()` coordinates are the same with the viewport as without
- [x] 2.3 Premultiplied-alpha compositing; stills diffed
- [x] 3.1 Rounded rectangles and outlines as 9-slices from a corner atlas
- [x] 3.2 Icons (and the weapon glyphs) baked into one atlas at their exact pixels, from each kit's own drawer (IconAtlas)
- [ ] 3.3 The Concord's pieces as 9-slice and hero pieces, baked at load
- [x] 3.4 Text in its own layer per panel (Fonts.UseLayer)
- [x] 3.5 Untextured shapes batched into one draw per item (Draw2D, 211 call sites)
- [x] 3.6 Rendered kits' pieces packed into shared atlas pages (RenderedPieces.Pack)
- [ ] 4.1 Draw calls and FPS on the Pi 5 at each step; stills diffed at each step
