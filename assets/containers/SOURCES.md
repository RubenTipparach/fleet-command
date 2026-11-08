# Containers: where their textures came from

The freighters' container (openspec/changes/freighters, task 2.5; mockup 24) takes its surface from a CC0 texture set
(owner, 2026-11-07: "you need like better cargo textures, lookem up online for cc0 options, back ao and get diffuse and
normals and specular for each container"). The source maps are fetched by `tools/textures/fetch_cc0.py`, which refuses an
asset whose page does not say CC0, into `tools/materials/raw/cc0/` (never committed). What is committed is what
`tools/blender/build_container.py` bakes from them onto the container's own box.

| Asset | Author | Licence | Maps used (2k JPG, downloaded 2026-11-07) | SHA-256 |
|---|---|---|---|---|
| [Container Side](https://polyhaven.com/a/container_side), Poly Haven | Dimitrios Savva | CC0 (https://polyhaven.com/license) | Diffuse (its luminance, the green taken out) | `f6d88ea2c1f7e6edb3d969af5610017bc8ccf455ef45f99ba5bac60a3963fda4` |
| | | | Normal, OpenGL (`nor_gl`) | `5bf3d7b304d98bec2065ad2501926cf880e014442bd1457a6a60df79be3a4d34` |
| | | | Roughness | `fb6cdbebffb5214729e9e56dc3800544e29ff534200feb77e5d7ea2d92f9c99f` |
| | | | AO (fetched, not used: the AO is baked from the modelled container) | `a340cc49542caa832326ad22826df796f62ca12e17ff1dc7657eb765bb563a35` |
| | | | Displacement (fetched, not used) | `329deeee15357f7060765d1aca8144857d7f8727f4986da887bbd7dd563a9d91` |

The baked maps (`container_diffuse.png`, `_normal`, `_ao`, `_rough`, `_specular`) and `manifest.json` are written by the
generator; never edit them by hand.
