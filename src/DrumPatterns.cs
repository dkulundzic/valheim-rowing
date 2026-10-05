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
        }

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

            /// <summary>A hit. Unless exact, the hand varies a little in timing (±6 ms), strength and pitch.</summary>
            public void Add(Drum drum, float fraction, float gain, float pitch = 1f, bool exact = false)
            {
                if (gain <= 0f)
                {
                    return;
                }
                if (!exact)
                {
                    fraction += Gaussian(0.006f) / Period;
                    gain *= Range(0.88f, 1f);
                    pitch *= Range(0.97f, 1.03f);
                }
                Hits.Add(new Hit { Fraction = Mathf.Max(0f, fraction), Drum = drum, Gain = gain, Pitch = pitch });
            }

            /// <summary>ONE: the deep drum, a lower one under it for weight, and the middle drum for attack. Exact.</summary>
            public void Stroke()
            {
                Add(Drum.Kick, 0f, 1f, exact: true);
                Add(Drum.Kick, 0f, 0.5f, 0.82f, exact: true);
                Add(Drum.Mid, 0f, 0.32f, exact: true);
            }

            public void Kick(float fraction, float gain, float pitch = 1f) => Add(Drum.Kick, fraction, gain, pitch);
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
            Measure m = new Measure
            {
                Index = index,
                Period = Mathf.Max(0.1f, period),
                Random = new System.Random(unchecked((int)(seed ^ (seed >> 32)) * 31 + pattern)),
            };
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
    }
}
