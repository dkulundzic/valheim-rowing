"""Renders previews of the war drum's transitions into and out of ramming speed, made from the game's drum kit.

Every rhythm starts its measure with the stroke, so a transition only bridges one stroke to the next and works
between any normal and any ramming rhythm:
- a lead-in: the beat speeds up in steps (one normal measure a bit quicker, then the lead-in measure), and the
  lead-in builds briefly, ends on a double kick and stops: silence until ramming's first stroke;
- a release: one measure, again at the halfway tempo, that opens on a big hit left to ring, before the normal
  rhythm comes back.

Each preview: 2 normal measures (1.5 s), 1 normal at 1.3 s, the lead-in (1.05 s), 8 ramming measures (0.8 s), the
release (1.15 s), 2 normal measures.
Run with: python3 samples/make_transition_previews.py   (needs numpy)
Writes samples/transitions/01_*.wav to 06_*.wav (not committed).
"""
import os
import wave

import numpy as np

import make_ramming_previews as kit
from make_ramming_previews import KICK, MID, HIGH, Measure, place

OUT = os.path.join(kit.HERE, "transitions")
NORMAL = 1.5
RAMMING = 0.8
# Into ramming the beat speeds up in steps: one normal measure at ACCELERATE, the lead-in at LEAD_IN. Out of it,
# the release measure is at BRIDGE.
ACCELERATE = 1.3
LEAD_IN = 1.05
BRIDGE = (NORMAL + RAMMING) / 2


# Normal rhythms (from the 16), with the game's kit.
def battle_march(m):
    kit.battle_march(m)


def gallop(m):
    m.stroke(short=False)
    for step in range(1, 6):
        m.tap(step / 6, (0.2, 0.13, 0.15)[step % 3])
    m.high(2 / 6, 0.24)
    m.mid(4 / 6, 0.32)
    if m.fourth:
        m.mid(5 / 6, 0.44)


def heartbeat(m):
    m.stroke(short=False)
    m.kick(min(0.22, m.period * 0.16) / m.period, 0.46, pitch=0.93)
    for step in range(3, 8):
        m.tap(step / 8, 0.15 if step % 2 else 0.2)
    m.high(3 / 4, 0.26)


# Lead-ins: the stroke and a short build, then a double kick and a stop: silence until ramming's first stroke.

def double_kick_and_stop(m, first, gain=0.62):
    """Two kicks an eighth apart, damped short so the stop after them is silent."""
    for k in range(2):
        place(m.bus, KICK, m.at(first + k / 8), gain + 0.08 * k, pitch=0.97 - 0.03 * k, exact=True, length=0.24)


# A: double kick. Eighth-note taps swell through the first half, then the double kick on three-and and four.
def lead_double_kick(m):
    m.stroke(short=False)
    for step in range(1, 5):
        m.tap(step / 8, 0.12 + 0.04 * step)
    double_kick_and_stop(m, 5 / 8)


# B: call. The middle and high drums call on two and the "and" of two, the double kick comes early on three,
# and the stop is long.
def lead_call(m):
    m.stroke(short=False)
    m.mid(1 / 4, 0.36)
    m.high(3 / 8, 0.32)
    double_kick_and_stop(m, 4 / 8)


# C: gather. A soft kick on two, the middle drum on two-and and three, then the double kick with the high drum
# on its second hit.
def lead_gather(m):
    m.stroke(short=False)
    m.kick(1 / 4, 0.32, pitch=1.02)
    m.mid(3 / 8, 0.3)
    m.mid(4 / 8, 0.36)
    double_kick_and_stop(m, 5 / 8)
    place(m.bus, HIGH, m.at(6 / 8), 0.34, exact=True)


# Releases: a big hit on the first stroke after ramming, left to ring.
def big_hit(m):
    place(m.bus, KICK, m.start, 1.0, exact=True)
    place(m.bus, KICK, m.start, 0.6, pitch=0.82, exact=True)
    place(m.bus, MID, m.start, 0.5, exact=True)
    place(m.bus, HIGH, m.start, 0.34, exact=True)


# A: crash. The big hit alone.
def release_crash(m):
    big_hit(m)


# B: settle. The big hit, then two soft taps fading out.
def release_settle(m):
    big_hit(m)
    m.tap(2 / 4, 0.14)
    m.tap(3 / 4, 0.1)


# C: echo. The big hit and a softer kick answering it halfway, like a heartbeat calming down.
def release_echo(m):
    big_hit(m)
    place(m.bus, KICK, m.at(1 / 2), 0.42, pitch=0.93)


def render(name, normal, lead, ramming, release):
    plan = ([(normal, NORMAL)] * 2 + [(normal, ACCELERATE), (lead, LEAD_IN)] + [(ramming, RAMMING)] * 8 + [(release, BRIDGE)]
            + [(normal, NORMAL)] * 2)
    total = int(kit.RATE * (sum(p for _, p in plan) + 2.0))
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
        w.setframerate(kit.RATE)
        w.writeframes((bus * 32767).astype("<i2").tobytes())
    print(os.path.basename(path))


if __name__ == "__main__":
    for number, (normal, lead, ramming, release) in enumerate([
        (battle_march, lead_double_kick, kit.pound, release_crash),
        (gallop, lead_call, kit.stampede, release_settle),
        (heartbeat, lead_gather, kit.berserker, release_echo),
        (battle_march, lead_call, kit.thunder_roll, release_echo),
        (gallop, lead_gather, kit.hammer, release_crash),
        (heartbeat, lead_double_kick, kit.war_call, release_settle),
    ], start=1):
        render(f"{number:02d}_{lead.__name__[5:]}__{release.__name__[8:]}", normal, lead, ramming, release)
