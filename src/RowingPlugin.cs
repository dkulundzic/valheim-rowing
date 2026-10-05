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
        public const string Version = "1.2.0";

        internal static ManualLogSource Log;

        internal static ConfigEntry<KeyCode> RowKey;
        internal static ConfigEntry<float> StrokeCycleStill;
        internal static ConfigEntry<float> StrokeCycleTopSpeed;
        internal static ConfigEntry<float> SweetSpotWidth;
        internal static ConfigEntry<float> WeakStrokeFactor;
        internal static ConfigEntry<float> StaminaPerStroke;
        internal static ConfigEntry<float> HeadwindStaminaFactor;
        internal static ConfigEntry<float> StrokeStrength;
        internal static ConfigEntry<float> MaxBoost;
        internal static ConfigEntry<float> StrokeFade;
        internal static ConfigEntry<float> TopSpeedMultiplier;
        internal static ConfigEntry<float> SyncBonusPerRower;
        internal static ConfigEntry<float> MaxSyncBonus;
        internal static ConfigEntry<float> ClashBrake;
        internal static ConfigEntry<float> BarOffset;
        internal static ConfigEntry<KeyCode> BrakeKey;
        internal static ConfigEntry<float> BrakeStrength;
        internal static ConfigEntry<float> BrakeStaminaPerSecond;
        internal static ConfigEntry<bool> BrakeTurning;
        internal static ConfigEntry<bool> ShowOars;
        internal static ConfigEntry<float> UIScale;
        internal static ConfigEntry<bool> ShowCrewPanel;
        internal static ConfigEntry<bool> CrewNames;
        internal static ConfigEntry<string> SplashSound;
        internal static ConfigEntry<float> SplashVolume;
        internal static ConfigEntry<string> RunoffSound;
        internal static ConfigEntry<string> DripSound;
        internal static ConfigEntry<string> KnockSound;
        internal static ConfigEntry<string> SyncSound;
        internal static ConfigEntry<string> ClashSound;
        internal static ConfigEntry<string> CreakSound;
        internal static ConfigEntry<string> SplashEffect;
        internal static ConfigEntry<bool> ShowSplashes;
        internal static ConfigEntry<float> CreakVolume;
        internal static ConfigEntry<float> DrumVolume;
        internal static ConfigEntry<string> DrumSound;
        internal static ConfigEntry<KeyCode> DrumKey;
        internal static ConfigEntry<KeyCode> TempoUpKey;
        internal static ConfigEntry<KeyCode> TempoDownKey;
        internal static ConfigEntry<KeyCode> HoldWaterCallKey;
        internal static ConfigEntry<float> EasyTempoFactor;
        internal static ConfigEntry<float> HardTempoFactor;
        internal static ConfigEntry<float> RestedDiscount;
        internal static ConfigEntry<bool> ShowWakes;
        internal static ConfigEntry<string> WakeEffect;
        internal static ConfigEntry<bool> LogSoundCandidates;

        private void Awake()
        {
            Log = Logger;

            RowKey = Config.Bind("Controls", "RowKey", KeyCode.H,
                "Key a seated passenger presses to make a stroke. Movement, attack, jump and crouch keys stand you up, so don't use those.");

            DrumKey = Config.Bind("Controls", "DrumKey", KeyCode.H,
                "Key the helmsman presses to turn the ship's war drum on or off. At the helm it doesn't clash with rowing.");
            TempoUpKey = Config.Bind("Controls", "TempoUpKey", KeyCode.U,
                "At the helm: call a quicker beat (Easy, Steady, Hard).");
            TempoDownKey = Config.Bind("Controls", "TempoDownKey", KeyCode.N,
                "At the helm: call a slower beat (Hard, Steady, Easy).");
            HoldWaterCallKey = Config.Bind("Controls", "HoldWaterCallKey", KeyCode.J,
                "At the helm: call \"Hold water!\", telling the crew to brake. Rowers brake themselves with the brake key.");

            EasyTempoFactor = Config.Bind("Helm", "EasyTempoFactor", 1.25f,
                "The helmsman's Easy call: the beat is this many times longer than the automatic one.");
            HardTempoFactor = Config.Bind("Helm", "HardTempoFactor", 0.8f,
                "The helmsman's Hard call: the beat is this many times the automatic one.");

            BrakeKey = Config.Bind("Controls", "BrakeKey", KeyCode.J,
                "Key a seated rower holds to hold water (brake with the oar). Movement, attack, jump and crouch keys stand you up, so don't use those.");

            BrakeStrength = Config.Bind("Brake", "Strength", 0.1f,
                "How hard one braking rower slows the ship: a deceleration of this times the ship's speed, per second. The crew's braking adds up.");
            BrakeStaminaPerSecond = Config.Bind("Brake", "StaminaPerSecond", 3f,
                "Stamina braking costs per second.");
            BrakeTurning = Config.Bind("Brake", "Turning", true,
                "Braking drags at the oar's side of the hull, so braking on one side swings the bow toward that side. Off: braking slows the ship straight. The ship owner's setting decides.");

            StrokeCycleStill = Config.Bind("Timing", "StrokeCycleStill", 1.8f,
                "Seconds between the ship's beats when it's still. The beat speeds up with the ship.");
            StrokeCycleTopSpeed = Config.Bind("Timing", "StrokeCycleTopSpeed", 1.2f,
                "Seconds between the ship's beats at its top sail speed.");
            SweetSpotWidth = Config.Bind("Timing", "SweetSpotWidth", 0.2f,
                "Width of the green zone around each beat, as a fraction of the beat.");
            WeakStrokeFactor = Config.Bind("Timing", "WeakStrokeFactor", 0.35f,
                "Strength of an early or late stroke compared with a well-timed one.");

            StaminaPerStroke = Config.Bind("Stamina", "StaminaPerStroke", 6f,
                "Stamina each stroke costs, including a wasted second press in the same beat.");
            RestedDiscount = Config.Bind("Stamina", "RestedDiscount", 0.1f,
                "Rowers with the Rested buff pay this fraction less stamina for strokes and braking (0.1 = 10% less).");
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

            SyncBonusPerRower = Config.Bind("Crew", "SyncBonusPerRower", 0.15f,
                "Extra strength of a well-timed stroke for each other rower who also hit the same beat.");
            MaxSyncBonus = Config.Bind("Crew", "MaxSyncBonus", 0.45f,
                "Most extra strength the sync bonus can give one stroke.");
            ClashBrake = Config.Bind("Crew", "ClashBrake", 0.2f,
                "Braking from an off-beat stroke on a beat someone else hit (clashing oars), as a fraction of the ship's paddle force. It only slows the ship, never reverses it.");

            BarOffset = Config.Bind("UI", "BarOffset", 0f,
                "Extra pixels to raise the stroke bar above the stamina bar. Negative values lower it.");
            UIScale = Config.Bind("UI", "Scale", 0f,
                "Size of the stroke bar, messages and crew panel. 0 is automatic (scaled for the screen height, 1 at 1080p); e.g. 1.5 makes them 50% bigger than at 1080p.");
            ShowCrewPanel = Config.Bind("UI", "ShowCrewPanel", true,
                "Show the crew panel (a top-down view of the ship with its rowers) in the bottom-right corner while you row or steer.");
            CrewNames = Config.Bind("UI", "CrewNames", false,
                "Show player names next to the benches in the crew panel.");
            ShowOars = Config.Bind("UI", "ShowOars", true,
                "Show an oar beside every rowing seat, resting in the water and swinging with each stroke. Only players with the mod see them.");

            const string soundHelp = " One or more game sound prefabs, comma-separated (their clips are pooled and picked at random). Empty uses the default ({0}); \"generated\" uses a sound made by the mod.";
            SplashSound = Config.Bind("Sounds", "SplashSound", "",
                "The blade entering the water." + string.Format(soundHelp, RowingSounds.DefaultSplash));
            RunoffSound = Config.Bind("Sounds", "RunoffSound", "",
                "Water running off just after a splash (a short slice is played)." + string.Format(soundHelp, RowingSounds.DefaultRunoff));
            DripSound = Config.Bind("Sounds", "DripSound", "",
                "Drips as the blade lifts out of the water (a short slice is played)." + string.Format(soundHelp, RowingSounds.DefaultDrip));
            KnockSound = Config.Bind("Sounds", "KnockSound", "",
                "The oar knocking in its oarlock as it swings back." + string.Format(soundHelp, RowingSounds.DefaultKnock));
            SyncSound = Config.Bind("Sounds", "SyncSound", "",
                "The deeper splash added when rowers hit the same beat." + string.Format(soundHelp, RowingSounds.DefaultSync));
            ClashSound = Config.Bind("Sounds", "ClashSound", "",
                "An oar clashing with the crew's rhythm." + string.Format(soundHelp, RowingSounds.DefaultClash));
            CreakSound = Config.Bind("Sounds", "CreakSound", "",
                "Wood creaking under strong strokes (a short slice is played)." + string.Format(soundHelp, RowingSounds.DefaultCreak));
            CreakVolume = Config.Bind("Sounds", "CreakVolume", 0.5f,
                "Volume of the creak (0 to 1; 0 turns it off). Louder when the crew pushes harder.");
            SplashVolume = Config.Bind("Sounds", "SplashVolume", 0.8f,
                "Volume of the stroke splash (0 to 1); weak strokes are quieter. Everyone nearby hears it.");
            DrumVolume = Config.Bind("Sounds", "DrumVolume", 0.8f,
                "Volume of the ship's war drum (0 to 1). The helmsman turns the drum on or off with the drum key.");
            DrumSound = Config.Bind("Sounds", "DrumSound", "",
                "The war drum's sound. Empty uses a recorded drum (sounds/dundun.wav next to the mod) if present, otherwise a drum made by the mod; \"generated\" always uses the one made by the mod.");
            ShowWakes = Config.Bind("UI", "ShowWakes", true,
                "Show subtle wakes on the water where blades sweep through.");
            WakeEffect = Config.Bind("UI", "WakeEffect", "",
                $"Game effect prefab whose particles show as the wake (its sound is removed). Empty uses the default ({RowingSounds.DefaultWakeEffect}).");
            ShowSplashes = Config.Bind("UI", "ShowSplashes", true,
                "Show water spray at the blade on each stroke.");
            SplashEffect = Config.Bind("UI", "SplashEffect", "",
                $"Game effect prefab whose particles show as the spray (its sound is removed). Empty uses the default ({RowingSounds.DefaultSplashEffect}).");
            LogSoundCandidates = Config.Bind("Debug", "LogSoundCandidates", false,
                "Log the game's water, splash and wood sounds once per session, to pick a SplashSound.");

            gameObject.AddComponent<Rower>();
            gameObject.AddComponent<CrewPanel>();
            new Harmony(Guid).PatchAll();
            Log.LogInfo($"{Name} {Version} loaded");
        }
    }
}
