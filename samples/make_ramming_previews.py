"""Renders previews of the war drum's ramming-speed rhythms, made from the drum kit the game ships
(package/sounds/drum_*.wav, so the kick's room reverb is already in it).

Ramming speed is a fixed 0.8 s beat for 10 s. Each preview plays it in context: 2 measures of the battle march at
1.4 s, 12 ramming measures, then 2 battle-march measures again.

At 0.8 s the kick's 1.36 s ring would pile up, so ramming kicks are cut short with a quick fade (as the game does
with a sound slice), and the stroke's deep kick is cut at half a second.

Run with: python3 samples/make_ramming_previews.py   (needs numpy)
Writes samples/ramming/01_*.wav to 08_*.wav (not committed).
"""
import os
import wave

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
KIT = os.path.join(HERE, "..", "package", "sounds")
OUT = os.path.join(HERE, "ramming")
RATE = 44100
NORMAL = 1.4
RAMMING = 0.8
# The stroke's kick and the other kicks, cut short during ramming speed.
STROKE_KICK = 0.5
SHORT_KICK = 0.3
FADE_OUT = 0.15
rng = np.random.default_rng(5)


def load(name):
    with wave.open(os.path.join(KIT, name + ".wav")) as w:
        return np.frombuffer(w.readframes(w.getnframes()), "<i2").astype(float) / 32768


KICK = load("drum_kick")
MID = load("drum_mid")
TAP = load("drum_tap")
HIGH = load("drum_high")


def pitched(sound, pitch):
    if abs(pitch - 1.0) < 1e-4:
        return sound
    positions = np.arange(0, len(sound) - 1, pitch)
    return np.interp(positions, np.arange(len(sound)), sound)


def cut(sound, seconds):
    """The first seconds of a sound, fading out over its last 0.15 s (no fade in, so the attack stays)."""
    n = min(len(sound), int(seconds * RATE))
    out = sound[:n].copy()
    fade = min(n, int(FADE_OUT * RATE))
    out[n - fade:] *= np.linspace(1.0, 0.0, fade)
    return out


def place(bus, sound, at, gain, pitch=1.0, exact=False, length=0.0):
    """A hit. Unless exact, the hand varies a little in timing, strength and pitch."""
    if not exact:
        at += rng.normal(0, 0.004)
        gain *= rng.uniform(0.88, 1.0)
        pitch *= rng.uniform(0.97, 1.03)
    hit = pitched(sound, pitch)
    if length > 0:
        hit = cut(hit, length * pitch)
    hit = hit * gain
    start = max(0, int(at * RATE))
    end = min(bus.size, start + hit.size)
    bus[start:end] += hit[: end - start]


class Measure:
    def __init__(self, bus, index, start, period):
        self.bus, self.index, self.start, self.period = bus, index, start, period

    def at(self, fraction):
        return self.start + self.period * fraction

    def stroke(self, short=True):
        """ONE: the deep drum doubled an octave-ish lower, and the middle drum for attack. Always exact."""
        length = STROKE_KICK if short else 0.0
        place(self.bus, KICK, self.start, 1.0, exact=True, length=length)
        place(self.bus, KICK, self.start, 0.5, pitch=0.82, exact=True, length=length)
        place(self.bus, MID, self.start, 0.36 if short else 0.32, exact=True)

    def kick(self, fraction, gain, pitch=1.0):
        place(self.bus, KICK, self.at(fraction), gain, pitch=pitch, length=SHORT_KICK)

    def tap(self, fraction, gain):
        place(self.bus, TAP, self.at(fraction), gain)

    def mid(self, fraction, gain):
        place(self.bus, MID, self.at(fraction), gain)

    def high(self, fraction, gain):
        place(self.bus, HIGH, self.at(fraction), gain)

    def eighths(self, gains=(0.2, 0.15)):
        for step in range(1, 8):
            self.tap(step / 8, gains[step % len(gains)])

    @property
    def fourth(self):
        return self.index % 4 == 3


def battle_march(m):
    """The normal rhythm around the ramming, for context."""
    m.stroke(short=False)
    for step in range(1, 8):
        m.tap(step / 8, 0.2 if step % 2 == 0 else 0.14)
    m.mid(5 / 8, 0.3)
    m.high(3 / 4, 0.28)
    if m.fourth:
        m.mid(7 / 8, 0.44)


# 01: pound. A short kick on every count: four hammer blows per stroke, the first the heaviest.
def pound(m):
    m.stroke()
    for count in (1, 2, 3):
        m.kick(count / 4, 0.42, pitch=1.04)
    for eighth in (1, 3, 5, 7):
        m.tap(eighth / 8, 0.18)


# 02: hammer. Middle drum on two and four, the high drum on three, over a steady eighth-note pulse.
def hammer(m):
    m.stroke()
    m.eighths((0.22, 0.16))
    m.mid(1 / 4, 0.34)
    m.high(2 / 4, 0.3)
    m.mid(3 / 4, 0.38)


# 03: backbeat. A kick on three answers the stroke; the high drum snaps on two and four like a snare.
def backbeat(m):
    m.stroke()
    m.eighths((0.18, 0.13))
    m.kick(2 / 4, 0.5, pitch=0.96)
    m.high(1 / 4, 0.34)
    m.high(3 / 4, 0.36)


# 04: stampede. Kicks gallop on the "and" of two, three, and the "and" of four, driving into the next stroke.
def stampede(m):
    m.stroke()
    m.kick(3 / 8, 0.36, pitch=1.02)
    m.kick(4 / 8, 0.44, pitch=0.98)
    m.kick(7 / 8, 0.4, pitch=1.0)
    for eighth in (1, 2, 5, 6):
        m.tap(eighth / 8, 0.16)
    m.high(3 / 4, 0.28)


# 05: triplet charge. The pulse in triplets, a middle drum on the last one and a kick pickup into the stroke.
def triplet_charge(m):
    m.stroke()
    for step in range(1, 6):
        m.tap(step / 6, 0.2 if step % 2 == 0 else 0.15)
    m.mid(4 / 6, 0.34)
    m.kick(5 / 6, 0.36, pitch=1.03)


# 06: thunder roll. A middle-drum roll swells through every measure into the next stroke.
def thunder_roll(m):
    m.stroke()
    for step in range(2, 8):
        m.mid(step / 8, 0.14 + 0.05 * (step - 2))
    m.high(1 / 4, 0.26)


# 07: war call. Measures alternate: kicks on the counts, then the high and middle drums calling back.
def war_call(m):
    m.stroke()
    m.eighths((0.17, 0.12))
    if m.index % 2 == 0:
        m.kick(1 / 4, 0.38, pitch=1.04)
        m.kick(2 / 4, 0.4, pitch=1.0)
        m.kick(3 / 4, 0.42, pitch=0.97)
    else:
        m.high(1 / 4, 0.32)
        m.mid(2 / 4, 0.36)
        m.high(3 / 4, 0.32)
        m.mid(7 / 8, 0.42)


# 08: berserker. Everything at once: kicks on the counts, high-drum backbeat, and a middle-drum fill every fourth.
def berserker(m):
    m.stroke()
    m.eighths((0.2, 0.15))
    m.kick(2 / 4, 0.44, pitch=0.97)
    m.high(1 / 4, 0.3)
    m.high(3 / 4, 0.32)
    if m.fourth:
        for k, gain in enumerate((0.3, 0.38, 0.46)):
            m.mid(5 / 8 + k / 8, gain)
    else:
        m.kick(3 / 4 + 1 / 8, 0.34, pitch=1.03)


def render(name, pattern):
    plan = [(battle_march, NORMAL)] * 2 + [(pattern, RAMMING)] * 12 + [(battle_march, NORMAL)] * 2
    total = int(RATE * (sum(p for _, p in plan) + 2.0))
    bus = np.zeros(total)
    start = 0.0
    for index, (rhythm, period) in enumerate(plan):
        rhythm(Measure(bus, index, start, period))
        start += period
    bus = bus / np.max(np.abs(bus)) * 0.89
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((bus * 32767).astype("<i2").tobytes())
    print(os.path.basename(path))


if __name__ == "__main__":
    for number, pattern in enumerate([
        pound, hammer, backbeat, stampede, triplet_charge, thunder_roll, war_call, berserker,
    ], start=1):
        render(f"{number:02d}_{pattern.__name__}", pattern)
