#!/usr/bin/env python3
"""Writes the Fleet Command Sounds page (docs/mockups/sounds.html) from output/sfx/sfx.json.

Ported from Crab Breeze (tools/audio/gen_sfx_page.py at 0e569e3). Every sound
tools/audio/render_sfx.sh makes (recorded CC0 cuts, synthesised cues and the bridge bed), grouped as
the game uses them, with the words and the link back to the mockup tracker (CLAUDE.md section 4).
The page's look and player are tools/audio/audio_page.py, shared with the Music page.

Usage: python3 tools/audio/gen_sfx_page.py   (after tools/audio/render_sfx.sh)
"""
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import audio_page  # noqa: E402  (the page's one look and player)

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

GROUPS = {"Weapons": "Weapons (placed at the mount)", "Impacts": "Impacts (placed where they land)",
          "Ship": "The ship (placed at the ship)", "Console": "Console keys (not placed)",
          "Alerts": "Alerts (not placed)", "Ambience": "The bridge (a stereo bed, not placed)"}

BODY = """<div class="wrap">
  <header class="mast">
    <div class="stripe" aria-hidden="true"></div>
    <a class="master" href="https://claude.ai/artifact/8VqA9YjhCrgoiY2CGR1Qxu" target="_blank" rel="noopener">All Fleet Command mockups (the tracker)</a>
    <span class="eyebrow">Fleet Command sound effects · works sheet 1 · 2026-11-04</span>
    <h1>Fleet Command Sounds</h1>
    <p>__COUNT__ sounds for the battle: every weapon mount firing, what the shots hit, the ship itself, the console keys and the alerts, and the bridge under it all. __REC__ are cut from real recordings (CC0 packs from OpenGameArt: digital lasers and phasers, electric charges and shield hits, real explosions, rocket motors, a ship's cannon, a computer room), slowed and layered to starship size; the console tones and alerts are synthesised. Battle sounds are mono and placed in 3D at their ship or mount; the console, the alerts and the bridge bed are not. Cues marked <em class="rec">rec</em> are recorded.</p>
    <div class="facts"><span>Cues <b>__COUNT__</b></span><span>Recorded <b>__REC__</b></span><span>Synthesised <b>__SYN__</b></span><span>Format <b>mono 44.1 kHz OGG</b></span><span>Licence <b>CC0 and ours</b></span></div>
    <p class="status">Each recorded cue names its originals in assets/audio/sfx/SOURCES.md. Not in the game yet: playing them is the sound plan (openspec/changes/sound-effects), after the owner's approval.</p>
  </header>
  <div id="groups" style="display:grid;gap:44px"></div>
  <footer>
    <span>Cuts: tools/audio/cut_cc0.py to tools/audio/cc0_cues.json · Synthesis: tools/audio/gen_sfx.py · Encode: tools/audio/render_sfx.sh · Game copies: assets/audio/sfx/*.ogg · Plan: openspec/changes/sound-effects</span>
    <span>Every pack and its licence: AUDIO_SOURCES.md · Method: the sound-effects skill</span>
  </footer>
</div>
"""


def main() -> None:
    data = json.load(open(os.path.join(REPO, "output", "sfx", "sfx.json")))
    for s in data:
        s["src"] = audio_page.embed(os.path.join(REPO, "output", "sfx", s["name"] + ".mp3"))
    rec = sum(1 for s in data if s.get("recorded"))
    body = BODY.replace("__COUNT__", str(len(data))).replace("__REC__", str(rec)).replace("__SYN__", str(len(data) - rec))
    out = os.path.join(REPO, "docs", "mockups", "sounds.html")
    audio_page.write(out, "Fleet Command Sounds", body, GROUPS, data)
    print(f"wrote {out}: {len(data)} sounds")


if __name__ == "__main__":
    main()
