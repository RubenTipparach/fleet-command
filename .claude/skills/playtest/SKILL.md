---
name: playtest
description: Run, watch, record and report a Fleet Command playtest the repository's way. A scripted captain plays the real Godot game through the same input actions a player uses (clicks, taps, keys), at desktop and phone screen sizes, panels expanded and collapsed; one game per device on the owner's most powerful connected machine; a check every five minutes while a desktop run plays; the take recorded with OBS between PLAYTEST_START and PLAYTEST_END, looked at on a contact sheet, a share copy uploaded to Google Drive and confirmed with the Drive connector; a written report. Use whenever asked to playtest, test the game as it is played, record or upload gameplay, check that a feature works in the running game, run the scripted captain, or report on a run ("playtest it", "record a run", "upload the video", "does it play right", "show me the battle").
metadata:
  author: Fleet Command (Claude Code), after Crab Breeze's playtest skill (69ce196), Star Crew's --playtest, Mining Mike's five-minute checks and Tom Lander's bot-run
  version: "1.0"
---

# Playtests

A feature is not done until a playtest shows it in the running game (CLAUDE.md section 9). This is
how to run one. The scripted captain, the `--playtest` flag and the beats do not exist yet: they are
the first `playtest-pipeline` change once the game project exists. Until then a playtest is the owner
playing and recording with `obs-record` by hand, and a mockup is reviewed with `threejs-mockups`.

## What a playtest is

- **The game as it is played**: the console, the 3D view, the captain's inputs, from load to the
  end of what it shows, with nothing cut together.
- **The scripted captain plays fair.** It sends the same input actions a player sends, through
  `Input.parse_input_event` (key and touch events for actions, pointer events on panel controls,
  drags on the view). It decides nothing a player could not, and the game checks it as it checks
  a player. One scripted captain; a new beat is added to it, never a second bot.
- **A script is a list of beats** in data (`data/playtests/<name>.json`): `wait_ready`,
  `select_target`, `set_speed`, `turn_to`, `allocate`, `arm` (offline, normal, overload),
  `fire`, `fire_multiple`, `expand_panel`, `collapse_panel`, `orbit_view`, `hold`.
- **Both screen shapes.** A script names its sizes: the desktop reference size and a phone size
  (landscape and portrait, survey `U2`). The phone pass expands each panel it uses and
  checks the 3D view is live at the side.

## The output lines

Printed on stdout, one per line, so tools can wait on them:

| Line | When |
| --- | --- |
| `GAME_READY` | every asset, shader and the battle are loaded (the loading screen is gone) |
| `PLAYTEST_START <name>` | the first beat begins; recording starts here, never before `GAME_READY` |
| `PLAYTEST_BEAT <i> <beat>` | each beat begins |
| `PLAYTEST_RESULT <ok|stuck|crash> <detail>` | the script ended, and how |
| `PLAYTEST_END <name>` | after a two-second hold on the last frame; recording stops here |

A beat with no progress for 10 s of game time is `stuck`, and the line names the beat and the state.

## Where it runs

1. **Look for a connected session on the owner's hardware first** (`list_sessions`, then
   `send_message` with the commit and the command). Results come back through git.
2. **Never playtest on the owner's machine while they are using it** (CLAUDE.md section 8), unless
   they have said in this conversation that Claude may.
3. **A cloud session has no GPU and no display.** It cannot run OBS. It can render stills under
   Xvfb with Mesa (`xvfb-run -a .tools/godot/godot --path . --rendering-driver opengl3 ...`) to check
   what a frame looks like, never how fast it runs, and says so in the report.
4. **One game per device.** Look for a running game process first; no build or benchmark beside a
   recorded take.

## Run and record

```bash
# Play a script and get a verdict (no recording), once the flag exists:
.tools/godot/godot --path . -- --playtest first-battle --size desktop

# Record it on the owner's Windows machine, then upload the share copy:
python .claude/skills/obs-record/scripts/obs_record.py \
  --launch "builds/windows/fleet-command.exe -- --playtest first-battle" --window-exe fleet-command.exe \
  --capture game --start-on "^PLAYTEST_START" --stop-on "^PLAYTEST_END" \
  --fps 60 --out output/playtests/<date>-first-battle.mp4 --contact-sheet
ffmpeg -i output/playtests/<date>-first-battle.mp4 -c:v libx264 -crf 26 -preset slow -an \
  "G:/My Drive/dungeon-crawler-2026/fleet-command-<date>-first-battle.mp4"
```

- Record in **real time**: a take shows how the game feels.
- **Look at the contact sheet before calling it done**: a dialog over the game, a black capture,
  the wrong window, a panel that overflowed or a view that stopped rendering are easy to miss.
- `output/` is git-ignored. The full take stays local; only the share copy goes to Drive.

## Upload to Google Drive

Every recorded take is uploaded, to `G:/My Drive/dungeon-crawler-2026/` (Drive for Desktop),
named `fleet-command-<YYYY-MM-DD>-<what>.mp4`. Confirm it with the Google Drive connector
(`search_files` for the title); the connector cannot upload video itself. Put the link in the report
and the reply. If the folder is missing, say so and give the owner the copy command.

## While a desktop run plays: a check every five minutes

Starting the first run of a session arms a five-minute check (`CronCreate` `*/5 * * * *`), removed
(`CronDelete`) when no more runs are planned or the owner says stop. This is the one exception to "no
scheduled check-ins". Each check takes a screenshot, looks at it, reports in two or three lines,
and stops a run that is plainly stuck.

## Report

`docs/playtests/<YYYY-MM-DD>-<name>.md`: the commit and the machine (CPU, GPU, resolution, refresh
rate), or "cloud, Xvfb stills, no frame time"; each beat's outcome and the `PLAYTEST_RESULT` line;
the Drive link and the contact sheet (`docs/playtests/<date>-<name>_frames.png`); what looked wrong,
one line each, and anything to fix as a task in its OpenSpec change.

## Do not

- Cut between views, fast-forward a take, or upload the full OBS file.
- Call a feature done from a unit test, a mockup or a still when it can be played.
- Run two games on one machine, or a build beside a recorded take.
