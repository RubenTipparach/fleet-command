# Playtest 2026-11-06: every demo scene, recorded

Owner, 2026-11-06: "record a playthrough of all the demo scenes, to make sure all ui elements are implmented fully".

- **Commit**: `541acc7` (branch `claude/goals-comms`, PR #50), with the goals in Comms, distances x10, drone shields, shield
  bands by class, the map's pause menu, the debrief on black and shots drawn between steps.
- **Machine**: the owner's Mac (desktop session), Forward+, a 1920 x 1080 window (the takes include the macOS title bar).
- **How**: the scripted captain played each scene's suite script (`data/playtests/suite.json`) in real time; OBS recorded the
  window between `PLAYTEST_START` and `PLAYTEST_END`, one take per scene, capped at 200 s; one OBS at a time, closed after each
  take. The takes were joined into one H.264 MP4 at the window's own resolution.
- **Scenes**: every Concord scene (26), and the Krai and Ssarith battle and duel (the Krai and Ssarith consoles).
- **Video**: `fleet-command-2026-11-06-demo-scenes.mp4` (46 min, 293 MB) on Google Drive, `dungeon-crawler-2026/`. Link: https://drive.google.com/file/d/1Dja65UAOQ2HewlDouN5_DoD82H92ILtB/view
- **Contact sheets**: three frames of each take, `2026-11-06-demo-scenes/contact0.jpg` to `contact2.jpg`.

## Takes

| Starts | Scene | Script | Length |
|---|---|---|---|
| 00:00 | concord_battle | battle | 149 s |
| 02:28 | concord_duel | battle | 76 s |
| 03:45 | concord_arming | arming | 199 s (capped) |
| 07:04 | krai_battle | battle | 199 s (capped) |
| 10:24 | krai_duel | battle | 77 s |
| 11:41 | concord_wrecks | wrecks | 113 s |
| 13:34 | concord_range | range | 63 s |
| 14:37 | concord_hunt | battle | 199 s (capped) |
| 17:57 | concord_hunt_wrecks | wrecks | 148 s |
| 20:24 | concord_tractor | tractor | 19 s |
| 20:43 | concord_mines | mines | 62 s |
| 21:45 | concord_shuttle | shuttle | 29 s |
| 22:14 | concord_fighters | fighters | 87 s |
| 23:41 | concord_disable | disable | 97 s |
| 25:18 | concord_pinned | pinned | 154 s |
| 27:52 | ssarith_battle | battle | 199 s (capped) |
| 31:12 | ssarith_duel | battle | 119 s |
| 33:10 | concord_allied | allied | 110 s |
| 35:00 | concord_four_races | hop | 5 s |
| 35:05 | concord_warp | warp | 26 s |
| 35:31 | concord_escape | escape | 73 s |
| 36:44 | concord_cloak | cloak | 97 s |
| 38:21 | concord_nebula | nebula | 38 s |
| 38:59 | concord_repel | repel | 199 s (capped) |
| 42:18 | concord_boarding | boarding | 28 s |
| 42:46 | concord_relief | relief | 86 s |
| 44:12 | concord_vip | vip | 48 s |
| 45:00 | concord_distress | distress | 54 s |
| 45:53 | concord_assault | assault | 65 s |

## What the contact sheets show

- Every take has the whole console and the live battle: the Concord skin in the Concord scenes, the Krai skin in the Krai
  ones, the Ssarith skin in the Ssarith ones. Comms is open at the start of each battle, as now set; the scripts open the
  Helm, Power, Tactical, Transport, Science and Security pages as their beats need them.
- Readouts at the x10 scale: ranges of tens of kilometres (62.3 km, 70.4 km), speeds of hundreds to over a thousand m/s.
- Shield bands by class: the frigates and corvettes show one band, the cruisers three.
- The station pages for each system show in its scene: tractor (rocks), mines, shuttles, fighters, cloak sweep, the deck plan
  and teams (repel, boarding), transport cargo (relief, VIP, distress, assault), the reactor and warp (escape, warp).

## Found

- `concord_four_races` (hop) ends after 5 s: its script hops between ships and ends, so the take shows little. A longer beat
  list would show each race's console.
- OBS timed out on 4 takes in the first pass (`OBSSDKTimeoutError` while starting); each was recorded on a second pass.
- Not checked by these takes: phone sizes (landscape, portrait) and expanded panels beyond the ones the scripts expand.
