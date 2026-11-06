# Design: space fly-through

## 1. The clouds, ray-marched

The noise is baked once, at load, into a tileable 3D texture on the GPU, then a full-screen fragment shader marches each
pixel's ray through it. Version 1 computed the noise in place, about 130 hashes a step, and ran 16 fps at High on an M4 Max
(owner, 2026-11-06: "the performance is terrible!").

| Part | How | Numbers |
|---|---|---|
| Noise volume | 128 cells a side, half floats, baked in 128 slice draws (9 ms): r a 5-octave gradient noise, g 3 octaves (the dust), b and a 2 octaves each (the warp and the tint). Gradients from PCG3D, an integer hash (Jarzynski and Olano 2020), on the lattice wrapped to the period, octaves at exact doublings of an 8-cell period, so it tiles | 16 MB |
| Reads | five a step with the hardware's trilinear filtering: the warp, the cloud and dust, a detail octave at 2.7x and a fine one at 7.3x (the scales are not whole multiples, so the 440 m tile does not show) | 1 cell is 3.4 m |
| Shape | a thick sheet that wanders in height (70 m half width); outside it the march strides on without reading | |
| Gas | `smoothstep(0.07, 0.6, n)`: emits its palette's two colours, mixed by the tint field; extinction 0.026 per metre at full density | |
| Dust | a second field, emitting almost nothing, extinction 0.16 per metre: the dark lanes | |
| Buried stars | 4 lamps in the gas, a warm core (Gaussian, 22-34 m) and a soft 1/r^2 reach | |
| March | up to 48, 80 or 120 steps (Low, High, Best), each `max(1.6, 0.03 t)` m, longer through empty gas; stops at 2 % transmittance or 900 m | |
| Jitter | the first step offset by PCG3D of the pixel and the frame, each frame blended 55 % with the last, so no bands and no grain | |
| Resolution | the march at 40, 65 or 100 % of the window; the stars always at full size, added behind it by the light it lets through, then the ACES tone curve | |

## 2. The stars

33,000 points from Mulberry32 (seed 20261105): 26,000 on a far shell 1.5-2.4 km out, directions uniform on the sphere and
kept more often near a tilted galactic disc (a Gaussian of latitude, 0.22 rad), and 7,000 near the path (a Gaussian cloud,
160 by 90 by 160 m). Classes, brightness and colours are `data/space/starfield.json`'s (inlined), so most stars are faint,
3 % brilliant. Each is a soft point (core and halo), larger when near. The clouds dim them by the light they let through.

## 3. The flight

A closed centripetal Catmull-Rom spline of 10 points, 240 s a loop at Cruise (Slow 0.4x, Fast 2.5x), the camera looking a
little ahead and banking with the curve. A drag turns the look (yaw, pitch to 1.2 rad); it stays where it is left, and Look
ahead puts it back (CLAUDE.md section 8: the camera is the player's).

## 4. Cost

Measured with `tools/mockups/flythrough_perf.mjs` (Chromium, ANGLE on Metal, Apple M4 Max, 1920 x 1080), milliseconds a
frame with the GPU finished after each, uncapped:

| Quality | Version 1 (noise in place) | Version 2 (baked volume) |
|---|---|---|
| Low | 50 fps | 1.3 ms |
| High (the default) | 16 fps | 4.5 ms |
| Best | 8 fps | 8.9 ms |

All hold the display's 60 fps. A 2022 phone (survey T3) has perhaps a tenth of this GPU: Low would be near its limit, so a
phone build would march at a quarter of the window or play a baked loop.

## Open questions

| Question | Recommendation |
|---|---|
| Should the main menu fly through this, in place of the flat stars? | Yes, at High on desktop, a baked still on phones |
| Should nebula sectors in battle use ray-marched clouds? | Not yet: measure the cost beside the battle first |
