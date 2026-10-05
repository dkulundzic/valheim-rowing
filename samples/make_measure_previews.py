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


if __name__ == "__main__":
    render("measure_A_plain_counts", plain)
    render("measure_B_build_up", build_up)
    render("measure_C_phrases_with_fill", phrases)
    render("measure_D_grace_notes", grace_notes)
