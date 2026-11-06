# Design: music

## 1. The score

The motifs and the cues are `docs/design/music.md`; the method is the `music-authoring` skill. In short:

| Cue | Key and tempo | Length | Built from |
| --- | --- | --- | --- |
| Main Title | E flat, 96 BPM | 22 bars, 55 s | the wonder chord, the Concord on horns, the Concord in full, the wonder chord |
| Battle Stations | C minor, 132 BPM | 24 bars, 43.64 s, loops | the Threat's ostinato and call, the Concord's answer, the two against each other |
| Victory | E flat, 96 BPM | 5 bars, 12.5 s | the Concord's bars 5-8, the wonder chord |
| Ship Lost | E flat minor, 66 BPM | 5 bars, 18 s | the Lost |

**The factions** (owner, 2026-11-07: "we need custom music for each faction (just do the ones in game right now) and
add ambient music as well"):

| Faction | Battle loop | Ambient loop | Built from |
| --- | --- | --- | --- |
| Concord | Battle Stations (132 BPM, 43.6 s) | Concord Watch (64 BPM, 60 s) | The Concord theme and the wonder chord |
| Krai Warhost | the Line (140 BPM, 43 s) | the Forge at Rest (60 BPM, 60 s) | Tradition kept and added to: a ground bass with a voice added each pass; the emperor over three houses; tallies in fives |
| Ssarith Hunt | the Bows (116 BPM, 41 s) | the Long Watch (56 BPM, 60 s) | The Bow Rites: one figure at three sizes; the Long Watch's patience; the Fair Chase's open gate; the single strike |
| Kelvane Institute | the Measurement (126 BPM, 38 s) | the Long Baseline (60 BPM, 60 s) | Hydrogen's Balmer lines as pitches; the needle brought to the mark; every finding repeated |

Each choice's source is in `docs/design/music.md`. **Battle music follows the faction of the ship you command**, as the
console skin does; the ambient loop plays while no enemy is in contact. Seven instruments new to the repository were
calibrated (`music/instruments.json`: celesta, marimba, vibraphone, tubular bells, pizzicato strings, the bowed pad,
voice oohs). The Kethran and the Vos, opponents on stand-in ships with no skin, get music when they are in the game.

Checked (not approved): no clipping in any raw mix, -16 LUFS each, a loudness profile with the cue's shape, and a
spectrogram where the melody is the brightest line (`docs/screenshots/audio/music-spectrograms.jpg`). The leads are
lifted with `@mix` after the first render showed the chords and basses over them.

Size: the twelve OGGs are about 11 MB (Vorbis q6, stereo).

## 2. Playing it in the game (after approval)

- **A music player in `src/Game`** on the Music bus, with one rule table in `data/audio/music.json`: which cue plays in
  which state, the crossfade in seconds, and the loop points (from the score: bars x 240 / BPM).
- **States**: the menus play the Main Title; a battle with no enemy in contact range plays the commanded faction's
  ambient loop over the bridge bed (`amb_bridge`, `openspec/changes/sound-effects`); contact starts that faction's battle
  loop with a 2 s crossfade; a won battle
  plays Victory over a 1 s fade of the loop; a lost ship plays Ship Lost once, and the loop resumes if the battle goes on.
- **Contact** is read from the simulation (an enemy within its weapons' longest range of any ship in the fleet), the
  same rule the console's alerts use (CLAUDE.md section 6.1).
- **Pause** (survey G2) ducks the music by 6 dB and keeps it playing.

## Open questions

| # | Question | Status |
| --- | --- | --- |
| S2 | Do rival empires get themes of their own? | **Decided** (owner, 2026-11-07, in conversation): every faction in the game gets its own music |
| S5-S7 | Is each faction's musical reading of its culture right (Krai, Ssarith, Kelvane)? | Open (survey), recommendation: keep as written |
| S3 | How does the battle music follow the fight? | Open (survey), recommendation provisional here: one loop now, intensity layers next |
| - | The score itself | Waiting on the owner's ears (the Music page, the tracker) |
