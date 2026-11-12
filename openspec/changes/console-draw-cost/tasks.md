# Tasks: console-draw-cost

- [x] 1.1 Capped panel redraws (`panel_redraw_hz`, `panel_redraw_hz_lowspec`), at once when touched (PR #84)
- [x] 1.2 Rounded rectangles as cached meshes and fills from cached triangles (an interim step; 1.4 replaces the meshes)
- [ ] 1.3 Stills on main and the branch, diffed; any difference fixed or shown
- [ ] 2.1 The console's panels in a `SubViewport` rendered only when a panel redraws; overlay and hover cards stay live
- [ ] 2.2 A test: `Controls()` coordinates are the same with the viewport as without
- [ ] 2.3 Premultiplied-alpha compositing; stills diffed
- [ ] 3.1 Rounded rectangles and outlines as 9-slices from a corner atlas
- [ ] 3.2 Icons baked into an atlas per skin at load
- [ ] 3.3 The Concord's pieces as 9-slice and hero pieces, baked at load
- [ ] 3.4 Text in its own layer per panel
- [ ] 4.1 Draw calls and FPS on the Pi 5 at each step; stills diffed at each step
