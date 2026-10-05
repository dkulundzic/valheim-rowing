using HarmonyLib;
using UnityEngine;

namespace RowingMod
{
    /// <summary>
    /// A "Rowing" skill, raised by rowing like any other Valheim skill, shown in the skills screen, saved with the
    /// character and lowered on death like the rest. With level it makes rowing cheaper, easier to time and stronger.
    ///
    /// Valheim has no API for new skills, so the skill uses a SkillType number outside the game's enum and patches
    /// the three places that would reject it: validity (so it loads from saves), its definition (icon and
    /// description) and its name in the current language.
    /// </summary>
    public static class RowingSkill
    {
        public const string Name = "Rowing";
        private const string Description = "Pulling an oar in time with the crew. Higher levels widen the green zone and make rowing cheaper and stronger.";

        /// <summary>The skill's number: a stable hash of the mod's skill id, far from the game's own values.</summary>
        public static readonly Skills.SkillType Type = (Skills.SkillType)Mathf.Abs("com.dkulundzic.rowingmod.skill.rowing".GetStableHashCode());

        private static Skills.SkillDef s_def;

        public static Skills.SkillDef Def
        {
            get
            {
                if (s_def == null)
                {
                    s_def = new Skills.SkillDef
                    {
                        m_skill = Type,
                        m_icon = MakeIcon(),
                        m_description = Description,
                        m_increseStep = Mathf.Max(0.01f, RowingPlugin.SkillGain.Value),
                    };
                }
                return s_def;
            }
        }

        /// <summary>The local player's Rowing skill as 0..1 (level / 100).</summary>
        public static float Factor(Player player)
        {
            Skills skills = player != null ? player.GetSkills() : null;
            return skills != null ? skills.GetSkillFactor(Type) : 0f;
        }

        public static float StaminaMultiplier(Player player)
        {
            return 1f - Mathf.Clamp01(RowingPlugin.SkillStaminaReduction.Value) * Factor(player);
        }

        /// <summary>
        /// The green zone's width, as a fraction of the beat: narrow for a beginner (Skill.SweetSpotAtLevel0) and
        /// widening steadily with level to Skill.SweetSpotAtLevel100.
        /// </summary>
        public static float SweetSpotWidth(Player player)
        {
            return Mathf.Clamp01(Mathf.Lerp(RowingPlugin.SkillSweetSpotAtLevel0.Value, RowingPlugin.SkillSweetSpotAtLevel100.Value, Factor(player)));
        }

        public static float StrengthMultiplier(Player player)
        {
            return 1f + Mathf.Max(0f, RowingPlugin.SkillStrengthBonus.Value) * Factor(player);
        }

        /// <summary>Practice: a strong stroke counts fully, a weak one a little.</summary>
        public static void Practice(Player player, bool strong)
        {
            player?.RaiseSkill(Type, strong ? 1f : 0.3f);
        }

        /// <summary>The skill's name in every language (the game looks it up as "$skill_" + the number).</summary>
        public static void AddName(Localization localization)
        {
            if (localization != null)
            {
                Traverse.Create(localization).Method("AddWord", "skill_" + ((int)Type).ToString(), Name).GetValue();
            }
        }

        /// <summary>A simple icon made in code: an oar, shaft and blade, white on transparent.</summary>
        private static Sprite MakeIcon()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            Vector2 handle = new Vector2(12f, 52f);
            Vector2 bladeStart = new Vector2(38f, 26f);
            Vector2 tip = new Vector2(54f, 10f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float shaft = 3f - DistanceToSegment(p, handle, bladeStart);
                    float blade = 7.5f - DistanceToSegment(p, bladeStart, tip);
                    float grip = 4.5f - Vector2.Distance(p, handle);
                    float alpha = Mathf.Clamp01(Mathf.Max(shaft, Mathf.Max(blade, grip)));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
    }

    /// <summary>Lets the Rowing skill load from saves: the game only accepts SkillType values in its enum.</summary>
    [HarmonyPatch(typeof(Skills), "IsSkillValid")]
    internal static class Skills_IsSkillValid_Patch
    {
        private static void Postfix(Skills.SkillType type, ref bool __result)
        {
            if (type == RowingSkill.Type)
            {
                __result = true;
            }
        }
    }

    /// <summary>Gives the Rowing skill its definition (icon, description, how fast it rises).</summary>
    [HarmonyPatch(typeof(Skills), "GetSkillDef")]
    internal static class Skills_GetSkillDef_Patch
    {
        private static void Postfix(Skills.SkillType type, ref Skills.SkillDef __result)
        {
            if (__result == null && type == RowingSkill.Type)
            {
                __result = RowingSkill.Def;
            }
        }
    }

    /// <summary>Adds the skill's name whenever a language is (re)loaded.</summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    internal static class Localization_SetupLanguage_Patch
    {
        private static void Postfix(Localization __instance)
        {
            RowingSkill.AddName(__instance);
        }
    }
}
