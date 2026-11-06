# Fleet Command music: the motifs and the rules of the score

Read this before writing, arranging or rendering any music for the game. It holds the musical
world, every motif with its notes and what it means, and the cues built from them so far. The
method (Strudel score, multitrack MIDI, LMMS headless with real instruments) is the
`music-authoring` skill. The canonical text of every motif is the `MOTIF LIBRARY` block of
`music/motifs.strudel`; the notes below are that block written out for reading.

Listen to everything on the Fleet Command Music page (the tracker links it; record in
`docs/artifacts/music/`).

## The musical world

- **Orchestral space opera, brass, strings and timpani, over a synth bed** (survey S1, owner
  2026-10-30). Classic space opera (survey G1): bright, named starships, a federation of peoples.
  The orchestra is the GeneralUser GS soundfont's (horns, trumpets, trombones, tuba, string
  sections, tremolo strings, cellos, basses, timpani, harp, glockenspiel, choir); the bed is LMMS
  synths (`gm_pad_poly` TINTNpad, `gm_synth_strings_1` PowerStrings, `gm_fx_sci_fi` Supernova).
- **Our own music.** No quotation, paraphrase or pastiche of any film, series or game score. A
  motif that reminds anyone of a known theme is rewritten.
- **Home key: E flat major, with the lift to F major.** E flat is the brass's key. The lift (an F
  major chord, whose A natural is the raised fourth of E flat) is the Concord's sense of wonder;
  the **wonder chord** (E flat, B flat, D, G, A) is the Concord at rest and ends the title and
  the victory.
- **The enemy is C minor**, E flat's relative minor, so the Concord's theme can rise straight
  out of the battle's key. The flat two (D flat) and the tritone (the call's D flat down to G) are
  the enemy's edge.
- **Every faction in the game has its own music** (owner, 2026-11-07: "we need custom music for each faction (just
  do the ones in game right now) and add ambient music as well"): the Concord, the Krai Warhost, the Ssarith Hunt and
  the Kelvane Institute, each with a battle loop and an ambient loop. **A faction's music comes only from its written
  culture**, each choice naming its source below, never from what the species looks like (CLAUDE.md section 1), and
  never a real-world people's signature instrument (no koto, sitar, bagpipe or the like standing for an alien
  culture). Culture is the owner's to set, so each reading is provisional until the owner hears it (survey S5-S7).
  The Threat stays the enemy's material in the Concord's battle.
- **Tempi**: 96 BPM for the title and the victory, 132 for the Concord's battle, 66 for a loss, 56-64 for the ambient
  loops; each faction's battle has its own (Krai 140, Ssarith 116, Kelvane 126). One cycle of
  Strudel is one 4/4 bar (`setcps(BPM/60/4)`).

## The motifs

Notes are scientific pitch (C4 is middle C). Durations are in eighths: `@2` a quarter, `@3` a
dotted quarter, `@4` a half, `@6` a dotted half, `@8` a whole bar; riffs are in sixteenths.

### 1. The Concord: the player's fleet (8 bars)

A sentence (see "Writing a melody" below), over its own chords:

```
idea           Eb    eb4@3 c5 c5@2 bb4 c5      | Cm   eb5@4 c5@2 g4@2
idea, a 4th up Ab    ab4@3 f5 f5@2 eb5 f5      | F    g5@4 f5@2 a4@2
continuation   Gm    bb4@2 d5@2 bb4 d5 g5@2    | Ab   c5@2 eb5@2 c5 eb5 ab5@2
high point     Bbsus4, Bb7   bb5@6 ab5@2       | Eb   g5@3 f5 eb5@4
```

The idea is a dotted leap of a sixth (E flat up to C) with the gap filled by step, then a fall
through the chord. Bar 3 is bar 1 a fourth higher on A flat. Bar 4 lifts to F major: its G leans on
F, then drops to the A natural, the raised fourth. Bars 5 and 6 cut the idea to quarters and
eighths and climb a chord at a time. The one high point, B flat 5, is the start of bar 7, three
quarters of the way through, and the line falls to E flat. The range is E flat 4 to B flat 5, an
octave and a fifth, so a horn or a trumpet carries it.

Use it: whenever the fleet is the subject. On one horn it is a promise; on trumpets over the
orchestra it is the fleet in full.

### 2. The wonder chord

`[eb2, bb2, g3, d4, a4]`: E flat major with the major seventh and the raised fourth, spread wide.
It ends the title and the victory, and opens the title under a harp.

### 3. The Threat: whoever the fleet is fighting

**The ostinato** (4 bars of sixteenths, grouped 3 + 3 + 3 + 3 + 4, accents on each group), on
cellos with the basses an octave below, timpani on the accents:

```
c2 c3 c2 | c2 c3 c2 | c2 c3 c2 | db2 db3 db2 | c2 g1 ab1 g1
c2 c3 c2 | c2 c3 c2 | eb2 eb3 eb2 | db2 db3 db2 | c2 bb1 ab1 g1
c2 c3 c2 | c2 c3 c2 | c2 c3 c2 | db2 db3 db2 | c2 g1 ab1 g1
g1 g2 g1 | g1 g2 g1 | ab1 ab2 ab1 | g1 g2 g1 | b1 c2 d2 eb2
```

Over `Cm | Db/C | Ab | G(add b9)`. The uneven grouping (three groups of three, then four) keeps
the battle leaning forward; the last bar climbs out of G back to C.

**The call** (2 bars, horns, trombones an octave below): `c4@2 db4@2 g3@4 | c4@2 eb4 db4 c4@4`.
Up a semitone, down to G (from D flat, a tritone), then an answer that sinks back to C.

Use them: whenever an enemy is near. The ostinato alone says "they are here"; the call says
"they are coming for you".

### 4. The Lost: a ship or a battle lost (4 bars)

The Concord's idea in E flat minor, slow, on one horn: `eb4@3 cb5 cb5@2 bb4 cb5 | db5@4 bb4@2 gb4@2 |
f4@4 gb4@2 f4@2 | eb4@8`, over `Ebm | Cb | Bbm | Ebm`. The same leap and fall as the Concord, so a
loss sounds like the fleet's own theme broken.

### 5. The Krai Warhost: the Line, the houses, the Crown and the tally

Sources: "this faction believes strongly in tradition" (owner, 2026-11-04), read as tradition "kept, carried and added
to, never replaced" (`openspec/changes/faction-emblems/design.md`); "an emperor cheif ... a star ruling over the 3 great
houses" (owner); "battle tallies in fives" on the livery (`openspec/changes/krai-ship-concepts/design.md`); "War is the
only honest test" and forged steel (the world view accepted under survey U7). D Phrygian (its flat second, E flat,
stern and old), five-bar phrases.

- **The Line** (a ground bass that never changes, 5 bars): `d2@4 f2@2 eb2@2 | c2@4 d2@4 | bb1@4 c2@2 eb2@2 | d2@4 a1@4 |
  d2@8`. Tradition kept: it is never varied. Tradition added to: every pass adds a voice over it.
- **The houses** (open fifths, unadorned, one per bar): `D-A | C-G | B flat-F | D-A | D-A`. They enter one house at a
  time: horns, then strings, then a low choir.
- **The Crown** (the emperor over the three houses, trumpets, 5 bars): `a4@2 d5@2 f5@4 | eb5@3 d5 c5@2 d5@2 | f5@2 g5@2
  a5@4 | g5@3 f5 eb5@2 d5@2 | d5@8`, one high point (A5) in bar 3.
- **The tally**: five strikes on a tubular bell in the fifth bar of every phrase, counted out like the tallies on a hull.
- Battle: **the Line** (140 BPM, 25 bars, 43 s, loops) builds over five passes, the Crown arriving last. Ambient: **the
  Forge at Rest** (60 BPM, 15 bars, 60 s): the Line on low strings, the houses hummed, a warm bed (the forge lamps), the
  tally soft, the Crown far off on one horn.

### 6. The Ssarith Hunt: the Bows, the stalk, the call and the strike

Sources: the Bow Rites (owner, 2026-11-04: bows of "different sizes to represent their different stages", "3 bow and
arrows overlapping pointed to the right"); the drafted traditions (`openspec/changes/faction-emblems/design.md`, provisional
until the owner picks): the Long Watch ("its ships still wait rather than chase"), the Fair Chase ("was the gate left
open?"); and the skin's rules "nothing moves until it strikes" and "a hunter shows nothing the quarry could see"
(`openspec/changes/console-skins/design.md`). B minor pentatonic, no third in the held harmony.

- **The Bows** (the Bow Rites: one figure at three sizes, overlapping, rising like arrows pointed right): the young bow
  `b4 d5 e5 f#5` in eighths, high, on pizzicato strings; the grown bow the same an octave down at half speed on harp; the
  elder's two octaves down at quarter speed on cellos or basses. All three start together.
- **The stalk** (the Long Watch: two bars of sixteenths, mostly rests): `b1 . . . . . b1 . . . . . . . . . | . . . .
  f#1 . . . . . . . b1 . . .`, on pizzicato basses and muted timpani.
- **The call** (2 bars, low flute or horn): `b4@6 a4 f#4 | e4@2 f#4@6`. It ends on the fifth, never home: the gate left
  open.
- **The strike**: one tutti chord, B minor, given once ("nothing moves until it strikes").
- Battle: **the Bows** (116 BPM, 20 bars, 41 s, loops): the stalk, the call, the bows, the stalk quickening, then the
  strike and the open fifth held. Ambient: **the Long Watch** (56 BPM, 14 bars, 60 s): a held open fifth, the stalk on a
  low harp, the call on a far flute, the young bow plucked softly.

### 7. The Kelvane Institute: the Balmer motif, the calibration and the baseline

Sources: "Truth is measured, never assumed" (the world view accepted under U7); the hydrogen atom as its emblem (owner,
2026-11-06: "I would like this hydrogen atom eigen plot used as the logo/emblem"), read as the Institute's founding
measurement, "the simplest atom's states, computed and then checked against its spectrum"
(`openspec/changes/kelvane-battle-console/design.md`); and the drafted traditions (provisional): the Calibrators ("the
needle brought exactly to the true mark"), the Open Notebook ("a finding nobody has repeated is called a rumour"), the
Long Baseline ("began by measuring the distance to the stars"). Analog lab instruments (the owner's look): struck,
precise voices (marimba, vibraphone, celesta) over strings and a warm synth bed.

- **The Balmer motif**: hydrogen's first four visible lines (656.3, 486.1, 434.0 and 410.2 nm, frequency ratios 1,
  1.350, 1.512, 1.600) rounded to the nearest semitones above C: `c5 f5 g5 ab5`. Each true line is 14-20 cents sharp of
  its note; the motif is the measurement written down.
- **The calibration** (2 bars): `b4 c5@3 e5 f5@3 | f#5 g5@3 g5 ab5@3`: each line brought up to its note from a semitone
  below, the needle to the mark.
- **The baseline**: a bass falling one equal step a bar, `c2 | bb1 | ab1 | g1`, the measured steps of the Long Baseline.
- **Repetition is verification**: every section is played twice, exactly (the Open Notebook).
- Battle: **the Measurement** (126 BPM, 20 bars, 38 s, loops): the Balmer motif as a marimba ostinato in sixteenths over
  a lab clock's tick, the baseline on low strings, the calibration on horns and vibraphone twice, then the motif in long
  notes on the brass twice. Ambient: **the Long Baseline** (60 BPM, 15 bars, 60 s): the motif one note a bar on
  vibraphone, stated and repeated, the calibration on celesta, the baseline on a warm bed, a quiet tick.

## The cues

| Cue | Score | Length | What it is |
| --- | --- | --- | --- |
| Concord Watch (ambient) | `music/concord_ambient.strudel` | 16 bars, 64 BPM, 60 s, **loops** | The Concord theme at half speed on a flute over a harp and a soft bed |
| Krai: the Line (battle) | `music/krai_battle.strudel` | 25 bars, 140 BPM, 43 s, **loops** | The Line, the houses one by one, the Crown, the tally |
| Krai: the Forge at Rest (ambient) | `music/krai_ambient.strudel` | 15 bars, 60 BPM, 60 s, **loops** | The Line at rest, the houses hummed, the Crown far off |
| Ssarith: the Bows (battle) | `music/ssarith_battle.strudel` | 20 bars, 116 BPM, 41 s, **loops** | The stalk, the call, the three bows, the strike |
| Ssarith: the Long Watch (ambient) | `music/ssarith_ambient.strudel` | 14 bars, 56 BPM, 60 s, **loops** | A held fifth, the stalk, a far call |
| Kelvane: the Measurement (battle) | `music/kelvane_battle.strudel` | 20 bars, 126 BPM, 38 s, **loops** | The Balmer ostinato, the baseline, the calibration, each twice |
| Kelvane: the Long Baseline (ambient) | `music/kelvane_ambient.strudel` | 15 bars, 60 BPM, 60 s, **loops** | The Balmer motif one note a bar, the calibration, a tick |
| Main Title | `music/main_theme.strudel` | 22 bars, 96 BPM, 55 s | The wonder chord opening (bed, harp, the idea on glockenspiel), the Concord on horns, the Concord in full on trumpets and strings, the wonder chord held |
| Battle Stations | `music/battle.strudel` | 24 bars, 132 BPM, 43.6 s, **loops** | The Threat (8 bars: the ostinato, then the call over it), the Concord's answer on trumpets over the ostinato's rhythm (8), the two against each other (8) |
| Victory | `music/victory.strudel` | 5 bars, 96 BPM, 12.5 s | The Concord's last four bars in full, the wonder chord held |
| Ship Lost | `music/defeat.strudel` | 5 bars, 66 BPM, 18 s | The Lost on one horn, the last chord held |
| The motifs | `music/motifs.strudel` | 43 bars, 96 BPM, 108 s | Every motif plainly, in turn, the factions' included |

A looping cue says `// @loop` in its header: `tools/music/render.sh` cuts it at its scored length
and folds the release and reverb tail onto its start (`tools/music/fold_loop.py`), so it loops
with no gap.

## Writing a melody

The rules every melody here follows, from Crab Breeze's `docs/design/music.md` (the owner on its
first hook: "Look into music theory and how to compose good melodies"), with its sources:

- **Build from one short idea**: a 1 to 2 bar rhythm and contour, repeated and developed, not a new
  shape in every bar ([Point Blank](https://www.pointblankmusicschool.com/blog/creating-melodic-hooks-that-stick/)).
- **An 8-bar theme is a sentence**: the idea (2 bars), the idea again varied or moved (2 bars), then
  a continuation that shortens it and drives to a cadence (4 bars)
  ([Fundamentals, Function, and Form](https://milnepublishing.geneseo.edu/fundamentals-function-form/chapter/35-sentences-and-periods/)).
- **Rhythm first**: fix one rhythmic figure and keep it.
- **Change one thing at a time**: the pitch level, the ending note or the rhythm of the last beat.
- **One high point**, about two thirds to three quarters of the way through, and the line falls
  away from it.
- **Mostly steps, few leaps**; after a leap, move by step the other way. Keep the range near an
  octave and a half.
- **Strong beats on chord tones**; tension notes on weak beats or leaning onto a chord tone.
- **Hum it.** If it does not work hummed, it will not work on a horn.

## Orchestration and the mix

- **The melody is on top.** The calibrated levels (`music/instruments.json`) put every instrument
  at the same loudness; each cue lifts its lead with `@mix` (horn `vol=100`, trumpet `vol=165`)
  and trims the bed, basses and tuba. Check it on a spectrogram with a narrow range, where the
  melody's contour must be the brightest line above the chords:
  `ffmpeg -i output/music/<cue>.mp3 -lavfi "showspectrumpic=s=1200x300:fscale=log:start=80:stop=2500:scale=log:drange=40" spec.png`.
  The latest is `docs/screenshots/audio/music-spectrograms.jpg`.
- **Accompaniment gains 0.3 to 0.6, leads 1.0 to 1.1.**
- **The bed is under the orchestra, never in front**: pad gain about 0.25, the swell about 0.12.
- **The orchestra's reverb is the soundfont's** (`@reverb 0.4` to `0.55`), with a ReverbSC on the
  leads and the pad (`@space`).

## Open

- Each faction's reading of its culture (survey S5-S7), battle layers (S3).
- Not heard by the owner yet: every render here is checked by its profile and spectrogram, which is
  not approval.
