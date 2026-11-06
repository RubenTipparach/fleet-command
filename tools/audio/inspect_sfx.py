#!/usr/bin/env python3
"""Look at sounds without ears: the one place Fleet Command's sound effects are measured.

A cloud session cannot listen, so choosing a recording and judging a cut are done by measurement
(the sound-effects skill). Three jobs:

- inventory: every recording under a fetched pack folder with its length and spectral centroid,
  to shortlist files by name, length and brightness;
- envelope REF ...: a 0.1 s loudness strip per 'pack:file' (the cc0_cues.json form), with the share
  of energy under 250 Hz and over 4 kHz, to find a take's start, its shots and its weight;
- check: every built cue in output/sfx: no clipping, no non-finite samples, loop seams no bigger than
  a few ordinary sample steps, and a spectrogram contact sheet (output/sfx/sheet.png) to look at
  before a set is called done.

Written in this repository (2026-11-04) beside the tools ported from Crab Breeze.

Usage: python3 tools/audio/inspect_sfx.py inventory [--src DIR]
       python3 tools/audio/inspect_sfx.py envelope [--src DIR] REF ...
       python3 tools/audio/inspect_sfx.py check [--out DIR]
Needs: python3 with numpy, ffmpeg.
"""
import argparse
import glob
import json
import os
import subprocess
import sys
import wave

import numpy as np

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
AUDIO = (".wav", ".ogg", ".mp3", ".flac")
SR = 22050  # analysis rate: enough for the centroid and the bands


def read(path: str) -> np.ndarray:
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", path, "-ac", "1", "-ar", str(SR), "-f", "f32le", "-"],
                         capture_output=True, check=True).stdout
    return np.frombuffer(raw, np.float32).astype(np.float64)


def find(src: str, ref: str) -> str:
    pack, _, name = ref.partition(":")
    for root, _, files in os.walk(os.path.join(src, pack)):
        if name in files:
            return os.path.join(root, name)
    raise FileNotFoundError(ref)


def inventory(src: str) -> int:
    for site in sorted(os.listdir(src)):
        if not os.path.isdir(os.path.join(src, site)):
            continue
        for pack in sorted(os.listdir(os.path.join(src, site))):
            rows = []
            for root, _, files in os.walk(os.path.join(src, site, pack)):
                for f in sorted(files):
                    if not f.lower().endswith(AUDIO) or f.startswith("._"):
                        continue
                    x = read(os.path.join(root, f))
                    if len(x) < 100:
                        continue
                    seg = x[: SR * 4]
                    sp = np.abs(np.fft.rfft(seg))
                    fr = np.fft.rfftfreq(len(seg), 1 / SR)
                    rows.append(f"{f}({len(x) / SR:.1f}s,{(sp * fr).sum() / (sp.sum() or 1) / 1000:.1f}k)")
            print(f"## {site}/{pack} [{len(rows)}]: " + " ".join(rows))
    return 0


def envelope(src: str, refs) -> int:
    for ref in refs:
        x = read(find(src, ref))
        step = SR // 10
        rms = [np.sqrt(np.mean(x[i:i + step] ** 2)) for i in range(0, len(x), step)]
        top = max(rms) or 1.0
        strip = "".join(" .:-=+*#%@"[min(9, int(r / top * 9.99))] for r in rms)
        sp = np.abs(np.fft.rfft(x))
        fr = np.fft.rfftfreq(len(x), 1 / SR)
        lo, hi = sp[fr < 250].sum() / sp.sum(), sp[fr > 4000].sum() / sp.sum()
        print(f"{ref} {len(x) / SR:.1f}s low {lo:.2f} high {hi:.2f} |{strip}|")
    return 0


def check(out: str) -> int:
    manifest = {m["name"]: m for m in json.load(open(os.path.join(out, "sfx.json")))}
    status = 0
    for path in sorted(glob.glob(os.path.join(out, "*.wav"))):
        name = os.path.basename(path)[:-4]
        with wave.open(path) as w:
            x = np.frombuffer(w.readframes(w.getnframes()), "<i2").astype(np.float64).reshape(-1, w.getnchannels())
        clipped = int((np.abs(x) >= 32767).sum())
        problems = []
        if clipped > 4:
            problems.append(f"{clipped} clipped samples")
        if manifest.get(name, {}).get("loop"):
            seam = np.abs(x[0] - x[-1]).max()
            step = np.median(np.abs(np.diff(x, axis=0))) or 1.0
            if seam > 6 * step:
                problems.append(f"loop seam {seam:.0f} against a typical step of {step:.0f}")
        print(f"{name:20} {len(x) / 44100:4.1f} s  {'ok' if not problems else '; '.join(problems)}")
        status |= bool(problems)
    cells = os.path.join(out, "sheet")
    os.makedirs(cells, exist_ok=True)
    for path in sorted(glob.glob(os.path.join(out, "*.wav"))):
        name = os.path.basename(path)[:-4]
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", path, "-lavfi",
                        "showspectrumpic=s=400x160:legend=0:scale=log:fscale=log:color=intensity,"
                        f"drawtext=text='{name}':x=4:y=4:fontcolor=white:fontsize=16",
                        os.path.join(cells, name + ".png")], check=True)
    n = len(glob.glob(os.path.join(cells, "*.png")))
    rows = -(-n // 5)
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-pattern_type", "glob", "-i", os.path.join(cells, "*.png"),
                    "-vf", f"scale=400:200,tile=5x{rows}", "-frames:v", "1", os.path.join(out, "sheet.png")], check=True)
    print(f"contact sheet: {os.path.relpath(os.path.join(out, 'sheet.png'), REPO)} ({n} cues); look at it")
    return status


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("job", choices=["inventory", "envelope", "check"])
    ap.add_argument("refs", nargs="*")
    ap.add_argument("--src", default=os.environ.get("SFX_SRC", "/opt/sfx-src"))
    ap.add_argument("--out", default=os.path.join(REPO, "output/sfx"))
    a = ap.parse_args()
    if a.job == "inventory":
        return inventory(a.src)
    if a.job == "envelope":
        return envelope(a.src, a.refs)
    return check(a.out)


if __name__ == "__main__":
    sys.exit(main())
