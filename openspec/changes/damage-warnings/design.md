## Context

A ship's damage is drawn today only as it is dealt (`ship-damage-visuals`: scorch marks with ember rings that cool over
20 s, torpedo holes, broken-off pieces burning for about 20 s, the core breach). Between hits a nearly dead ship looks
whole. The console's hull bar turns red below 35 % (`Panels.cs`, `Bar(..., hull < 0.35)`; the mockup's `uiTop`), and a
reactor heading for a meltdown is said once in the log by Engineering (`Battle.Reactor.cs`, `rx.Warned`). The owner asked
for the ship to look like it is dying, and for a flashing bar, then for the reactor to get one too (2026-11-09).

## Goals / Non-Goals

**Goals:** a hull state anyone reads at a glance, on every ship; one warning component that says what is about to kill
you and how long you have; the same in every skin; a phone budget.

**Non-goals:** new damage rules (nothing here changes the simulation; the reactor's count is read, not changed); hit
marks and breakup (they stay `ship-damage-visuals`); a warning for every system (only the two that end the ship).

## Decisions

### D1. Three hull thresholds, every ship

| Hull left | What the ship shows |
|---|---|
| 50 % to 35 % | **Smoke**: grey puffs from its worst-hit place, 5 a second, each living 2.2-4.2 s, swelling from 0.12 to about 0.5 of the ship's length and fading. Left behind in space, so a moving ship draws a trail |
| 35 % to 20 % | **Burning**: the smoke darker and 1.6 times as thick; flames at two places (22 a second, each 0.36-0.84 s, 0.13-0.22 of the length, yellow, orange and deep red); sparks thrown out fast (7 a second, 0.35-1 s); **scraps** of hull shed and tumbling away, glowing hot and cooling (1.4 a second, 3.5-6.5 s) |
| under 20 % | **Heavy**: all of it at twice the rate, from a third place |

- The places are fixed on the hull per ship, seeded by its id (the Core's `Rng`, not a hash of position: CLAUDE.md
  section 10), the first the worst, along the middle 60 % of the length and within 11 % of the centreline. In the game they
  are taken from the ship's own hits where it has them (`ShipDamage`'s scorch points, the heaviest first), so the fire is
  where the damage is; the seeded places fill in for a ship with fewer hits than places.
- Sized from the ship as drawn (the model's bounds, `ship_frame_x` in the game), so a frigate and a dreadnought burn in
  proportion.
- Every ship, yours and theirs (Open question Q2): a burning target reads as nearly done, which is the point.
- The flames are drawn with normal blending in saturated colour, on top of the hull: additive orange over a white
  Concord hull washes to white (seen in the mockup's first shots). Sparks stay additive.

### D2. One particle pool, a phone budget

- One pool for every ship, `max` 240 particles, the oldest of a kind reused first; four kinds (smoke, fire, spark,
  scrap). Sprites share two textures (the explosion's `cloudTex` and `glowTex`), scraps share one box mesh and material.
- Draw calls: in the game, one `MultiMeshInstance3D` per kind (4 draw calls whatever the count), billboarded quads with a
  per-instance colour and alpha, scraps a MultiMesh of one box. The mockup uses sprites (one call each), which is fine
  for a page.
- Budget at the phone layout (survey T3, a 2022 mid-range phone at 60 fps): 240 particles and 4 draw calls at most, no
  lights (the fire's glow is in the sprite, not an `OmniLight3D`), overdraw kept down by the smoke's 0.5 starting alpha
  and its fade. Measured on the owner's phone before the change is called done (task 3.4).
- **Reduced** (Options, Q4): smoke and warnings as Full; fire, sparks and scraps at a quarter of the rate, the pool at 80.

### D3. The warning band

- **One component** (`Panels.WarningBand` in the game, `warnBands` in the mockup) draws a list of warnings, so a third
  later (none is planned) is a line, not a copy (CLAUDE.md section 6.1).
- **Where**: the top of the live view, under the top strip, in the open span between the scene list and the Target box,
  centred. Two warnings sit side by side when the span holds two 300 px bands, else stacked. In an expanded panel's live
  strip, stacked under "Live · tap to return". The centre notice (a jump refused, a jump failed) moves down below them.
- **How it looks**: 380 x 38 px at most, in the skin's own `frame.inset` piece, its alert colour (Caution for Reactor
  cooling), a warning sign (a triangle with a bar and a dot, the icon guide's "warning" subject) and two texts: the title
  left, the value right. It **flashes**: lit (the face filled with the colour, ink black or white by contrast) for 60 % of
  each period, then dark with the colour in the frame and the text; both halves read. Period 1 s; 0.4 s when a meltdown is
  under 10 s off. Lit and still while paused.
- **Low hull integrity**: below `low_hull.warn_below` (0.35) of the hull, the share as a percentage. The top strip's hull
  bar, already red below the same share, labels itself Low hull integrity too: one threshold, one rule.
- **Reactor unstable**: while the core counts toward a meltdown (`Battle.Reactor.cs`: coolant offline or the core at its
  limit, `rx.Melt` rising to `R.MeltdownS`), "Meltdown N s" with N = `MeltdownS - Melt` rounded up, from the same numbers
  the step uses. As the core counts back (`Melt` falling at `MeltRecoverX`), **Reactor cooling** in Caution, "Safe in N s"
  (`Melt / MeltRecoverX`). Gone at `Melt` 0. The simulation gains a read (`Battle.MeltdownLeftS(ship)`), not a rule; the
  mockup gained the same `reactorCritical` and `MELT_RECOVER_X` its step now uses.
- **Tap**: Low hull integrity opens Damage control (the repair station); Reactor unstable opens the Power station's Reactor
  page. Its hover card names the reading and where a tap goes.
- **Sound** (Q3): the red alert cue (`assets/audio/sfx/red_alert.ogg`) once as each warning comes on, not repeating; the
  Engineering officer's line stays.

### D4. Every skin

The band and the flames are part of the Concord's master UI and so of every skin (CLAUDE.md section 7). What changes per
skin is how it is drawn: its own inset frame (the Krai's riveted plate, the Kelvane's brass bezel), its own alert colour
and its own typeface. The 12 faction pages are written from the Concord page (`tools/mockups/skin_page.py`). Checked on
the Concord, Krai and Kelvane pages: the lit face is drawn over the frame, because the Krai's inset glazes its middle and
dimmed the red under it.

### D5. Tuning in data

`data/battle/tuning.json` gains, with units in the keys:

- `low_hull`: `smoke_below_frac` 0.5, `fire_below_frac` 0.35, `heavy_below_frac` 0.2, `heavy_x` 2, `places` 2,
  `smoke_per_s` 5, `fire_per_s` 22, `sparks_per_s` 7, `scraps_per_s` 1.4, `smoke_life_s` 3.2, `fire_life_s` 0.6,
  `spark_life_s` 0.7, `scrap_life_s` 5, `max_particles` 240, `reduced_share` 0.25, `reduced_max_particles` 80.
- `warnings`: `hull_below_frac` 0.35 (and `Panels.cs`'s literal 0.35 reads it), `period_s` 1, `fast_period_s` 0.4,
  `fast_below_s` 10, `lit_share` 0.6, `band_w_px` 380, `band_h_px` 38.

## Costs

- Simulation: none. A read of the reactor's count.
- View: at most 240 particles stepped a frame (a few microseconds) and 4 draw calls.
- Console: one band, two at most, in the canvas pass the strips already make.

## Risks / Trade-offs

- **Smoke hiding the ship you aim at.** Its alpha starts at 0.5 and fades, it drifts behind a moving ship, and it is
  capped by the pool. If it still hides a target, the target's smoke can be drawn thinner (Q2).
- **A band over the view.** It sits where the view is empty (between the scene list and the Target box), and only while
  something is wrong.
- **Flash and accessibility.** 1 Hz, and 2.5 Hz under 10 s, both well under the 3 Hz photosensitivity limit (WCAG 2.3.1);
  both halves of the flash carry the text, so nothing is read only in the lit half.

## Open questions

| # | Question | Recommendation (provisional until answered) |
|---|---|---|
| Q1 | The thresholds: smoke below 50 %, fire, sparks and scraps below 35 %, heavy below 20 %; the warning below 35 % | As listed: 35 % matches the hull bar's red today |
| Q2 | Do enemy and friendly ships burn too, or only yours? | Every ship: a burning target reads as nearly done |
| Q3 | Does a warning sound? | The red alert cue once as each warning comes on, not repeating; the officer's line stays |
| Q4 | "Opt to have": an Options switch? | Options, Damage effects: Full (default) or Reduced (a quarter of the fire, sparks and scraps, for a slow phone) |
| Q5 | What a tap on a warning opens | Low hull: Damage control. Reactor: the Power station's Reactor page |
| Q6 | Reactor cooling: show it as the core counts back, or clear the band at once? | Show it, in Caution, with "Safe in N s", until it is safe: a captain who just restored coolant sees it worked |
