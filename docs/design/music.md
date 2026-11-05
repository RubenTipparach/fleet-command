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
- **No faction themes yet.** The Threat stands for whoever the fleet is fighting. A people's music
  is part of its culture, which is the owner's to set (CLAUDE.md section 1); survey S2 asks.
- **Tempi**: 96 BPM for the title and the victory, 132 for battle, 66 for a loss. One cycle of
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

## The cues

| Cue | Score | Length | What it is |
| --- | --- | --- | --- |
| Main Title | `music/main_theme.strudel` | 22 bars, 96 BPM, 55 s | The wonder chord opening (bed, harp, the idea on glockenspiel), the Concord on horns, the Concord in full on trumpets and strings, the wonder chord held |
| Battle Stations | `music/battle.strudel` | 24 bars, 132 BPM, 43.6 s, **loops** | The Threat (8 bars: the ostinato, then the call over it), the Concord's answer on trumpets over the ostinato's rhythm (8), the two against each other (8) |
| Victory | `music/victory.strudel` | 5 bars, 96 BPM, 12.5 s | The Concord's last four bars in full, the wonder chord held |
| Ship Lost | `music/defeat.strudel` | 5 bars, 66 BPM, 18 s | The Lost on one horn, the last chord held |
| The motifs | `music/motifs.strudel` | 26 bars, 96 BPM, 65 s | Every motif plainly, in turn |

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

- Faction themes (survey S2), battle layers (S3).
- Not heard by the owner yet: every render here is checked by its profile and spectrogram, which is
  not approval.
