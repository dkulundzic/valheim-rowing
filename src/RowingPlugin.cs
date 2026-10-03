using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RowingMod
{
    [BepInPlugin(Guid, Name, Version)]
    public class RowingPlugin : BaseUnityPlugin
    {
        public const string Guid = "com.dkulundzic.rowingmod";
        public const string Name = "Rowing";
        public const string Version = "1.0.1";

        internal static ManualLogSource Log;

        internal static ConfigEntry<KeyCode> RowKey;
        internal static ConfigEntry<float> StrokeCycle;
        internal static ConfigEntry<float> SweetSpotWidth;
        internal static ConfigEntry<float> WeakStrokeFactor;
        internal static ConfigEntry<float> StaminaPerStroke;
        internal static ConfigEntry<float> HeadwindStaminaFactor;
        internal static ConfigEntry<float> StrokeStrength;
        internal static ConfigEntry<float> MaxBoost;
        internal static ConfigEntry<float> StrokeFade;
        internal static ConfigEntry<float> TopSpeedMultiplier;
        internal static ConfigEntry<float> BarOffset;

        private void Awake()
        {
            Log = Logger;

            RowKey = Config.Bind("Controls", "RowKey", KeyCode.H,
                "Key a seated passenger presses to make a stroke. Movement, attack, jump and crouch keys stand you up, so don't use those.");

            StrokeCycle = Config.Bind("Timing", "StrokeCycle", 1.5f,
                "Seconds from one stroke to the middle of the next sweet spot.");
            SweetSpotWidth = Config.Bind("Timing", "SweetSpotWidth", 0.2f,
                "Width of the sweet spot as a fraction of the stroke cycle.");
            WeakStrokeFactor = Config.Bind("Timing", "WeakStrokeFactor", 0.35f,
                "Strength of an early or late stroke compared with a well-timed one.");

            StaminaPerStroke = Config.Bind("Stamina", "StaminaPerStroke", 6f,
                "Stamina each stroke costs, including strokes that come too fast to count.");
            HeadwindStaminaFactor = Config.Bind("Stamina", "HeadwindStaminaFactor", 1f,
                "Extra stamina cost when rowing into the wind, as a fraction of StaminaPerStroke. 1 means up to double straight into a full-strength wind; it scales with the wind's strength and angle. 0 turns it off.");

            StrokeStrength = Config.Bind("Force", "StrokeStrength", 0.6f,
                "Boost one well-timed stroke adds, as a fraction of the ship's own paddle force.");
            MaxBoost = Config.Bind("Force", "MaxBoost", 2f,
                "Most boost the whole crew can build up, as a multiple of the ship's paddle force.");
            StrokeFade = Config.Bind("Force", "StrokeFade", 1.2f,
                "Seconds for a stroke's push to fade out.");
            TopSpeedMultiplier = Config.Bind("Force", "TopSpeedMultiplier", 1f,
                "Rowing can't push a ship past its top sail speed (full sail, best wind) times this. Strokes weaken as the ship nears it.");

            BarOffset = Config.Bind("UI", "BarOffset", 0f,
                "Extra pixels to raise the stroke bar above the stamina bar. Negative values lower it.");

            gameObject.AddComponent<Rower>();
            new Harmony(Guid).PatchAll();
            Log.LogInfo($"{Name} {Version} loaded");
        }
    }
}
