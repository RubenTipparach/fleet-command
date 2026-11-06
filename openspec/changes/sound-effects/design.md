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
| Weapons | `lance_fire` (0.55 s), `disruptor_fire` (0.55 s), `torpedo_launch` (0.9 s), `overload_charge` (loops), `missile_launch` (1.2 s), `pd_burst` (0.5 s) | at the mount node |
| Impacts | `shield_hit`, `hull_hit`, `torpedo_detonate`, `explosion_small`, each with a short alternative (`*_short`, 0.6-1.1 s), and `ship_destroyed` | where the hit lands |
| Ship | `engine_hum` (loops), `reactor_hum` (loops), `thruster_burst`, `shields_raise`, `shields_collapse` | at the ship |
| Console | `ui_chirp` (Concord), `ui_clunk` (the metal skins), `ui_confirm`, `ui_deny`, `weapon_charged`, `target_lock`, `hail`, `pause_on`, `pause_off` | no |
| Alerts | `red_alert` (loops; alternatives `red_alert_twotone`, `red_alert_wail`), `shields_failing`, `hull_critical`, `core_breach` | no |
| Ambience | `amb_bridge` (stereo, a minute, loops: the air duct and a low hum, with soft bleeps scattered over it) | no |

**The owner's first notes** (2026-11-06): "weapons all are too long, lance fire should sound more punchy": every weapon
cut to 0.5-1.2 s, starting at its peak, with a kick under it (`punch_thump`) and, for the Lance, a crack and a snap
(`beam_snap`); "hull hits are all great btw, I would make some shorter sfx as alternatives": the hits kept, a short
alternative of each added; "red_alert sucks": the synthesised whoop replaced by a recorded klaxon, with two recorded
alternatives; "amb_bridge - should be like aminute with bleeps and boops on theb ridge and scanning sounds": the bed
made a minute long with sounds scattered over it (`scatter` in `cc0_cues.json`). Then (2026-11-07) "it has a really
annoying whirling noise ... get rid of the sweep sound, just subtle bleeps and airduct noise": the sweeps, the pings and
the swelling computer-room layer removed, the bleeps about 10 dB quieter. A bed stays background: no tone that moves
in pitch, no swell, nothing more than about 12 dB over it.

**Softened the Crab Breeze way** (owner, 2026-11-07: "soften bridge ambient along with a lot of the other weapons, check
the procedure in crab breeze how audio was softened with low passes"). Crab Breeze has two rules, read at `6819b52`:
its ambience beds (`openspec/changes/ambience/design.md` section 2, after its owner's "ambient sounds needs to be softer
and queiter") are compressed 3:1 from -18 dB, low passed at 3.5 kHz, given a diffuse reverb and written 11 dB lower;
every sound from outside its sealed cockpit (`openspec/changes/audio/design.md` section 3) is 6 dB down and low passed
at 4 kHz, twice over. Here the bridge bed takes the first rule (`distance` 3.5 kHz, 35% wet, -14 dB peak, and a
24 dB/octave low pass over the whole bed so the bleeps are softened with it), and every weapon the second, heard from
the bridge through the hull (`lp` 4000, `lp_order` 4, `peak_db` -7). Measured: the Lance's spectral centre fell from
1196 to 815 Hz and its energy above 4 kHz from 4.8% to 0.2%, 7 dB quieter on average, its first 20 ms still its
loudest; every weapon is 6-7 dB quieter on average. The hits keep their level (the owner: "hull hits are all great").

**A bed is comfortable, not only quiet** (owner, 2026-11-07: "low rumble bridge ambient, it hurts my ears a bit",
"make it sound pleaasent like proper ambience"). Low-passing the bed at 3.5 kHz left only its low end, a 53 Hz hum and
a boxy 200-400 Hz roar, which tires the ear. The bed is now a soft room tone (pink noise, equal energy per octave, from
200 Hz to 6 kHz, two uncorrelated sides for width) under the air duct cut below 180 Hz, with nothing under 180 Hz
(24 dB/octave) and a gentle top to 6.5 kHz. A bed's rules: no steady tone under 150 Hz, energy spread across the
octaves rather than piled in one, nothing moving in pitch, its loudness within about 5 dB.

**Alternatives in the game**: a hit plays its short alternative when another hit on the same ship is still sounding,
so rapid fire stays crisp; which battle stations alarm plays is the owner's pick on the Sounds page.

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
