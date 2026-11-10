# Playtest 2026-11-10: heavy fleets, three a side, in nebula plumes and ion storms

Owner, 2026-11-10: "record some battles for each of the 4 completerd(mostly) factions, record a battle with
cruisers/carriers a larger fleet of 3 v 3 ships on each side", "make sure the bot uses as many systems at their disposal:
shuttles, fighters, missiles, tractor beams, mines, and trransporters", "and of course special weapons".

- **Commit:** `e19a1e9f` (branch `claude/rank-and-options`, PR #73).
- **Machine:** Apple M4 Max MacBook Pro, 3456 x 2234 Retina display at 120 Hz; the game in a 1920 x 1080 window (Forward+),
  recorded by OBS window capture at 60 fps (the take is 1920 x 1136: the window with its title bar).
- **Script:** `data/playtests/heavy-fleet.json`: `wait_ready`, `pick_scene`, `all_out hop`. The scripted captain fights
  while using every system its ships carry, and hops between its ships.
- **Recorded:** in real time, between `PLAYTEST_START` and `PLAYTEST_END`, nothing cut. OBS was started and closed once a
  take; no OBS was left running.

## The four takes

| Side | Scene | Opponent | Result | Drive |
|---|---|---|---|---|
| Concord | `concord_heavy`: Garnet Front sky, a cinder ion storm, a garnet plume | Krai: a dreadnought and two cruisers | `PLAYTEST_RESULT ok concord_heavy side=concord skin=Concord outcome=won t=149s foes_left=0 hull=92%` | [concord/fleet-command-2026-11-10-heavy-fleet-concord.mp4](https://drive.google.com/file/d/1WO4VxDOV_JZLiw1jkLq4k3_3riPGLOx_/view?usp=drivesdk) |
| Krai | `krai_heavy`: Brass Squall, a brass plume, a slate ion storm | Kelvane: research vessel, carrier, battlecruiser | `PLAYTEST_RESULT ok krai_heavy side=krai skin=Krai Warhost outcome=won t=115s foes_left=0 hull=98%` | [krai/fleet-command-2026-11-10-heavy-fleet-krai.mp4](https://drive.google.com/file/d/10SxO3nTSoumDKdcLPtIoxnOoXPcZlmLw/view?usp=drivesdk) |
| Kelvane | `kelvane_heavy`: Teal Anvil, a slate ion storm, a teal plume | Ssarith Hunt: two cruisers and a carrier | `PLAYTEST_RESULT ok kelvane_heavy side=kelvane skin=Kelvane Institute outcome=won t=104s foes_left=0 hull=100%` | [kelvane/fleet-command-2026-11-10-heavy-fleet-kelvane.mp4](https://drive.google.com/file/d/1xE-Syebq0-y37C7BljI-5xK1T_zwlmsP/view?usp=drivesdk) |
| Ssarith Hunt | `ssarith_heavy`: Orchid Wall, an orchid plume, a cinder ion storm | Concord: three heavy cruisers | `PLAYTEST_RESULT ok ssarith_heavy side=ssarith skin=Ssarith Hunt outcome=won t=204s foes_left=0 hull=66%` | [ssarith/fleet-command-2026-11-10-heavy-fleet-ssarith.mp4](https://drive.google.com/file/d/1SmTWOinfilzZVZ-qrQcS3RHl0NpZFRZN/view?usp=drivesdk) |

Every beat (`wait_ready`, `pick_scene`, `all_out hop`) ended in all four. What the captain used, from its
`PLAYTEST_SYSTEMS` line:

| Side | Fighters | Barrages | Shuttles | Tractor | Mines | Raids | Special weapon | Ships taken | Power pages |
|---|---|---|---|---|---|---|---|---|---|
| Concord | 12 | 3 | 3 | 4 | 3 | 4 | Chorus lance 11 | 3 | 3 |
| Krai | 14 | 4 | 3 | 3 | 5 | 2 | Forge driver 8 | 2 | 3 |
| Kelvane | 20 | 4 | 3 | 3 | 2 | 0 | none fitted | 2 | 3 |
| Ssarith Hunt | 20 | 4 | 3 | 2 | 4 | 3 | none fitted | 4 | 2 |

Lightning tripped a core in three takes: Steadfast at 140 s (Concord), Gorrh at 62 s (Krai), the Kelvane flagship at 63 s
and the Hunt carrier at 84 s (Kelvane).

Contact sheets: [Concord](2026-11-10-heavy-fleets-concord_frames.png), [Krai](2026-11-10-heavy-fleets-krai_frames.png),
[Kelvane](2026-11-10-heavy-fleets-kelvane_frames.png), [Ssarith](2026-11-10-heavy-fleets-ssarith_frames.png). Looked at:
the console, the 3D view, the plumes and planets are in every frame, with no dialog, black frame or wrong window.

## Frame rate

Measured on the same machine before the takes (the scripted captain playing `concord_heavy` in a window, 173 s): median
85 fps (11.8 ms), slowest second 60 fps, fastest 103 fps. OBS held 60 fps through every take. Two plumes at a distance
cost about 3 ms a frame over none (5.7 ms to 8.2-8.6 ms). Not measured on the phone or the Windows PC.

## What looked wrong

- The Krai and Kelvane fights are short and one-sided (115 s and 104 s, the winners at 98% and 100% hull): the Kelvane
  and Hunt AI captains do not use fighters, shuttles, barrages, tractors or mines, and do not keep out of a storm.
- Only the Chorus lance and the Forge driver are mounted on these classes; no Kelvane or Hunt heavy ship has a special
  weapon fitted (the Quell is not fitted, the Blood charge has no key), so those two sides fire none.
- A tripped core shows only in the log and the dark hull lamps; the ship systems display has no readout of it
  (`nebula-plumes` task 2.9).
- The take includes the window's title bar ("Fleet Command (DEBUG)").
- The first Ssarith attempt recorded nothing: OBS did not answer its first WebSocket request (`CreateScene` timed out) and
  the script closed OBS and the game; the second attempt recorded normally.
