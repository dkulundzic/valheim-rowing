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

        /// <summary>How much less stamina rowing costs at this level (0..Skill.StaminaReduction); see StaminaCost.</summary>
        public static float StaminaRelief(Player player)
        {
            return Mathf.Clamp01(RowingPlugin.SkillStaminaReduction.Value) * Factor(player);
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

        /// <summary>
        /// The skill's icon, made in code: a wooden oar (grip, shaft and blade) with grain running along it, rounded
        /// shading, a leather-wrapped grip and a dark outline, so it reads on the skills screen like the game's own.
        /// </summary>
        private static Sprite MakeIcon()
        {
            const int size = 128;
            const float k = size / 64f;
            Vector2 handle = new Vector2(12f, 52f) * k;
            Vector2 bladeStart = new Vector2(38f, 26f) * k;
            Vector2 tip = new Vector2(54f, 10f) * k;
            float shaftRadius = 3f * k;
            float bladeRadius = 7.5f * k;
            float gripRadius = 4.5f * k;
            Vector2 along = (tip - handle).normalized;
            Vector2 across = new Vector2(-along.y, along.x);
            Color light = new Color(0.80f, 0.58f, 0.34f);
            Color dark = new Color(0.47f, 0.29f, 0.15f);
            Color outline = new Color(0.16f, 0.09f, 0.04f);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float shaft = DistanceToSegment(p, handle, bladeStart) - shaftRadius;
                    float blade = DistanceToSegment(p, bladeStart, tip) - bladeRadius;
                    float grip = Vector2.Distance(p, handle) - gripRadius;
                    float edge = Mathf.Min(shaft, Mathf.Min(blade, grip)); // < 0 inside the oar
                    float alpha = Mathf.Clamp01(0.5f - edge);
                    if (alpha <= 0f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    // Grain: stripes across the width, wavering slowly along the length, with fine fibres.
                    float u = Vector2.Dot(p - handle, along);
                    float v = Vector2.Dot(p - handle, across);
                    float warp = Mathf.PerlinNoise(u * 0.06f, v * 0.06f + 10f) * 3f + Mathf.PerlinNoise(u * 0.02f + 20f, v * 0.3f) * 2f;
                    float grain = Mathf.Pow(Mathf.Sin(v * 1.1f + warp * 2.2f) * 0.5f + 0.5f, 1.5f);
                    float fibres = Mathf.PerlinNoise(u * 0.5f + 30f, v * 2f);
                    Color color = Color.Lerp(dark, light, Mathf.Clamp01(grain * 0.7f + fibres * 0.3f));

                    // Rounded: brighter in the middle of each part, darker toward its edge.
                    float radius = blade < Mathf.Min(shaft, grip) ? bladeRadius : grip < shaft ? gripRadius : shaftRadius;
                    color *= 0.62f + 0.38f * Mathf.Sqrt(Mathf.Clamp01(-edge / radius));

                    // A leather wrap on the grip: darker bands.
                    if (u < 9f * k && Mathf.Sin(u * 1.6f) > 0.2f)
                    {
                        color = color * 0.55f + new Color(0.10f, 0.05f, 0.02f);
                    }

                    // A dark outline, so the oar stands out on any background.
                    color = Color.Lerp(color, outline, Mathf.Clamp01((edge + 1.6f * k) / (1.2f * k)));
                    color.a = alpha;
                    pixels[y * size + x] = color;
                }
            }
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels(pixels);
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
