#!/usr/bin/env python3
"""Fleet Command's synthesised sounds: the one place each synthesised cue and layer is made.

Ported from Crab Breeze (tools/audio/gen_sfx.py at 0e569e3): its building blocks (oscillators,
noise, filters, envelopes, the small reverb, the WAV writer) unchanged, the sounds below written
new for a starship battle. Deterministic (each cue seeds its own generator from its name), mono
44.1 kHz (the game places battle sounds in 3D at their ship or mount), peak-normalised (the cue's
peak_db, its volume in the game's data sets its level).

Two kinds of entry, one registry:
- a cue (group Weapons, Impacts, Ship, Console, Alerts): written to the page and the game;
- a layer (group "_layer"): never written alone, only mixed into a recorded cue by
  tools/audio/cut_cc0.py ({"synth": "<name>"} in tools/audio/cc0_cues.json), where a recording
  lacks the body or the shimmer a starship weapon needs.

A recorded CC0 cut in tools/audio/cc0_cues.json replaces a synthesised cue of the same name.

Usage: python3 tools/audio/gen_sfx.py [--out DIR] [--only NAME ...]
Writes DIR/<cue>.wav and DIR/sfx.json (default output/sfx); tools/audio/render_sfx.sh encodes them.
"""
import argparse
import json
import os
import sys
import wave
import zlib

import numpy as np

SR = 44100


# ---------------------------------------------------------------- building blocks

def rng_for(name: str) -> np.random.Generator:
    """A generator seeded from the cue's name, so every run makes the same sound."""
    return np.random.default_rng(zlib.crc32(name.encode()))


def t_axis(seconds: float) -> np.ndarray:
    return np.arange(int(seconds * SR)) / SR


def phase(freq_hz) -> np.ndarray:
    """Running phase (radians) for a frequency curve, so sweeps stay continuous."""
    return 2 * np.pi * np.cumsum(np.asarray(freq_hz, dtype=np.float64)) / SR


def sine(freq_hz) -> np.ndarray:
    return np.sin(phase(freq_hz))


def saw(freq_hz) -> np.ndarray:
    p = phase(freq_hz) / (2 * np.pi)
    return 2 * (p - np.floor(p + 0.5))


def square(freq_hz, duty: float = 0.5) -> np.ndarray:
    p = (phase(freq_hz) / (2 * np.pi)) % 1.0
    return np.where(p < duty, 1.0, -1.0)


def noise(rng: np.random.Generator, n: int) -> np.ndarray:
    return rng.uniform(-1, 1, n)


def sweep(a: float, b: float, n: int, curve: float = 1.0) -> np.ndarray:
    """From a to b over n samples; curve > 1 lingers near a, < 1 rushes to b. Exponential in pitch."""
    x = np.linspace(0, 1, n) ** curve
    if a > 0 and b > 0:
        return a * (b / a) ** x
    return a + (b - a) * x


def exp_decay(n: int, seconds: float) -> np.ndarray:
    """An envelope falling by 60 dB over `seconds`."""
    return np.exp(-6.9 * np.arange(n) / (seconds * SR))


def adsr(n: int, a: float, d: float, s: float, r: float) -> np.ndarray:
    a_n, d_n, r_n = int(a * SR), int(d * SR), int(r * SR)
    s_n = max(0, n - a_n - d_n - r_n)
    env = np.concatenate([
        np.linspace(0, 1, a_n, endpoint=False),
        np.linspace(1, s, d_n, endpoint=False),
        np.full(s_n, s),
        np.linspace(s, 0, r_n),
    ])
    return np.pad(env, (0, max(0, n - len(env))))[:n]


def onepole_lp(x: np.ndarray, cutoff_hz) -> np.ndarray:
    """One-pole lowpass with a fixed or per-sample cutoff."""
    c = np.broadcast_to(np.asarray(cutoff_hz, dtype=np.float64), x.shape)
    a = 1 - np.exp(-2 * np.pi * c / SR)
    y = np.empty_like(x)
    z = 0.0
    for i in range(len(x)):
        z += a[i] * (x[i] - z)
        y[i] = z
    return y


def onepole_hp(x: np.ndarray, cutoff_hz) -> np.ndarray:
    return x - onepole_lp(x, cutoff_hz)


def bandpass(x: np.ndarray, centre_hz, q: float) -> np.ndarray:
    """A state-variable bandpass with a fixed or swept centre (Chamberlin form, stable below SR/6)."""
    f = np.broadcast_to(np.asarray(centre_hz, dtype=np.float64), x.shape)
    k = 2 * np.sin(np.pi * np.minimum(f, SR / 6.5) / SR)
    damp = 1.0 / q
    low = band = 0.0
    y = np.empty_like(x)
    for i in range(len(x)):
        low += k[i] * band
        high = x[i] - low - damp * band
        band += k[i] * high
        y[i] = band
    return y


def shape(x: np.ndarray, drive: float) -> np.ndarray:
    """Soft saturation."""
    return np.tanh(x * drive) / np.tanh(drive)


def _svf(x: np.ndarray, cutoff_hz, q: float, mode: str) -> np.ndarray:
    """One zero-delay-feedback state-variable filter stage (12 dB/octave), stable while its
    cutoff sweeps per sample (Zavalishin's topology-preserving form)."""
    c = np.broadcast_to(np.asarray(cutoff_hz, dtype=np.float64), x.shape)
    g = np.tan(np.pi * np.clip(c, 5.0, SR * 0.45) / SR)
    k = 1.0 / q
    a1 = 1.0 / (1.0 + g * (g + k))
    a2 = g * a1
    a3 = g * a2
    ic1 = ic2 = 0.0
    y = np.empty_like(x)
    low = mode == "lp"
    for i in range(len(x)):
        v3 = x[i] - ic2
        v1 = a1[i] * ic1 + a2[i] * v3
        v2 = ic2 + a2[i] * ic1 + a3[i] * v3
        ic1 = 2 * v1 - ic1
        ic2 = 2 * v2 - ic2
        y[i] = v2 if low else x[i] - k * v1 - v2
    return y


def lp(x: np.ndarray, cutoff_hz, order: int = 4, q: float = 0.707) -> np.ndarray:
    """A steep lowpass: `order` / 2 SVF stages (order 4 is 24 dB/octave). Combustion, blasts and
    rumble live under it: the onepole_lp above is too gentle and leaves them hissing."""
    for _ in range(order // 2):
        x = _svf(x, cutoff_hz, q, "lp")
    return x


def hp(x: np.ndarray, cutoff_hz, order: int = 2) -> np.ndarray:
    """A steep highpass (order 2 is 12 dB/octave); used at 20-30 Hz to keep sub energy audible."""
    for _ in range(order // 2):
        x = _svf(x, cutoff_hz, 0.707, "hp")
    return x


def brown(rng: np.random.Generator, n: int) -> np.ndarray:
    """Brown (red) noise: integrated white noise with a slight leak, the colour of a roar."""
    w = rng.uniform(-1, 1, n)
    y = np.empty(n)
    z = 0.0
    for i in range(n):
        z = 0.998 * z + w[i] * 0.05
        y[i] = z
    return y / (np.max(np.abs(y)) or 1.0)


def pink(rng: np.random.Generator, n: int) -> np.ndarray:
    """Pink noise (equal energy per octave, Paul Kellet's filter on white noise): the colour of soft
    air, gentler on the ear than white noise's hiss or brown noise's roar."""
    w = rng.uniform(-1, 1, n)
    b = np.zeros(7)
    out = np.empty(n)
    for i in range(n):
        x = w[i]
        b[0] = 0.99886 * b[0] + x * 0.0555179
        b[1] = 0.99332 * b[1] + x * 0.0750759
        b[2] = 0.96900 * b[2] + x * 0.1538520
        b[3] = 0.86650 * b[3] + x * 0.3104856
        b[4] = 0.55000 * b[4] + x * 0.5329522
        b[5] = -0.7616 * b[5] - x * 0.0168980
        out[i] = b[0] + b[1] + b[2] + b[3] + b[4] + b[5] + b[6] + x * 0.5362
        b[6] = x * 0.115926
    return out / (np.max(np.abs(out)) or 1.0)


def turbulence(rng: np.random.Generator, n: int, rate_hz: float = 7.0, depth: float = 0.45) -> np.ndarray:
    """A burning flame's unsteady swell: slow random amplitude between 1 - depth and 1 + depth."""
    m = lp(rng.uniform(-1, 1, n), rate_hz, order=2)
    m = m / (np.max(np.abs(m)) or 1.0)
    return 1.0 + depth * m


def crackle(rng: np.random.Generator, n: int, density: float = 0.004, cutoff_hz: float = 2500.0) -> np.ndarray:
    """Sparse pops of burning fuel, low passed so they sound like fire, not like static."""
    pops = rng.uniform(-1, 1, n) * (rng.uniform(0, 1, n) < density)
    return lp(pops, cutoff_hz, order=2)


def ring(freqs, decays_s, n: int, amps=None) -> np.ndarray:
    """Inharmonic struck-metal partials."""
    amps = amps or [1.0] * len(freqs)
    t = np.arange(n) / SR
    out = np.zeros(n)
    for f, d, a in zip(freqs, decays_s, amps):
        out += a * np.sin(2 * np.pi * f * t) * np.exp(-6.9 * t / d)
    return out


def reverb(x: np.ndarray, room: float = 0.5, wet: float = 0.3, tail_s: float = 1.5) -> np.ndarray:
    """A small Schroeder reverb (four combs, two allpasses), with a tail appended."""
    x = np.concatenate([x, np.zeros(int(tail_s * SR))])
    out = np.zeros_like(x)
    for delay_ms, g in ((29.7, 0.805), (37.1, 0.827), (41.1, 0.783), (43.7, 0.764)):
        d = int(delay_ms * SR / 1000 * (0.6 + room))
        gg = g * (0.75 + 0.25 * room)
        buf = np.zeros_like(x)
        for i in range(len(x)):
            buf[i] = x[i] + (gg * buf[i - d] if i >= d else 0.0)
        out += buf
    for delay_ms, g in ((5.0, 0.7), (1.7, 0.7)):
        d = int(delay_ms * SR / 1000)
        y = np.zeros_like(out)
        for i in range(len(out)):
            xd = out[i - d] if i >= d else 0.0
            yd = y[i - d] if i >= d else 0.0
            y[i] = -g * out[i] + xd + g * yd
        out = y
    return x * (1 - wet) + out * wet * 0.25


def fit(x: np.ndarray, n: int) -> np.ndarray:
    return np.pad(x, (0, max(0, n - len(x))))[:n]


def mix(*parts: np.ndarray) -> np.ndarray:
    n = max(len(p) for p in parts)
    return sum(fit(p, n) for p in parts)


def loopable(x: np.ndarray, fade_s: float = 0.15) -> np.ndarray:
    """Crossfades the end into the start so the sound loops with no click."""
    f = int(fade_s * SR)
    head, body, tail = x[:f], x[f:-f], x[-f:]
    w = np.linspace(0, 1, f)
    return np.concatenate([tail * (1 - w) + head * w, body])


def at(x: np.ndarray, seconds: float, n: int) -> np.ndarray:
    """x placed `seconds` into a buffer of n samples."""
    s = int(seconds * SR)
    out = np.zeros(n)
    if s >= n:
        return out
    end = min(n, s + len(x))
    out[s:end] = x[: end - s]
    return out


# ---------------------------------------------------------------- the sounds

SOUNDS = {}


def sound(name, group, what, peak_db=-1.0):
    """Register a cue or a layer. peak_db is the level it is written at: -1 dBFS for weapons and
    blasts, lower for console tones that should sit under the fight rather than over it."""
    def deco(fn):
        SOUNDS[name] = (group, what, fn, peak_db)
        return fn
    return deco


def is_layer(name: str) -> bool:
    """A layer is mixed into recorded cues and never written as a cue of its own."""
    return SOUNDS[name][0].startswith("_")


def chorus(f_hz, n: int, cents=(0.0, -9.0, 7.0), fn=None) -> np.ndarray:
    """Detuned copies of one oscillator, summed: the width of an energy weapon's tone."""
    fn = fn or saw
    f = np.broadcast_to(np.asarray(f_hz, dtype=np.float64), (n,))
    return sum(fn(f * 2 ** (c / 1200)) for c in cents) / len(cents)


def tone(freq_hz: float, seconds: float, a=0.004, d=0.03, s=0.6, r=0.05, fn=None) -> np.ndarray:
    """A short enveloped console tone, sine by default. The release is shortened to fit inside
    the note, so the tone always fades to zero and never ends on a click."""
    n = int(seconds * SR)
    r = max(0.005, min(r, seconds - a - d))
    return (fn or sine)(np.full(n, float(freq_hz))) * adsr(n, a, d, s, r)


# --- layers (mixed into recorded cues, never written alone)

@sound("beam_body", "_layer", "The Lance's held tone: a detuned saw buzz that opens and closes.")
def beam_body(r):
    n = int(1.3 * SR)
    f = sweep(140, 180, n, 0.4)
    body = chorus(f, n) + 0.5 * chorus(f * 2.01, n, fn=square)
    wob = 1 + 0.15 * sine(np.full(n, 31.0))
    env = adsr(n, 0.02, 0.1, 0.8, 0.35)
    return lp(body * wob, sweep(5000, 1600, n), order=2) * env


@sound("energy_bloom", "_layer", "A torpedo's energy release: a falling sine and a noise flare.")
def energy_bloom(r):
    n = int(1.6 * SR)
    drop = sine(sweep(420, 45, n, 0.5)) * exp_decay(n, 1.4)
    flare = bandpass(noise(r, n), sweep(6000, 400, n, 0.6), 2.0) * exp_decay(n, 0.6)
    return mix(drop, 0.6 * flare)


@sound("shield_shimmer", "_layer", "A shield taking a hit: inharmonic glassy partials with a slow wobble.")
def shield_shimmer(r):
    n = int(1.4 * SR)
    partials = ring([620, 931, 1384, 2077, 2950], [1.0, 0.8, 0.6, 0.45, 0.3], n)
    wob = 1 + 0.3 * sine(np.full(n, 11.0))
    return partials * wob * adsr(n, 0.003, 0.2, 0.6, 0.9)


@sound("sub_thump", "_layer", "A deep sub thump under launches and hits.")
def sub_thump(r):
    n = int(0.8 * SR)
    return sine(sweep(90, 32, n, 0.5)) * exp_decay(n, 0.6)


@sound("punch_thump", "_layer", "A weapon's kick: a sine dropping from 150 to 45 Hz in a tenth of a second, the punch under a shot.")
def punch_thump(r):
    n = int(0.22 * SR)
    body = sine(sweep(150, 45, n, 0.35)) * exp_decay(n, 0.16)
    click = onepole_hp(noise(r, n), 3000) * exp_decay(n, 0.004)
    return mix(body, 0.25 * click)


@sound("beam_snap", "_layer", "The Lance's snap: a detuned saw buzz falling from 330 to 130 Hz and gone in a third of a second.")
def beam_snap(r):
    n = int(0.36 * SR)
    f = sweep(330, 130, n, 0.5)
    body = chorus(f, n) + 0.4 * chorus(f * 2.01, n, fn=square)
    return lp(body, sweep(7000, 1200, n, 0.6), order=2) * adsr(n, 0.002, 0.05, 0.55, 0.22)


@sound("room_air", "_layer", "The bridge's room tone: soft pink air, nothing under 200 Hz, an open top to 6 kHz, drifting a decibel either way over many seconds.")
def room_air(r):
    n = int(60.0 * SR)
    air = pink(r, n)
    air = hp(air, 200.0, order=4)
    air = lp(air, 6000.0, order=2)
    drift = 1.0 + 0.11 * np.interp(np.arange(n), np.linspace(0, n, 9), r.uniform(-1, 1, 9))
    return air * drift


# the same room tone from a second seed, so the left and right of the bed are uncorrelated and the room sounds wide
sound("room_air_r", "_layer", "The room tone again from its own seed, for the other side of the stereo bed.")(room_air)


@sound("console_bleep", "_layer", "A console working: two or three quick bleeps, the pitches seeded.")
def console_bleep(r):
    n = int(0.45 * SR)
    notes = r.choice([1047, 1319, 1568, 1760, 2093, 2349, 2637], size=int(r.integers(2, 4)))
    return mix(*[at(tone(f, 0.06, s=0.6, r=0.03, fn=lambda f: sine(f) + 0.15 * square(f)), i * 0.085, n) for i, f in enumerate(notes)])


@sound("breach_whine", "_layer", "A reactor running away: a whine climbing for two seconds.")
def breach_whine(r):
    n = int(2.4 * SR)
    f = sweep(220, 1800, n, 1.6)
    w = chorus(f, n, fn=sine) + 0.3 * chorus(f * 1.5, n, fn=sine)
    trem = 1 + 0.4 * sine(sweep(4, 22, n))
    return w * trem * adsr(n, 0.4, 0.1, 1.0, 0.02)


# --- console (unplaced, the bridge itself)

@sound("ui_chirp", "Console", "A Concord console key: a soft two-note chirp (survey U6, soft chirps).", peak_db=-9.0)
def ui_chirp(r):
    n = int(0.16 * SR)
    a = tone(1760, 0.05, s=0.5, r=0.03)
    b = tone(2349, 0.07, s=0.5, r=0.05)
    return mix(at(a, 0.0, n), at(b, 0.045, n)) * 0.8


@sound("ui_confirm", "Console", "An order taken: three rising notes.", peak_db=-8.0)
def ui_confirm(r):
    n = int(0.34 * SR)
    return mix(*[at(tone(f, 0.09, s=0.5, r=0.06), i * 0.07, n) for i, f in enumerate((1175, 1568, 2093))])


@sound("ui_deny", "Console", "An order the ship cannot carry out: two low buzzes.", peak_db=-8.0)
def ui_deny(r):
    n = int(0.36 * SR)
    buzz = lp(square(np.full(int(0.12 * SR), 196.0)), 1800, order=2) * adsr(int(0.12 * SR), 0.004, 0.02, 0.7, 0.03)
    return mix(at(buzz, 0.0, n), at(buzz, 0.17, n)) * 0.7


@sound("weapon_charged", "Console", "A weapon charged and ready: a bright ping with a short tail.", peak_db=-8.0)
def weapon_charged(r):
    n = int(0.6 * SR)
    ping = ring([2637, 3951, 5274], [0.35, 0.25, 0.15], n) * adsr(n, 0.002, 0.05, 0.6, 0.3)
    return reverb(ping, room=0.5, wet=0.25, tail_s=0.3)


@sound("target_lock", "Console", "A target locked: a quick rising sweep and two pips.", peak_db=-8.0)
def target_lock(r):
    n = int(0.5 * SR)
    m = int(0.14 * SR)
    swp = sine(sweep(700, 1600, m)) * adsr(m, 0.005, 0.02, 0.7, 0.03)
    pip = tone(1760, 0.06, s=0.6, r=0.03, fn=lambda f: 0.7 * square(f) + 0.3 * sine(f))
    return mix(swp, at(lp(pip, 4000, order=2), 0.2, n), at(lp(pip, 4000, order=2), 0.32, n)) * 0.8


@sound("hail", "Console", "A hail coming in: an ascending four-note figure, an open channel.", peak_db=-8.0)
def hail(r):
    n = int(0.9 * SR)
    notes = (880, 1109, 1319, 1760)
    out = mix(*[at(tone(f, 0.16, s=0.5, r=0.12, fn=lambda f: sine(f) + 0.25 * sine(f * 3)), i * 0.13, n) for i, f in enumerate(notes)])
    return reverb(out, room=0.55, wet=0.3, tail_s=0.4)


@sound("pause_on", "Console", "Time stopped for orders: a falling tape-stop swoop (survey G2).", peak_db=-6.0)
def pause_on(r):
    n = int(0.55 * SR)
    f = sweep(660, 55, n, 0.6)
    s = chorus(f, n, fn=sine) + 0.3 * chorus(f * 2, n, fn=triangle_like)
    return lp(s, sweep(5000, 500, n), order=2) * adsr(n, 0.005, 0.1, 0.8, 0.2)


@sound("pause_off", "Console", "Time running again: the swoop reversed, rising.", peak_db=-6.0)
def pause_off(r):
    return pause_on(r)[::-1] * adsr(int(0.55 * SR), 0.15, 0.1, 0.9, 0.01)


def triangle_like(f_hz) -> np.ndarray:
    """A triangle wave: the saw folded, softer than a square."""
    return 2 * np.abs(saw(f_hz)) - 1


# --- alerts (unplaced)

@sound("shields_failing", "Alerts", "A shield facing about to fail: a pulsing two-tone warble.", peak_db=-6.0)
def shields_failing(r):
    n = int(1.2 * SR)
    f = onepole_lp(np.where(square(np.full(n, 6.0)) > 0, 988.0, 740.0), 60.0)
    return lp(chorus(f, n, fn=triangle_like), 3000, order=2) * adsr(n, 0.01, 0.05, 0.9, 0.1)


@sound("hull_critical", "Alerts", "Hull integrity critical: three low, urgent pulses.", peak_db=-5.0)
def hull_critical(r):
    n = int(1.3 * SR)
    b = int(0.24 * SR)
    pulse = lp(chorus(np.full(b, 233.0), b) + 0.5 * sine(np.full(b, 116.5)), 1500, order=2) * adsr(b, 0.005, 0.04, 0.8, 0.06)
    return mix(*[at(pulse, i * 0.34, n) for i in range(3)])


@sound("core_breach", "Alerts", "The reactor breaching: a climbing whine, then the blast.", peak_db=-1.0)
def core_breach(r):
    n = int(4.2 * SR)
    whine = breach_whine(r)
    blast = lp(brown(r, int(2.2 * SR)), sweep(3000, 150, int(2.2 * SR)), order=2) * exp_decay(int(2.2 * SR), 2.0)
    return mix(at(whine, 0.0, n) * 0.6, at(blast, 2.35, n), at(sub_thump(r), 2.35, n))


# ---------------------------------------------------------------- output

def write_wav(path: str, x: np.ndarray, peak_db: float = -1.0, loop: bool = False) -> None:
    """Peak-normalised 16-bit WAV, mono, or stereo when x is (2, n). A one-shot gets a 5 ms fade
    out; a loop must not, or the fade clicks at the seam on every repeat."""
    peak = float(np.max(np.abs(x))) or 1.0
    y = np.clip(x / peak * 10 ** (peak_db / 20), -1, 1)
    if not loop:
        fade = min(y.shape[-1], int(0.005 * SR))
        y[..., -fade:] *= np.linspace(1, 0, fade)
    channels = 1 if y.ndim == 1 else y.shape[0]
    if channels == 2:
        y = y.T.reshape(-1)   # interleave left and right
    with wave.open(path, "wb") as w:
        w.setnchannels(channels)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((y * 32767).astype("<i2").tobytes())


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--out", default="output/sfx")
    ap.add_argument("--only", nargs="*")
    args = ap.parse_args()
    os.makedirs(args.out, exist_ok=True)
    manifest = []
    for name, (group, what, fn, peak_db) in SOUNDS.items():
        if is_layer(name) or (args.only and name not in args.only):
            continue
        x = fn(rng_for(name))
        if not np.all(np.isfinite(x)):
            print(f"{name}: produced a non-finite sample", file=sys.stderr)
            return 1
        write_wav(os.path.join(args.out, f"{name}.wav"), x, peak_db, loop="(loops)" in what)
        # a 120-point peak outline for the page's waveform strip
        peaks = np.abs(x).reshape(-1)[: len(x) // 120 * 120].reshape(120, -1).max(axis=1)
        peaks = peaks / (peaks.max() or 1.0)
        manifest.append({"name": name, "group": group, "what": what, "seconds": round(len(x) / SR, 2),
                         "loop": "(loops)" in what, "peaks": [round(float(p), 3) for p in peaks]})
        print(f"{group:8} {name:18} {len(x) / SR:4.1f} s")
    with open(os.path.join(args.out, "sfx.json"), "w") as f:
        json.dump(manifest, f)
    return 0


if __name__ == "__main__":
    sys.exit(main())
