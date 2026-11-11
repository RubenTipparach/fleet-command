#!/usr/bin/env python3
"""Compose many nebula sky recipes from themed palettes, for tools/space/nebula_skies.py to paint.

The owner, 2026-11-04: "make sure to generate lots of cool nebulas to be used for our demos, I'm getting sick of the
same one being used all the time". Star Crew's four recipes (bloom, ember, quiet, verdigris) were painted by hand; this
writes more of the same kind from a palette and a seed each, so a new sky is one line in THEMES. Each is built as the
painted ones are: one main cloud of overlapping strokes along a long path, dark lanes down its spine and its core on it,
one or two smaller clouds on the far side, and a gently tilted galaxy band. They keep to the band of sky the battle camera sees (it looks down at the battle plane from
about 20 degrees above it), latitudes about -35 to +45, spread round every longitude so any heading shows nebula.

The composed recipes are written into data/space/skies/recipes.json after the hand-painted ones (which this leaves
alone), each marked "composed": true; then run tools/space/nebula_skies.py to paint them. Deterministic: a recipe is a
function of its theme and seed only.

Usage: python3 tools/space/nebula_recipes.py
"""

import json
import math
import os
import random

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
RECIPES = os.path.join(ROOT, "data", "space", "skies", "recipes.json")

# The mesh's vertex budget: the painters' own (more did not soften anything: hard edges came from folded paths, above).
VERTEX_BUDGET = 8000

# (id, name, look, seed, space, band colour, glows [(heart, edge)], dust redden, cores [(heart, edge)], exposure)
# Colours sRGB 0-1. A theme's glows are its clouds' colours, heart and edge; cores its bright hearts.
THEMES = [
    ("crimson_reach", "Crimson Reach", "A wide blood-red cloud with ember edges, black lanes across it and a white-hot core.",
     2101, [0.022, 0.01, 0.014], [0.9, 0.6, 0.55], [((1.0, 0.25, 0.2), (0.35, 0.02, 0.06)), ((1.0, 0.55, 0.3), (0.5, 0.08, 0.06))],
     [0.7, 1.0, 1.3], [((1.0, 0.9, 0.8), (1.0, 0.4, 0.3))], 1.2),
    ("abyssal_teal", "Abyssal Teal", "Deep sea-green veils over near black, a cold cyan core and thin dark lanes.",
     2102, [0.008, 0.02, 0.026], [0.55, 0.8, 0.85], [((0.2, 0.85, 0.8), (0.02, 0.2, 0.26)), ((0.4, 0.95, 0.7), (0.04, 0.25, 0.2))],
     [0.8, 1.0, 1.1], [((0.8, 1.0, 1.0), (0.3, 0.8, 0.9))], 1.1),
    ("violet_storm", "Violet Storm", "Storm-purple clouds piled high, magenta at their hearts, a pale lilac core.",
     2103, [0.018, 0.01, 0.034], [0.7, 0.6, 0.95], [((0.75, 0.35, 1.0), (0.18, 0.04, 0.36)), ((1.0, 0.35, 0.8), (0.3, 0.04, 0.3))],
     [0.7, 0.9, 1.2], [((0.95, 0.85, 1.0), (0.7, 0.45, 1.0))], 1.25),
    ("golden_veil", "Golden Veil", "A thin gold veil across a dark sky, warm amber knots and a bright sun-coloured core.",
     2104, [0.02, 0.016, 0.012], [0.95, 0.82, 0.6], [((1.0, 0.78, 0.35), (0.4, 0.22, 0.06)), ((1.0, 0.62, 0.25), (0.34, 0.12, 0.04))],
     [0.8, 1.0, 1.1], [((1.0, 0.96, 0.82), (1.0, 0.7, 0.35))], 1.1),
    ("emerald_rift", "Emerald Rift", "Two green clouds torn apart by a dark rift, a yellow-green core between them.",
     2105, [0.01, 0.022, 0.014], [0.6, 0.9, 0.6], [((0.35, 1.0, 0.45), (0.04, 0.26, 0.08)), ((0.7, 1.0, 0.4), (0.12, 0.3, 0.04))],
     [0.8, 1.0, 1.1], [((0.95, 1.0, 0.8), (0.5, 0.9, 0.4))], 1.15),
    ("ice_halo", "Ice Halo", "Pale blue-white wisps round a brilliant blue star, cold and clean, full of stars.",
     2106, [0.012, 0.016, 0.032], [0.75, 0.85, 1.0], [((0.6, 0.8, 1.0), (0.1, 0.2, 0.45)), ((0.85, 0.92, 1.0), (0.25, 0.35, 0.6))],
     [0.9, 1.0, 1.0], [((0.95, 0.98, 1.0), (0.5, 0.7, 1.0))], 1.0),
    ("rose_quartz", "Rose Quartz", "Soft pink clouds with peach edges and a gentle white core: a calm, bright sky.",
     2107, [0.026, 0.016, 0.026], [0.95, 0.75, 0.85], [((1.0, 0.55, 0.7), (0.4, 0.12, 0.26)), ((1.0, 0.75, 0.6), (0.4, 0.2, 0.18))],
     [0.8, 0.9, 1.1], [((1.0, 0.95, 0.95), (1.0, 0.6, 0.7))], 1.15),
    ("rust_lanes", "Rust Lanes", "Brown and rust dust thick with dark lanes, a dim orange glow behind: a gritty sky.",
     2108, [0.016, 0.011, 0.009], [0.8, 0.65, 0.5], [((0.75, 0.38, 0.18), (0.22, 0.08, 0.03)), ((0.6, 0.42, 0.28), (0.18, 0.1, 0.06))],
     [0.6, 0.85, 1.3], [((1.0, 0.75, 0.5), (0.8, 0.35, 0.15))], 1.0),
    ("twin_lights", "Twin Lights", "A cyan cloud and a magenta cloud meeting, each round its own core.",
     2109, [0.014, 0.012, 0.03], [0.7, 0.7, 0.95], [((0.2, 0.9, 1.0), (0.02, 0.2, 0.4)), ((1.0, 0.3, 0.75), (0.34, 0.03, 0.26))],
     [0.8, 0.95, 1.1], [((0.85, 1.0, 1.0), (0.3, 0.85, 1.0)), ((1.0, 0.85, 0.95), (1.0, 0.4, 0.8))], 1.2),
    ("ashen_void", "Ashen Void", "Almost empty: grey-blue dust drifts and a single faint smudge, stars everywhere.",
     2110, [0.006, 0.007, 0.012], [0.6, 0.66, 0.8], [((0.5, 0.58, 0.72), (0.1, 0.12, 0.2))],
     [0.9, 1.0, 1.1], [((0.85, 0.9, 1.0), (0.5, 0.55, 0.7))], 0.9),
    ("aurora", "Aurora", "Green and violet ribbons like an aurora, draped along the horizon, a teal core.",
     2111, [0.01, 0.014, 0.026], [0.6, 0.85, 0.85], [((0.3, 1.0, 0.6), (0.04, 0.26, 0.16)), ((0.6, 0.35, 1.0), (0.12, 0.04, 0.32))],
     [0.8, 0.9, 1.1], [((0.8, 1.0, 0.95), (0.3, 0.9, 0.8))], 1.2),
    ("solar_furnace", "Solar Furnace", "A blazing yellow-white heart in orange fire, the brightest sky: battle under a star.",
     2112, [0.024, 0.014, 0.008], [1.0, 0.8, 0.55], [((1.0, 0.7, 0.2), (0.45, 0.14, 0.02)), ((1.0, 0.88, 0.45), (0.5, 0.3, 0.06))],
     [0.7, 0.95, 1.2], [((1.0, 1.0, 0.92), (1.0, 0.75, 0.3))], 1.3),
    ("deep_indigo", "Deep Indigo", "Indigo clouds lit blue from within, a white-blue core and crossing black lanes.",
     2113, [0.008, 0.008, 0.03], [0.6, 0.65, 1.0], [((0.3, 0.4, 1.0), (0.04, 0.06, 0.32)), ((0.5, 0.65, 1.0), (0.08, 0.12, 0.36))],
     [0.8, 0.9, 1.0], [((0.9, 0.95, 1.0), (0.45, 0.55, 1.0))], 1.15),
    ("coral_drift", "Coral Drift", "Coral and turquoise side by side, like a reef seen from above, a warm core.",
     2114, [0.016, 0.016, 0.026], [0.85, 0.8, 0.8], [((1.0, 0.45, 0.35), (0.38, 0.08, 0.08)), ((0.3, 0.85, 0.8), (0.03, 0.22, 0.24))],
     [0.8, 0.9, 1.1], [((1.0, 0.92, 0.8), (1.0, 0.55, 0.4))], 1.15),
    # the tractor, mines and craft test scenes' own (data/battle/scenes/tractor.json), one each, as every scenario has
    ("iron_shoal", "Iron Shoal", "Cold steel-blue banks of dust with a pale silver core, a sky for a field of rocks.",
     2115, [0.01, 0.012, 0.018], [0.7, 0.75, 0.85], [((0.55, 0.65, 0.85), (0.1, 0.13, 0.22)), ((0.7, 0.75, 0.8), (0.16, 0.17, 0.2))],
     [0.8, 0.95, 1.1], [((0.95, 0.97, 1.0), (0.6, 0.7, 0.9))], 1.05),
    ("amber_tide", "Amber Tide", "A long amber swell over deep brown, honey-coloured knots and a soft gold core.",
     2116, [0.02, 0.014, 0.008], [0.9, 0.78, 0.55], [((1.0, 0.66, 0.28), (0.36, 0.16, 0.04)), ((0.95, 0.8, 0.45), (0.3, 0.2, 0.08))],
     [0.7, 0.9, 1.2], [((1.0, 0.94, 0.75), (1.0, 0.65, 0.3))], 1.15),
    ("cobalt_drift", "Cobalt Drift", "Cobalt clouds drifting over black, electric blue hearts and a white core.",
     2117, [0.006, 0.01, 0.03], [0.55, 0.7, 1.0], [((0.15, 0.45, 1.0), (0.02, 0.06, 0.3)), ((0.35, 0.75, 1.0), (0.04, 0.14, 0.34))],
     [0.8, 0.9, 1.05], [((0.92, 0.97, 1.0), (0.4, 0.65, 1.0))], 1.2),
    ("saffron_haze", "Saffron Haze", "A saffron haze with rose edges, warm and bright, a cream core low on the horizon.",
     2118, [0.024, 0.016, 0.012], [0.95, 0.8, 0.65], [((1.0, 0.72, 0.3), (0.4, 0.18, 0.06)), ((1.0, 0.55, 0.5), (0.36, 0.1, 0.12))],
     [0.75, 0.9, 1.15], [((1.0, 0.97, 0.88), (1.0, 0.72, 0.45))], 1.2),
    ("jade_veil", "Jade Veil", "A thin jade veil, sea-green and grey, with a quiet green-white core.",
     2119, [0.008, 0.018, 0.016], [0.6, 0.85, 0.75], [((0.3, 0.85, 0.6), (0.04, 0.2, 0.14)), ((0.55, 0.8, 0.7), (0.1, 0.18, 0.16))],
     [0.85, 1.0, 1.05], [((0.9, 1.0, 0.95), (0.45, 0.85, 0.7))], 1.05),
    ("orchid_deep", "Orchid Deep", "Orchid and plum clouds deep in the dark, violet lanes and a pink-white core.",
     2120, [0.02, 0.01, 0.028], [0.8, 0.65, 0.9], [((0.9, 0.4, 0.95), (0.28, 0.06, 0.32)), ((0.65, 0.3, 0.8), (0.18, 0.05, 0.28))],
     [0.75, 0.9, 1.2], [((1.0, 0.9, 1.0), (0.9, 0.5, 0.9))], 1.2),
    ("slate_ember", "Slate Ember", "Slate-grey dust with embers glowing through it, a dull red core.",
     2121, [0.012, 0.011, 0.012], [0.75, 0.68, 0.66], [((0.55, 0.52, 0.56), (0.12, 0.11, 0.13)), ((0.9, 0.38, 0.22), (0.26, 0.06, 0.04))],
     [0.7, 0.9, 1.25], [((1.0, 0.8, 0.65), (0.9, 0.35, 0.2))], 1.05),
    ("copper_mist", "Copper Mist", "Copper and verdigris mist, burnished orange beside green, a bright brass core.",
     2122, [0.016, 0.014, 0.012], [0.85, 0.75, 0.62], [((0.95, 0.5, 0.25), (0.32, 0.1, 0.04)), ((0.35, 0.75, 0.6), (0.05, 0.2, 0.15))],
     [0.8, 0.95, 1.1], [((1.0, 0.92, 0.7), (1.0, 0.65, 0.35))], 1.15),
    # the weapon-targeting package's test scenes (Disable and Pinned fire, from both sides)
    ("tactical_slate", "Tactical Slate", "Cold slate-blue haze with steel-grey edges and a pale silver core.",
     2201, [0.012, 0.014, 0.022], [0.7, 0.75, 0.85], [((0.55, 0.65, 0.85), (0.08, 0.1, 0.2)), ((0.7, 0.78, 0.9), (0.12, 0.14, 0.24))],
     [0.8, 0.9, 1.0], [((0.95, 0.97, 1.0), (0.6, 0.7, 0.9))], 1.1),
    ("tactical_amber", "Tactical Amber", "Smouldering amber and copper clouds with brown lanes and a gold core.",
     2202, [0.02, 0.014, 0.008], [0.9, 0.75, 0.55], [((1.0, 0.65, 0.2), (0.4, 0.15, 0.03)), ((0.9, 0.45, 0.2), (0.35, 0.1, 0.04))],
     [0.7, 0.95, 1.2], [((1.0, 0.95, 0.75), (1.0, 0.65, 0.25))], 1.15),
    ("tactical_jade", "Tactical Jade", "Jade and moss veils over a dark green-black, a pale mint core.",
     2203, [0.008, 0.02, 0.014], [0.6, 0.85, 0.7], [((0.3, 0.85, 0.5), (0.03, 0.22, 0.1)), ((0.55, 0.9, 0.45), (0.08, 0.25, 0.08))],
     [0.8, 1.0, 1.1], [((0.9, 1.0, 0.92), (0.45, 0.9, 0.6))], 1.1),
    ("tactical_magenta", "Tactical Magenta", "Hot magenta and plum folds, cut by black lanes round a pink-white core.",
     2204, [0.02, 0.008, 0.02], [0.85, 0.6, 0.85], [((1.0, 0.3, 0.7), (0.32, 0.03, 0.22)), ((0.75, 0.3, 0.9), (0.2, 0.04, 0.3))],
     [0.7, 0.9, 1.2], [((1.0, 0.9, 0.95), (1.0, 0.45, 0.75))], 1.2),
    # the faction skins package's scenarios (2026-11-06: the Ssarith Hunt's own, Allied fleet, Four races), each under a sky
    # of its own, named skins_ so they keep apart from the other packages' skies
    ("skins_moss_haze", "Moss Haze", "An olive-green haze with amber knots in it and a dim gold core: a murky, close sky.",
     2115, [0.012, 0.016, 0.01], [0.75, 0.8, 0.6], [((0.55, 0.7, 0.3), (0.12, 0.18, 0.05)), ((0.9, 0.7, 0.3), (0.26, 0.16, 0.04))],
     [0.7, 0.95, 1.2], [((1.0, 0.95, 0.7), (0.8, 0.7, 0.3))], 1.1),
    ("skins_copper_tide", "Copper Tide", "Copper and teal clouds rolling against each other, a hot copper core between them.",
     2116, [0.014, 0.014, 0.018], [0.8, 0.75, 0.7], [((1.0, 0.5, 0.25), (0.36, 0.1, 0.04)), ((0.25, 0.75, 0.7), (0.03, 0.18, 0.2))],
     [0.75, 0.95, 1.15], [((1.0, 0.9, 0.75), (1.0, 0.55, 0.3))], 1.15),
    ("skins_cobalt_crown", "Cobalt Crown", "A ring of cobalt clouds round a bright violet-white core, dark lanes through the ring.",
     2117, [0.008, 0.01, 0.03], [0.65, 0.7, 1.0], [((0.2, 0.45, 1.0), (0.03, 0.08, 0.34)), ((0.6, 0.45, 1.0), (0.14, 0.06, 0.34))],
     [0.8, 0.9, 1.05], [((0.95, 0.92, 1.0), (0.6, 0.55, 1.0))], 1.2),
    ("skins_jade_lantern", "Jade Lantern", "Jade-green veils lit from within by a warm yellow lantern of a core.",
     2118, [0.008, 0.018, 0.016], [0.6, 0.85, 0.75], [((0.25, 0.9, 0.6), (0.03, 0.24, 0.16)), ((0.5, 1.0, 0.75), (0.06, 0.28, 0.2))],
     [0.8, 1.0, 1.1], [((1.0, 0.98, 0.75), (0.9, 0.85, 0.4))], 1.15),
    ("skins_amber_lull", "Amber Lull", "Still amber clouds low on the horizon, soft and wide, a pale honey core: a quiet sky.",
     2119, [0.018, 0.014, 0.01], [0.9, 0.8, 0.62], [((1.0, 0.68, 0.32), (0.32, 0.16, 0.05))],
     [0.85, 1.0, 1.1], [((1.0, 0.94, 0.8), (1.0, 0.72, 0.4))], 1.0),
    ("skins_pearl_mist", "Pearl Mist", "Pale pearl-grey mist with faint rose and blue in it, a soft white core.",
     2120, [0.016, 0.016, 0.022], [0.85, 0.85, 0.92], [((0.85, 0.8, 0.9), (0.22, 0.2, 0.28)), ((0.7, 0.8, 1.0), (0.14, 0.2, 0.34))],
     [0.9, 1.0, 1.05], [((1.0, 0.98, 1.0), (0.85, 0.82, 0.95))], 1.0),
    ("skins_tidal_glass", "Tidal Glass", "Clear sea-blue glass with one green current through it and a cool white core.",
     2121, [0.008, 0.016, 0.024], [0.6, 0.8, 0.95], [((0.3, 0.7, 1.0), (0.04, 0.16, 0.34)), ((0.35, 1.0, 0.7), (0.04, 0.26, 0.18))],
     [0.8, 0.95, 1.1], [((0.92, 1.0, 1.0), (0.5, 0.85, 1.0))], 1.1),
    ("skins_saffron_ring", "Saffron Ring", "A saffron cloud curled round a hot white core, rose at its outer edge.",
     2122, [0.02, 0.012, 0.012], [0.95, 0.78, 0.65], [((1.0, 0.72, 0.25), (0.4, 0.18, 0.04)), ((1.0, 0.45, 0.45), (0.34, 0.08, 0.12))],
     [0.75, 0.95, 1.15], [((1.0, 0.98, 0.9), (1.0, 0.75, 0.4))], 1.2),
    ("skins_slate_drift", "Slate Drift", "Slate-blue drifts thinning into dark, a faint lavender knot: a still, open sky.",
     2123, [0.008, 0.009, 0.016], [0.65, 0.68, 0.85], [((0.5, 0.55, 0.85), (0.08, 0.1, 0.24)), ((0.7, 0.6, 0.95), (0.14, 0.1, 0.26))],
     [0.9, 1.0, 1.1], [((0.92, 0.9, 1.0), (0.6, 0.6, 0.9))], 1.0),
    # the reactor package's scenes (data/battle/scenes/reactor.json): the Warp scene by a neutron star, the Escape's limp
    ("reactor_neutron_glare", "Neutron Glare", "A hard white-violet glare off a dead star, thin blue filaments round it, the dark close by.",
     2115, [0.01, 0.01, 0.022], [0.8, 0.8, 1.0], [((0.7, 0.65, 1.0), (0.12, 0.08, 0.34)), ((0.55, 0.8, 1.0), (0.06, 0.16, 0.4))],
     [0.85, 0.95, 1.05], [((1.0, 1.0, 1.0), (0.7, 0.7, 1.0))], 1.1),
    ("reactor_pulsar_wake", "Pulsar Wake", "A cold teal wake trailing a pulsar, pale gold knots along it, sharp black lanes.",
     2116, [0.008, 0.016, 0.02], [0.7, 0.85, 0.9], [((0.25, 0.8, 0.85), (0.03, 0.18, 0.24)), ((0.95, 0.8, 0.45), (0.3, 0.18, 0.06))],
     [0.75, 0.95, 1.2], [((0.95, 1.0, 1.0), (0.5, 0.85, 0.95))], 1.1),
    ("reactor_cinder_drift", "Cinder Drift", "Smouldering brown and ember dust, as if a battle burned here, a low red glow behind it.",
     2117, [0.016, 0.009, 0.008], [0.85, 0.6, 0.5], [((0.85, 0.32, 0.16), (0.26, 0.05, 0.03)), ((0.55, 0.35, 0.3), (0.16, 0.08, 0.07))],
     [0.6, 0.85, 1.35], [((1.0, 0.7, 0.45), (0.85, 0.3, 0.12))], 1.0),
    ("reactor_smoke_veil", "Smoke Veil", "Grey-violet smoke drawn thin across the dark, a faint rose core far off: a quiet place to run.",
     2118, [0.01, 0.009, 0.016], [0.7, 0.65, 0.8], [((0.6, 0.52, 0.72), (0.14, 0.1, 0.22)), ((0.85, 0.5, 0.6), (0.24, 0.08, 0.14))],
     [0.85, 0.95, 1.1], [((1.0, 0.85, 0.9), (0.8, 0.45, 0.6))], 0.95),
    # the cloak and sensors scenes' skies (data/battle/scenes/cloak.json): every scenario has its own
    ("cloak_night_shroud", "Night Shroud", "A dark slate shroud with faint steel-blue edges and one dim cold core: a sky to hide in.",
     2115, [0.006, 0.008, 0.012], [0.55, 0.62, 0.72], [((0.36, 0.46, 0.6), (0.05, 0.08, 0.14)), ((0.5, 0.56, 0.7), (0.08, 0.1, 0.16))],
     [0.9, 1.0, 1.2], [((0.8, 0.88, 1.0), (0.35, 0.45, 0.65))], 0.95),
    ("cloak_static_haze", "Static Haze", "Pink-violet haze shot with white static, a hot magenta core: the jammer's nebula.",
     2116, [0.02, 0.01, 0.024], [0.85, 0.65, 0.9], [((1.0, 0.45, 0.85), (0.34, 0.06, 0.3)), ((0.85, 0.6, 1.0), (0.24, 0.1, 0.36))],
     [0.7, 0.9, 1.2], [((1.0, 0.92, 1.0), (1.0, 0.5, 0.9))], 1.2),
    ("cloak_iris_drift", "Iris Drift", "Blue-violet iris petals drifting apart, gold at their hearts.",
     2117, [0.012, 0.01, 0.03], [0.7, 0.7, 1.0], [((0.5, 0.45, 1.0), (0.1, 0.06, 0.36)), ((1.0, 0.8, 0.4), (0.3, 0.16, 0.08))],
     [0.8, 0.95, 1.1], [((1.0, 0.95, 0.85), (0.6, 0.5, 1.0))], 1.15),
    ("cloak_still_water", "Still Water", "A calm grey-green pool of light, almost still, with a pale core: a quiet sky to lie in wait.",
     2118, [0.008, 0.012, 0.012], [0.6, 0.75, 0.72], [((0.4, 0.7, 0.62), (0.05, 0.14, 0.12))],
     [0.9, 1.0, 1.1], [((0.85, 1.0, 0.95), (0.4, 0.7, 0.62))], 1.0),
    ("cloak_bulkhead", "Bulkhead", "Copper and teal clouds pressed against each other along a hard dark seam.",
     2119, [0.016, 0.012, 0.012], [0.8, 0.75, 0.7], [((0.9, 0.5, 0.3), (0.3, 0.1, 0.05)), ((0.3, 0.8, 0.75), (0.04, 0.2, 0.2))],
     [0.7, 0.9, 1.3], [((1.0, 0.9, 0.8), (0.9, 0.55, 0.35))], 1.1),
    ("cloak_alarm_red", "Alarm Red", "A low scarlet glow under violet dust, like a ship's red alert seen through smoke.",
     2120, [0.02, 0.008, 0.016], [0.85, 0.55, 0.7], [((1.0, 0.25, 0.35), (0.35, 0.03, 0.1)), ((0.6, 0.3, 0.9), (0.15, 0.05, 0.3))],
     [0.7, 0.9, 1.2], [((1.0, 0.85, 0.85), (1.0, 0.35, 0.45))], 1.15),
    # the comms and transport test scenes' skies (data/battle/scenes/comms.json), one a scene and side
    ("comms_harbour_dusk", "Harbour Dusk", "Warm violet dusk over a calm blue veil, a soft rose core: a quiet anchorage.",
     2301, [0.016, 0.012, 0.026], [0.8, 0.7, 0.9], [((0.7, 0.45, 0.9), (0.2, 0.08, 0.32)), ((0.45, 0.6, 1.0), (0.08, 0.14, 0.36))],
     [0.8, 0.9, 1.1], [((1.0, 0.85, 0.9), (0.9, 0.5, 0.7))], 1.1),
    ("comms_amber_shoals", "Amber Shoals", "Low amber shoals of dust with olive edges, a honey-coloured core.",
     2302, [0.018, 0.015, 0.01], [0.9, 0.8, 0.55], [((0.95, 0.7, 0.3), (0.3, 0.2, 0.05)), ((0.7, 0.75, 0.35), (0.18, 0.2, 0.06))],
     [0.8, 1.0, 1.2], [((1.0, 0.95, 0.75), (0.95, 0.7, 0.3))], 1.1),
    ("comms_cobalt_drift", "Cobalt Drift", "Cobalt clouds drifting in long bands, a steel-white core and fine dark lanes.",
     2303, [0.008, 0.01, 0.028], [0.6, 0.7, 1.0], [((0.25, 0.5, 1.0), (0.03, 0.1, 0.34)), ((0.4, 0.75, 1.0), (0.06, 0.16, 0.36))],
     [0.8, 0.95, 1.1], [((0.92, 0.96, 1.0), (0.5, 0.65, 1.0))], 1.1),
    ("comms_saffron_lanes", "Saffron Lanes", "Saffron and orange clouds cut by many black lanes, a pale yellow core.",
     2304, [0.02, 0.013, 0.008], [0.95, 0.75, 0.5], [((1.0, 0.62, 0.15), (0.38, 0.14, 0.02)), ((1.0, 0.8, 0.35), (0.4, 0.22, 0.05))],
     [0.6, 0.9, 1.3], [((1.0, 0.98, 0.8), (1.0, 0.8, 0.4))], 1.15),
    ("comms_jade_mist", "Jade Mist", "A jade mist hanging low, sea-glass edges and a pale mint core.",
     2305, [0.008, 0.018, 0.016], [0.6, 0.9, 0.8], [((0.3, 0.9, 0.65), (0.03, 0.22, 0.14)), ((0.55, 0.95, 0.85), (0.08, 0.26, 0.24))],
     [0.8, 0.9, 1.1], [((0.9, 1.0, 0.95), (0.45, 0.95, 0.75))], 1.1),
    ("comms_garnet_veil", "Garnet Veil", "A dark garnet veil with plum shadows and a deep red core.",
     2306, [0.02, 0.008, 0.016], [0.85, 0.55, 0.7], [((0.85, 0.15, 0.35), (0.26, 0.02, 0.12)), ((0.6, 0.2, 0.6), (0.18, 0.04, 0.22))],
     [0.7, 0.9, 1.2], [((1.0, 0.8, 0.85), (0.9, 0.3, 0.45))], 1.15),
    ("comms_frost_lantern", "Frost Lantern", "Frost-white wisps round a lantern-blue core, thin and bright.",
     2307, [0.012, 0.014, 0.028], [0.8, 0.88, 1.0], [((0.7, 0.85, 1.0), (0.14, 0.22, 0.45)), ((0.9, 0.95, 1.0), (0.3, 0.38, 0.6))],
     [0.9, 1.0, 1.0], [((1.0, 1.0, 1.0), (0.55, 0.75, 1.0))], 1.0),
    ("comms_copper_haze", "Copper Haze", "A copper haze with teal glints at its edge, a burnished core.",
     2308, [0.016, 0.012, 0.012], [0.85, 0.7, 0.6], [((0.85, 0.45, 0.25), (0.26, 0.1, 0.05)), ((0.35, 0.75, 0.7), (0.05, 0.2, 0.2))],
     [0.7, 0.9, 1.2], [((1.0, 0.85, 0.7), (0.9, 0.5, 0.3))], 1.1),
    ("comms_orchid_reef", "Orchid Reef", "Orchid and lilac reefs of cloud, magenta knots and a white core.",
     2309, [0.02, 0.012, 0.03], [0.85, 0.7, 0.95], [((0.95, 0.45, 0.95), (0.3, 0.06, 0.32)), ((0.75, 0.6, 1.0), (0.2, 0.12, 0.4))],
     [0.8, 0.9, 1.1], [((1.0, 0.95, 1.0), (0.95, 0.55, 0.95))], 1.2),
    ("comms_storm_slate", "Storm Slate", "Slate-grey storm clouds lit from below in dull orange, a smouldering core.",
     2310, [0.01, 0.01, 0.014], [0.7, 0.7, 0.75], [((0.5, 0.55, 0.65), (0.1, 0.11, 0.16)), ((0.9, 0.5, 0.25), (0.25, 0.08, 0.03))],
     [0.7, 0.9, 1.2], [((1.0, 0.8, 0.6), (0.85, 0.4, 0.2))], 1.0),
    # the Kelvane Institute's scenarios (2026-11-07, change kelvane-ships-engine): The Institute and Institute wrecks from
    # each side, and the Institute's own Battle, Duel, Arming and Four races, each under a sky of its own
    ("institute_champagne_haze", "Champagne Haze", "A pale champagne haze, amber knots through it and a soft cream core.",
     2401, [0.016, 0.014, 0.012], [0.92, 0.86, 0.74], [((0.95, 0.85, 0.6), (0.3, 0.22, 0.1)), ((1.0, 0.7, 0.35), (0.3, 0.15, 0.04))],
     [0.85, 1.0, 1.1], [((1.0, 0.97, 0.88), (1.0, 0.8, 0.5))], 1.05),
    ("institute_phosphor_drift", "Phosphor Drift", "Phosphor-green drifts over a dark teal, thin and even, a pale mint core.",
     2402, [0.006, 0.016, 0.014], [0.62, 0.86, 0.74], [((0.4, 0.95, 0.6), (0.04, 0.22, 0.12)), ((0.3, 0.75, 0.7), (0.03, 0.18, 0.18))],
     [0.8, 1.0, 1.1], [((0.9, 1.0, 0.92), (0.5, 0.95, 0.7))], 1.1),
    ("institute_brass_lanes", "Brass Lanes", "Brass-brown clouds parted by long straight lanes of dark, a warm gold core.",
     2403, [0.014, 0.011, 0.008], [0.85, 0.75, 0.58], [((0.85, 0.62, 0.3), (0.24, 0.14, 0.04))],
     [0.6, 0.85, 1.2], [((1.0, 0.92, 0.7), (0.95, 0.68, 0.3))], 1.1),
    ("institute_parchment_veil", "Parchment Veil", "A faint parchment veil with rose in its folds, a quiet white core: an open sky.",
     2404, [0.014, 0.012, 0.014], [0.9, 0.84, 0.8], [((0.9, 0.82, 0.7), (0.24, 0.18, 0.14)), ((0.95, 0.65, 0.6), (0.26, 0.1, 0.1))],
     [0.9, 1.0, 1.05], [((1.0, 0.98, 0.95), (0.95, 0.85, 0.75))], 1.0),
    ("institute_ink_tide", "Ink Tide", "Blue-black ink clouds rolling in from one side, an amber spark at their edge.",
     2405, [0.006, 0.008, 0.02], [0.6, 0.65, 0.9], [((0.25, 0.35, 0.8), (0.03, 0.05, 0.22)), ((1.0, 0.7, 0.3), (0.3, 0.14, 0.03))],
     [0.75, 0.9, 1.15], [((1.0, 0.94, 0.8), (1.0, 0.7, 0.35))], 1.15),
    ("institute_beamline", "Beamline", "One long violet-white filament straight across the sky, blue glow along it.",
     2406, [0.008, 0.008, 0.02], [0.75, 0.75, 1.0], [((0.7, 0.6, 1.0), (0.12, 0.08, 0.34)), ((0.4, 0.7, 1.0), (0.04, 0.14, 0.34))],
     [0.85, 0.95, 1.05], [((1.0, 1.0, 1.0), (0.75, 0.7, 1.0))], 1.15),
    ("institute_lamp_amber", "Lamp Amber", "Deep amber clouds round a lamp-bright yellow core, smoky brown at the edges.",
     2407, [0.018, 0.012, 0.008], [0.92, 0.78, 0.6], [((1.0, 0.62, 0.2), (0.34, 0.14, 0.03)), ((0.7, 0.45, 0.25), (0.18, 0.1, 0.05))],
     [0.7, 0.95, 1.2], [((1.0, 0.96, 0.7), (1.0, 0.75, 0.3))], 1.2),
    ("institute_quartz_field", "Quartz Field", "Cold quartz-white wisps scattered wide, faint lilac between them, a small blue core.",
     2408, [0.01, 0.011, 0.018], [0.8, 0.82, 0.95], [((0.85, 0.88, 1.0), (0.18, 0.2, 0.3)), ((0.75, 0.65, 0.95), (0.16, 0.1, 0.28))],
     [0.9, 1.0, 1.05], [((0.95, 0.97, 1.0), (0.6, 0.75, 1.0))], 1.05),
    # the Institute's own side of every general scene (2026-11-07; owner: "test all kelvan general mission scenarios")
    ("institute_cobalt_lantern", "Cobalt Lantern", "A cobalt cloud hung with one warm lantern-orange knot, a white core beside it.",
     2409, [0.006, 0.008, 0.022], [0.62, 0.7, 0.95], [((0.2, 0.4, 1.0), (0.03, 0.07, 0.28)), ((1.0, 0.6, 0.25), (0.3, 0.1, 0.03))],
     [0.8, 0.95, 1.1], [((1.0, 0.97, 0.9), (0.6, 0.75, 1.0))], 1.15),
    ("institute_sepia_shelf", "Sepia Shelf", "A long low shelf of sepia dust, its top edge lit gold, darker lanes under it.",
     2410, [0.014, 0.011, 0.008], [0.82, 0.72, 0.58], [((0.7, 0.5, 0.3), (0.2, 0.12, 0.06)), ((0.95, 0.78, 0.4), (0.3, 0.2, 0.06))],
     [0.6, 0.85, 1.25], [((1.0, 0.9, 0.7), (0.9, 0.6, 0.3))], 1.05),
    ("institute_verdant_folio", "Verdant Folio", "Leaf-green sheets layered like pages, edged in pale yellow, a lime core.",
     2411, [0.008, 0.016, 0.01], [0.66, 0.86, 0.62], [((0.45, 0.9, 0.4), (0.06, 0.22, 0.06)), ((0.9, 0.95, 0.5), (0.24, 0.26, 0.06))],
     [0.8, 1.0, 1.1], [((0.96, 1.0, 0.8), (0.6, 0.95, 0.4))], 1.1),
    ("institute_cinnabar_drift", "Cinnabar Drift", "Cinnabar-red drifts thinning to coral, a peach core and wide dark gaps.",
     2412, [0.02, 0.01, 0.01], [0.9, 0.65, 0.6], [((1.0, 0.32, 0.22), (0.32, 0.05, 0.04)), ((1.0, 0.6, 0.5), (0.36, 0.14, 0.12))],
     [0.7, 0.9, 1.2], [((1.0, 0.92, 0.84), (1.0, 0.55, 0.4))], 1.15),
    ("institute_slate_lumen", "Slate Lumen", "Slate-blue banks with a cold white light breaking through, faint grey between.",
     2413, [0.009, 0.01, 0.014], [0.72, 0.76, 0.85], [((0.5, 0.58, 0.75), (0.1, 0.12, 0.18)), ((0.85, 0.9, 1.0), (0.2, 0.24, 0.32))],
     [0.85, 1.0, 1.1], [((1.0, 1.0, 1.0), (0.7, 0.78, 0.95))], 1.05),
    ("institute_honey_reef", "Honey Reef", "Honey-gold reefs of cloud with teal water between them, a bright gold core.",
     2414, [0.01, 0.014, 0.014], [0.85, 0.8, 0.62], [((1.0, 0.75, 0.3), (0.32, 0.18, 0.04)), ((0.3, 0.75, 0.75), (0.03, 0.18, 0.2))],
     [0.75, 0.95, 1.15], [((1.0, 0.95, 0.75), (1.0, 0.75, 0.35))], 1.15),
    ("institute_mauve_ledger", "Mauve Ledger", "Mauve clouds in ruled bands, one above another, a pale pink core.",
     2415, [0.016, 0.01, 0.02], [0.82, 0.7, 0.88], [((0.8, 0.5, 0.85), (0.22, 0.08, 0.26)), ((0.95, 0.65, 0.8), (0.28, 0.12, 0.22))],
     [0.8, 0.95, 1.1], [((1.0, 0.92, 0.98), (0.95, 0.6, 0.85))], 1.15),
    ("institute_teal_meridian", "Teal Meridian", "A tall teal pillar of cloud crossing the band, pale aqua at its heart.",
     2416, [0.005, 0.014, 0.016], [0.6, 0.85, 0.88], [((0.2, 0.8, 0.85), (0.02, 0.18, 0.22)), ((0.5, 0.95, 0.9), (0.06, 0.24, 0.22))],
     [0.8, 1.0, 1.1], [((0.88, 1.0, 1.0), (0.4, 0.9, 0.95))], 1.1),
    ("institute_ochre_furnace", "Ochre Furnace", "Ochre and burnt-orange clouds stacked round a furnace-white core.",
     2417, [0.02, 0.012, 0.006], [0.92, 0.72, 0.5], [((1.0, 0.55, 0.15), (0.36, 0.12, 0.02)), ((0.85, 0.6, 0.25), (0.26, 0.14, 0.04))],
     [0.7, 0.9, 1.25], [((1.0, 0.98, 0.9), (1.0, 0.7, 0.3))], 1.2),
    ("institute_frost_index", "Frost Index", "Fine frost-blue threads spread thin over a deep sky, a small sharp core.",
     2418, [0.008, 0.01, 0.02], [0.75, 0.82, 1.0], [((0.6, 0.75, 1.0), (0.08, 0.14, 0.36))],
     [0.9, 1.0, 1.05], [((1.0, 1.0, 1.0), (0.6, 0.8, 1.0))], 1.0),
    ("institute_garnet_lamp", "Garnet Lamp", "Garnet clouds deep and dark with one amber lamp glowing through.",
     2419, [0.016, 0.006, 0.01], [0.82, 0.6, 0.62], [((0.75, 0.15, 0.3), (0.22, 0.02, 0.08)), ((1.0, 0.65, 0.25), (0.32, 0.12, 0.03))],
     [0.65, 0.9, 1.25], [((1.0, 0.9, 0.7), (1.0, 0.6, 0.3))], 1.15),
    ("institute_pale_orrery", "Pale Orrery", "Pale cream rings of haze about a quiet core, grey-blue sky between.",
     2420, [0.01, 0.011, 0.015], [0.86, 0.84, 0.8], [((0.9, 0.86, 0.75), (0.22, 0.2, 0.16)), ((0.65, 0.72, 0.88), (0.12, 0.14, 0.22))],
     [0.9, 1.0, 1.05], [((1.0, 0.98, 0.94), (0.9, 0.86, 0.75))], 1.0),
    ("institute_indigo_well", "Indigo Well", "A deep indigo well of cloud with violet rims, a blue-white core at its bottom.",
     2421, [0.008, 0.006, 0.024], [0.66, 0.62, 0.95], [((0.35, 0.25, 0.9), (0.06, 0.03, 0.28)), ((0.7, 0.45, 1.0), (0.18, 0.06, 0.32))],
     [0.75, 0.9, 1.15], [((0.92, 0.95, 1.0), (0.55, 0.6, 1.0))], 1.2),
    ("institute_rust_quire", "Rust Quire", "Rust and umber dust in folded sheets, a dim red core, dark lanes through it.",
     2422, [0.014, 0.009, 0.007], [0.8, 0.62, 0.5], [((0.7, 0.32, 0.15), (0.2, 0.06, 0.02)), ((0.55, 0.38, 0.25), (0.16, 0.08, 0.05))],
     [0.6, 0.85, 1.3], [((1.0, 0.7, 0.5), (0.8, 0.3, 0.15))], 1.0),
    ("institute_celadon_mist", "Celadon Mist", "A soft celadon mist, grey-green and even, with a pearl core.",
     2423, [0.01, 0.014, 0.012], [0.75, 0.85, 0.78], [((0.6, 0.82, 0.7), (0.12, 0.2, 0.14)), ((0.8, 0.9, 0.82), (0.2, 0.24, 0.2))],
     [0.9, 1.0, 1.05], [((0.98, 1.0, 0.97), (0.75, 0.9, 0.8))], 1.05),
    ("institute_saffron_arc", "Saffron Arc", "A saffron arc of cloud bowed over the band, red at its ends, gold at its crown.",
     2424, [0.018, 0.012, 0.008], [0.92, 0.8, 0.55], [((1.0, 0.72, 0.2), (0.34, 0.18, 0.02)), ((1.0, 0.4, 0.2), (0.3, 0.06, 0.03))],
     [0.7, 0.95, 1.15], [((1.0, 0.96, 0.75), (1.0, 0.72, 0.25))], 1.15),
    ("institute_azure_fold", "Azure Fold", "Azure clouds folded over each other, white where they crease.",
     2425, [0.006, 0.01, 0.022], [0.66, 0.8, 1.0], [((0.25, 0.6, 1.0), (0.03, 0.12, 0.3)), ((0.75, 0.88, 1.0), (0.16, 0.22, 0.36))],
     [0.8, 0.95, 1.1], [((1.0, 1.0, 1.0), (0.5, 0.75, 1.0))], 1.15),
    ("institute_umber_vault", "Umber Vault", "A dark umber vault of dust, few stars through it, a faint copper core.",
     2426, [0.01, 0.008, 0.006], [0.7, 0.6, 0.5], [((0.5, 0.32, 0.2), (0.14, 0.07, 0.03))],
     [0.6, 0.8, 1.3], [((0.95, 0.7, 0.5), (0.7, 0.4, 0.2))], 0.95),
    ("institute_lilac_shoal", "Lilac Shoal", "Shallow lilac shoals of cloud over a blue-grey sky, a soft violet core.",
     2427, [0.012, 0.01, 0.02], [0.8, 0.76, 0.92], [((0.78, 0.6, 0.95), (0.2, 0.12, 0.3)), ((0.6, 0.7, 0.9), (0.12, 0.16, 0.26))],
     [0.85, 0.95, 1.1], [((1.0, 0.95, 1.0), (0.8, 0.65, 1.0))], 1.1),
    ("institute_copper_rule", "Copper Rule", "One straight copper-bright streak ruled across a dark sky, smoke along it.",
     2428, [0.012, 0.009, 0.008], [0.85, 0.72, 0.6], [((1.0, 0.58, 0.3), (0.3, 0.12, 0.04)), ((0.55, 0.45, 0.4), (0.14, 0.1, 0.08))],
     [0.7, 0.9, 1.2], [((1.0, 0.9, 0.75), (1.0, 0.6, 0.3))], 1.1),
    # the Ssarith Hunt's own side of every general scene (2026-11-07; owner: "Ensure spaceships for ssarith are also implemented")
    ("hunt_umbral_reed", "Umbral Reed", "Dark olive reeds of dust standing up through a near-black sky, a faint bronze core.",
     2429, [0.008, 0.01, 0.007], [0.7, 0.72, 0.58], [((0.5, 0.55, 0.3), (0.1, 0.12, 0.04)), ((0.7, 0.55, 0.3), (0.16, 0.1, 0.04))],
     [0.65, 0.85, 1.25], [((0.95, 0.85, 0.65), (0.7, 0.5, 0.25))], 1.0),
    ("hunt_silt_veil", "Silt Veil", "A grey-brown silt veil thick across the band, green-grey light leaking through it.",
     2430, [0.011, 0.012, 0.01], [0.76, 0.76, 0.68], [((0.55, 0.52, 0.42), (0.14, 0.12, 0.08)), ((0.5, 0.65, 0.55), (0.1, 0.16, 0.12))],
     [0.7, 0.9, 1.2], [((0.92, 0.95, 0.88), (0.6, 0.7, 0.6))], 1.0),
    ("hunt_ember_gate", "Ember Gate", "Two ember-red pillars of cloud with a dark gap between them, a hot orange core in the gap.",
     2431, [0.02, 0.009, 0.008], [0.9, 0.62, 0.52], [((1.0, 0.35, 0.12), (0.34, 0.06, 0.02)), ((0.85, 0.2, 0.15), (0.26, 0.03, 0.03))],
     [0.65, 0.9, 1.25], [((1.0, 0.92, 0.75), (1.0, 0.5, 0.2))], 1.2),
    ("hunt_iron_bloom", "Iron Bloom", "A gunmetal-blue bloom of cloud opening wide, a steel-white core and rust at its rim.",
     2432, [0.009, 0.011, 0.016], [0.72, 0.76, 0.86], [((0.45, 0.55, 0.7), (0.08, 0.12, 0.2)), ((0.7, 0.4, 0.25), (0.2, 0.08, 0.04))],
     [0.8, 0.95, 1.1], [((0.95, 0.97, 1.0), (0.6, 0.7, 0.85))], 1.1),
    ("hunt_dawn_terrace", "Dawn Terrace", "Terraces of peach and pale gold cloud stepping up the band, a warm white core.",
     2433, [0.02, 0.015, 0.014], [0.95, 0.84, 0.72], [((1.0, 0.72, 0.5), (0.36, 0.18, 0.1)), ((1.0, 0.88, 0.55), (0.34, 0.26, 0.1))],
     [0.8, 0.95, 1.1], [((1.0, 0.97, 0.9), (1.0, 0.78, 0.55))], 1.15),
    ("hunt_rose_quay", "Rose Quay", "A long rose-red bank with a straight lower edge, deep blue water-dark sky below it.",
     2434, [0.014, 0.01, 0.022], [0.85, 0.7, 0.85], [((1.0, 0.45, 0.55), (0.34, 0.08, 0.16)), ((0.3, 0.4, 0.8), (0.04, 0.06, 0.22))],
     [0.75, 0.9, 1.15], [((1.0, 0.92, 0.95), (1.0, 0.5, 0.6))], 1.15),
    ("hunt_glacier_bell", "Glacier Bell", "A bell-shaped cloud of glacier blue and white, cold green at its mouth.",
     2435, [0.008, 0.014, 0.024], [0.72, 0.86, 0.98], [((0.65, 0.88, 1.0), (0.08, 0.2, 0.36)), ((0.5, 0.95, 0.8), (0.06, 0.24, 0.18))],
     [0.85, 1.0, 1.05], [((1.0, 1.0, 1.0), (0.6, 0.85, 1.0))], 1.05),
    ("hunt_brass_spindle", "Brass Spindle", "A thin spindle of brass-yellow cloud wound tight, smoke-grey threads off it.",
     2436, [0.014, 0.012, 0.008], [0.88, 0.8, 0.6], [((0.95, 0.75, 0.3), (0.3, 0.2, 0.04)), ((0.5, 0.48, 0.45), (0.12, 0.11, 0.1))],
     [0.75, 0.9, 1.15], [((1.0, 0.95, 0.75), (0.95, 0.72, 0.3))], 1.1),
    ("hunt_violet_rapids", "Violet Rapids", "Fast-looking streaks of violet and electric blue, broken white water at the core.",
     2437, [0.012, 0.008, 0.026], [0.72, 0.66, 0.98], [((0.6, 0.3, 1.0), (0.14, 0.04, 0.34)), ((0.3, 0.55, 1.0), (0.04, 0.1, 0.32))],
     [0.75, 0.9, 1.15], [((0.97, 0.95, 1.0), (0.65, 0.55, 1.0))], 1.2),
    ("hunt_scarlet_kiln", "Scarlet Kiln", "A scarlet kiln of cloud glowing from inside, soot-black lanes over it.",
     2438, [0.022, 0.008, 0.008], [0.9, 0.58, 0.55], [((1.0, 0.18, 0.12), (0.36, 0.02, 0.02)), ((1.0, 0.5, 0.25), (0.4, 0.1, 0.03))],
     [0.6, 0.85, 1.3], [((1.0, 0.88, 0.7), (1.0, 0.35, 0.15))], 1.2),
    ("hunt_pewter_lane", "Pewter Lane", "Pewter-grey clouds either side of one straight dark lane, a dull silver core.",
     2439, [0.01, 0.01, 0.012], [0.78, 0.78, 0.82], [((0.6, 0.62, 0.66), (0.14, 0.14, 0.16))],
     [0.7, 0.85, 1.2], [((0.96, 0.96, 0.98), (0.7, 0.72, 0.78))], 1.0),
    ("hunt_aqua_trellis", "Aqua Trellis", "Aqua clouds crossed in a loose lattice, lime light in the gaps.",
     2440, [0.006, 0.016, 0.016], [0.62, 0.88, 0.86], [((0.25, 0.9, 0.85), (0.02, 0.22, 0.22)), ((0.7, 1.0, 0.4), (0.12, 0.28, 0.04))],
     [0.8, 1.0, 1.1], [((0.9, 1.0, 0.98), (0.45, 0.95, 0.85))], 1.15),
    ("hunt_basalt_wake", "Basalt Wake", "A black-basalt sky with one long pale wake of grey dust through it, few stars.",
     2441, [0.006, 0.006, 0.008], [0.6, 0.62, 0.66], [((0.55, 0.55, 0.58), (0.1, 0.1, 0.12))],
     [0.6, 0.8, 1.3], [((0.9, 0.9, 0.92), (0.6, 0.6, 0.65))], 0.95),
    ("hunt_mint_channel", "Mint Channel", "Mint-green banks on both sides of a dark channel, a cool white core downstream.",
     2442, [0.008, 0.018, 0.012], [0.66, 0.9, 0.74], [((0.5, 1.0, 0.7), (0.06, 0.26, 0.12)), ((0.75, 0.95, 0.85), (0.16, 0.26, 0.2))],
     [0.8, 1.0, 1.1], [((0.95, 1.0, 0.97), (0.6, 0.95, 0.75))], 1.1),
    ("hunt_tawny_dune", "Tawny Dune", "Tawny dunes of dust in long low rolls, a sandy gold core behind the crest.",
     2443, [0.016, 0.012, 0.008], [0.88, 0.78, 0.6], [((0.85, 0.62, 0.35), (0.26, 0.15, 0.05)), ((0.95, 0.78, 0.5), (0.3, 0.2, 0.08))],
     [0.65, 0.85, 1.25], [((1.0, 0.94, 0.8), (0.95, 0.7, 0.4))], 1.05),
    ("hunt_coral_spire", "Coral Spire", "A tall coral-pink spire of cloud, orange at its foot, a bright white tip.",
     2444, [0.022, 0.012, 0.014], [0.95, 0.72, 0.68], [((1.0, 0.5, 0.45), (0.36, 0.1, 0.1)), ((1.0, 0.62, 0.3), (0.36, 0.14, 0.04))],
     [0.75, 0.9, 1.15], [((1.0, 0.97, 0.95), (1.0, 0.6, 0.5))], 1.15),
    ("hunt_dusk_orchard", "Dusk Orchard", "Rounded dusk-plum clouds dotted with small amber knots, a soft rose core.",
     2445, [0.016, 0.01, 0.018], [0.82, 0.7, 0.82], [((0.6, 0.3, 0.55), (0.16, 0.05, 0.16)), ((1.0, 0.7, 0.3), (0.3, 0.14, 0.04))],
     [0.75, 0.9, 1.15], [((1.0, 0.9, 0.9), (0.95, 0.55, 0.6))], 1.1),
    ("hunt_flint_line", "Flint Line", "A hard flint-blue line of cloud along the band, a thin white spark at its middle.",
     2446, [0.007, 0.009, 0.016], [0.68, 0.74, 0.9], [((0.4, 0.5, 0.8), (0.06, 0.08, 0.2))],
     [0.8, 0.95, 1.1], [((1.0, 1.0, 1.0), (0.55, 0.65, 0.95))], 1.05),
    ("hunt_plum_harbour", "Plum Harbour", "A sheltered bay of deep plum cloud round a calm blue-white core, gold at the bay's mouth.",
     2447, [0.014, 0.008, 0.02], [0.78, 0.66, 0.9], [((0.55, 0.22, 0.6), (0.14, 0.03, 0.18)), ((1.0, 0.8, 0.4), (0.3, 0.2, 0.06))],
     [0.75, 0.9, 1.15], [((0.94, 0.96, 1.0), (0.6, 0.7, 1.0))], 1.15),
    ("hunt_ash_lattice", "Ash Lattice", "Pale ash-grey wisps in a crossed lattice over dark, a faint warm core.",
     2448, [0.009, 0.009, 0.01], [0.78, 0.76, 0.74], [((0.7, 0.68, 0.64), (0.16, 0.15, 0.14)), ((0.8, 0.6, 0.45), (0.18, 0.12, 0.08))],
     [0.75, 0.9, 1.15], [((0.98, 0.95, 0.9), (0.8, 0.65, 0.5))], 1.0),
]


def rnd_path(r, n, lon0, lat_lo, lat_hi, step):
    """A path of n points: a smooth walk round the sky from lon0, always onward and turning gently, kept inside the
    camera's band of latitudes. (A path that doubles back folds its stroke over itself, and the painter, which shades a
    point by the nearest point of the path, then draws a hard straight seam where the two folds meet.)"""
    lon, lat = lon0, r.uniform(lat_lo, lat_hi)
    slope = r.uniform(-0.25, 0.25)
    pts = [[round(((lon + 180) % 360) - 180, 1), round(lat, 1)]]
    for _ in range(n - 1):
        dl = step * r.uniform(0.8, 1.2)
        slope = max(-0.45, min(0.45, slope + r.uniform(-0.18, 0.18)))
        lon += dl
        lat = max(lat_lo, min(lat_hi, lat + slope * dl))
        pts.append([round(((lon + 180) % 360) - 180, 1), round(lat, 1)])
    return pts


def c3(c):
    return [round(float(x), 3) for x in c]


def stroke(r, path, width, heart, edge, intensity):
    return {"kind": "glow", "path_deg": path, "width_deg": round(width, 1),
            "taper": [round(r.uniform(0.2, 0.32), 2), round(r.uniform(0.25, 0.38), 2)],
            "colour_srgb": c3(heart), "edge_srgb": c3(edge), "intensity": round(intensity, 2),
            "warp": round(r.uniform(0.12, 0.2), 2), "warp_scale": round(r.uniform(1.4, 2.0), 1),
            "grain": round(r.uniform(0.4, 0.55), 2), "grain_scale": round(r.uniform(2.0, 3.0), 1),
            "hides_stars": round(r.uniform(0.35, 0.5), 2), "filaments": round(r.uniform(0.35, 0.55), 2),
            "filament_scale": round(r.uniform(3.8, 4.8), 1)}


def shifted(path, dlon, dlat):
    return [[round(((p[0] + dlon + 180) % 360) - 180, 1), round(max(-35, min(45, p[1] + dlat)), 1)] for p in path]


def compose(theme):
    """One sky: a main cloud (two or three overlapping strokes along one long path, as the painted skies are built),
    dark lanes down its spine, its core on it, one or two smaller clouds elsewhere and a gently tilted galaxy band."""
    tid, name, look, seed, space, band, glows, redden, cores, exposure = theme
    r = random.Random(seed)
    layers = []
    lon0 = r.uniform(-180, 180)
    spine = rnd_path(r, r.randint(5, 7), lon0, -18, 28, r.uniform(24, 34))     # 100-200 degrees of sky
    (h0, e0), (h1, e1) = glows[0], glows[min(1, len(glows) - 1)]
    layers.append(stroke(r, spine, r.uniform(17, 24), h0, e0, r.uniform(0.55, 0.85)))
    layers.append(stroke(r, shifted(spine, r.uniform(-12, 12), r.uniform(-8, 8)), r.uniform(9, 14), h1, e1, r.uniform(0.35, 0.6)))
    if r.random() < 0.7:
        mid = spine[1:-1] or spine
        layers.append(stroke(r, shifted(mid, r.uniform(-8, 8), r.uniform(-6, 6)), r.uniform(5, 8), h0, e1, r.uniform(0.4, 0.6)))
    for _ in range(r.randint(1, 2)):   # dark lanes down the main cloud's spine
        lane = shifted(spine[r.randint(0, 1):len(spine) - r.randint(0, 1)], r.uniform(-6, 6), r.uniform(-5, 5))
        layers.append({"kind": "dust", "path_deg": lane, "width_deg": round(r.uniform(2.6, 4.2), 1), "taper": [0.2, 0.25],
                       "density": round(r.uniform(2.2, 3.4), 1), "redden": c3(redden), "warp": round(r.uniform(0.08, 0.12), 2),
                       "warp_scale": 2.8, "grain": 0.5, "grain_scale": 4.0})
    far = spine[0][0] + 180   # smaller clouds on the far side, so no heading is bare
    for i in range(r.randint(1, 2)):
        heart, edge = glows[(i + 1) % len(glows)]
        layers.append(stroke(r, rnd_path(r, r.randint(3, 4), far + r.uniform(-50, 50), -20, 30, r.uniform(14, 22)),
                             r.uniform(8, 13), heart, edge, r.uniform(0.2, 0.4)))
    for i, (heart, edge) in enumerate(cores[: r.randint(1, len(cores))]):
        at = spine[len(spine) // 2 + i * 2 - (1 if i else 0)] if i < 2 else spine[0]
        layers.append({"kind": "core", "at_deg": [at[0], round(at[1] + r.uniform(-3, 3), 1)],
                       "radius_deg": round(r.uniform(2.2, 3.6), 1), "halo": round(r.uniform(0.55, 0.75), 2),
                       "colour_srgb": c3(heart), "edge_srgb": c3(edge), "intensity": round(r.uniform(1.0, 1.7), 2),
                       "warp": 0.03, "warp_scale": 3.0, "hides_stars": 0.75})
    core0 = next(l for l in layers if l["kind"] == "core")["at_deg"]
    return {"id": tid, "label": "", "name": name, "for": "the test scenes and demos (composed)", "look": look,
            "composed": True, "seed": seed, "space_srgb": c3(space), "exposure": exposure,
            "vertex_budget": VERTEX_BUDGET,
            "band": {"through_deg": [round(r.uniform(-180, 180)), round(r.uniform(-12, 12))], "tilt_deg": round(r.uniform(12, 32)),
                     "width_deg": round(r.uniform(6, 10), 1), "colour_srgb": c3(band), "intensity": round(r.uniform(0.04, 0.07), 3),
                     "bulge": 1.4, "bulge_width_deg": 30, "warp": 0.06, "warp_scale": 2.5, "grain": 0.35, "grain_scale": 4.0},
            "stars": {"count": r.choice([1400, 1600, 1900]), "band_bias": 2.0,
                      "cluster": {"at_deg": core0, "radius_deg": 2.5, "share": 0.03}},
            "dust_stars": {"density": 0.06, "brightness": 0.75}, "ambient": {"strength": 0.06},
            "layers": layers}


def main():
    with open(RECIPES, encoding="utf-8") as f:
        R = json.load(f)
    hand = [s for s in R["skies"] if not s.get("composed")]
    R["skies"] = hand + [compose(t) for t in THEMES]
    with open(RECIPES, "w", encoding="utf-8") as f:
        f.write(json.dumps(R, indent=1, ensure_ascii=False) + "\n")
    print(f"{RECIPES}: {len(hand)} painted and {len(THEMES)} composed recipes")


if __name__ == "__main__":
    main()
