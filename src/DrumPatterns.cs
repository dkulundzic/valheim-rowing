using System.Collections.Generic;
using UnityEngine;

namespace RowingMod
{
    /// <summary>
    /// The war drum's 16 patterns, all built on the "battle march". One measure is one stroke: ONE (the stroke, at the
    /// start of each of the ship's beats) is a heavy, doubled deep drum, and the rest of the measure counts in to the
    /// next stroke. The helmsman picks the pattern.
    ///
    /// A pattern turns one measure into a list of hits at fractions of the measure. The "human hand" (slight timing,
    /// strength and pitch variation on everything but ONE) comes from a random generator seeded with the beat's time,
    /// so every client hears exactly the same hits. These mirror samples/make_measure_previews.py.
    ///
    /// Ramming speed has its own rhythms (8, at a 0.8 s beat, with kicks cut short so they don't pile up), and the
    /// drum bridges into and out of it with a lead-in (3: a short build, a double kick and a stop) and a release
    /// (3: a big hit left to ring). These mirror samples/make_ramming_previews.py and make_transition_previews.py.
    /// </summary>
    public static class DrumPatterns
    {
        public enum Drum
        {
            Kick,   // deep dundunba, with a little room
            Mid,    // sangban
            Tap,    // muted sangban: the pulse
            High,   // kenkeni
        }

        public struct Hit
        {
            public float Fraction;
            public Drum Drum;
            public float Gain;
            public float Pitch;
            // Seconds of the clip to play (cut short with a quick fade), or 0 for all of it.
            public float Length;
        }

        /// <summary>What a measure plays: one of the helmsman's rhythms, or part of a ramming-speed run.</summary>
        public enum Kind
        {
            Normal,
            LeadIn,
            Ramming,
            Release,
        }

        public static readonly string[] RammingNames =
        {
            "Pound", "Hammer", "Backbeat", "Stampede", "Triplet charge", "Thunder roll", "War call", "Berserker",
        };

        public static readonly string[] LeadInNames = { "Double kick", "Call", "Gather" };

        public static readonly string[] ReleaseNames = { "Crash", "Settle", "Echo" };

        // During ramming, kicks are cut to this many seconds and the stroke's kick to StrokeKick, so their ring
        // doesn't pile up at a 0.8 s beat. The lead-in's double kick is cut even shorter, so the stop is silent.
        private const float ShortKick = 0.3f;
        private const float StrokeKick = 0.5f;
        private const float DoubleKick = 0.24f;

        public static readonly string[] Names =
        {
            "Battle march", "Kick on three", "Charge", "Heartbeat", "Sixteenths", "Stomp", "Gallop", "Crescendo",
            "Tension", "Kick roll", "Drum roll", "Sub", "Syncopation", "Four on the floor", "Call and response",
            "War party march",
        };

        public static int Count => Names.Length;

        /// <summary>One measure being built: its number, length and the hits so far.</summary>
        private class Measure
        {
            public int Index;
            public float Period;
            public System.Random Random;
            // Timing variation of the hand (seconds), and how long kicks ring (0 = in full).
            public float Jitter = 0.006f;
            public float KickLength;
            public readonly List<Hit> Hits = new List<Hit>();

            public bool Fourth => Index % 4 == 3;
            public bool Roomy => Period >= 1.4f;

            private float Range(float min, float max)
            {
                return min + (float)Random.NextDouble() * (max - min);
            }

            private float Gaussian(float sigma)
            {
                double u1 = 1.0 - Random.NextDouble();
                double u2 = Random.NextDouble();
                return (float)(System.Math.Sqrt(-2.0 * System.Math.Log(u1)) * System.Math.Sin(2.0 * System.Math.PI * u2)) * sigma;
            }

            /// <summary>
            /// A hit. Unless exact, the hand varies a little in timing (Jitter), strength and pitch. With a
            /// <paramref name="length"/>, it rings for about that many seconds and is cut short.
            /// </summary>
            public void Add(Drum drum, float fraction, float gain, float pitch = 1f, bool exact = false, float length = 0f)
            {
                if (gain <= 0f)
                {
                    return;
                }
                if (!exact)
                {
                    fraction += Gaussian(Jitter) / Period;
                    gain *= Range(0.88f, 1f);
                    pitch *= Range(0.97f, 1.03f);
                }
                // A clip pitched down plays slower, so cut more of it for the same ring (as the previews do).
                float clipLength = length > 0f ? length * pitch * pitch : 0f;
                Hits.Add(new Hit { Fraction = Mathf.Max(0f, fraction), Drum = drum, Gain = gain, Pitch = pitch, Length = clipLength });
            }

            /// <summary>ONE: the deep drum, a lower one under it for weight, and the middle drum for attack. Exact.</summary>
            public void Stroke()
            {
                Add(Drum.Kick, 0f, 1f, exact: true);
                Add(Drum.Kick, 0f, 0.5f, 0.82f, exact: true);
                Add(Drum.Mid, 0f, 0.32f, exact: true);
            }

            /// <summary>Ramming's ONE: the doubled deep drum cut at half a second, and a harder middle drum.</summary>
            public void ShortStroke()
            {
                Add(Drum.Kick, 0f, 1f, exact: true, length: StrokeKick);
                Add(Drum.Kick, 0f, 0.5f, 0.82f, exact: true, length: StrokeKick);
                Add(Drum.Mid, 0f, 0.36f, exact: true);
            }

            /// <summary>The release's big hit: every drum at once, left to ring.</summary>
            public void BigHit()
            {
                Add(Drum.Kick, 0f, 1f, exact: true);
                Add(Drum.Kick, 0f, 0.6f, 0.82f, exact: true);
                Add(Drum.Mid, 0f, 0.5f, exact: true);
                Add(Drum.High, 0f, 0.34f, exact: true);
            }

            /// <summary>The lead-in's end: two kicks an eighth apart, damped short, then silence.</summary>
            public void DoubleKickAndStop(float first)
            {
                for (int k = 0; k < 2; k++)
                {
                    Add(Drum.Kick, first + k / 8f, 0.62f + 0.08f * k, 0.97f - 0.03f * k, exact: true, length: DoubleKick);
                }
            }

            /// <summary>Taps on every eighth after ONE: <paramref name="even"/> on the counts, <paramref name="odd"/> between.</summary>
            public void Eighths(float even, float odd)
            {
                for (int step = 1; step < 8; step++)
                {
                    Tap(step / 8f, step % 2 == 0 ? even : odd);
                }
            }

            public void Kick(float fraction, float gain, float pitch = 1f) => Add(Drum.Kick, fraction, gain, pitch, length: KickLength);
            public void Tap(float fraction, float gain) => Add(Drum.Tap, fraction, gain);
            public void Mid(float fraction, float gain) => Add(Drum.Mid, fraction, gain);
            public void High(float fraction, float gain) => Add(Drum.High, fraction, gain);

            /// <summary>The battle march's driving pulse: dry taps on every step after ONE.</summary>
            public void Pulse(int steps = 8, float accent = 0.2f, float weak = 0.14f)
            {
                for (int step = 1; step < steps; step++)
                {
                    Tap(step / (float)steps, step % 2 == 0 ? accent : weak);
                }
            }

            public void Pulse3(int steps, float a, float b, float c)
            {
                float[] gains = { a, b, c };
                for (int step = 1; step < steps; step++)
                {
                    Tap(step / (float)steps, gains[step % 3]);
                }
            }
        }

        /// <summary>
        /// The hits of one measure: <paramref name="pattern"/> (0..Count-1), the measure's number on the ship
        /// (<paramref name="index"/>, for the every-fourth-measure fills), its length in seconds, and a seed shared
        /// by all clients (the beat's time).
        /// </summary>
        public static List<Hit> Build(int pattern, int index, float period, long seed)
        {
            return Build(Kind.Normal, pattern, index, period, seed);
        }

        /// <summary>
        /// The hits of one measure of the given <paramref name="kind"/>: a helmsman's rhythm (0..Count-1), a lead-in
        /// (0..2), a ramming rhythm (0..7) or a release (0..2).
        /// </summary>
        public static List<Hit> Build(Kind kind, int variant, int index, float period, long seed)
        {
            Measure m = new Measure
            {
                Index = index,
                Period = Mathf.Max(0.1f, period),
                Random = new System.Random(unchecked((int)(seed ^ (seed >> 32)) * 31 + (int)kind * 101 + variant)),
            };
            switch (kind)
            {
                case Kind.LeadIn:
                    m.KickLength = ShortKick;
                    LeadIn(m, variant);
                    return m.Hits;
                case Kind.Ramming:
                    m.KickLength = ShortKick;
                    m.Jitter = 0.004f;
                    Ramming(m, variant);
                    return m.Hits;
                case Kind.Release:
                    Release(m, variant);
                    return m.Hits;
            }
            int pattern = variant;
            switch (((pattern % Count) + Count) % Count)
            {
                case 0: BattleMarch(m); break;
                case 1: BattleMarch(m); m.Kick(2 / 4f, 0.36f, 0.93f); break;
                case 2: Charge(m); break;
                case 3: Heartbeat(m); break;
                case 4: Sixteenths(m); break;
                case 5: Stomp(m); break;
                case 6: Gallop(m); break;
                case 7: Crescendo(m); break;
                case 8: Tension(m); break;
                case 9: KickRoll(m); break;
                case 10: DrumRoll(m); break;
                case 11: Sub(m); break;
                case 12: Syncopation(m); break;
                case 13: FourOnTheFloor(m); break;
                case 14: CallAndResponse(m); break;
                default: WarPartyMarch(m); break;
            }
            return m.Hits;
        }

        // 1: the stroke, the eighth-note pulse, the middle drum on three-and, the high drum on four, and a pickup on
        // four-and every fourth measure.
        private static void BattleMarch(Measure m)
        {
            m.Stroke();
            m.Pulse();
            m.Mid(5 / 8f, 0.3f);
            m.High(3 / 4f, 0.28f);
            if (m.Fourth)
            {
                m.Mid(7 / 8f, 0.44f);
            }
        }

        // 3: a kick on four-and, charging into the next stroke.
        private static void Charge(Measure m)
        {
            m.Stroke();
            m.Pulse();
            m.Mid(5 / 8f, 0.3f);
            m.High(3 / 4f, 0.26f);
            m.Kick(7 / 8f, 0.34f, 0.95f);
        }

        // 4: a softer kick just after the stroke (BOOM-boom), then the pulse from the "and" of two.
        private static void Heartbeat(Measure m)
        {
            m.Stroke();
            m.Kick(Mathf.Min(0.22f, m.Period * 0.16f) / m.Period, 0.46f, 0.93f);
            for (int step = 3; step < 8; step++)
            {
                m.Tap(step / 8f, step % 2 == 1 ? 0.15f : 0.2f);
            }
            m.High(3 / 4f, 0.26f);
        }

        // 5: the pulse doubles to sixteenths with the eighths accented; eighths again at the fastest beat.
        private static void Sixteenths(Measure m)
        {
            m.Stroke();
            if (m.Roomy)
            {
                m.Pulse(16, 0.19f, 0.1f);
            }
            else
            {
                m.Pulse();
            }
            m.Mid(5 / 8f, 0.3f);
            m.High(3 / 4f, 0.28f);
            if (m.Fourth)
            {
                m.Mid(7 / 8f, 0.44f);
            }
        }

        // 6: the pulse accents the "and"s, pushing against the beat like stamping feet.
        private static void Stomp(Measure m)
        {
            m.Stroke();
            m.Pulse(8, 0.11f, 0.24f);
            m.Mid(3 / 8f, 0.28f);
            m.Mid(7 / 8f, 0.38f);
            m.High(2 / 4f, 0.24f);
        }

        // 7: the pulse in triplets, six per measure, for a rolling charge.
        private static void Gallop(Measure m)
        {
            m.Stroke();
            m.Pulse3(6, 0.2f, 0.13f, 0.15f);
            m.High(2 / 6f, 0.24f);
            m.Mid(4 / 6f, 0.32f);
            if (m.Fourth)
            {
                m.Mid(5 / 6f, 0.44f);
            }
        }

        // 8: the pulse swells through each measure: quiet after the stroke, loud right before the next.
        private static void Crescendo(Measure m)
        {
            m.Stroke();
            for (int step = 1; step < 8; step++)
            {
                m.Tap(step / 8f, 0.07f + 0.03f * step);
            }
            m.High(3 / 4f, 0.28f);
            m.Mid(7 / 8f, m.Fourth ? 0.46f : 0.32f);
        }

        // 9: silence after the stroke, then the pulse starts halfway and drives into the next ONE.
        private static void Tension(Measure m)
        {
            m.Stroke();
            for (int step = 4; step < 8; step++)
            {
                m.Tap(step / 8f, 0.15f + 0.03f * (step - 4));
            }
            m.High(2 / 4f, 0.22f);
            m.Mid(3 / 4f, 0.34f);
            if (m.Fourth)
            {
                m.Mid(7 / 8f, 0.46f);
            }
        }

        // 10: the battle march, and every fourth measure three kicks rising into the next stroke.
        private static void KickRoll(Measure m)
        {
            m.Stroke();
            m.Pulse();
            m.High(2 / 4f, 0.22f);
            if (m.Fourth)
            {
                float[] gains = { 0.28f, 0.38f, 0.5f };
                for (int k = 0; k < 3; k++)
                {
                    m.Kick(3 / 4f + k / 12f, gains[k], 1f + 0.03f * k);
                }
            }
            else
            {
                m.Mid(5 / 8f, 0.3f);
                m.High(3 / 4f, 0.26f);
            }
        }

        // 11: high-drum counts, and a middle-drum roll into the stroke every fourth measure.
        private static void DrumRoll(Measure m)
        {
            m.Stroke();
            m.Pulse();
            m.High(1 / 4f, 0.2f);
            m.High(2 / 4f, 0.26f);
            if (m.Fourth)
            {
                float[] gains = { 0.26f, 0.34f, 0.46f };
                for (int k = 0; k < 3; k++)
                {
                    m.Mid(3 / 4f + k / 12f, gains[k]);
                }
            }
            else
            {
                m.Mid(5 / 8f, 0.3f);
                m.High(3 / 4f, 0.28f);
            }
        }

        // 12: an extra-low kick under the stroke, a sparser pulse: deep and wide.
        private static void Sub(Measure m)
        {
            m.Stroke();
            m.Add(Drum.Kick, 0f, 0.45f, 0.7f, exact: true);
            m.Pulse(8, 0.16f, 0f);
            m.Mid(5 / 8f, 0.26f);
            m.High(3 / 4f, 0.26f);
        }

        // 13: kicks on two-and and three-and roll under the pulse.
        private static void Syncopation(Measure m)
        {
            m.Stroke();
            m.Pulse(8, 0.15f, 0.1f);
            m.Kick(3 / 8f, 0.32f, 0.97f);
            m.Kick(5 / 8f, 0.36f, 0.95f);
            m.High(3 / 4f, 0.24f);
        }

        // 14: a kick on every count, the stroke far deeper and stronger, the pulse on the "and"s.
        private static void FourOnTheFloor(Measure m)
        {
            m.Stroke();
            for (int count = 1; count < 4; count++)
            {
                m.Kick(count / 4f, 0.3f, 1.04f);
            }
            for (int eighth = 1; eighth < 8; eighth += 2)
            {
                m.Tap(eighth / 8f, 0.16f);
            }
        }

        // 15: measures alternate the high drum and the middle drum on the counts, over the pulse.
        private static void CallAndResponse(Measure m)
        {
            m.Stroke();
            m.Pulse();
            if (m.Index % 2 == 0)
            {
                m.High(2 / 4f, 0.26f);
                m.High(3 / 4f, 0.3f);
            }
            else
            {
                m.Mid(2 / 4f, 0.3f);
                m.Mid(3 / 4f, 0.36f);
            }
            if (m.Fourth)
            {
                m.Mid(7 / 8f, 0.46f);
            }
        }

        // 16: a soft kick on three, high-drum counts, and a roll into the stroke every fourth measure.
        private static void WarPartyMarch(Measure m)
        {
            m.Stroke();
            m.Pulse();
            m.Kick(2 / 4f, 0.3f, 0.93f);
            m.High(1 / 4f, 0.2f);
            if (m.Fourth)
            {
                float[] gains = { 0.26f, 0.34f, 0.46f };
                for (int k = 0; k < 3; k++)
                {
                    m.Mid(3 / 4f + k / 12f, gains[k]);
                }
            }
            else
            {
                m.Mid(5 / 8f, 0.3f);
                m.High(3 / 4f, 0.28f);
            }
        }

        // Lead-ins: the stroke and a short build, then a double kick and a stop: silence until ramming's first stroke.
        private static void LeadIn(Measure m, int variant)
        {
            m.Stroke();
            switch (((variant % 3) + 3) % 3)
            {
                case 0:
                    // Double kick: eighth-note taps swell through the first half, then the double kick on three-and.
                    for (int step = 1; step < 5; step++)
                    {
                        m.Tap(step / 8f, 0.12f + 0.04f * step);
                    }
                    m.DoubleKickAndStop(5 / 8f);
                    break;
                case 1:
                    // Call: the middle and high drums call on two and its "and", the double kick early on three.
                    m.Mid(1 / 4f, 0.36f);
                    m.High(3 / 8f, 0.32f);
                    m.DoubleKickAndStop(4 / 8f);
                    break;
                default:
                    // Gather: a soft kick on two, the middle drum on two-and and three, then the double kick with the
                    // high drum on its second hit.
                    m.Kick(1 / 4f, 0.32f, 1.02f);
                    m.Mid(3 / 8f, 0.3f);
                    m.Mid(4 / 8f, 0.36f);
                    m.DoubleKickAndStop(5 / 8f);
                    m.Add(Drum.High, 6 / 8f, 0.34f, exact: true);
                    break;
            }
        }

        // Ramming rhythms, for a 0.8 s beat.
        private static void Ramming(Measure m, int variant)
        {
            m.ShortStroke();
            switch (((variant % 8) + 8) % 8)
            {
                case 0:
                    // Pound: a short kick on every count, four hammer blows per stroke.
                    for (int count = 1; count < 4; count++)
                    {
                        m.Kick(count / 4f, 0.42f, 1.04f);
                    }
                    for (int eighth = 1; eighth < 8; eighth += 2)
                    {
                        m.Tap(eighth / 8f, 0.18f);
                    }
                    break;
                case 1:
                    // Hammer: the middle drum on two and four, the high drum on three, over the eighths.
                    m.Eighths(0.22f, 0.16f);
                    m.Mid(1 / 4f, 0.34f);
                    m.High(2 / 4f, 0.3f);
                    m.Mid(3 / 4f, 0.38f);
                    break;
                case 2:
                    // Backbeat: a kick on three answers the stroke; the high drum snaps on two and four.
                    m.Eighths(0.18f, 0.13f);
                    m.Kick(2 / 4f, 0.5f, 0.96f);
                    m.High(1 / 4f, 0.34f);
                    m.High(3 / 4f, 0.36f);
                    break;
                case 3:
                    // Stampede: kicks gallop on two-and, three and four-and.
                    m.Kick(3 / 8f, 0.36f, 1.02f);
                    m.Kick(4 / 8f, 0.44f, 0.98f);
                    m.Kick(7 / 8f, 0.4f);
                    m.Tap(1 / 8f, 0.16f);
                    m.Tap(2 / 8f, 0.16f);
                    m.Tap(5 / 8f, 0.16f);
                    m.Tap(6 / 8f, 0.16f);
                    m.High(3 / 4f, 0.28f);
                    break;
                case 4:
                    // Triplet charge: the pulse in triplets, a middle drum on the last, a kick pickup into the stroke.
                    for (int step = 1; step < 6; step++)
                    {
                        m.Tap(step / 6f, step % 2 == 0 ? 0.2f : 0.15f);
                    }
                    m.Mid(4 / 6f, 0.34f);
                    m.Kick(5 / 6f, 0.36f, 1.03f);
                    break;
                case 5:
                    // Thunder roll: a middle-drum roll swells through every measure into the next stroke.
                    for (int step = 2; step < 8; step++)
                    {
                        m.Mid(step / 8f, 0.14f + 0.05f * (step - 2));
                    }
                    m.High(1 / 4f, 0.26f);
                    break;
                case 6:
                    // War call: measures alternate kicks on the counts and the high and middle drums calling back.
                    m.Eighths(0.17f, 0.12f);
                    if (m.Index % 2 == 0)
                    {
                        m.Kick(1 / 4f, 0.38f, 1.04f);
                        m.Kick(2 / 4f, 0.4f);
                        m.Kick(3 / 4f, 0.42f, 0.97f);
                    }
                    else
                    {
                        m.High(1 / 4f, 0.32f);
                        m.Mid(2 / 4f, 0.36f);
                        m.High(3 / 4f, 0.32f);
                        m.Mid(7 / 8f, 0.42f);
                    }
                    break;
                default:
                    // Berserker: kicks, a high-drum backbeat, and a middle-drum fill every fourth measure.
                    m.Eighths(0.2f, 0.15f);
                    m.Kick(2 / 4f, 0.44f, 0.97f);
                    m.High(1 / 4f, 0.3f);
                    m.High(3 / 4f, 0.32f);
                    if (m.Fourth)
                    {
                        float[] gains = { 0.3f, 0.38f, 0.46f };
                        for (int k = 0; k < 3; k++)
                        {
                            m.Mid(5 / 8f + k / 8f, gains[k]);
                        }
                    }
                    else
                    {
                        m.Kick(7 / 8f, 0.34f, 1.03f);
                    }
                    break;
            }
        }

        // Releases: the first stroke after ramming, a big hit left to ring.
        private static void Release(Measure m, int variant)
        {
            m.BigHit();
            switch (((variant % 3) + 3) % 3)
            {
                case 1:
                    // Settle: two soft taps fading out.
                    m.Tap(2 / 4f, 0.14f);
                    m.Tap(3 / 4f, 0.1f);
                    break;
                case 2:
                    // Echo: a softer kick answering halfway, like a heartbeat calming down.
                    m.Kick(1 / 2f, 0.42f, 0.93f);
                    break;
            }
        }
    }
}
