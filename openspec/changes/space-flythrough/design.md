# Design: space fly-through

## 1. The clouds, ray-marched

A full-screen fragment shader marches each pixel's ray through a density field:

| Part | How | Numbers |
|---|---|---|
| Noise | Gradient noise on PCG3D, an integer hash (Jarzynski and Olano 2020), quintic fade; never `fract(sin)` | lattice 1 unit |
| Fractal | 5 octaves, lacunarity 2.03, gain 0.5, a fixed rotation between octaves so no axis shows | |
| Warp | the point moved by three 2-octave fields, 1.4 units, before the 5-octave field | scale 0.018 per metre |
| Shape | a thick sheet that wanders in height (70 m half width), so there is open space to fly through and walls beside | |
| Gas | `smoothstep(0.07, 0.6, n)`: emits its palette's two colours, mixed by a third field; extinction 0.026 per metre at full density | |
| Dust | a second field, emitting almost nothing, extinction 0.16 per metre: the dark lanes | |
| Buried stars | 4 lamps in the gas, a warm core (Gaussian, 22-34 m) and a soft 1/r^2 reach | |
| March | up to 64, 110 or 150 steps (Low, High, Best), each `max(1.2, 0.022 t)` m, doubled in empty space; stops at 1 % transmittance or 900 m | |
| Jitter | the first step offset by PCG3D of the pixel and the frame, then each frame blended 55 % with the last, so no bands and no grain | |
| Output | the stars' layer times the transmittance, plus the gas's light, ACES tone curve, gamma 2.2 | |

The march renders at 45, 70 or 100 % of the window and is drawn up to it.

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

Measured so far only in the headless shots on the owner's Mac: at Best (100 %, 150 steps, 1920 x 1080) the page's meter read
25-45 fps, depending on how much cloud is in view. High and Low are cheaper; they are not yet measured in a browser window. Too heavy as is for a 2022 phone (survey T3): a phone build would march a quarter-resolution buffer
with fewer octaves, or play a baked loop.

## Open questions

| Question | Recommendation |
|---|---|
| Should the main menu fly through this, in place of the flat stars? | Yes, at High on desktop, a baked still on phones |
| Should nebula sectors in battle use ray-marched clouds? | Not yet: measure the cost beside the battle first |
