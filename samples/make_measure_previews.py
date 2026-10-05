"""Renders war drum previews where one measure is one stroke: ONE (the stroke) on the deep dundunba, then "two,
three, four" counting in on lighter drums. Built from real recordings: the CC0 dunun set by JIMMYJAMES112 on
Freesound (see SOURCES.md).

Each preview is 12 measures across the in-game tempo range: 4 of 1.8 s (ship still), 4 of 1.5 s, 4 of 1.2 s.

Run with: python3 samples/make_measure_previews.py   (needs numpy)
Writes samples/drums/measure/*.wav (not committed).
"""
import os
import wave

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "originals", "fs-jimmyjames112-dunun")
OUT = os.path.join(HERE, "drums", "measure")
RATE = 44100
MEASURES = [1.8] * 4 + [1.5] * 4 + [1.2] * 4
rng = np.random.default_rng(5)


def load(file_id):
    with wave.open(os.path.join(SRC, file_id + ".wav")) as w:
        samples = np.frombuffer(w.readframes(w.getnframes()), "<i2").astype(float) / 32768
    return samples / np.max(np.abs(samples))


DUNDUNBA = load("170180")   # deep: the stroke (ONE)
SANGBAN = load("170177")    # middle: pickups and fills
SANGBAN_MUTE = load("170178")  # short, dry: grace notes
KENKENI = load("170179")    # high: the counts


def pitched(sound, pitch):
    """Plays a sound faster or slower (pitch 1.05 = 5% higher and shorter)."""
    if abs(pitch - 1.0) < 1e-4:
        return sound
    positions = np.arange(0, len(sound) - 1, pitch)
    return np.interp(positions, np.arange(len(sound)), sound)


def place(track, sound, at, gain, pitch=1.0, exact=False):
    """Puts a hit on the track. Unless exact, the hand varies a little in timing, strength and pitch."""
    if not exact:
        at += rng.normal(0, 0.006)
        gain *= rng.uniform(0.88, 1.0)
        pitch *= rng.uniform(0.97, 1.03)
    hit = pitched(sound, pitch) * gain
    start = max(0, int(at * RATE))
    end = min(track.size, start + hit.size)
    track[start:end] += hit[: end - start]


def measures():
    at = 0.0
    for index, period in enumerate(MEASURES):
        yield index, at, period
        at += period


def render(name, pattern):
    track = np.zeros(int(RATE * (sum(MEASURES) + 2.0)))
    for index, start, period in measures():
        pattern(track, index, start, period)
    track = track / np.max(np.abs(track)) * 0.89
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((track * 32767).astype("<i2").tobytes())
    print(path)


def stroke(track, start):
    """ONE: the stroke. Always the deep drum, full strength, exactly on time."""
    place(track, DUNDUNBA, start, 1.0, exact=True)


# A: plain counts. ONE on the deep drum; two, three, four on the high drum, evenly and lightly.
def plain(track, index, start, period):
    stroke(track, start)
    for count in (1, 2, 3):
        place(track, KENKENI, start + period * count / 4, 0.32)


# B: build-up. The counts grow toward the stroke, and "four" is a firm pickup on the middle drum.
def build_up(track, index, start, period):
    stroke(track, start)
    place(track, KENKENI, start + period * 1 / 4, 0.2)
    place(track, KENKENI, start + period * 2 / 4, 0.3)
    place(track, SANGBAN, start + period * 3 / 4, 0.42)


# C: phrases. B for three measures, then a fill in the fourth: a quick double on "four-and" into the next ONE.
def phrases(track, index, start, period):
    stroke(track, start)
    place(track, KENKENI, start + period * 1 / 4, 0.2)
    place(track, KENKENI, start + period * 2 / 4, 0.3)
    if index % 4 == 3:
        place(track, SANGBAN, start + period * 3 / 4, 0.4)
        place(track, SANGBAN, start + period * 7 / 8, 0.52)
    else:
        place(track, SANGBAN, start + period * 3 / 4, 0.42)


# D: C with grace notes. Soft, dry taps on some of the "and"s, varying from measure to measure. At the fastest beat
# there's no room, so it thins out to the counts.
def grace_notes(track, index, start, period):
    phrases(track, index, start, period)
    if period < 1.4:
        return
    for eighth in (1, 3, 5):
        if rng.random() < 0.45:
            place(track, SANGBAN_MUTE, start + period * eighth / 8, rng.uniform(0.12, 0.2))


# E: call and response. The high drum on two and four, the middle drum answering on three.
def call_and_response(track, index, start, period):
    stroke(track, start)
    place(track, KENKENI, start + period * 1 / 4, 0.22)
    place(track, SANGBAN, start + period * 2 / 4, 0.4)
    place(track, KENKENI, start + period * 3 / 4, 0.3)


# F: triplet gallop. The measure in three: ONE-ta-ta, with a middle-drum pickup just before the next ONE.
def triplet_gallop(track, index, start, period):
    stroke(track, start)
    place(track, KENKENI, start + period * 1 / 3, 0.24)
    place(track, KENKENI, start + period * 2 / 3, 0.3)
    place(track, SANGBAN, start + period * 5 / 6, 0.36)


# G: double-skinned stroke. ONE doubled with the middle drum for a fuller hit; the counts as dry, quiet taps.
def doubled_stroke(track, index, start, period):
    stroke(track, start)
    place(track, SANGBAN, start, 0.45, exact=True)
    for count, gain in ((1, 0.22), (2, 0.26), (3, 0.34)):
        place(track, SANGBAN_MUTE, start + period * count / 4, gain)


# H: roll-in. Two and three on the high drum, then a short three-hit roll on four, getting louder into ONE.
def roll_in(track, index, start, period):
    stroke(track, start)
    place(track, KENKENI, start + period * 1 / 4, 0.2)
    place(track, KENKENI, start + period * 2 / 4, 0.24)
    for k, gain in enumerate((0.22, 0.3, 0.4)):
        place(track, KENKENI, start + period * (3 / 4 + k / 12), gain)


# I: off-beat groove. The counts land on the "and"s between the quarter beats, so ONE is the only hit on the grid.
def offbeat_groove(track, index, start, period):
    stroke(track, start)
    place(track, KENKENI, start + period * 3 / 8, 0.26)
    place(track, KENKENI, start + period * 5 / 8, 0.3)
    place(track, SANGBAN, start + period * 7 / 8, 0.4)


# J: deep answer. A softer, lower dundunba on three answers the stroke; the high drum on two and four.
def deep_answer(track, index, start, period):
    stroke(track, start)
    place(track, KENKENI, start + period * 1 / 4, 0.22)
    place(track, DUNDUNBA, start + period * 2 / 4, 0.38, pitch=0.92)
    place(track, KENKENI, start + period * 3 / 4, 0.28)


# K: sparse. Calm and open: ONE, a ghost on three, and a single middle-drum pickup on four.
def sparse(track, index, start, period):
    stroke(track, start)
    place(track, SANGBAN_MUTE, start + period * 2 / 4, 0.14)
    place(track, SANGBAN, start + period * 3 / 4, 0.36)


# L: war party. A doubled stroke, build-up counts, grace notes, and alternating measures with a roll every fourth.
def war_party(track, index, start, period):
    stroke(track, start)
    place(track, SANGBAN, start, 0.4, exact=True)
    place(track, KENKENI, start + period * 1 / 4, 0.2)
    place(track, KENKENI, start + period * 2 / 4, 0.28)
    if index % 4 == 3:
        for k, gain in enumerate((0.26, 0.34, 0.44)):
            place(track, SANGBAN, start + period * (3 / 4 + k / 12), gain)
    elif index % 2 == 1:
        place(track, SANGBAN, start + period * 5 / 8, 0.3)
        place(track, SANGBAN, start + period * 3 / 4, 0.42)
    else:
        place(track, SANGBAN, start + period * 3 / 4, 0.42)
    if period >= 1.4:
        for eighth in (1, 3):
            if rng.random() < 0.5:
                place(track, SANGBAN_MUTE, start + period * eighth / 8, rng.uniform(0.12, 0.18))


def war_party_base(track, start, period):
    """L's core: the doubled stroke and build-up counts on two and three."""
    stroke(track, start)
    place(track, SANGBAN, start, 0.4, exact=True)
    place(track, KENKENI, start + period * 1 / 4, 0.2)
    place(track, KENKENI, start + period * 2 / 4, 0.28)


def grace(track, start, period, eighths=(1, 3), chance=0.5):
    if period >= 1.4:
        for eighth in eighths:
            if rng.random() < chance:
                place(track, SANGBAN_MUTE, start + period * eighth / 8, rng.uniform(0.12, 0.18))


# M: war party gallop. L, but the fill measure gallops in threes into the next ONE.
def war_party_gallop(track, index, start, period):
    war_party_base(track, start, period)
    if index % 4 == 3:
        for k, gain in enumerate((0.28, 0.36, 0.46)):
            place(track, SANGBAN, start + period * (2 / 3 + k / 9), gain)
    else:
        place(track, SANGBAN, start + period * 3 / 4, 0.42)
    grace(track, start, period)


# N: war party heavy. L with a second, lower deep drum answering on three: thunderous.
def war_party_heavy(track, index, start, period):
    stroke(track, start)
    place(track, SANGBAN, start, 0.4, exact=True)
    place(track, KENKENI, start + period * 1 / 4, 0.2)
    place(track, DUNDUNBA, start + period * 2 / 4, 0.36, pitch=0.9)
    if index % 4 == 3:
        for k, gain in enumerate((0.26, 0.34, 0.44)):
            place(track, SANGBAN, start + period * (3 / 4 + k / 12), gain)
    else:
        place(track, SANGBAN, start + period * 3 / 4, 0.42)
    grace(track, start, period, eighths=(3,))


# O: war party groove. L's doubled stroke, but the counts sit between the beats, and a fill every fourth measure.
def war_party_groove(track, index, start, period):
    stroke(track, start)
    place(track, SANGBAN, start, 0.4, exact=True)
    place(track, KENKENI, start + period * 3 / 8, 0.24)
    place(track, KENKENI, start + period * 5 / 8, 0.3)
    if index % 4 == 3:
        place(track, SANGBAN, start + period * 3 / 4, 0.36)
        place(track, SANGBAN, start + period * 7 / 8, 0.48)
    else:
        place(track, SANGBAN, start + period * 7 / 8, 0.42)
    grace(track, start, period, eighths=(1,))


# P: war party dialogue. Measures alternate call (high drums) and response (middle drums), with L's roll every fourth.
def war_party_dialogue(track, index, start, period):
    stroke(track, start)
    place(track, SANGBAN, start, 0.4, exact=True)
    drum = KENKENI if index % 2 == 0 else SANGBAN
    place(track, drum, start + period * 1 / 4, 0.22)
    place(track, drum, start + period * 2 / 4, 0.3)
    if index % 4 == 3:
        for k, gain in enumerate((0.26, 0.34, 0.44)):
            place(track, SANGBAN, start + period * (3 / 4 + k / 12), gain)
    else:
        place(track, SANGBAN if index % 2 == 0 else KENKENI, start + period * 3 / 4, 0.4)
    grace(track, start, period)


# Q: war party long phrase. A roll-in every second measure, and a bigger two-hit-per-count fill every fourth.
def war_party_long_phrase(track, index, start, period):
    war_party_base(track, start, period)
    if index % 4 == 3:
        for k in range(6):
            place(track, SANGBAN if k % 2 else KENKENI, start + period * (0.5 + k / 12), 0.24 + 0.05 * k)
    elif index % 2 == 1:
        for k, gain in enumerate((0.24, 0.32, 0.42)):
            place(track, KENKENI, start + period * (3 / 4 + k / 12), gain)
    else:
        place(track, SANGBAN, start + period * 3 / 4, 0.42)
    grace(track, start, period, eighths=(1,))


# R: battle march. A steady pulse of quiet taps on every eighth, the doubled stroke, and the middle drum on three-and.
def battle_march(track, index, start, period):
    stroke(track, start)
    place(track, SANGBAN, start, 0.4, exact=True)
    for eighth in range(1, 8):
        place(track, SANGBAN_MUTE, start + period * eighth / 8, 0.16 if eighth % 2 else 0.22)
    place(track, SANGBAN, start + period * 5 / 8, 0.32)
    place(track, KENKENI, start + period * 3 / 4, 0.3)
    if index % 4 == 3:
        place(track, SANGBAN, start + period * 7 / 8, 0.44)


# S: thunder. The doubled stroke, light ticks on two and three, and a deep double hit on "four-and" rolling into ONE.
def thunder(track, index, start, period):
    stroke(track, start)
    place(track, SANGBAN, start, 0.4, exact=True)
    place(track, KENKENI, start + period * 1 / 4, 0.2)
    place(track, KENKENI, start + period * 2 / 4, 0.24)
    place(track, DUNDUNBA, start + period * 3 / 4, 0.3, pitch=0.95)
    place(track, DUNDUNBA, start + period * 7 / 8, 0.38, pitch=0.95)
    grace(track, start, period, eighths=(1,), chance=0.35)


# T: shaman. Quick pairs of dry taps (ruffs) on two and three, the high drum on four, intricate but light; thins out
# at the fastest beat.
def shaman(track, index, start, period):
    stroke(track, start)
    place(track, SANGBAN, start, 0.35, exact=True)
    pair = min(0.07, period / 20)
    for count in (1, 2):
        if period >= 1.4 or count == 2:
            place(track, SANGBAN_MUTE, start + period * count / 4 - pair, 0.14)
        place(track, SANGBAN_MUTE, start + period * count / 4, 0.22)
    place(track, KENKENI, start + period * 3 / 4, 0.34)
    if index % 4 == 3:
        place(track, KENKENI, start + period * 7 / 8, 0.4)


def heavy_stroke(track, start):
    """Battle march's stroke: the deep drum doubled with the middle drum."""
    stroke(track, start)
    place(track, SANGBAN, start, 0.4, exact=True)


def pulse(track, start, period, steps, gains):
    """A steady pulse of dry taps: `steps` per measure (skipping ONE), with gains cycling through `gains`."""
    for step in range(1, steps):
        place(track, SANGBAN_MUTE, start + period * step / steps, gains[step % len(gains)])


# U: sixteenth drive. The pulse doubles to sixteenths, with the eighths accented; at the fastest beat it falls back to
# eighths so it doesn't blur.
def march_sixteenths(track, index, start, period):
    heavy_stroke(track, start)
    if period >= 1.4:
        pulse(track, start, period, 16, (0.2, 0.11))
    else:
        pulse(track, start, period, 8, (0.22, 0.16))
    place(track, SANGBAN, start + period * 5 / 8, 0.32)
    place(track, KENKENI, start + period * 3 / 4, 0.3)
    if index % 4 == 3:
        place(track, SANGBAN, start + period * 7 / 8, 0.46)


# V: march with rolls. Battle march, plus L's high-drum counts and a three-hit roll into ONE every fourth measure.
def march_rolls(track, index, start, period):
    heavy_stroke(track, start)
    pulse(track, start, period, 8, (0.22, 0.16))
    place(track, KENKENI, start + period * 1 / 4, 0.22)
    place(track, KENKENI, start + period * 2 / 4, 0.28)
    if index % 4 == 3:
        for k, gain in enumerate((0.28, 0.36, 0.48)):
            place(track, SANGBAN, start + period * (3 / 4 + k / 12), gain)
    else:
        place(track, SANGBAN, start + period * 5 / 8, 0.32)
        place(track, KENKENI, start + period * 3 / 4, 0.3)


# W: heavy march. Battle march with a lower deep drum answering on three: two hits of weight per measure.
def march_heavy(track, index, start, period):
    heavy_stroke(track, start)
    pulse(track, start, period, 8, (0.22, 0.16))
    place(track, DUNDUNBA, start + period * 2 / 4, 0.34, pitch=0.9)
    place(track, SANGBAN, start + period * 5 / 8, 0.3)
    place(track, KENKENI, start + period * 3 / 4, 0.3)
    if index % 4 == 3:
        place(track, SANGBAN, start + period * 7 / 8, 0.46)


# X: stomp. The pulse accents the "and"s instead of the counts, pushing against the beat like stamping feet.
def march_stomp(track, index, start, period):
    heavy_stroke(track, start)
    pulse(track, start, period, 8, (0.12, 0.26))
    place(track, SANGBAN, start + period * 3 / 8, 0.3)
    place(track, SANGBAN, start + period * 7 / 8, 0.4)
    place(track, KENKENI, start + period * 2 / 4, 0.26)


# Y: march gallop. The pulse in triplets (six per measure) for a rolling, charging feel.
def march_gallop(track, index, start, period):
    heavy_stroke(track, start)
    pulse(track, start, period, 6, (0.22, 0.14, 0.16))
    place(track, KENKENI, start + period * 2 / 6, 0.26)
    place(track, SANGBAN, start + period * 4 / 6, 0.34)
    if index % 4 == 3:
        place(track, SANGBAN, start + period * 5 / 6, 0.46)


# Z: crescendo march. The pulse swells across each measure, quiet just after the stroke and loud right before the next.
def march_crescendo(track, index, start, period):
    heavy_stroke(track, start)
    for eighth in range(1, 8):
        place(track, SANGBAN_MUTE, start + period * eighth / 8, 0.08 + 0.03 * eighth)
    place(track, KENKENI, start + period * 3 / 4, 0.3)
    if index % 4 == 3:
        place(track, SANGBAN, start + period * 7 / 8, 0.48)
    else:
        place(track, SANGBAN, start + period * 7 / 8, 0.34)


# AA: charge. Battle march with a deep hit on "four-and" as well: relentless, like a ram hitting a gate.
def march_charge(track, index, start, period):
    heavy_stroke(track, start)
    pulse(track, start, period, 8, (0.22, 0.16))
    place(track, SANGBAN, start + period * 5 / 8, 0.32)
    place(track, KENKENI, start + period * 3 / 4, 0.28)
    place(track, DUNDUNBA, start + period * 7 / 8, 0.34, pitch=0.94)


# AB: rising tension. Silence right after the stroke, then the pulse starts at three and drives into the next ONE.
def march_tension(track, index, start, period):
    heavy_stroke(track, start)
    for eighth in range(4, 8):
        place(track, SANGBAN_MUTE, start + period * eighth / 8, 0.16 + 0.03 * (eighth - 4))
    place(track, KENKENI, start + period * 2 / 4, 0.24)
    place(track, SANGBAN, start + period * 3 / 4, 0.36)
    if index % 4 == 3:
        place(track, SANGBAN, start + period * 7 / 8, 0.48)


def fft_convolve(signal, response):
    size = 1
    while size < signal.size + response.size:
        size *= 2
    out = np.fft.irfft(np.fft.rfft(signal, size) * np.fft.rfft(response, size), size)
    return out[: signal.size]


def room(seconds=1.6, darkness=0.06):
    """A synthetic open-air reverb: a dark, smoothly decaying wash of noise."""
    t = np.arange(int(RATE * seconds)) / RATE
    noise = rng.uniform(-1, 1, t.size)
    dark = np.empty_like(noise)
    acc = 0.0
    for i, x in enumerate(noise):
        acc += (x - acc) * darkness
        dark[i] = acc
    response = dark * np.exp(-t / (seconds / 4))
    response[0] = 0.0
    return response / np.sqrt(np.sum(response ** 2))


ROOM = room()


def echo_bus(kicks, delay=0.33, feedback=0.38, repeats=4):
    """A darker, fading echo of the kicks, like the boom coming back off the water."""
    out = np.copy(kicks)
    tap = kicks
    shift = int(delay * RATE)
    for _ in range(repeats):
        moved = np.zeros_like(tap)
        moved[shift:] = tap[: tap.size - shift]
        darker = np.empty_like(moved)
        acc = 0.0
        step = moved[::1]
        for i in range(0, step.size, 64):
            block = step[i:i + 64]
            acc_block = np.empty_like(block)
            for j, x in enumerate(block):
                acc += (x - acc) * 0.25
                acc_block[j] = acc
            darker[i:i + 64] = acc_block
        tap = darker * feedback
        out += tap
    return out


def render_spacey(name, pattern, reverb=0.45, echo=False):
    """Like render, but the kicks go on their own bus with a reverb tail (and optionally an echo), mixed under the dry taps."""
    total = int(RATE * (sum(MEASURES) + 3.0))
    track = np.zeros(total)
    kicks = np.zeros(total)
    for index, start, period in measures():
        pattern(track, kicks, index, start, period)
    wet = kicks
    if echo:
        wet = echo_bus(wet)
    if reverb > 0:
        wet = wet + fft_convolve(wet, ROOM) * reverb
    mix = track + wet
    mix = mix / np.max(np.abs(mix)) * 0.89
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((mix * 32767).astype("<i2").tobytes())
    print(path)


def kick(kicks, at, gain, pitch=1.0, exact=False):
    place(kicks, DUNDUNBA, at, gain, pitch=pitch, exact=exact)


def big_stroke(track, kicks, start):
    """The stroke: the kick doubled with a lower kick underneath for depth, and the middle drum for attack."""
    kick(kicks, start, 1.0, exact=True)
    kick(kicks, start, 0.55, pitch=0.82, exact=True)
    place(track, SANGBAN, start, 0.3, exact=True)


def light_pulse(track, start, period, gains=(0.16, 0.11)):
    for step in range(1, 8):
        place(track, SANGBAN_MUTE, start + period * step / 8, gains[step % len(gains)])


# AC: kick march. Battle march with the kick carrying the groove: soft kicks on three and four-and under a light pulse.
def kick_march(track, kicks, index, start, period):
    big_stroke(track, kicks, start)
    light_pulse(track, start, period)
    kick(kicks, start + period * 2 / 4, 0.4, pitch=0.95)
    kick(kicks, start + period * 7 / 8, 0.32, pitch=0.97)
    place(track, KENKENI, start + period * 3 / 4, 0.24)


# AD: big room. Battle march with the deep stroke in a large open space: the boom hangs in the air between strokes.
def big_room(track, kicks, index, start, period):
    big_stroke(track, kicks, start)
    light_pulse(track, start, period)
    place(track, SANGBAN, start + period * 5 / 8, 0.26)
    place(track, KENKENI, start + period * 3 / 4, 0.24)


# AE: heartbeat kick. BOOM-boom: the stroke, then a softer kick right after, with a light pulse filling the rest.
def heartbeat_kick(track, kicks, index, start, period):
    big_stroke(track, kicks, start)
    kick(kicks, start + min(0.22, period * 0.16), 0.5, pitch=0.93)
    for step in range(3, 8):
        place(track, SANGBAN_MUTE, start + period * step / 8, 0.15 if step % 2 else 0.2)
    place(track, KENKENI, start + period * 3 / 4, 0.24)


# AF: four on the floor. A kick on every count, the stroke deeper and much stronger; quiet taps on the "and"s.
def four_on_the_floor(track, kicks, index, start, period):
    big_stroke(track, kicks, start)
    for count in (1, 2, 3):
        kick(kicks, start + period * count / 4, 0.32, pitch=1.04)
    for eighth in (1, 3, 5, 7):
        place(track, SANGBAN_MUTE, start + period * eighth / 8, 0.15)


# AG: sub stroke. An extra-low kick under the stroke with a long tail, and sparse taps: deep and spacious.
def sub_stroke(track, kicks, index, start, period):
    big_stroke(track, kicks, start)
    kick(kicks, start, 0.5, pitch=0.7, exact=True)
    place(track, SANGBAN_MUTE, start + period * 2 / 4, 0.14)
    place(track, SANGBAN_MUTE, start + period * 5 / 8, 0.12)
    place(track, SANGBAN, start + period * 3 / 4, 0.3)


# AH: kick syncopation. Kicks on ONE, two-and and three-and: a rolling, dotted feel under the pulse.
def kick_syncopation(track, kicks, index, start, period):
    big_stroke(track, kicks, start)
    light_pulse(track, start, period, gains=(0.14, 0.1))
    kick(kicks, start + period * 3 / 8, 0.36, pitch=0.97)
    kick(kicks, start + period * 5 / 8, 0.4, pitch=0.95)
    place(track, KENKENI, start + period * 3 / 4, 0.24)


# AI: dub echo. The battle march with a fading echo on the kicks: the stroke repeats softly across the water.
def dub_echo(track, kicks, index, start, period):
    big_stroke(track, kicks, start)
    light_pulse(track, start, period, gains=(0.14, 0.1))
    place(track, KENKENI, start + period * 3 / 4, 0.22)


# AJ: kick roll. Battle march, and every fourth measure a three-kick roll rising into the next stroke.
def kick_roll(track, kicks, index, start, period):
    big_stroke(track, kicks, start)
    light_pulse(track, start, period)
    place(track, KENKENI, start + period * 2 / 4, 0.22)
    if index % 4 == 3:
        for k, gain in enumerate((0.3, 0.4, 0.52)):
            kick(kicks, start + period * (3 / 4 + k / 12), gain, pitch=1.0 + 0.03 * k)
    else:
        kick(kicks, start + period * 7 / 8, 0.32, pitch=0.97)


if __name__ == "__main__":
    render_spacey("measure_AC_kick_march", kick_march)
    render_spacey("measure_AD_big_room", big_room, reverb=0.9)
    render_spacey("measure_AE_heartbeat_kick", heartbeat_kick)
    render_spacey("measure_AF_four_on_the_floor", four_on_the_floor)
    render_spacey("measure_AG_sub_stroke", sub_stroke, reverb=0.6)
    render_spacey("measure_AH_kick_syncopation", kick_syncopation)
    render_spacey("measure_AI_dub_echo", dub_echo, reverb=0.35, echo=True)
    render_spacey("measure_AJ_kick_roll", kick_roll)
    render("measure_U_march_sixteenths", march_sixteenths)
    render("measure_V_march_rolls", march_rolls)
    render("measure_W_march_heavy", march_heavy)
    render("measure_X_march_stomp", march_stomp)
    render("measure_Y_march_gallop", march_gallop)
    render("measure_Z_march_crescendo", march_crescendo)
    render("measure_AA_march_charge", march_charge)
    render("measure_AB_march_tension", march_tension)
    render("measure_M_war_party_gallop", war_party_gallop)
    render("measure_N_war_party_heavy", war_party_heavy)
    render("measure_O_war_party_groove", war_party_groove)
    render("measure_P_war_party_dialogue", war_party_dialogue)
    render("measure_Q_war_party_long_phrase", war_party_long_phrase)
    render("measure_R_battle_march", battle_march)
    render("measure_S_thunder", thunder)
    render("measure_T_shaman", shaman)
    render("measure_E_call_and_response", call_and_response)
    render("measure_F_triplet_gallop", triplet_gallop)
    render("measure_G_doubled_stroke", doubled_stroke)
    render("measure_H_roll_in", roll_in)
    render("measure_I_offbeat_groove", offbeat_groove)
    render("measure_J_deep_answer", deep_answer)
    render("measure_K_sparse", sparse)
    render("measure_L_war_party", war_party)
    render("measure_A_plain_counts", plain)
    render("measure_B_build_up", build_up)
    render("measure_C_phrases_with_fill", phrases)
    render("measure_D_grace_notes", grace_notes)
