using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;

namespace RowingMod
{
    /// <summary>
    /// Rowing sounds and the blade's splash effect. Every stroke is built from layers with randomness, so no two
    /// strokes sound alike: a splash, sometimes water running off, a deeper splash when the crew hits the beat
    /// together, a creak of wood under load, a knock in the oarlock and drips as the blade lifts. A clash is a
    /// wooden thud. A war drum can play the ship's beat for everyone aboard; blades leave subtle wakes.
    ///
    /// Sounds reuse clips from the game's own prefabs, played through our own AudioSource so volume, pitch and
    /// length are ours. Each Sounds.* setting names one or more prefabs (comma-separated; their clips are pooled),
    /// is empty for the default below, or says "generated" for a sound made in code. Everything goes through the
    /// game's SFX mixer group, so the game's SFX volume applies.
    /// </summary>
    public static class RowingSounds
    {
        // Defaults, chosen from the game's sounds (see the Debug.LogSoundCandidates log).
        public const string DefaultSplash = "fx_footstep_water";
        public const string DefaultRunoff = "sfx_ship_waterimpact";
        public const string DefaultDrip = "sfx_footstep_swim";
        public const string DefaultKnock = "fx_footstep_wood_jog";
        public const string DefaultClash = "sfx_wood_blocked";
        public const string DefaultCreak = "sfx_bogwitch_creak,sfx_ship_sailposition_change_vibration_only";
        public const string DefaultSync = "sfx_land_water";
        public const string DefaultSplashEffect = "fx_footstep_water";
        public const string DefaultWakeEffect = "vfx_water_surface";
        private const string Generated = "generated";

        private const int SampleRate = 44100;
        private const float MaxDistance = 40f;
        // The drum carries further than oars: heard across the ship and by nearby ships.
        private const float DrumMaxDistance = 70f;
        private const float EffectLifetime = 4f;

        private static bool s_initialized;
        private static AudioMixerGroup s_sfxGroup;
        private static AudioClip[] s_splash;
        private static AudioClip[] s_runoff;
        private static AudioClip[] s_drip;
        private static AudioClip[] s_knock;
        private static AudioClip[] s_clash;
        private static AudioClip[] s_creak;
        private static AudioClip[] s_sync;
        private static AudioClip s_drum;
        private static AudioClip s_drumAccent;
        // The war drum's kit: four real drums shipped with the mod (sounds/drum_*.wav), indexed by DrumPatterns.Drum.
        // Null if any is missing or Sounds.DrumSound is "generated"; then the drum plays one generated hit per beat.
        private static AudioClip[] s_drumKit;
        private static readonly string[] DrumKitFiles = { "drum_kick.wav", "drum_mid.wav", "drum_tap.wav", "drum_high.wav" };
        private static GameObject s_splashEffect;
        private static GameObject s_wakeEffect;
        private static GameObject s_effectHolder;
        // Effect prefabs that aren't registered in ZNetScene but hang off other prefabs (footsteps, ship effects).
        private static readonly Dictionary<string, GameObject> s_extraPrefabs = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, string> s_extraSources = new Dictionary<string, string>();

        /// <summary>Finds the game's sounds once the prefabs are loaded. Safe to call every frame.</summary>
        private static bool EnsureInitialized()
        {
            if (s_initialized)
            {
                return true;
            }
            if (ZNetScene.instance == null)
            {
                return false;
            }
            s_initialized = true;

            CollectExtraPrefabs();
            LogCandidates();

            s_splash = LoadClips(RowingPlugin.SplashSound.Value, DefaultSplash, MakeSplash);
            s_runoff = LoadClips(RowingPlugin.RunoffSound.Value, DefaultRunoff, MakeSplash);
            s_drip = LoadClips(RowingPlugin.DripSound.Value, DefaultDrip, MakeSplash);
            s_knock = LoadClips(RowingPlugin.KnockSound.Value, DefaultKnock, () => MakeKnock("RowingMod_Knock", 520f, 0.05f, 0.4f));
            s_clash = LoadClips(RowingPlugin.ClashSound.Value, DefaultClash, () => MakeKnock("RowingMod_Clash", 420f, 0.09f, 0.55f));
            s_creak = LoadClips(RowingPlugin.CreakSound.Value, DefaultCreak, MakeCreak);
            s_sync = LoadClips(RowingPlugin.SyncSound.Value, DefaultSync, MakeSplash);
            s_drum = MakeDrum("RowingMod_Drum", accent: false);
            s_drumAccent = MakeDrum("RowingMod_DrumAccent", accent: true);
            if (!IsGenerated(RowingPlugin.DrumSound.Value))
            {
                s_drumKit = new AudioClip[DrumKitFiles.Length];
                for (int i = 0; i < DrumKitFiles.Length; i++)
                {
                    s_drumKit[i] = LoadBundledWav(DrumKitFiles[i]);
                    if (s_drumKit[i] == null)
                    {
                        s_drumKit = null;
                        break;
                    }
                }
            }
            s_sfxGroup = FindSfxGroup();

            s_effectHolder = new GameObject("RowingMod_EffectHolder");
            s_effectHolder.SetActive(false);
            Object.DontDestroyOnLoad(s_effectHolder);
            s_splashEffect = FindEffect(RowingPlugin.SplashEffect.Value, DefaultSplashEffect, "Splash");
            s_wakeEffect = FindEffect(RowingPlugin.WakeEffect.Value, DefaultWakeEffect, "Wake");
            return true;
        }

        /// <summary>
        /// Everything a stroke sounds (and looks) like. <paramref name="strongOnBeat"/> is how many well-timed
        /// strokes this beat has had so far, this one included; <paramref name="recovery"/> is when the blade lifts
        /// out of the water (seconds from now); <paramref name="creak"/> allows a creak, with <paramref name="load"/>
        /// (0..1) for how hard the crew pushes.
        /// </summary>
        public static void PlayStroke(Vector3 blade, Vector3 oarlock, bool strong, int strongOnBeat, float recovery, bool creak, float load)
        {
            if (!EnsureInitialized())
            {
                return;
            }
            float volume = RowingPlugin.SplashVolume.Value * (strong ? 1f : 0.55f);

            // The blade entering the water: a random clip, pitch and volume each time.
            PlayAt(Pick(s_splash), blade, volume * Random.Range(0.8f, 1.05f), Random.Range(0.9f, 1.12f), 0f, 0f, 0f);
            // Often, water running off a moment later: a soft slice of a longer after-splash.
            if (Random.value < 0.5f)
            {
                PlaySlice(s_runoff, blade, volume * Random.Range(0.25f, 0.4f), Random.Range(0.95f, 1.15f), 0.7f, Random.Range(0.1f, 0.25f));
            }
            // Crew in sync: each stroke that lands on a beat others hit adds a deeper splash, so a synced crew
            // sounds fuller than the same strokes scattered.
            if (strong && strongOnBeat >= 2)
            {
                float fullness = Mathf.Min(0.6f, 0.25f + 0.1f * (strongOnBeat - 1));
                PlayAt(Pick(s_sync), blade, RowingPlugin.SplashVolume.Value * fullness, Random.Range(0.78f, 0.9f), 0f, 0f, Random.Range(0f, 0.04f));
            }
            // As the blade lifts out on the recovery: sometimes drips, sometimes a knock of the oar in its oarlock.
            if (Random.value < 0.6f)
            {
                PlaySlice(s_drip, blade, volume * Random.Range(0.12f, 0.22f), Random.Range(1.0f, 1.25f), 0.6f, recovery + Random.Range(0.05f, 0.2f));
            }
            if (Random.value < 0.4f)
            {
                PlayAt(Pick(s_knock), oarlock, RowingPlugin.SplashVolume.Value * Random.Range(0.15f, 0.3f), Random.Range(1.05f, 1.3f),
                    0f, 0f, recovery + Random.Range(0f, 0.1f));
            }
            // Wood straining under a strong stroke, on most strokes but not all, louder when the crew pushes hard.
            if (strong && creak && RowingPlugin.CreakVolume.Value > 0f && Random.value < 0.6f)
            {
                float creakVolume = RowingPlugin.CreakVolume.Value * Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(load));
                PlaySlice(s_creak, oarlock, creakVolume, Random.Range(0.9f, 1.1f), 0.9f, 0f);
            }

            ShowSplash(blade, strong);
        }

        /// <summary>
        /// Water rushing past a blade held in the water (braking): a short slice of the swim splashes, and now and
        /// then a bit of after-splash. <paramref name="intensity"/> (0..1) grows with the ship's speed.
        /// </summary>
        public static void PlayGurgle(Vector3 position, float intensity)
        {
            if (!EnsureInitialized())
            {
                return;
            }
            float volume = RowingPlugin.SplashVolume.Value * Mathf.Lerp(0.35f, 0.75f, Mathf.Clamp01(intensity));
            // Alternate between the start of a swim splash (where its energy is; the tails are quiet) and a
            // wading splash pitched down, so the rush keeps changing.
            if (Random.value < 0.5f)
            {
                PlaySliceFromStart(s_drip, position, volume, Random.Range(0.85f, 1.05f), 0.6f, 0.4f);
            }
            else
            {
                PlayAt(Pick(s_splash), position, volume * 0.8f, Random.Range(0.7f, 0.85f), 0f, 0f, 0f);
            }
            if (Random.value < 0.3f)
            {
                PlaySliceFromStart(s_runoff, position, volume * 0.5f, Random.Range(0.9f, 1.1f), 0.5f, 0.4f);
            }
        }

        /// <summary>The blade digging into the water as a rower starts to brake: a firm splash and spray.</summary>
        public static void PlayBrakeCatch(Vector3 position)
        {
            if (!EnsureInitialized())
            {
                return;
            }
            PlayAt(Pick(s_splash), position, RowingPlugin.SplashVolume.Value * Random.Range(0.8f, 1f), Random.Range(0.8f, 0.95f), 0f, 0f, 0f);
            ShowSplash(position, strong: true);
        }

        /// <summary>
        /// Like PlaySlice, but the slice starts within the first <paramref name="startWithin"/> fraction of the
        /// clip, where splash recordings are loud; their tails are often near silent.
        /// </summary>
        private static void PlaySliceFromStart(AudioClip[] clips, Vector3 position, float volume, float pitch, float length, float startWithin)
        {
            AudioClip clip = Pick(clips);
            if (clip == null)
            {
                return;
            }
            float slice = Mathf.Min(length, clip.length);
            float start = Random.Range(0f, Mathf.Max(0f, Mathf.Min(clip.length * startWithin, clip.length - slice)));
            PlayAt(clip, position, volume, pitch, start, slice, 0f);
        }

        /// <summary>Spray at a blade, outside a stroke (e.g. while braking at speed).</summary>
        public static void ShowSpray(Vector3 position, bool strong)
        {
            if (EnsureInitialized())
            {
                ShowSplash(position, strong);
            }
        }

        /// <summary>A wooden thud: an oar clashing with the crew's rhythm.</summary>
        public static void PlayClash(Vector3 position)
        {
            if (!EnsureInitialized())
            {
                return;
            }
            PlayAt(Pick(s_clash), position, RowingPlugin.SplashVolume.Value, Random.Range(0.95f, 1.05f), 0f, 0f, 0f);
            ShowSplash(position, strong: false);
        }

        /// <summary>One beat of the ship's war drum, from the ship itself; <paramref name="accent"/> marks the first of four.</summary>
        public static void PlayDrum(Vector3 position, bool accent)
        {
            if (!EnsureInitialized())
            {
                return;
            }
            float volume = RowingPlugin.DrumVolume.Value * (accent ? 1f : 0.72f);
            PlayAt(accent ? s_drumAccent : s_drum, position, volume, Random.Range(0.97f, 1.03f), 0f, 0f, 0f, DrumMaxDistance);
        }

        /// <summary>Whether the real drum kit is available, so the drum can play its patterns.</summary>
        public static bool HasDrumKit()
        {
            return EnsureInitialized() && s_drumKit != null;
        }

        /// <summary>One hit of a drum pattern, <paramref name="delay"/> seconds from now (scheduled ahead for timing).</summary>
        public static void PlayDrumHit(DrumPatterns.Drum drum, Vector3 position, float gain, float pitch, float delay)
        {
            if (!EnsureInitialized() || s_drumKit == null)
            {
                return;
            }
            PlayAt(s_drumKit[(int)drum], position, RowingPlugin.DrumVolume.Value * gain, pitch, 0f, 0f, Mathf.Max(0f, delay), DrumMaxDistance);
        }

        /// <summary>A subtle wake on the water where a blade swept through (UI.ShowWakes).</summary>
        public static void ShowWake(Vector3 position, float scale)
        {
            if (EnsureInitialized() && RowingPlugin.ShowWakes.Value)
            {
                SpawnEffect(s_wakeEffect, position, scale);
            }
        }

        private static AudioClip Pick(AudioClip[] clips)
        {
            return clips != null && clips.Length > 0 ? clips[Random.Range(0, clips.Length)] : null;
        }

        /// <summary>Plays a random slice (at most <paramref name="length"/> seconds) from a random clip of a set.</summary>
        private static void PlaySlice(AudioClip[] clips, Vector3 position, float volume, float pitch, float length, float delay)
        {
            AudioClip clip = Pick(clips);
            if (clip == null)
            {
                return;
            }
            float slice = Mathf.Min(length, clip.length);
            float start = Random.Range(0f, Mathf.Max(0f, clip.length - slice));
            PlayAt(clip, position, volume, pitch, start, slice, delay);
        }

        /// <summary>
        /// Plays a clip at a position (3D) after <paramref name="delay"/> seconds. With a <paramref name="length"/>
        /// above zero it plays only that slice, starting at <paramref name="start"/> seconds, faded in and out.
        /// </summary>
        private static void PlayAt(AudioClip clip, Vector3 position, float volume, float pitch, float start, float length, float delay,
            float maxDistance = MaxDistance)
        {
            if (clip == null || volume <= 0f)
            {
                return;
            }
            GameObject sound = new GameObject("RowingMod_Sound");
            sound.transform.position = position;
            AudioSource source = sound.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = pitch;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 2f;
            source.maxDistance = maxDistance;
            source.outputAudioMixerGroup = s_sfxGroup;
            source.time = Mathf.Clamp(start, 0f, Mathf.Max(0f, clip.length - 0.05f));
            source.PlayDelayed(delay);

            float playSeconds = (length > 0f ? length : clip.length - start) / Mathf.Max(0.1f, pitch);
            if (length > 0f)
            {
                sound.AddComponent<SliceFade>().Begin(source, playSeconds, delay);
            }
            Object.Destroy(sound, delay + playSeconds + 0.1f);
        }

        /// <summary>Fades a sound slice in quickly and out at its end, so a cut from a longer clip doesn't click.</summary>
        private class SliceFade : MonoBehaviour
        {
            private const float FadeIn = 0.05f;
            private const float FadeOut = 0.15f;
            private AudioSource m_source;
            private float m_volume;
            private float m_length;
            private float m_start;

            public void Begin(AudioSource source, float length, float delay)
            {
                m_source = source;
                m_volume = source.volume;
                m_length = length;
                m_start = Time.time + delay;
                source.volume = 0f;
            }

            private void Update()
            {
                if (m_source == null)
                {
                    return;
                }
                float t = Time.time - m_start;
                m_source.volume = m_volume * Mathf.Min(Mathf.Clamp01(t / FadeIn), Mathf.Clamp01((m_length - t) / FadeOut));
            }
        }

        /// <summary>
        /// Water spray at the blade: the particles of a game water effect (UI.SplashEffect) without its sound,
        /// since the sounds above are played separately. Smaller for a weak stroke.
        /// </summary>
        private static void ShowSplash(Vector3 position, bool strong)
        {
            if (RowingPlugin.ShowSplashes.Value)
            {
                SpawnEffect(s_splashEffect, position, strong ? 1f : 0.7f);
            }
        }

        /// <summary>
        /// Shows a game effect's particles at a position, with its own sounds removed (ours play separately).
        /// </summary>
        private static void SpawnEffect(GameObject prefab, Vector3 position, float scale)
        {
            if (prefab == null)
            {
                return;
            }
            // Instantiate under an inactive holder so the effect's own components don't wake up (and play their
            // sound) before the sound components are removed.
            GameObject effect = Object.Instantiate(prefab, s_effectHolder.transform);
            foreach (ZSFX sfx in effect.GetComponentsInChildren<ZSFX>(includeInactive: true))
            {
                Object.DestroyImmediate(sfx);
            }
            foreach (AudioSource source in effect.GetComponentsInChildren<AudioSource>(includeInactive: true))
            {
                Object.DestroyImmediate(source);
            }
            effect.transform.SetParent(null, worldPositionStays: false);
            effect.transform.position = position;
            effect.transform.localScale *= scale;
            Object.Destroy(effect, EffectLifetime);
        }

        /// <summary>
        /// A game effect prefab from a setting (empty = the default), if it has particles and isn't networked
        /// (instantiating a networked prefab would create a world object for everyone).
        /// </summary>
        private static GameObject FindEffect(string setting, string defaultName, string what)
        {
            string name = string.IsNullOrEmpty(setting?.Trim()) ? defaultName : setting.Trim();
            GameObject prefab = FindPrefab(name);
            if (prefab == null || prefab.GetComponentInChildren<ParticleSystem>(includeInactive: true) == null)
            {
                RowingPlugin.Log.LogWarning($"{what} effect '{name}' not found or has no particles; it won't show");
                return null;
            }
            if (prefab.GetComponent<ZNetView>() != null)
            {
                RowingPlugin.Log.LogWarning($"{what} effect '{name}' is a networked object; it won't show");
                return null;
            }
            return prefab;
        }

        private static bool IsGenerated(string setting)
        {
            return string.Equals(setting?.Trim(), Generated, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The clips for one sound setting: the clips of every named game prefab (comma-separated; empty means the
        /// default), or a generated stand-in when the setting says "generated" or nothing usable is found.
        /// </summary>
        private static AudioClip[] LoadClips(string setting, string defaultNames, System.Func<AudioClip> generate)
        {
            if (!IsGenerated(setting))
            {
                string names = string.IsNullOrEmpty(setting?.Trim()) ? defaultNames : setting;
                List<AudioClip> clips = new List<AudioClip>();
                foreach (string part in names.Split(','))
                {
                    string name = part.Trim();
                    if (name.Length == 0)
                    {
                        continue;
                    }
                    GameObject prefab = FindPrefab(name);
                    ZSFX sfx = prefab != null ? prefab.GetComponentInChildren<ZSFX>(includeInactive: true) : null;
                    if (sfx != null && sfx.m_audioClips != null && sfx.m_audioClips.Length > 0)
                    {
                        foreach (AudioClip clip in sfx.m_audioClips)
                        {
                            if (clip != null)
                            {
                                clips.Add(clip);
                            }
                        }
                    }
                    else
                    {
                        RowingPlugin.Log.LogWarning($"Sound '{name}' not found or has no clips");
                    }
                }
                if (clips.Count > 0)
                {
                    return clips.ToArray();
                }
                RowingPlugin.Log.LogWarning($"No usable sounds in '{names}'; using a generated sound");
            }
            return new[] { generate() };
        }

        private static GameObject FindPrefab(string name)
        {
            GameObject prefab = ZNetScene.instance.GetPrefab(name);
            if (prefab == null)
            {
                s_extraPrefabs.TryGetValue(name, out prefab);
            }
            return prefab;
        }

        /// <summary>
        /// The game's SFX mixer group, taken from a game sound prefab's AudioSource, so the game's SFX volume
        /// slider applies to our sounds too.
        /// </summary>
        private static AudioMixerGroup FindSfxGroup()
        {
            foreach (string name in new[] { "sfx_wood_blocked", "sfx_land_water" })
            {
                GameObject prefab = FindPrefab(name);
                AudioSource source = prefab != null ? prefab.GetComponentInChildren<AudioSource>(includeInactive: true) : null;
                if (source != null && source.outputAudioMixerGroup != null)
                {
                    return source.outputAudioMixerGroup;
                }
            }
            foreach (GameObject prefab in AllPrefabs())
            {
                if (prefab == null || !prefab.name.StartsWith("sfx_"))
                {
                    continue;
                }
                AudioSource source = prefab.GetComponentInChildren<AudioSource>(includeInactive: true);
                if (source != null && source.outputAudioMixerGroup != null)
                {
                    return source.outputAudioMixerGroup;
                }
            }
            RowingPlugin.Log.LogWarning("No SFX mixer group found; rowing sounds ignore the game's SFX volume");
            return null;
        }

        private static IEnumerable<GameObject> AllPrefabs()
        {
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                yield return prefab;
            }
            foreach (GameObject prefab in ZNetScene.instance.m_nonNetViewPrefabs)
            {
                yield return prefab;
            }
        }

        /// <summary>
        /// Effect prefabs referenced by the player (footsteps, water effects) and the ships (water impact, sail
        /// change), which aren't all registered in ZNetScene. Keyed by name so the settings can use them.
        /// </summary>
        private static void CollectExtraPrefabs()
        {
            GameObject player = ZNetScene.instance.GetPrefab("Player");
            if (player != null)
            {
                FootStep footStep = player.GetComponent<FootStep>();
                if (footStep != null)
                {
                    foreach (FootStep.StepEffect step in footStep.m_effects)
                    {
                        foreach (GameObject prefab in step.m_effectPrefabs)
                        {
                            AddExtra(prefab, $"footstep {step.m_name} ({step.m_material}, {step.m_motionType})");
                        }
                    }
                }
                Character character = player.GetComponent<Character>();
                if (character != null)
                {
                    AddExtra(character.m_waterEffects, "player water effects");
                }
                // Every effect list on the player (hurt, jump, death...), which is where any voice sounds would be.
                foreach (MonoBehaviour component in player.GetComponents<MonoBehaviour>())
                {
                    if (component == null)
                    {
                        continue;
                    }
                    foreach (System.Reflection.FieldInfo field in component.GetType().GetFields(
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                    {
                        if (field.FieldType == typeof(EffectList))
                        {
                            AddExtra((EffectList)field.GetValue(component), $"player {field.Name}");
                        }
                    }
                }
            }
            foreach (string shipName in new[] { "Karve", "VikingShip", "Raft" })
            {
                GameObject shipPrefab = ZNetScene.instance.GetPrefab(shipName);
                Ship ship = shipPrefab != null ? shipPrefab.GetComponent<Ship>() : null;
                if (ship != null)
                {
                    AddExtra(ship.m_waterImpactEffect, $"{shipName} water impact");
                    AddExtra(ship.m_changeSailPosEffect, $"{shipName} sail change");
                }
            }
        }

        private static void AddExtra(EffectList effects, string source)
        {
            if (effects?.m_effectPrefabs == null)
            {
                return;
            }
            foreach (EffectList.EffectData data in effects.m_effectPrefabs)
            {
                AddExtra(data?.m_prefab, source);
            }
        }

        private static void AddExtra(GameObject prefab, string source)
        {
            if (prefab == null || s_extraPrefabs.ContainsKey(prefab.name))
            {
                return;
            }
            if (prefab.GetComponentInChildren<ZSFX>(includeInactive: true) == null
                && prefab.GetComponentInChildren<ParticleSystem>(includeInactive: true) == null)
            {
                return;
            }
            s_extraPrefabs[prefab.name] = prefab;
            s_extraSources[prefab.name] = source;
        }

        /// <summary>
        /// Lists the game's sounds and particle effects that might suit rowing, once per session, so better ones
        /// can be chosen for the Sounds.* and UI.SplashEffect settings.
        /// </summary>
        private static void LogCandidates()
        {
            if (!RowingPlugin.LogSoundCandidates.Value)
            {
                return;
            }
            string[] keywords = { "water", "splash", "swim", "wave", "paddle", "oar", "boat", "ship", "drum", "wood", "knock",
                "bubble", "fish", "creak", "squeak", "strain", "stress", "rope", "bend", "door", "chest", "crack", "spray", "drip" };
            StringBuilder log = new StringBuilder("Sound and effect candidates for rowing (Sounds.*, UI.SplashEffect):");
            int count = 0;
            foreach (GameObject prefab in AllPrefabs())
            {
                if (prefab == null)
                {
                    continue;
                }
                string name = prefab.name.ToLowerInvariant();
                if (!(name.StartsWith("sfx") || name.StartsWith("vfx") || name.StartsWith("fx")) || !ContainsAny(name, keywords))
                {
                    continue;
                }
                AppendCandidate(log, prefab, null);
                count++;
            }
            foreach (KeyValuePair<string, GameObject> extra in s_extraPrefabs)
            {
                AppendCandidate(log, extra.Value, s_extraSources[extra.Key]);
                count++;
            }
            log.Append($"\n  ({count} found)");
            RowingPlugin.Log.LogInfo(log.ToString());
            LogVoiceCandidates();
        }

        /// <summary>
        /// Game sound clips whose names suggest a voice (grunts, effort, breathing...), with the prefab they're in,
        /// to find rowing grunts among the game's own sounds.
        /// </summary>
        private static void LogVoiceCandidates()
        {
            string[] keywords = { "grunt", "effort", "exert", "breath", "pant", "hurt", "pain", "jump", "voice", "male", "female",
                "vocal", "vox", "groan", "sigh", "strain", "attack_m", "attack_f", "player" };
            StringBuilder log = new StringBuilder("Voice candidates for rowing grunts (clip names):");
            int count = 0;
            HashSet<string> seen = new HashSet<string>();
            foreach (GameObject prefab in AllPrefabs())
            {
                AppendVoice(log, prefab, null, keywords, seen, ref count);
            }
            foreach (KeyValuePair<string, GameObject> extra in s_extraPrefabs)
            {
                AppendVoice(log, extra.Value, s_extraSources[extra.Key], keywords, seen, ref count);
            }
            log.Append($"\n  ({count} prefab(s) with matching clips)");
            RowingPlugin.Log.LogInfo(log.ToString());
        }

        private static void AppendVoice(StringBuilder log, GameObject prefab, string source, string[] keywords, HashSet<string> seen, ref int count)
        {
            if (prefab == null || !seen.Add(prefab.name))
            {
                return;
            }
            List<string> matches = new List<string>();
            foreach (ZSFX sfx in prefab.GetComponentsInChildren<ZSFX>(includeInactive: true))
            {
                if (sfx.m_audioClips == null)
                {
                    continue;
                }
                foreach (AudioClip clip in sfx.m_audioClips)
                {
                    if (clip != null && ContainsAny(clip.name.ToLowerInvariant(), keywords))
                    {
                        matches.Add($"{clip.name} ({clip.length:0.00} s)");
                    }
                }
            }
            if (matches.Count == 0)
            {
                return;
            }
            string from = source != null ? $" [{source}]" : "";
            log.Append($"\n  {prefab.name}{from}: {string.Join(", ", matches.ToArray())}");
            count++;
        }

        private static bool ContainsAny(string text, string[] keywords)
        {
            foreach (string keyword in keywords)
            {
                if (text.Contains(keyword))
                {
                    return true;
                }
            }
            return false;
        }

        private static void AppendCandidate(StringBuilder log, GameObject prefab, string source)
        {
            ZSFX sfx = prefab.GetComponentInChildren<ZSFX>(includeInactive: true);
            List<string> clips = new List<string>();
            if (sfx != null && sfx.m_audioClips != null)
            {
                foreach (AudioClip clip in sfx.m_audioClips)
                {
                    if (clip != null)
                    {
                        clips.Add($"{clip.name} ({clip.length:0.00} s)");
                    }
                }
            }
            int particles = prefab.GetComponentsInChildren<ParticleSystem>(includeInactive: true).Length;
            string from = source != null ? $" [{source}]" : "";
            string particleNote = particles > 0 ? $" [{particles} particle system(s)]" : "";
            string networked = prefab.GetComponent<ZNetView>() != null ? " [networked]" : "";
            log.Append($"\n  {prefab.name}{from}{particleNote}{networked}: {string.Join(", ", clips.ToArray())}");
        }

        /// <summary>A short wooden knock: two decaying tones and a click.</summary>
        private static AudioClip MakeKnock(string name, float frequency, float decay, float noise)
        {
            int length = (int)(SampleRate * decay * 6f);
            float[] samples = new float[length];
            System.Random random = new System.Random(name.GetHashCode());
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Exp(-t / decay);
                float tone = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * frequency * 2.7f * t) * 0.3f;
                float click = (float)(random.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t / 0.004f) * noise;
                samples[i] = (tone * envelope + click) * 0.8f;
            }
            return MakeClip(name, samples);
        }

        /// <summary>A stand-in splash: a soft, low-passed burst of noise.</summary>
        private static AudioClip MakeSplash()
        {
            int length = (int)(SampleRate * 0.45f);
            float[] samples = new float[length];
            System.Random random = new System.Random(7);
            float low = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Clamp01(t / 0.012f) * Mathf.Exp(-t / 0.11f);
                float white = (float)(random.NextDouble() * 2.0 - 1.0);
                low += (white - low) * 0.18f;
                samples[i] = low * envelope * 1.6f;
            }
            return MakeClip("RowingMod_Splash", samples);
        }

        /// <summary>
        /// Loads a sound shipped with the mod, from the "sounds" folder next to the mod's DLL: a PCM WAV file
        /// (8/16/24/32-bit integer, any channels, mixed down to mono). Returns null if it's missing or unreadable.
        /// </summary>
        private static AudioClip LoadBundledWav(string fileName)
        {
            string folder = Path.Combine(Path.GetDirectoryName(typeof(RowingSounds).Assembly.Location) ?? "", "sounds");
            string path = Path.Combine(folder, fileName);
            try
            {
                if (!File.Exists(path))
                {
                    // Expected when a sound isn't part of this release.
                    RowingPlugin.Log.LogInfo($"No {fileName} next to the mod; using a generated sound");
                    return null;
                }
                byte[] bytes = File.ReadAllBytes(path);
                if (bytes.Length < 44 || Encoding.ASCII.GetString(bytes, 0, 4) != "RIFF" || Encoding.ASCII.GetString(bytes, 8, 4) != "WAVE")
                {
                    RowingPlugin.Log.LogWarning($"Sound file {path} isn't a WAV file; using a generated sound");
                    return null;
                }
                int channels = 0, sampleRate = 0, bits = 0, format = 0;
                int position = 12;
                while (position + 8 <= bytes.Length)
                {
                    string id = Encoding.ASCII.GetString(bytes, position, 4);
                    int size = System.BitConverter.ToInt32(bytes, position + 4);
                    int body = position + 8;
                    if (id == "fmt ")
                    {
                        format = System.BitConverter.ToInt16(bytes, body);
                        channels = System.BitConverter.ToInt16(bytes, body + 2);
                        sampleRate = System.BitConverter.ToInt32(bytes, body + 4);
                        bits = System.BitConverter.ToInt16(bytes, body + 14);
                    }
                    else if (id == "data")
                    {
                        if (format != 1 || channels < 1 || (bits != 8 && bits != 16 && bits != 24 && bits != 32))
                        {
                            RowingPlugin.Log.LogWarning($"Sound file {path} is format {format}, {bits}-bit; only integer PCM WAV works");
                            return null;
                        }
                        int bytesPerSample = bits / 8;
                        int frames = Mathf.Min(size, bytes.Length - body) / (bytesPerSample * channels);
                        float[] samples = new float[frames];
                        for (int frame = 0; frame < frames; frame++)
                        {
                            float sum = 0f;
                            for (int channel = 0; channel < channels; channel++)
                            {
                                sum += ReadSample(bytes, body + (frame * channels + channel) * bytesPerSample, bits);
                            }
                            samples[frame] = sum / channels;
                        }
                        RowingPlugin.Log.LogInfo($"Loaded {fileName} ({frames / (float)sampleRate:0.00} s)");
                        return MakeClip(Path.GetFileNameWithoutExtension(fileName), samples, sampleRate);
                    }
                    position = body + size + (size & 1);
                }
                RowingPlugin.Log.LogWarning($"Sound file {path} has no audio data; using a generated sound");
            }
            catch (System.Exception e)
            {
                RowingPlugin.Log.LogWarning($"Couldn't read sound file {path}: {e.Message}; using a generated sound");
            }
            return null;
        }

        private static float ReadSample(byte[] bytes, int offset, int bits)
        {
            switch (bits)
            {
                case 8:
                    return (bytes[offset] - 128) / 128f;
                case 16:
                    return System.BitConverter.ToInt16(bytes, offset) / 32768f;
                case 24:
                    return (bytes[offset] | (bytes[offset + 1] << 8) | ((sbyte)bytes[offset + 2] << 16)) / 8388608f;
                default:
                    return System.BitConverter.ToInt32(bytes, offset) / 2147483648f;
            }
        }

        /// <summary>
        /// A war drum hit made in code (the fallback when the shipped drum is missing or Sounds.DrumSound is "generated"): a deep tom whose pitch drops quickly,
        /// an overtone, and a skin slap. The accent (first of four beats) is deeper and longer.
        /// </summary>
        private static AudioClip MakeDrum(string name, bool accent)
        {
            float duration = accent ? 0.9f : 0.7f;
            float startFrequency = accent ? 150f : 175f;
            float endFrequency = accent ? 52f : 64f;
            float decay = accent ? 0.32f : 0.24f;
            int length = (int)(SampleRate * duration);
            float[] samples = new float[length];
            System.Random random = new System.Random(name.GetHashCode());
            float phase = 0f;
            float overtonePhase = 0f;
            float slap = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float frequency = endFrequency + (startFrequency - endFrequency) * Mathf.Exp(-t / 0.045f);
                phase += 2f * Mathf.PI * frequency / SampleRate;
                overtonePhase += 2f * Mathf.PI * frequency * 1.58f / SampleRate;
                float body = Mathf.Sin(phase) * Mathf.Exp(-t / decay);
                float overtone = 0.35f * Mathf.Sin(overtonePhase) * Mathf.Exp(-t / (decay * 0.35f));
                // The skin: a short burst of low-passed noise.
                slap += ((float)(random.NextDouble() * 2.0 - 1.0) - slap) * 0.25f;
                float skin = slap * Mathf.Exp(-t / 0.012f) * 0.9f;
                float attack = Mathf.Clamp01(t / 0.002f);
                // Soft saturation gives it weight without clipping.
                samples[i] = (float)System.Math.Tanh((body + overtone + skin) * 1.6f * attack) * 0.85f;
            }
            return MakeClip(name, samples);
        }

        /// <summary>A stand-in creak: a low, wavering tone with a rough, stick-slip texture.</summary>
        private static AudioClip MakeCreak()
        {
            int length = (int)(SampleRate * 0.7f);
            float[] samples = new float[length];
            System.Random random = new System.Random(11);
            float phase = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Clamp01(t / 0.08f) * Mathf.Clamp01((0.7f - t) / 0.2f);
                float frequency = 140f + 40f * Mathf.Sin(2f * Mathf.PI * 3f * t);
                phase += 2f * Mathf.PI * frequency / SampleRate;
                float rough = 0.6f + 0.4f * (float)random.NextDouble();
                float wave = Mathf.Sin(phase) + 0.5f * Mathf.Sin(2f * phase) + 0.25f * Mathf.Sin(3f * phase);
                samples[i] = wave * rough * envelope * 0.35f;
            }
            return MakeClip("RowingMod_Creak", samples);
        }

        /// <summary>
        /// Wraps generated samples in an AudioClip. Uses the PCM reader callback rather than SetData, because Unity 6
        /// adds a ReadOnlySpan overload of SetData that a net48 build can't compile against.
        /// </summary>
        private static AudioClip MakeClip(string name, float[] samples)
        {
            return MakeClip(name, samples, SampleRate);
        }

        private static AudioClip MakeClip(string name, float[] samples, int sampleRate)
        {
            int position = 0;
            return AudioClip.Create(name, samples.Length, 1, sampleRate, false,
                data =>
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        int index = position + i;
                        data[i] = index < samples.Length ? samples[index] : 0f;
                    }
                    position += data.Length;
                },
                newPosition => position = newPosition);
        }
    }
}
