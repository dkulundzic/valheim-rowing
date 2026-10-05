using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace RowingMod
{
    /// <summary>
    /// Stats for one stint at an oar (from sitting down to standing up): how far the ship went, how many strokes,
    /// how many were on the beat, in sync with others or clashing. When the rower stands up, a short summary appears
    /// in the top-left messages, and the distance and strokes are added to lifetime totals kept with the character.
    /// </summary>
    public class VoyageStats
    {
        // A stroke's sync or clash is settled this long after it, once the crew's strokes for that beat have arrived.
        private const float SettleSeconds = 2f;
        private const string TotalMetersKey = "RowingMod_TotalMeters";
        private const string TotalStrokesKey = "RowingMod_TotalStrokes";

        private ShipRowing m_rowing;
        private Transform m_ship;
        private Vector3 m_lastPosition;
        private float m_startTime;
        private float m_meters;
        private int m_strokes;
        private int m_onBeat;
        private int m_inSync;
        private int m_clashes;
        private readonly Queue<(long Beat, bool Strong, float Time)> m_pending = new Queue<(long, bool, float)>();

        public void Start(Ship ship, ShipRowing rowing)
        {
            m_rowing = rowing;
            m_ship = ship.transform;
            m_lastPosition = m_ship.position;
            m_startTime = Time.time;
            m_meters = 0f;
            m_strokes = m_onBeat = m_inSync = m_clashes = 0;
            m_pending.Clear();
        }

        /// <summary>Called every frame while seated: adds the ship's horizontal movement and settles old strokes.</summary>
        public void Update()
        {
            if (m_ship == null)
            {
                return;
            }
            Vector3 position = m_ship.position;
            Vector3 moved = position - m_lastPosition;
            moved.y = 0f;
            // Ignore jumps (teleporting, a ship loading in), which aren't rowing.
            if (moved.magnitude < 20f)
            {
                m_meters += moved.magnitude;
            }
            m_lastPosition = position;
            Settle(Time.time - SettleSeconds);
        }

        public void OnStroke(long beatMs, bool strong)
        {
            m_strokes++;
            if (strong)
            {
                m_onBeat++;
            }
            m_pending.Enqueue((beatMs, strong, Time.time));
        }

        /// <summary>
        /// Called when the rower stands up: adds this stint to the lifetime totals and returns the summary to show,
        /// or false when there's nothing to show (no strokes, or UI.ShowVoyageSummary off).
        /// </summary>
        public bool Finish(Player player, out string title, out string body)
        {
            title = null;
            body = null;
            if (m_ship == null)
            {
                return false;
            }
            Settle(float.MaxValue);
            m_ship = null;
            if (m_strokes == 0 || player == null || !RowingPlugin.ShowVoyageSummary.Value)
            {
                return false;
            }

            float totalMeters = GetFloat(player, TotalMetersKey) + m_meters;
            int totalStrokes = (int)GetFloat(player, TotalStrokesKey) + m_strokes;
            player.m_customData[TotalMetersKey] = totalMeters.ToString(CultureInfo.InvariantCulture);
            player.m_customData[TotalStrokesKey] = totalStrokes.ToString(CultureInfo.InvariantCulture);

            int seconds = Mathf.RoundToInt(Time.time - m_startTime);
            int onBeatPercent = Mathf.RoundToInt(100f * m_onBeat / m_strokes);
            title = $"Voyage: {Distance(m_meters)} in {seconds / 60}:{seconds % 60:00}";
            body = $"{m_strokes} strokes, {onBeatPercent}% on the beat, {m_inSync} in sync, {m_clashes} clashes.\n" +
                $"Lifetime: {Distance(totalMeters)}, {totalStrokes} strokes.";
            return true;
        }

        private void Settle(float before)
        {
            while (m_pending.Count > 0 && m_pending.Peek().Time <= before)
            {
                (long beat, bool strong, float _) = m_pending.Dequeue();
                int strongOnBeat = m_rowing != null ? m_rowing.GetStrongCount(beat) : 0;
                if (strong && strongOnBeat >= 2)
                {
                    m_inSync++;
                }
                else if (!strong && strongOnBeat >= 1)
                {
                    m_clashes++;
                }
            }
        }

        private static float GetFloat(Player player, string key)
        {
            return player.m_customData.TryGetValue(key, out string text)
                && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : 0f;
        }

        private static string Distance(float meters)
        {
            return meters >= 1000f ? $"{meters / 1000f:0.0} km" : $"{meters:0} m";
        }
    }
}
