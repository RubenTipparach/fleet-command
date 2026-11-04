# Playtest 2026-11-04: the scenario bot runs, both sides, recorded

The owner asked: "develop a bot to play through these test scenarios for each faction", "can you record all the scenario
bot runs?", "upload google drive at full resolution, mp4 encoded".

| | |
|---|---|
| Commit | `5007267` on `claude/ships-export-mac` (main at `0979292`, PR #20's two late commits, the ship export and `ShipSkin`) |
| Machine | The owner's Mac: Apple M4 Max, macOS 15.6.1, Godot 4.7.1 .NET (Forward+, Metal), .NET 8.0.427 |
| How | `data/playtests/suite.json`: the scripted captain plays each scenario through input actions, in a 1920 x 1080 window in real time. OBS Studio records the window from `PLAYTEST_START` to `PLAYTEST_END` (`.claude/skills/obs-record/scripts/obs_record.py`, ScreenCaptureKit window capture, scale 1.0, 60 fps) |
| Takes | H.264 MP4, 1920 x 1136 (the game window and its title bar), 60 fps, at the take's own size; copied to Drive as they are, never scaled down |
| Ships | The approved ships, exported today from the Krai page v21 and the Concord page v20, with the hull shader and lamps from the manifest |

## The runs

| Run | Scenario | Result | Game time | Take | Drive |
|---|---|---|---|---|---|
| `battle` | Concord, Battle | ok: won, 0 foes left, hull 100 % | 181 s | 3 min 00 s, 139 MB | [concord-battle](https://drive.google.com/file/d/1S-sPHaGCMmb7Gv-BC_OBRmTF3ls_bNjr/view?usp=drivesdk) |
| `battle` | Concord, Duel | ok: won, 0 foes left, hull 100 % | 270 s | 4 min 29 s, 208 MB | [concord-duel](https://drive.google.com/file/d/1DZv5HX43zJXXX4ibMnDCfCnCZ_u-3s8d/view?usp=drivesdk) |
| `arming` | Concord, Arming | ok: won, 0 foes left, hull 88 % | 252 s | 4 min 12 s, 194 MB | [concord-arming](https://drive.google.com/file/d/1eZbN0LPwZ5mQ5-E9mzL34ScmN5H_U9fg/view?usp=drivesdk) |
| `battle` | Krai, Battle | ok: lost, 1 foe left, hull 0 % | 523 s | 8 min 43 s, 403 MB | [krai-battle](https://drive.google.com/file/d/1LCeh_QTqTZtrnT945IDZE3YD8OoVSqKr/view?usp=drivesdk) |
| `battle` | Krai, Duel | ok: lost, 1 foe left, hull 0 % | 267 s | 4 min 26 s, 206 MB | [krai-duel](https://drive.google.com/file/d/12WlVvbrFWNZejKil1S-JArtqsI-wDPbp/view?usp=drivesdk) |
| `arming` | Krai, Arming | ok: lost, 2 foes left, hull 0 % | 216 s | 3 min 35 s, 166 MB | [krai-arming](https://drive.google.com/file/d/15TE2VUdeYcctHZ-7GT2oSeyfyST-wHLi/view?usp=drivesdk) |

"ok" is the suite's verdict: the run played through to a result with no crash or stall. Every file was confirmed with
the Google Drive connector at the same byte size as the local take.

Beats, every run: wait for `GAME_READY`, pick the scenario in the scene selector, then (`battle`) open the power station
and the helm, expand the Weapons panel, hold, collapse it, next target, intercept, fight; or (`arming`) arm overload,
next target, intercept, fight. All beats ran in all six.

## What the contact sheets show

Looked at, one per take (`docs/playtests/2026-11-04-scenario-bot-runs/<side>-<scenario>.jpg`):

- Each side's own console skin: the Concord's flat blue and lavender, the Krai Warhost's red and orange kit.
- The approved ships in the chase view, never a stand-in: the Concord's white hulls with their detail tile and glows,
  the Krai hulls with the crimson war stripe, window rows and bronze lines; targets bracketed with name, class, range
  and speed; fire lines between ships.
- In the Krai Battle, command passing to the Vekk when the Kharz is lost ("Comms: command passes to Vekk").
- No menu, dialog or blank frame over a take.

## Findings

- **The Krai side loses every scenario** (and the Concord wins every one), headless and recorded alike. The bot flies the
  same script for both sides, so this is the scenarios' balance or the Krai's arming, not the recorder; worth a look
  before the Krai scenarios are called done.
- The Concord hulls show the armour white and the detail tile only: their own paint rules (service band, livery
  stripes, window rows, light pools) are not yet ported to `hull.gdshader` (`docs/design/mockup-sync.md`).
- The recorder could not see OBS on macOS (it looked for a process named `obs`; the Mac's is `OBS`), so it would never
  have closed OBS and could have started a second one, and it launched OBS with `--multi`. Fixed before the takes: one
  OBS for the whole run, closed after each take, checked.
