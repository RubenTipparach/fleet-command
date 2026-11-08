# Playtest: the paper space chart on the Mac (2026-11-08)

The cloud session's handoff after PR #61 (owner, 2026-11-08: "when finished, merge to main, and have the mac session playtest
it"). Change `paper-map`; the mockup is the Space Chart, mockup 34 (https://claude.ai/artifact/A4E9dHbWA5qdvi1Z77sAjX, v8).

| | |
|---|---|
| Commit | main at `e5fd63cd` merged into `claude/freighter-missions` (PR #64) |
| Machine | the owner's Mac (Apple silicon, a 120 Hz display), Godot 4.7.1 .NET, Forward+ on Metal |
| How | `data/playtests/paper-chart.json` (now in the suite): a new Concord campaign's map, the wheel zoomed 10 notches in and 16 out, the fleet moved two sectors and three turns ended, then zoomed out over what it revealed. Played in a window at 1920 x 1080, 844 x 390 (a phone, landscape) and 390 x 844 (portrait) with Godot's `--print-fps`; the desktop run again recorded with OBS |
| Result | ok at all three sizes |

## Frame rate

Godot's own FPS counter, once a second, from the map's first frame to the end of each run (the display's vsync caps it at
120):

| Size | Samples | Min | 10th percentile | Median |
|---|---|---|---|---|
| 1920 x 1080 | 39 | 30 | 85 | 120 |
| 844 x 390 | 37 | 82 | 111 | 120 |
| 390 x 844 | 38 | 72 | 110 | 120 |

The low samples fall where the map is built and where a turn ends (the fog mask redrawn, the pieces moved). The chart holds
the display's 120 fps while it is looked at, panned and zoomed, at every size. This is the Mac's M-series GPU at a phone's
window size, not a phone: it says the 2,800 px sheet, the lamp and the fog mask cost little here, not what a 2022 phone
does (paper-map 2.3 is still open for that).

## What was there

- **The room:** the walnut table with its grain, its raised rim round the sheet, and the red carpet beyond it under the one
  warm pool of light. The green walls and sconces never came into frame at the zoom this run reached.
- **The chart:** hexes outlined, a heavier rule where the holder changes (the Concord border round Halcyon and Lyceum Depot),
  the uncharted under fog with a soft edge, the compass rose in its corner.
- **The drawn pieces:** cloud and storm billboards on the nebulae (grey storms with their lightning), rayed suns with their
  warm glow, drawn rocks over the belts, all standing on their own sectors.
- **The planets:** drawn live, the ringed world turning, its night side cross-hatched, on its hexagon stand.
- **The names:** lettered in IM Fell English SC (CNS Resolute, Halcyon, Freighter Larkspur, Keel Station).
- **Fog lifting:** after three turns' moves the revealed sectors had grown round the fleet's track (turn 4 still), and the
  pieces in them appeared. Markers the campaign already knows about (ports and offers on the far side) show in the fog,
  as the mockup shows them.

## Differs from the mockup (v8)

1. **The fog reads tan, not black.** Under the lamp the uncharted sheet is a warm khaki with its grid lines showing through
   (stills `desktop-04-sheet`, `desktop-07-revealed`); in the mockup it is near black with the pieces glowing over it. The
   Xvfb stills (`docs/screenshots/game/flow/map-far.png`) show the same warm wash in the lamp's pool, darker at the edge, so
   it is the lamp lighting the fog rather than a Mac difference.
2. **The hex rules are pale blue-white, not Concord blue.** In the mockup and in the Xvfb stills they are a saturated blue;
   on the Mac, under Forward+ on Metal, they read nearly white near the lamp and blue only at the edge (`desktop-02-close`
   beside `docs/screenshots/mockups/space-chart-close.png`).
3. **Not checked: the whole room.** Sixteen notches out showed the sheet's left half and its rim, and the run went no
   farther, so the walls and sconces of the mockup's room shot (`space-chart-room.png`) were not seen; the Xvfb `map-far`
   still, zoomed farther, shows the whole sheet but no walls either.
4. **Names overlap where pieces crowd** (Halcyon over Freighter Larkspur, CNS Resolute under both) at every size, most in
   portrait. The mockup has the same crowding at Halcyon.
5. Known and still open, as the handoff said: no underline stroke under the names, and phones load the full sheet.

## The take

The desktop run in a 1920 x 1080 window, recorded with OBS from `PLAYTEST_START` to `PLAYTEST_END`: 1 min 13 s, H.264 at
1920 x 1136 (the window and its title bar), 60 fps, at the take's own size:
https://drive.google.com/file/d/1aEdB1OnxKEyTMxZQ4h0DNDUqx2kEkIdv/view (`fleet-command-2026-11-08-paper-chart.mp4`). The
contact sheet showed the menu, the chart close in with the planet and the fleet, and the sheet zoomed out after turn 4.
Stills are in `docs/screenshots/game/paper-chart/` (desktop, landscape and portrait).
