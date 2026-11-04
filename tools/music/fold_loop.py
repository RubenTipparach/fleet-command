#!/usr/bin/env python3
"""Make a looping cue loop without a seam: the one place a render becomes a loop.

LMMS renders to the last note plus its release and reverb, so a cue scored as N bars comes out a
second or two longer. For a cue whose score says `// @loop`, this cuts the render at exactly its
length (bars x 240 / BPM, read from the score) and adds the part after the cut onto the start,
where the loop's first bar plays under the last bar's ringing tail, as it will in the game.

Written in this repository (2026-11-04) for tools/music/render.sh; Crab Breeze's render has no loops.

Usage: python3 tools/music/fold_loop.py SCORE.strudel IN.wav OUT.wav
"""
import re
import sys
import wave

import numpy as np


def main() -> int:
    score, src, dst = sys.argv[1:4]
    text = open(score).read()
    cycles = int(re.search(r"^// @cycles (\d+)", text, re.M).group(1))
    bpm = float(re.search(r"setcps\((\d+(?:\.\d+)?)/60/4\)", text).group(1))
    with wave.open(src) as w:
        ch, rate, width = w.getnchannels(), w.getframerate(), w.getsampwidth()
        if width != 2:
            print(f"{src}: expected 16-bit samples, got {8 * width}-bit", file=sys.stderr)
            return 1
        x = np.frombuffer(w.readframes(w.getnframes()), "<i2").astype(np.float64).reshape(-1, ch)
    n = int(round(cycles * 240 / bpm * rate))
    if len(x) < n:
        print(f"{src}: {len(x) / rate:.2f} s is shorter than the score's {n / rate:.2f} s", file=sys.stderr)
        return 1
    y = x[:n].copy()
    tail = x[n:]
    y[: len(tail)] += tail
    with wave.open(dst, "wb") as w:
        w.setnchannels(ch)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(np.clip(y, -32768, 32767).astype("<i2").tobytes())
    print(f"loop: cut at {n / rate:.2f} s, {len(tail) / rate:.2f} s of tail folded onto the start")
    return 0


if __name__ == "__main__":
    sys.exit(main())
