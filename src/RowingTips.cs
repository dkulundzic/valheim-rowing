using System.Collections.Generic;

namespace RowingMod
{
    /// <summary>
    /// Contextual tips: each explains one thing the moment it first matters (sitting down, the first stroke, another
    /// rower joining, the first clash...), once. The tips already shown are kept in Tutorial.SeenTips; with
    /// Tutorial.ResetOnLogout they're forgotten on logout, so a new session explains everything again.
    /// </summary>
    public static class RowingTips
    {
        public enum Tip
        {
            Beat,       // the first sit at an oar
            Panel,      // after the first stroke
            Together,   // another rower sits on a bench of this ship
            Clash,      // the first clash
            Brake,      // the ship first goes faster than BrakeSpeed
            Stamina,    // the stamina line above the bar first shows
            Tired,      // the first "Too tired to row"
        }

        /// <summary>The ship speed (m/s) at which braking is worth explaining.</summary>
        public const float BrakeSpeed = 3f;

        private static HashSet<Tip> s_seen;

        public static bool IsSeen(Tip tip)
        {
            Load();
            return s_seen.Contains(tip);
        }

        public static void MarkSeen(Tip tip)
        {
            Load();
            if (s_seen.Add(tip))
            {
                Save();
            }
        }

        /// <summary>Forgets every tip, so each shows again when it next matters.</summary>
        public static void ForgetAll()
        {
            Load();
            if (s_seen.Count > 0)
            {
                s_seen.Clear();
                Save();
            }
        }

        public static void Text(Tip tip, out string title, out string body)
        {
            string row = RowingPlugin.RowKey.Value.ToString();
            string brake = RowingPlugin.BrakeKey.Value.ToString();
            switch (tip)
            {
                case Tip.Beat:
                    title = "Tip: row on the beat";
                    body = $"Press {row} when the white marker crosses the green zone. That's the ship's beat; it speeds up as the ship does. Strong strokes train your Rowing skill, which widens the zone.";
                    return;
                case Tip.Panel:
                    title = "Tip: the crew panel";
                    body = "Bottom right: your ship from above, its rowers, the beat and the speed. Each stroke flashes on its bench: green strong, yellow weak, gold in sync, red a clash.";
                    return;
                case Tip.Together:
                    title = "Tip: row together";
                    body = "Hit the same beat as the other rowers for a sync bonus: the more of you on the beat, the stronger each stroke.";
                    return;
                case Tip.Clash:
                    title = "Tip: clash";
                    body = "An off-beat stroke while others hit the beat fights their rhythm and slows the ship. If you miss the green zone, skip that beat.";
                    return;
                case Tip.Brake:
                    title = "Tip: brake";
                    body = $"Hold {brake} to hold water and slow the ship. Braking on one side swings the bow toward that side.";
                    return;
                case Tip.Stamina:
                    title = "Tip: stamina";
                    body = "Headwind, storms and cold make strokes cost more; being Rested and your Rowing skill make them cheaper. The line above the bar shows the cost and why.";
                    return;
                default:
                    title = "Tip: out of breath";
                    body = "Every stroke costs stamina. Rest a few beats to recover; good food and the Rested buff help you row longer.";
                    return;
            }
        }

        private static void Load()
        {
            if (s_seen != null)
            {
                return;
            }
            s_seen = new HashSet<Tip>();
            foreach (string name in RowingPlugin.TutorialSeenTips.Value.Split(','))
            {
                if (System.Enum.TryParse(name.Trim(), out Tip tip))
                {
                    s_seen.Add(tip);
                }
            }
        }

        private static void Save()
        {
            List<string> names = new List<string>();
            foreach (Tip tip in s_seen)
            {
                names.Add(tip.ToString());
            }
            RowingPlugin.TutorialSeenTips.Value = string.Join(",", names);
        }
    }
}
