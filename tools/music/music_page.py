#!/usr/bin/env python3
"""Writes the Fleet Command Music page (docs/mockups/music.html) from the scores and their renders.

One row per score in music/ that tools/music/render.sh has rendered (output/music/<cue>.mp3, its share copy): its
title and its description, both read from the score's own header comment (so the page cannot drift
from the score), its length, a waveform strip and a play button. The page's look and player are
tools/audio/audio_page.py, shared with the Sounds page. Which cues loop is CUES below.

Usage: python3 tools/music/music_page.py   (after tools/music/render.sh on each score)
"""
import os
import re
import sys

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.join(REPO, "tools", "audio"))
import audio_page  # noqa: E402  (the page's one look and player)

# the page's order, each cue's group, and whether the game loops it
CUES = [("main_theme", "Cues", False), ("battle", "Cues", True), ("victory", "Stingers", False),
        ("defeat", "Stingers", False), ("motifs", "Motifs", False)]
GROUPS = {"Cues": "Cues", "Stingers": "Stingers", "Motifs": "The motif sheet"}

BODY = """<div class="wrap">
  <header class="mast">
    <div class="stripe" aria-hidden="true"></div>
    <a class="master" href="https://claude.ai/artifact/8VqA9YjhCrgoiY2CGR1Qxu" target="_blank" rel="noopener">All Fleet Command mockups (the tracker)</a>
    <span class="eyebrow">Fleet Command music · score sheet 1 · 2026-11-04</span>
    <h1>Fleet Command Music</h1>
    <p>The first score: orchestral space opera, brass, strings and timpani over a synth bed (survey S1). Every cue is built from four motifs written down first as notes: the Concord (the player's fleet, an eight-bar theme in E flat major that lifts to F major at its middle), the wonder chord (E flat with the raised fourth), the Threat (a low ostinato and a horn call in C minor, whoever the fleet is fighting) and the Lost (the Concord's idea in E flat minor). __COUNT__ renders, __MIN__ minutes in all.</p>
    <div class="facts"><span>Cues <b>__COUNT__</b></span><span>Instruments <b>GeneralUser GS soundfont, LMMS synths</b></span><span>Loudness <b>-16 LUFS</b></span><span>Format <b>stereo OGG</b></span></div>
    <p class="status">Each cue is a Strudel score in music/, exported to multitrack MIDI and an LMMS project and rendered headless. Checked by its loudness profile and spectrogram, not yet heard by the owner. Not in the game yet: playing them is the music plan (openspec/changes/music), after the owner's approval.</p>
  </header>
  <div id="groups" style="display:grid;gap:44px"></div>
  <footer>
    <span>Scores: music/*.strudel · Motifs: docs/design/music.md and the MOTIF LIBRARY in music/motifs.strudel · Render: tools/music/render.sh · Game copies: assets/audio/music/*.ogg · Plan: openspec/changes/music</span>
    <span>Method: the music-authoring skill</span>
  </footer>
</div>
"""


def header(path: str) -> tuple:
    """The score's @title, its bars and tempo, and its description (the plain comment lines of its
    header, after the // @ lines)."""
    text = open(path).read()
    title = re.search(r"^// @title (.+)$", text, re.M).group(1)
    cycles = int(re.search(r"^// @cycles (\d+)", text, re.M).group(1))
    bpm = float(re.search(r"setcps\((\d+(?:\.\d+)?)/60/4\)", text).group(1))
    lines = []
    for line in text.split("\n"):
        if line.startswith("setcps"):
            break
        if line.startswith("// ") and not line.startswith("// @"):
            lines.append(line[3:].strip())
    return title, cycles * 240 / bpm, " ".join(lines)


def main() -> int:
    items = []
    for name, group, loop in CUES:
        src = f"output/music/{name}.mp3"
        if not os.path.exists(os.path.join(REPO, src)):
            print(f"{name}: not rendered (tools/music/render.sh music/{name}.strudel)", file=sys.stderr)
            return 1
        title, seconds, what = header(os.path.join(REPO, "music", name + ".strudel"))
        items.append({"name": title.replace("Fleet Command: ", ""), "group": group, "what": what,
                      "seconds": round(seconds, 1), "loop": loop, "peaks": audio_page.peaks(os.path.join(REPO, src)),
                      "src": src})
    minutes = sum(i["seconds"] for i in items) / 60
    body = BODY.replace("__COUNT__", str(len(items))).replace("__MIN__", f"{minutes:.1f}")
    out = os.path.join(REPO, "docs", "mockups", "music.html")
    audio_page.write(out, "Fleet Command Music", body, GROUPS, items)
    print(f"wrote {out}: {len(items)} cues")
    return 0


if __name__ == "__main__":
    sys.exit(main())
