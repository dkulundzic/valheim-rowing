"""Renders war drum previews, all built on the "battle march": one measure per stroke, the stroke (ONE) a heavy,
doubled deep drum, and a driving pulse of dry taps counting in to the next stroke. Made from real recordings: the CC0
dunun set by JIMMYJAMES112 on Freesound (see SOURCES.md). The deep drum (the kick) gets a little open-air space;
everything else stays dry.

Each preview is 12 measures across the in-game tempo range: 4 of 1.8 s (ship still), 4 of 1.5 s, 4 of 1.2 s.

Run with: python3 samples/make_measure_previews.py   (needs numpy)
Writes samples/drums/01_*.wav to 16_*.wav (not committed).
"""
import os
import wave

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "originals", "fs-jimmyjames112-dunun")
OUT = os.path.join(HERE, "drums")
RATE = 44100
MEASURES = [1.8] * 4 + [1.5] * 4 + [1.2] * 4
# The kick's space, kept subtle: a short tail mixed low.
REVERB_SECONDS = 1.0
REVERB_LEVEL = 0.14
rng = np.random.default_rng(3)


def load(file_id):
    with wave.open(os.path.join(SRC, file_id + ".wav")) as w:
        samples = np.frombuffer(w.readframes(w.getnframes()), "<i2").astype(float) / 32768
    return samples / np.max(np.abs(samples))


DUNDUNBA = load("170180")      # deep: the kick
SANGBAN = load("170177")       # middle
SANGBAN_MUTE = load("170178")  # short, dry tap: the pulse
KENKENI = load("170179")       # high


def pitched(sound, pitch):
    if abs(pitch - 1.0) < 1e-4:
        return sound
    positions = np.arange(0, len(sound) - 1, pitch)
    return np.interp(positions, np.arange(len(sound)), sound)


def place(bus, sound, at, gain, pitch=1.0, exact=False):
    """A hit. Unless exact, the hand varies a little in timing, strength and pitch."""
    if not exact:
        at += rng.normal(0, 0.006)
        gain *= rng.uniform(0.88, 1.0)
        pitch *= rng.uniform(0.97, 1.03)
    hit = pitched(sound, pitch) * gain
    start = max(0, int(at * RATE))
    end = min(bus.size, start + hit.size)
    bus[start:end] += hit[: end - start]


def room(seconds):
    """A synthetic open-air reverb: a dark, smoothly decaying wash of noise."""
    t = np.arange(int(RATE * seconds)) / RATE
    noise = rng.uniform(-1, 1, t.size)
    dark = np.empty_like(noise)
    acc = 0.0
    for i, x in enumerate(noise):
        acc += (x - acc) * 0.06
        dark[i] = acc
    response = dark * np.exp(-t / (seconds / 4))
    response[0] = 0.0
    return response / np.sqrt(np.sum(response ** 2))


def fft_convolve(signal, response):
    size = 1
    while size < signal.size + response.size:
        size *= 2
    return np.fft.irfft(np.fft.rfft(signal, size) * np.fft.rfft(response, size), size)[: signal.size]


ROOM = room(REVERB_SECONDS)


class Measure:
    """One measure (one stroke): where it starts, how long it is, and the two buses to play on."""

    def __init__(self, dry, kicks, index, start, period):
        self.dry, self.kicks, self.index, self.start, self.period = dry, kicks, index, start, period

    def at(self, fraction):
        return self.start + self.period * fraction

    def stroke(self):
        """ONE: the deep drum, a lower one under it for weight, and the middle drum for attack. Always exact."""
        place(self.kicks, DUNDUNBA, self.start, 1.0, exact=True)
        place(self.kicks, DUNDUNBA, self.start, 0.5, pitch=0.82, exact=True)
        place(self.dry, SANGBAN, self.start, 0.32, exact=True)

    def kick(self, fraction, gain, pitch=1.0):
        place(self.kicks, DUNDUNBA, self.at(fraction), gain, pitch=pitch)

    def tap(self, fraction, gain):
        if gain > 0:
            place(self.dry, SANGBAN_MUTE, self.at(fraction), gain)

    def mid(self, fraction, gain):
        place(self.dry, SANGBAN, self.at(fraction), gain)

    def high(self, fraction, gain):
        place(self.dry, KENKENI, self.at(fraction), gain)

    def pulse(self, steps=8, gains=(0.2, 0.14)):
        """The battle march's driving pulse: dry taps on every step after ONE."""
        for step in range(1, steps):
            self.tap(step / steps, gains[step % len(gains)])

    @property
    def fourth(self):
        """Every fourth measure ends a phrase."""
        return self.index % 4 == 3

    @property
    def roomy(self):
        """Slow enough for extra notes (the 1.8 s and 1.5 s beats)."""
        return self.period >= 1.4


def render(name, pattern):
    total = int(RATE * (sum(MEASURES) + 2.5))
    dry = np.zeros(total)
    kicks = np.zeros(total)
    start = 0.0
    for index, period in enumerate(MEASURES):
        pattern(Measure(dry, kicks, index, start, period))
        start += period
    mix = dry + kicks + fft_convolve(kicks, ROOM) * REVERB_LEVEL
    mix = mix / np.max(np.abs(mix)) * 0.89
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((mix * 32767).astype("<i2").tobytes())
    print(os.path.basename(path))


# 01: the battle march. The stroke, the eighth-note pulse, the middle drum on three-and, the high drum on four,
# and a pickup on four-and every fourth measure.
def battle_march(m):
    m.stroke()
    m.pulse()
    m.mid(5 / 8, 0.3)
    m.high(3 / 4, 0.28)
    if m.fourth:
        m.mid(7 / 8, 0.44)


# 02: a soft kick on three, answering the stroke.
def kick_on_three(m):
    battle_march(m)
    m.kick(2 / 4, 0.36, pitch=0.93)


# 03: a kick on four-and, charging into the next stroke.
def charge(m):
    m.stroke()
    m.pulse()
    m.mid(5 / 8, 0.3)
    m.high(3 / 4, 0.26)
    m.kick(7 / 8, 0.34, pitch=0.95)


# 04: heartbeat. A softer kick just after the stroke (BOOM-boom), then the pulse from the "and" of two.
def heartbeat(m):
    m.stroke()
    m.kick(min(0.22, m.period * 0.16) / m.period, 0.46, pitch=0.93)
    for step in range(3, 8):
        m.tap(step / 8, 0.15 if step % 2 else 0.2)
    m.high(3 / 4, 0.26)


# 05: sixteenths. The pulse doubles to sixteenths with the eighths accented; eighths again at the fastest beat.
def sixteenths(m):
    m.stroke()
    if m.roomy:
        m.pulse(16, (0.19, 0.1))
    else:
        m.pulse(8, (0.2, 0.14))
    m.mid(5 / 8, 0.3)
    m.high(3 / 4, 0.28)
    if m.fourth:
        m.mid(7 / 8, 0.44)


# 06: stomp. The pulse accents the "and"s, pushing against the beat like stamping feet.
def stomp(m):
    m.stroke()
    m.pulse(8, (0.11, 0.24))
    m.mid(3 / 8, 0.28)
    m.mid(7 / 8, 0.38)
    m.high(2 / 4, 0.24)


# 07: gallop. The pulse in triplets, six per measure, for a rolling charge.
def gallop(m):
    m.stroke()
    m.pulse(6, (0.2, 0.13, 0.15))
    m.high(2 / 6, 0.24)
    m.mid(4 / 6, 0.32)
    if m.fourth:
        m.mid(5 / 6, 0.44)


# 08: crescendo. The pulse swells through each measure: quiet after the stroke, loud right before the next.
def crescendo(m):
    m.stroke()
    for step in range(1, 8):
        m.tap(step / 8, 0.07 + 0.03 * step)
    m.high(3 / 4, 0.28)
    m.mid(7 / 8, 0.46 if m.fourth else 0.32)


# 09: tension. Silence after the stroke, then the pulse starts halfway and drives into the next ONE.
def tension(m):
    m.stroke()
    for step in range(4, 8):
        m.tap(step / 8, 0.15 + 0.03 * (step - 4))
    m.high(2 / 4, 0.22)
    m.mid(3 / 4, 0.34)
    if m.fourth:
        m.mid(7 / 8, 0.46)


# 10: kick roll. The battle march, and every fourth measure three kicks rising into the next stroke.
def kick_roll(m):
    m.stroke()
    m.pulse()
    m.high(2 / 4, 0.22)
    if m.fourth:
        for k, gain in enumerate((0.28, 0.38, 0.5)):
            m.kick(3 / 4 + k / 12, gain, pitch=1.0 + 0.03 * k)
    else:
        m.mid(5 / 8, 0.3)
        m.high(3 / 4, 0.26)


# 11: drum roll. The battle march with high-drum counts, and a middle-drum roll into the stroke every fourth measure.
def drum_roll(m):
    m.stroke()
    m.pulse()
    m.high(1 / 4, 0.2)
    m.high(2 / 4, 0.26)
    if m.fourth:
        for k, gain in enumerate((0.26, 0.34, 0.46)):
            m.mid(3 / 4 + k / 12, gain)
    else:
        m.mid(5 / 8, 0.3)
        m.high(3 / 4, 0.28)


# 12: sub. An extra-low kick under the stroke, a sparser pulse: deep and wide.
def sub(m):
    m.stroke()
    place(m.kicks, DUNDUNBA, m.start, 0.45, pitch=0.7, exact=True)
    m.pulse(8, (0.16, 0.0))
    m.mid(5 / 8, 0.26)
    m.high(3 / 4, 0.26)


# 13: syncopation. Kicks on two-and and three-and roll under the pulse.
def syncopation(m):
    m.stroke()
    m.pulse(8, (0.15, 0.1))
    m.kick(3 / 8, 0.32, pitch=0.97)
    m.kick(5 / 8, 0.36, pitch=0.95)
    m.high(3 / 4, 0.24)


# 14: four on the floor. A kick on every count, the stroke far deeper and stronger, the pulse on the "and"s.
def four_on_the_floor(m):
    m.stroke()
    for count in (1, 2, 3):
        m.kick(count / 4, 0.3, pitch=1.04)
    for eighth in (1, 3, 5, 7):
        m.tap(eighth / 8, 0.16)


# 15: call and response. Measures alternate the high drum and the middle drum on the counts, over the pulse.
def call_and_response(m):
    m.stroke()
    m.pulse()
    if m.index % 2 == 0:
        m.high(2 / 4, 0.26)
        m.high(3 / 4, 0.3)
    else:
        m.mid(2 / 4, 0.3)
        m.mid(3 / 4, 0.36)
    if m.fourth:
        m.mid(7 / 8, 0.46)


# 16: war party march. A soft kick on three, high-drum counts, and a roll into the stroke every fourth measure.
def war_party_march(m):
    m.stroke()
    m.pulse()
    m.kick(2 / 4, 0.3, pitch=0.93)
    m.high(1 / 4, 0.2)
    if m.fourth:
        for k, gain in enumerate((0.26, 0.34, 0.46)):
            m.mid(3 / 4 + k / 12, gain)
    else:
        m.mid(5 / 8, 0.3)
        m.high(3 / 4, 0.28)


if __name__ == "__main__":
    for number, pattern in enumerate([
        battle_march, kick_on_three, charge, heartbeat, sixteenths, stomp, gallop, crescendo,
        tension, kick_roll, drum_roll, sub, syncopation, four_on_the_floor, call_and_response, war_party_march,
    ], start=1):
        render(f"{number:02d}_{pattern.__name__}", pattern)
