# Playtest: the star chart's reading and ships that overlap (2026-11-09)

The owner asked, of the star chart: "planets should be on the stand not floating above it", "remove ship names, show only
if I hover over it or target it", "add a search box to find ships", "need to make the starchart as easy to read as
possible"; and of the battle: "ships overlapping eachother, in SFC smaller ships would change their height and fly above
larger ships so that ships wouldnt clip inside of each other".

| | |
|---|---|
| Commit | `claude/rank-and-options` after `fad6e593`, with these changes |
| Machine | the owner's Mac (Apple silicon), Godot 4.7.1 .NET, a 1920 x 1080 window |
| How | `--playtest=chart-search --play=campaign` and `--playtest=stacking --play=concord_battle` |

## chart-search: ok

- `01-no_names`: the chart with no ship names but at the picked sector (your position, picked when the map opens): the
  CNS Resolute and the freighter beside her are named, nothing else. The planet Halcyon's disc rests on its post's top.
- `02-search`: "e" typed in the Find a ship box lists six ships (your fleet's and the Kelvane's Long Acre), each with its
  faction.
- `03-picked`: the Fleet key picks the fleet's sector.

First run, the planet still floated: lowering it in the world falls short because the camera looks down on a quad that
faces it. It is now lowered on screen (`planet.gdshader` `sink`), and rests on the post at any angle.

## stacking: ok, won in 162 s, hull 95 %

The battle played through with the stacking on. None of its three stills caught two hulls overlapping, so the rule is
proven by `StackingTests` (a smaller ship clears a larger one by the gap; sister ships keep their layers; a third climbs
over two; footprints turn with the heading), not by a still.

No take was recorded.
