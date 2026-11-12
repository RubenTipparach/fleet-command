# Low-spec runs on a Raspberry Pi 5, 2026-11-12

Asked by the Mac's fleet-command session for the owner ("see how well the game runs on a pi5... I just want to test if a
lowres version on pi would work cross platform"). The game was run in a window on the Pi's desktop, one run at a time,
with nothing else heavy running. Each run is the scripted captain, in real time.

## The machine

- Raspberry Pi 5 Model B Rev 1.1, 8 GB, Raspberry Pi OS (Debian 12 bookworm), kernel 6.12.34+rpt-rpi-2712, aarch64.
- Wayland desktop (labwc) on a 2560x1080 @ 60 Hz monitor. Godot ran through Xwayland: its desktop GL 3.3 context
  failed, so it fell back to **OpenGL ES 3.1, Mesa 24.2.8, V3D 7.1.10.2** (Compatibility renderer).
- Build: branch `claude/bridge-crew-coop` at `37b1df5e` (it contains `2654b27d`), Godot 4.7.1 mono linux arm64,
  .NET SDK 8.0.428. `scripts/lowspec-prep.py` capped 1015 textures at 512 px (local only, not committed).

## Setup times

| Step | Time |
|---|---|
| `godot --headless --import` (fresh `.godot`, 2310 imported files) | 45.6 s, no errors |
| `dotnet build FleetCommand.csproj` | 40.6 s, 0 warnings, 0 errors |

## The runs (`--rendering-method gl_compatibility --resolution 1280x720 --print-fps`)

| Run | Args | Result | FPS (min / median / max) | Temperature, throttling |
|---|---|---|---|---|
| A | `--lowspec --playtest=battle --play=concord_battle` | `ok`, won in 134 s, hull 100% | 1 / 21 / 46 (p10 19) | 53.8 C, 0x0 |
| D | A with `--lowspec-scale=0.35` | `ok`, won in 132 s, hull 93% | 8 / 21 / 45 (p10 20) | 53.8 C, 0x0 |
| B | `--lowspec --playtest=heavy-fleet --play=concord_heavy` | **hung**, killed at 300 s; no FPS line printed | none | 53.8 C, 0x0 |
| C | B without `--lowspec` | **hung the same way**, killed at 180 s | one line (11) before the stall | 54.3 C, 0x0 |

RAM during each run: about 3.0-3.5 GiB of 7.9 GiB in use, about 4.5 GiB available. The 512 MiB swap was already full
before the runs started.

### A and D: playable, but they hold at about 21 fps, and 3D resolution makes no difference

The FPS is spread evenly over 19-24, not pinned at 20 or 30, so v-sync is not what holds it there. Halving the 3D
resolution again (0.5 down to 0.35) changed nothing, so the frame is not bound by the GPU's fill rate. It is bound by
the CPU (simulation, C# view code, the console's UI drawn at full window size) or by draw calls. The next step is a
profile, not a lower resolution. The screenshots show the whole console and the battle drawn correctly: ships, beams,
the target bracket, every panel.

### B and C: the heavy-fleet scene hangs inside the GL driver

Both runs reach `PLAYTEST_BEAT 1 pick_scene`, load the ships, then load the planet shaders (`planet.gdshader`,
`planet_gas`, `planet_atmosphere`, `planet_craters`, `planet_clouds`, `planet_rings`) and the nebula shaders
(`nebula_plume.gdshader`, `nebula_billboard.gdshader`), and from then on the main thread never finishes another frame.
A `--verbose` rerun showed the same last lines. Diagnosis from two more runs:

- `top -H`: the main thread runs at 100% CPU for the whole stall; every other thread sleeps.
- `gdb` backtraces at 45 s and 53 s into the stall: the main thread is deep inside
  `libgallium-24.2.8-1~bpo12+rpt5.so` (Mesa's V3D driver), called from Godot. Taken together, this looks like the
  driver compiling (or optimising) one huge shader, synchronously, for minutes. The planet or nebula shaders are the
  prime suspects, since they are the last thing loaded before the stall. The `--lowspec` billboard nebula alone does
  not avoid it, because C (no lowspec) and B (lowspec) stall at the same point.
- The window stays frozen on the scene menu (`2026-11-12-run-B-40s.jpg`, `-C-40s.jpg`), the clock at 00:01-00:02.

What would settle which shader it is: run the heavy-fleet scene with the planets switched off, then with the nebula
switched off. A lowspec path that swaps the planet shaders for a plain textured sphere would probably avoid it.

## Files

- `2026-11-12-run-{A,B,C,D}.log`: each run's log, trimmed to the FPS, PLAYTEST and ERROR/WARNING lines, with the
  mid-run temperature, throttling and memory.
- `2026-11-12-run-{A,B,C,D}-40s.jpg`: the game window 40 s into each run, cropped from a full-screen `grim` capture.

ERROR lines: every run has one `ERROR: Condition "ctxErrorOccurred || !gl_display.context->glx_context"` at startup (the
GL 3.3 context failing before the GLES fallback). B and C add `RGBAFloat not supported by hardware, converting to
RGBAHalf` warnings (4 of them), and, on being killed, Godot's exit-time `Unreferenced static string` and
`Pages in use exist at exit` errors, which come from the kill, not from the game.

## Follow-up: 100% scale, and a bare scene with nothing but simple shading

The owner asked for a 100% 3D scale and wanted to know whether the Pi can draw simple things (plain lighting, low-poly
backgrounds) well.

**Run E**, the battle at `--lowspec --lowspec-scale=1.0`: `ok`, won in 169 s, FPS 8 / **20** / 40 (p10 19). Full
resolution costs nothing measurable against 0.5 (21) or 0.35 (21). The 3D scale can stay at 100%.

**A bare Godot scene** (`simple-scene/`, run from a scratch folder with the same Godot build): low-poly ships (6-sided
cones, StandardMaterial3D), a 24x12 sphere planet, 3000 billboard stars in one MultiMesh, one directional light and an
ambient colour, Compatibility renderer, v-sync off, 25 s each:

| Resolution | Scene | FPS (min / median / max) |
|---|---|---|
| 1280x720 | 40 ships | 124 / 130 / 132 |
| 1280x720 | 40 ships + depth fog | 101 / 103 / 107 |
| 1280x720 | 400 ships + fog | 77 / 79 / 81 |
| 1920x1080 | 40 ships + fog | 46 / 48 / 48 |
| 1920x1080 | 400 ships + fog | 40 / 41 / 42 |

So the Pi draws simple lit low-poly 3D at full 720p resolution at over 100 fps. At 1080p the GPU's fill rate becomes the
limit (about 48 fps). The game spends about 48 ms a frame where this scene spends about 8-10 ms, and resolution is not
where the time goes. The suspects are what the game adds per ship and per frame: the hull shader, the 10-16 floodlight
pools and 7-9 lamps per ship (if these are real lights, Compatibility draws one extra pass per light per mesh), the
console UI's many textured controls, and the C# view code. A profile (Godot's monitors, or the game with the console
hidden and then with the hull lights off) would say which.
