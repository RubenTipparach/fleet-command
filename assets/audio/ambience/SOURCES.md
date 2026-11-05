# Ambience sources

Recorded cues are cut from CC0 and public domain originals by `tools/audio/cut_cc0.py` to the spec in
`tools/audio/cc0_cues.json` (originals fetched by `tools/audio/fetch_cc0.py`, which refuses a pack
whose page does not say CC0 or public domain); the rest are synthesised by `tools/audio/gen_sfx.py`.
Rebuild all of them with `tools/audio/render_sfx.sh`. This file is written by `cut_cc0.py`: edit the
spec, not this table. CC0 asks for no credit; the people who recorded the originals are named anyway.
Candidates and what was looked at: `docs/analysis/audio-sources.md`.

| Cue | Made from | Edits |
| --- | --- | --- |
| amb_bridge | [sci-fi-ambience-sfx](https://opengameart.org/content/sci-fi-ambience-sfx) (Fun Gi Development) `SFX SCI FI Computer room ambience.mp3`, CC0; [airvent-loop](https://opengameart.org/content/airvent-loop) (Longor) `airvent-large-loop.wav`, CC0; [generator-loop](https://opengameart.org/content/generator-loop) (YCbCr) `loop_generator_1.mp3`, CC0 | three beds layered and panned, compressed, lowpassed and given a room, crossfaded into a loop |
