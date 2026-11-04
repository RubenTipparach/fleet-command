# Design: music

## 1. The score

The motifs and the cues are `docs/design/music.md`; the method is the `music-authoring` skill. In short:

| Cue | Key and tempo | Length | Built from |
| --- | --- | --- | --- |
| Main Title | E flat, 96 BPM | 22 bars, 55 s | the wonder chord, the Concord on horns, the Concord in full, the wonder chord |
| Battle Stations | C minor, 132 BPM | 24 bars, 43.64 s, loops | the Threat's ostinato and call, the Concord's answer, the two against each other |
| Victory | E flat, 96 BPM | 5 bars, 12.5 s | the Concord's bars 5-8, the wonder chord |
| Ship Lost | E flat minor, 66 BPM | 5 bars, 18 s | the Lost |

Checked (not approved): no clipping in any raw mix, -16 LUFS each, a loudness profile with the cue's shape, and a
spectrogram where the melody is the brightest line (`docs/screenshots/audio/music-spectrograms.jpg`). The leads are
lifted with `@mix` after the first render showed the chords and basses over them.

Size: the five OGGs are 4.2 MB (Vorbis q6, stereo).

## 2. Playing it in the game (after approval)

- **A music player in `src/Game`** on the Music bus, with one rule table in `data/audio/music.json`: which cue plays in
  which state, the crossfade in seconds, and the loop points (from the score: bars x 240 / BPM).
- **States**: the menus play the Main Title; a battle with no enemy in contact range plays only the bridge bed
  (`amb_bridge`, `openspec/changes/sound-effects`); contact starts Battle Stations with a 2 s fade-in; a won battle
  plays Victory over a 1 s fade of the loop; a lost ship plays Ship Lost once, and the loop resumes if the battle goes on.
- **Contact** is read from the simulation (an enemy within its weapons' longest range of any ship in the fleet), the
  same rule the console's alerts use (CLAUDE.md section 6.1).
- **Pause** (survey G2) ducks the music by 6 dB and keeps it playing.

## Open questions

| # | Question | Status |
| --- | --- | --- |
| S2 | Do rival empires get themes of their own? | Open (survey), recommendation provisional here: the Threat for every enemy until the owner sets a faction's culture |
| S3 | How does the battle music follow the fight? | Open (survey), recommendation provisional here: one loop now, intensity layers next |
| - | The score itself | Waiting on the owner's ears (the Music page, the tracker) |
