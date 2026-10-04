"""Renders richer war drum patterns as WAV previews. In every pattern the stroke beat (when to row) is the one big,
deep drum; everything between beats is lighter and higher, building toward it.

Each preview runs 12 beats across the in-game tempo range: 4 beats of 1.8 s (still), 4 of 1.5 s, 4 of 1.2 s (near
top speed), so you hear how a pattern scales as the beat speeds up.

Run with: python3 samples/make_drum_patterns.py   (needs numpy; reuses the instruments in make_drum_previews.py)
Writes samples/drums/patterns/*.wav (not committed).
"""
import os
import wave

import numpy as np

import make_drum_previews as d

RATE = d.RATE
BEATS = [1.8] * 4 + [1.5] * 4 + [1.2] * 4
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "drums", "patterns")
rng = np.random.default_rng(11)


def stroke_drum(accent):
    """The stroke: always the biggest, deepest hit."""
    return d.tom(115 if accent else 125, 40 if accent else 46, 0.55 if accent else 0.45, length=1.8, overtone=0.22, drive=2.0)


def mid_tom():
    return d.tom(220, 95, 0.16, length=0.6, overtone=0.4, drive=1.4)


def high_tom():
    return d.tom(320, 150, 0.10, length=0.4, overtone=0.45, skin=1.1, drive=1.3)


def rim():
    return d.stick(rng.uniform(1700, 2000))


def shaker(length=0.08):
    """A dry rattle (beads or a chain on wood), very short."""
    t = d.t_axis(length)
    noise = rng.uniform(-1, 1, t.size)
    noise = noise - d.lowpass(noise, 0.15)  # keep the bright part
    return noise * np.exp(-t / 0.02) * 0.4


def beat_starts():
    starts, at = [], 0.0
    for period in BEATS:
        starts.append((at, period))
        at += period
    return starts


def render(name, pattern, with_echo=True):
    total = sum(BEATS) + 2.5
    dry = np.zeros(int(RATE * total))
    for index, (start, period) in enumerate(beat_starts()):
        pattern(dry, index, start, period)
    track = d.echo(dry, feedback=0.25) if with_echo else dry
    peak = np.max(np.abs(track))
    track = track / peak * 0.89
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((track * 32767).astype("<i2").tobytes())
    print(path)


def hit(track, sound, at, gain):
    d.place(track, sound, d.human(at, 0.008), gain * rng.uniform(0.9, 1.0))


# G "Build-up": light hits getting louder and quicker toward each stroke, so the timing is easy to anticipate.
def build_up(track, i, start, period):
    hit(track, stroke_drum(i % 4 == 0), start, 1.0)
    for fraction, gain in ((0.5, 0.12), (0.75, 0.2), (0.875, 0.3)):
        hit(track, high_tom(), start + period * fraction, gain)


# H "Gallop": the beat split in three, BOOM-ta-ta, BOOM-ta-ta, with a mid tom answering every other beat.
def gallop(track, i, start, period):
    hit(track, stroke_drum(i % 4 == 0), start, 1.0)
    hit(track, rim(), start + period / 3, 0.22)
    hit(track, mid_tom() if i % 2 else rim(), start + 2 * period / 3, 0.3 if i % 2 else 0.25)


# I "Viking march": a two-beat phrase with syncopated mid toms and sticks between the strokes.
def viking_march(track, i, start, period):
    hit(track, stroke_drum(i % 4 == 0), start, 1.0)
    if i % 2 == 0:
        hit(track, rim(), start + period * 0.5, 0.2)
        hit(track, mid_tom(), start + period * 0.75, 0.28)
    else:
        hit(track, mid_tom(), start + period * 0.375, 0.26)
        hit(track, rim(), start + period * 0.625, 0.18)
        hit(track, mid_tom(), start + period * 0.875, 0.32)


# J "Taiko": the big drum on the stroke, small-drum doubles between, and a short fill into every fourth stroke.
def taiko(track, i, start, period):
    hit(track, stroke_drum(i % 4 == 0), start, 1.0)
    for fraction in (0.375, 0.5):
        hit(track, high_tom(), start + period * fraction, 0.2)
    if i % 4 == 3:
        for k in range(4):
            hit(track, mid_tom(), start + period * (0.7 + 0.075 * k), 0.18 + 0.06 * k)
    else:
        hit(track, rim(), start + period * 0.75, 0.18)


# K "Heartbeat": a quick pickup just before each stroke ("ba-BOOM"), with a dry shaker keeping the subdivisions.
def heartbeat(track, i, start, period):
    hit(track, stroke_drum(i % 4 == 0), start, 1.0)
    hit(track, mid_tom(), start - min(0.16, period * 0.12), 0.35)
    for k in range(1, 8):
        if k != 7:
            hit(track, shaker(), start + period * k / 8, 0.25 if k % 2 == 0 else 0.15)


# L "War party": G's build-up, I's syncopation and K's pickup, with a fill every fourth stroke.
def war_party(track, i, start, period):
    hit(track, stroke_drum(i % 4 == 0), start, 1.0)
    hit(track, mid_tom(), start - min(0.15, period * 0.1), 0.3)
    if i % 4 == 3:
        for k in range(5):
            hit(track, high_tom() if k % 2 else mid_tom(), start + period * (0.55 + 0.08 * k), 0.15 + 0.05 * k)
    elif i % 2 == 0:
        hit(track, rim(), start + period * 0.5, 0.2)
        hit(track, high_tom(), start + period * 0.75, 0.24)
    else:
        hit(track, high_tom(), start + period * 0.375, 0.2)
        hit(track, rim(), start + period * 0.625, 0.18)
    for k in (1, 3, 5):
        hit(track, shaker(), start + period * k / 8, 0.12)


if __name__ == "__main__":
    render("drum_G_build_up", build_up)
    render("drum_H_gallop", gallop)
    render("drum_I_viking_march", viking_march)
    render("drum_J_taiko", taiko)
    render("drum_K_heartbeat", heartbeat)
    render("drum_L_war_party", war_party)
