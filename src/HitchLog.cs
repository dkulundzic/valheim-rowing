using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace RowingMod
{
    /// <summary>
    /// Debug.LogHitches: finds what freezes the game. The mod's main per-frame parts time themselves with
    /// <see cref="Begin"/>/<see cref="End"/>; when a frame takes longer than <see cref="Threshold"/>, this logs the frame's
    /// length and how much of it each part took. If the mod's parts add up to little, the freeze is the game's own.
    /// </summary>
    public class HitchLog : MonoBehaviour
    {
        private const float Threshold = 0.25f;

        private static readonly Dictionary<string, double> s_ms = new Dictionary<string, double>();
        private static readonly Stopwatch s_watch = Stopwatch.StartNew();

        public static bool Enabled => RowingPlugin.LogHitches.Value;

        /// <summary>Starts timing a part; pass the result to <see cref="End"/>.</summary>
        public static double Begin()
        {
            return Enabled ? s_watch.Elapsed.TotalMilliseconds : 0.0;
        }

        public static void End(string part, double started)
        {
            if (!Enabled)
            {
                return;
            }
            double elapsed = s_watch.Elapsed.TotalMilliseconds - started;
            s_ms[part] = (s_ms.TryGetValue(part, out double total) ? total : 0.0) + elapsed;
        }

        // Runs once a frame; Time.unscaledDeltaTime is how long the frame before this one took.
        private void Update()
        {
            if (!Enabled)
            {
                s_ms.Clear();
                return;
            }
            float frame = Time.unscaledDeltaTime;
            if (frame > Threshold)
            {
                double mod = 0.0;
                List<string> parts = new List<string>();
                foreach (KeyValuePair<string, double> part in s_ms)
                {
                    mod += part.Value;
                    if (part.Value >= 1.0)
                    {
                        parts.Add($"{part.Key} {part.Value:0} ms");
                    }
                }
                Player player = Player.m_localPlayer;
                string state = player == null ? "no player" : player.IsAttachedToShip() ? "seated on a ship" : player.IsAttached() ? "attached" : "standing";
                RowingPlugin.Log.LogInfo($"Hitch: a frame took {frame * 1000f:0} ms ({state}); the mod's code took {mod:0} ms of it" +
                    (parts.Count > 0 ? ": " + string.Join(", ", parts) : ""));
            }
            s_ms.Clear();
        }
    }
}
