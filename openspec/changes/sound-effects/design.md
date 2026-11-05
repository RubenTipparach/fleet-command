# Design: sound-effects

## 1. Making the sounds

The pipeline is Crab Breeze's (`tools/audio` at `0e569e3`), the method is the `sound-effects` skill.

| Step | Tool | Output |
| --- | --- | --- |
| Fetch and licence-check every pack | `tools/audio/fetch_cc0.py` (refuses a page without CC0 or public domain) | `/opt/sfx-src` (never committed), `tools/audio/cc0_licences.json` |
| Synthesise cues and layers | `tools/audio/gen_sfx.py` (seeded from each cue's name) | `output/sfx/<cue>.wav`, `sfx.json` |
| Cut recorded cues | `tools/audio/cut_cc0.py` to `tools/audio/cc0_cues.json` | replaces the synthesised WAV; `assets/audio/*/SOURCES.md` |
| Encode | `tools/audio/render_sfx.sh` | `assets/audio/sfx/<cue>.ogg` (Vorbis q5, mono), `assets/audio/ambience/amb_*.ogg` (stereo, q4) |
| Check | `tools/audio/inspect_sfx.py check` | clipping and loop seams; `output/sfx/sheet.png` looked at |
| Audition | `tools/audio/gen_sfx_page.py` | `docs/mockups/sounds.html` |

Two changes from Crab Breeze: no muffled or far copies (Godot's `AudioStreamPlayer3D` filters by distance itself), and
synthesis layers (group `_layer`) that are mixed into recorded cues and never written alone.

**Chosen without listening.** A cloud session cannot hear. Files were picked by name, length, spectral centroid and
loudness strip (`inspect_sfx.py inventory`, `envelope`), and the built set judged on a spectrogram contact sheet
(`docs/screenshots/audio/sfx-sheet.jpg`). The owner's ears approve it, on the Sounds page.

## 2. The first set

| Group | Cues | Placed |
| --- | --- | --- |
| Weapons | `lance_fire`, `disruptor_fire`, `torpedo_launch`, `overload_charge` (loops), `missile_launch`, `pd_burst` | at the mount node |
| Impacts | `shield_hit`, `hull_hit`, `torpedo_detonate`, `explosion_small`, `ship_destroyed` | where the hit lands |
| Ship | `engine_hum` (loops), `reactor_hum` (loops), `thruster_burst`, `shields_raise`, `shields_collapse` | at the ship |
| Console | `ui_chirp` (Concord), `ui_clunk` (the metal skins), `ui_confirm`, `ui_deny`, `weapon_charged`, `target_lock`, `hail`, `pause_on`, `pause_off` | no |
| Alerts | `red_alert` (loops), `shields_failing`, `hull_critical`, `core_breach` | no |
| Ambience | `amb_bridge` (stereo, loops) | no |

Levels as written: weapons and blasts peak at -1 dBFS, alerts -4 to -6, console keys -8 to -9, the bridge bed -6. The
mix between cues is the cue table's `volume_db` in the game (section 3), not a re-cut.

Size: 29 mono cues 604 KB and the bed 120 KB as OGG, about 0.7 MB in all.

## 3. Playing them in the game (after approval)

- **One cue table**, `data/audio/sfx.json`: per cue its file, `bus` (`effects`, `console`, `alerts`, `ambience`),
  `volume_db`, `placed` (bool), `max_distance_u` (game units), `pitch_jitter` (0 to 0.1, seeded), validated on load like
  every data file (CLAUDE.md section 6.4). A skin names its key cue in its kit manifest (survey U6).
- **Sounds read the battle's own events, the ones the effects draw from** (CLAUDE.md section 6.1): `Battle.Hits`
  (`HitEvent`: kind, shield absorbed, hull damage, destroyed) and the shots fired, so a sound can never disagree with
  what the view shows. A mapping in the cue table says which event plays which cue: a hit with `Absorbed > 0` and no hull
  damage plays `shield_hit`, hull damage plays `hull_hit`, `Destroyed` plays `ship_destroyed`.
- **Placed sounds** are `AudioStreamPlayer3D` at the mount's or the ship's named node (CLAUDE.md section 10), attenuated
  by distance with the player's built-in low-pass. Console, alerts and the bed are `AudioStreamPlayer` on their buses.
- **Buses**: Master, Music, Effects, Console, Alerts, Ambience. The Options sheet's discrete volume boxes
  (`openspec/changes/game-flow`, 0-10) set Master, Music and Effects.
- **A voice cap** for phones (survey T3): at most 24 sounds at once; a new sound past the cap takes the quietest
  one's voice. Pause (survey G2) pauses the placed sounds and keeps the console live.
- **The scripted captain** (CLAUDE.md section 9) logs each cue it hears as `PLAYTEST_BEAT sound <cue>`, so a playtest
  can check that firing a Lance played `lance_fire` at the right mount.

## Open questions

| # | Question | Status |
| --- | --- | --- |
| S2 | Do rival empires get their own music? (and so their own key sounds) | Open (survey), recommendation: not until the owner sets each culture |
| S4 | How does a battle sound in silent space? | Open (survey), recommendation provisional here: classic space opera, every sound placed in 3D, duller with distance |
| - | The set itself | Waiting on the owner's ears (the Sounds page, the tracker) |
