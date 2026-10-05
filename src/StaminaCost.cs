using System.Collections.Generic;
using UnityEngine;

namespace RowingMod
{
    /// <summary>
    /// What a stroke, or a second of braking, costs in stamina. Every modifier goes through one chain, in this order:
    ///
    ///   cost = base × (1 + load) × relief
    ///
    /// 1. Base: Stamina.StaminaPerStroke for a stroke, Brake.StaminaPerSecond for braking.
    /// 2. Load: conditions that make pulling harder (a headwind). Their extra costs are added, not multiplied, and
    ///    capped at Stamina.MaxLoad, so several bad conditions together stay predictable.
    /// 3. Relief: the rower's own condition and practice (Rested, the Rowing skill). Each takes its share off what's
    ///    left, so discounts multiply and can never bring the cost to zero.
    /// </summary>
    public static class StaminaCost
    {
        public enum Use
        {
            Stroke,
            Brake,
        }

        /// <summary>One modifier in the chain: +0.71 is 71% more stamina, -0.1 is 10% less.</summary>
        public struct Part
        {
            public string Name;
            public float Amount;
        }

        // Reused by Describe, so the stroke bar doesn't allocate a list every frame.
        private static readonly List<Part> s_parts = new List<Part>();

        /// <summary>The multiplier on the base cost for this rower, ship and use; fills <paramref name="parts"/> if given.</summary>
        public static float Multiplier(Player player, Ship ship, Use use, List<Part> parts = null)
        {
            parts?.Clear();

            // Load: conditions that make pulling harder, added together and capped.
            float load = 0f;
            if (use == Use.Stroke)
            {
                load += Add(parts, "headwind", Headwind(ship));
            }
            load = Mathf.Min(load, Mathf.Max(0f, RowingPlugin.MaxLoad.Value));

            // Relief: the rower's condition and practice, each taking its share off what's left.
            float relief = 1f;
            relief *= 1f - Add(parts, "rested", -Rested(player));
            relief *= 1f - Add(parts, "Rowing skill", -RowingSkill.StaminaRelief(player));

            return (1f + load) * relief;
        }

        /// <summary>
        /// The stroke bar's stamina line, e.g. "Stamina ×1.54 (headwind +71%, rested -10%, Rowing skill -10%)", or null
        /// when nothing changes the cost noticeably.
        /// </summary>
        public static string Describe(Player player, Ship ship)
        {
            float multiplier = Multiplier(player, ship, Use.Stroke, s_parts);
            if (Mathf.Abs(multiplier - 1f) < 0.05f)
            {
                return null;
            }
            System.Text.StringBuilder text = new System.Text.StringBuilder($"Stamina ×{multiplier:0.00} (");
            bool first = true;
            foreach (Part part in s_parts)
            {
                if (Mathf.Abs(part.Amount) < 0.05f)
                {
                    continue;
                }
                text.Append(first ? "" : ", ").Append(part.Name).Append(part.Amount > 0f ? " +" : " -").Append((Mathf.Abs(part.Amount) * 100f).ToString("0")).Append('%');
                first = false;
            }
            return text.Append(')').ToString();
        }

        private static float Add(List<Part> parts, string name, float amount)
        {
            if (parts != null && Mathf.Abs(amount) > 0.0001f)
            {
                parts.Add(new Part { Name = name, Amount = amount });
            }
            return Mathf.Abs(amount);
        }

        /// <summary>
        /// Rowing into the wind: up to Stamina.HeadwindStaminaFactor more, straight into a full-strength wind. A
        /// tailwind costs no less than normal.
        /// </summary>
        private static float Headwind(Ship ship)
        {
            EnvMan env = EnvMan.instance;
            if (env == null || ship == null)
            {
                return 0f;
            }
            // GetWindDir is where the wind blows to, so rowing into it means the wind points against the rowing direction.
            Vector3 rowDir = ship.transform.forward * ShipRowing.RowDirection(ship);
            Vector3 wind = env.GetWindDir();
            wind.y = 0f;
            rowDir.y = 0f;
            float headwind = Mathf.Max(0f, Vector3.Dot(wind.normalized, -rowDir.normalized));
            return Mathf.Max(0f, RowingPlugin.HeadwindStaminaFactor.Value) * headwind * Mathf.Clamp01(env.GetWindIntensity());
        }

        /// <summary>Valheim's Rested buff (from sleeping or resting by a fire): Stamina.RestedDiscount less.</summary>
        private static float Rested(Player player)
        {
            SEMan seman = player != null ? player.GetSEMan() : null;
            bool rested = seman != null && seman.HaveStatusEffect(SEMan.s_statusEffectRested);
            return rested ? Mathf.Clamp01(RowingPlugin.RestedDiscount.Value) : 0f;
        }
    }
}
