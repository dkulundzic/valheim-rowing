"""Renders war drum variations as WAV previews (8 beats at a 1.5 s beat) to choose a drum sound by ear.

Run with: python3 samples/make_drum_previews.py   (needs numpy)
Writes samples/drums/*.wav (not committed).
"""
import os
import wave

import numpy as np

RATE = 44100
BEAT = 1.5
BEATS = 8
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "drums")
rng = np.random.default_rng(7)


def t_axis(seconds):
    return np.arange(int(RATE * seconds)) / RATE


def lowpass(signal, amount):
    """One-pole low-pass; amount 0..1, smaller is darker."""
    out = np.empty_like(signal)
    acc = 0.0
    for i, x in enumerate(signal):
        acc += (x - acc) * amount
        out[i] = acc
    return out


def tom(start_hz, end_hz, decay, length=1.2, sweep=0.045, overtone=0.35, skin=0.9, skin_decay=0.012, drive=1.6):
    """A drum hit: a body whose pitch drops fast, an inharmonic overtone and a burst of skin noise."""
    t = t_axis(length)
    freq = end_hz + (start_hz - end_hz) * np.exp(-t / sweep)
    phase = 2 * np.pi * np.cumsum(freq) / RATE
    body = np.sin(phase) * np.exp(-t / decay)
    over = overtone * np.sin(phase * 1.58) * np.exp(-t / (decay * 0.35))
    noise = lowpass(rng.uniform(-1, 1, t.size), 0.25) * np.exp(-t / skin_decay) * skin
    attack = np.clip(t / 0.002, 0, 1)
    return np.tanh((body + over + noise) * drive * attack) * 0.85


def stick(pitch=1800.0, decay=0.018, length=0.15):
    """A wooden stick or rim click: a short bright knock."""
    t = t_axis(length)
    tone = np.sin(2 * np.pi * pitch * t) * 0.6 + np.sin(2 * np.pi * pitch * 2.3 * t) * 0.3
    noise = rng.uniform(-1, 1, t.size) * np.exp(-t / 0.003) * 0.5
    return (tone * np.exp(-t / decay) + noise) * 0.5


def echo(signal, delay=0.19, feedback=0.32, damping=0.35, repeats=5):
    """Open-air slap echo: a few darker, quieter repeats, like the sound bouncing off water and hills."""
    out = np.copy(signal)
    tap = signal
    shift = int(delay * RATE)
    for _ in range(repeats):
        tap = lowpass(tap, damping) * feedback
        padded = np.zeros_like(out)
        padded[shift:] = tap[: out.size - shift]
        out += padded
        tap = padded
    return out


def place(track, hit, at, gain=1.0):
    start = max(0, int(at * RATE))
    end = min(track.size, start + hit.size)
    track[start:end] += hit[: end - start] * gain


def render(name, pattern):
    track = np.zeros(int(RATE * (BEAT * BEATS + 2.0)))
    pattern(track)
    peak = np.max(np.abs(track))
    track = track / peak * 0.89 if peak > 0 else track
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((track * 32767).astype("<i2").tobytes())
    print(path)


def human(time, amount=0.012):
    """A slightly human hand: hits land a few milliseconds early or late."""
    return time + rng.normal(0, amount / 2)


# A: what plays in the mod now (generated tom, accent every fourth beat).
def current(track):
    for i in range(BEATS):
        accent = i % 4 == 0
        place(track, tom(150 if accent else 175, 52 if accent else 64, 0.32 if accent else 0.24), i * BEAT, 1.0 if accent else 0.72)


# B: a big war drum, deeper and longer, with an open-air echo.
def war_drum(track):
    dry = np.zeros_like(track)
    for i in range(BEATS):
        accent = i % 4 == 0
        hit = tom(120 if accent else 135, 42 if accent else 50, 0.5 if accent else 0.38, length=1.6, overtone=0.25, drive=1.9)
        place(dry, hit, human(i * BEAT), (1.0 if accent else 0.75) * rng.uniform(0.92, 1.0))
    track += echo(dry)


# C: the tom with quiet stick ghost-notes on the off-beats ("BOOM . tak . boom . tak").
def ghost_notes(track):
    for i in range(BEATS):
        accent = i % 4 == 0
        place(track, tom(150 if accent else 175, 52 if accent else 64, 0.32 if accent else 0.24), human(i * BEAT), 1.0 if accent else 0.72)
        place(track, stick(rng.uniform(1650, 1950)), human(i * BEAT + BEAT / 2), rng.uniform(0.18, 0.28))


# D: two drums answering each other: a low drum on 1 and 3, a higher one on 2 and 4.
def call_and_response(track):
    for i in range(BEATS):
        low = i % 2 == 0
        hit = tom(130, 46, 0.45, length=1.5) if low else tom(210, 88, 0.22)
        place(track, hit, human(i * BEAT), (1.0 if i % 4 == 0 else 0.85) if low else 0.7)


# E: a frame drum (hide stretched on a hoop): mid pitch, lots of skin, a flam on the accent.
def frame_drum(track):
    for i in range(BEATS):
        accent = i % 4 == 0
        hit = tom(230, 110, 0.18, skin=1.6, skin_decay=0.03, overtone=0.5, drive=1.3)
        if accent:
            place(track, hit, human(i * BEAT) - 0.035, 0.45)  # the flam: a quick lighter hit just before
        place(track, hit, human(i * BEAT), 1.0 if accent else 0.7)


# F: B + C together, with a two-beat pickup roll before every fourth beat: the full "crew war drum".
def full_war_drum(track):
    dry = np.zeros_like(track)
    for i in range(BEATS):
        accent = i % 4 == 0
        hit = tom(120 if accent else 135, 42 if accent else 50, 0.5 if accent else 0.38, length=1.6, overtone=0.25, drive=1.9)
        place(dry, hit, human(i * BEAT), (1.0 if accent else 0.75) * rng.uniform(0.92, 1.0))
        place(dry, stick(rng.uniform(1600, 1900)), human(i * BEAT + BEAT / 2), rng.uniform(0.15, 0.25))
        if i % 4 == 3:
            # A quick pickup into the next accent: two light hits in the last third of the beat.
            for k, gain in ((0.66, 0.3), (0.83, 0.42)):
                place(dry, tom(160, 70, 0.12), human(i * BEAT + BEAT * k), gain)
    track += echo(dry, feedback=0.28)


if __name__ == "__main__":
    render("drum_A_current", current)
    render("drum_B_war_drum_echo", war_drum)
    render("drum_C_ghost_notes", ghost_notes)
    render("drum_D_call_and_response", call_and_response)
    render("drum_E_frame_drum", frame_drum)
    render("drum_F_full_war_drum", full_war_drum)
