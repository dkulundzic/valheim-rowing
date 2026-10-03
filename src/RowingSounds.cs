using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;

namespace RowingMod
{
    /// <summary>
    /// Rowing sounds: a splash where an oar's blade enters the water on each stroke, a wooden thud for a clash and
    /// a creak of wood under load on strong strokes (heard by everyone nearby with the mod), plus a soft beat tick
    /// only the seated rower hears.
    ///
    /// The splash, clash and creak reuse audio clips from the game's own sound prefabs (Sounds.*Sound settings,
    /// empty = the default listed below, "generated" = a sound made in code), played through our own AudioSource so
    /// volume, pitch and length are ours to set. Everything goes through the game's SFX mixer group, so the game's
    /// SFX volume applies.
    /// </summary>
    public static class RowingSounds
    {
        // Defaults chosen from the game's sounds (see the Debug.LogSoundCandidates log).
        public const string DefaultSplash = "sfx_land_water";
        public const string DefaultClash = "sfx_wood_blocked";
        public const string DefaultCreak = "sfx_bogwitch_creak";
        private const string Generated = "generated";

        private const int SampleRate = 44100;
        private const float MaxDistance = 40f;
        // The default splash is a body falling into deep water; higher and quieter it's closer to a blade.
        private const float SplashPitch = 1.3f;
        // The default creak clips are seconds long; play a short faded slice from a random point.
        private const float CreakSlice = 0.9f;

        private static bool s_initialized;
        private static AudioMixerGroup s_sfxGroup;
        private static AudioClip[] s_splashClips;
        private static AudioClip[] s_clashClips;
        private static AudioClip[] s_creakClips;
        private static AudioClip s_tickClip;
        private static AudioSource s_tickSource;
        // Sound prefabs that aren't registered in ZNetScene but hang off other prefabs (footsteps, ship effects).
        private static readonly Dictionary<string, GameObject> s_extraPrefabs = new Dictionary<string, GameObject>();

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
            LogSoundCandidates();

            s_splashClips = LoadClips(RowingPlugin.SplashSound.Value, DefaultSplash, MakeSplash);
            s_clashClips = LoadClips(RowingPlugin.ClashSound.Value, DefaultClash, () => MakeKnock("RowingMod_Clash", 420f, 0.09f, 0.55f));
            s_creakClips = LoadClips(RowingPlugin.CreakSound.Value, DefaultCreak, MakeCreak);
            s_tickClip = MakeKnock("RowingMod_Tick", 1250f, 0.035f, 0.25f);
            s_sfxGroup = FindSfxGroup();

            GameObject tickObject = new GameObject("RowingMod_Tick");
            Object.DontDestroyOnLoad(tickObject);
            s_tickSource = tickObject.AddComponent<AudioSource>();
            s_tickSource.spatialBlend = 0f;
            s_tickSource.playOnAwake = false;
            s_tickSource.outputAudioMixerGroup = s_sfxGroup;
            return true;
        }

        /// <summary>A splash at the blade, louder for a strong stroke.</summary>
        public static void PlaySplash(Vector3 position, bool strong)
        {
            if (!EnsureInitialized())
            {
                return;
            }
            bool generated = IsGenerated(RowingPlugin.SplashSound.Value);
            float basePitch = generated ? 1f : SplashPitch;
            float volume = RowingPlugin.SplashVolume.Value * (strong ? 1f : 0.55f);
            // Every splash differs a little: a random clip, pitch and volume...
            PlayAt(Pick(s_splashClips), position, volume * Random.Range(0.85f, 1.1f), basePitch * Random.Range(0.9f, 1.1f), 0f, 0f, 0f);
            // ...and half the time a quieter, higher second splash just after, like water running off the blade.
            if (Random.value < 0.5f)
            {
                PlayAt(Pick(s_splashClips), position, volume * Random.Range(0.25f, 0.45f), basePitch * Random.Range(1.3f, 1.6f),
                    0f, 0f, Random.Range(0.08f, 0.2f));
            }
        }

        /// <summary>
        /// A soft knock of the oar in its oarlock as it swings back, on some strokes only and at a random volume.
        /// </summary>
        public static void MaybePlayOarlock(Vector3 position, float delay)
        {
            if (!EnsureInitialized() || Random.value > 0.4f)
            {
                return;
            }
            PlayAt(Pick(s_clashClips), position, RowingPlugin.SplashVolume.Value * Random.Range(0.15f, 0.3f),
                Random.Range(1.2f, 1.5f), 0f, 0f, delay);
        }

        /// <summary>A wooden thud: an oar clashing with the crew's rhythm.</summary>
        public static void PlayClash(Vector3 position)
        {
            if (!EnsureInitialized())
            {
                return;
            }
            PlayAt(Pick(s_clashClips), position, RowingPlugin.SplashVolume.Value, Random.Range(0.95f, 1.05f), 0f, 0f, 0f);
        }

        /// <summary>
        /// Wood straining under a strong stroke, on most strokes but not all; <paramref name="load"/> (0..1) is how
        /// hard the crew pushes. Each creak is a random slice of a random clip.
        /// </summary>
        public static void MaybePlayCreak(Vector3 position, float load)
        {
            if (!EnsureInitialized() || RowingPlugin.CreakVolume.Value <= 0f || Random.value > 0.6f)
            {
                return;
            }
            AudioClip clip = Pick(s_creakClips);
            if (clip == null)
            {
                return;
            }
            float volume = RowingPlugin.CreakVolume.Value * Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(load));
            float length = Mathf.Min(CreakSlice, clip.length);
            float start = Random.Range(0f, Mathf.Max(0f, clip.length - length));
            PlayAt(clip, position, volume, Random.Range(0.9f, 1.1f), start, length, 0f);
        }

        /// <summary>The ship's beat, heard only by the local seated rower.</summary>
        public static void PlayTick()
        {
            if (!EnsureInitialized() || !RowingPlugin.BeatTick.Value)
            {
                return;
            }
            s_tickSource.PlayOneShot(s_tickClip, RowingPlugin.BeatTickVolume.Value);
        }

        private static AudioClip Pick(AudioClip[] clips)
        {
            return clips != null && clips.Length > 0 ? clips[Random.Range(0, clips.Length)] : null;
        }

        /// <summary>
        /// Plays a clip at a position (3D) after <paramref name="delay"/> seconds. With a <paramref name="length"/>
        /// above zero it plays only that slice, starting at <paramref name="start"/> seconds, and fades out at the end.
        /// </summary>
        private static void PlayAt(AudioClip clip, Vector3 position, float volume, float pitch, float start, float length, float delay)
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
            source.maxDistance = MaxDistance;
            source.outputAudioMixerGroup = s_sfxGroup;
            source.time = Mathf.Clamp(start, 0f, Mathf.Max(0f, clip.length - 0.05f));
            source.PlayDelayed(delay);

            float playSeconds = (length > 0f ? length : clip.length - start) / Mathf.Max(0.1f, pitch);
            if (length > 0f)
            {
                sound.AddComponent<FadeOut>().Begin(source, playSeconds, delay);
            }
            Object.Destroy(sound, delay + playSeconds + 0.1f);
        }

        /// <summary>Fades a sound slice in quickly and out at its end, so a cut from a longer clip doesn't click.</summary>
        private class FadeOut : MonoBehaviour
        {
            private const float Fade = 0.15f;
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
                float fadeIn = Mathf.Clamp01(t / 0.05f);
                float fadeOut = Mathf.Clamp01((m_length - t) / Fade);
                m_source.volume = m_volume * Mathf.Min(fadeIn, fadeOut);
            }
        }

        private static bool IsGenerated(string setting)
        {
            return string.Equals(setting?.Trim(), Generated, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The clips for one sound setting: the named game prefab (or the default when the setting is empty), or the
        /// generated stand-in when the setting says "generated" or the prefab can't be found.
        /// </summary>
        private static AudioClip[] LoadClips(string setting, string fallbackName, System.Func<AudioClip> generate)
        {
            if (!IsGenerated(setting))
            {
                string name = string.IsNullOrEmpty(setting?.Trim()) ? fallbackName : setting.Trim();
                GameObject prefab = FindPrefab(name);
                ZSFX sfx = prefab != null ? prefab.GetComponentInChildren<ZSFX>(includeInactive: true) : null;
                if (sfx != null && sfx.m_audioClips != null && sfx.m_audioClips.Length > 0)
                {
                    return sfx.m_audioClips;
                }
                RowingPlugin.Log.LogWarning($"Sound '{name}' not found or has no clips; using a generated sound");
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
            foreach (string name in new[] { DefaultSplash, DefaultClash })
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
        /// Sound prefabs referenced by the player (footsteps, water effects) and the ships (water impact, sail
        /// change), which aren't all registered in ZNetScene. Keyed by name so the Sounds.* settings can use them.
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

        private static readonly Dictionary<string, string> s_extraSources = new Dictionary<string, string>();

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
            if (prefab == null || prefab.GetComponentInChildren<ZSFX>(includeInactive: true) == null)
            {
                return;
            }
            if (!s_extraPrefabs.ContainsKey(prefab.name))
            {
                s_extraPrefabs[prefab.name] = prefab;
                s_extraSources[prefab.name] = source;
            }
        }

        /// <summary>
        /// Lists the game's sound prefabs that might suit rowing, with their clips, once per session, so better
        /// sounds can be chosen for the Sounds.* settings.
        /// </summary>
        private static void LogSoundCandidates()
        {
            if (!RowingPlugin.LogSoundCandidates.Value)
            {
                return;
            }
            string[] keywords = { "water", "splash", "swim", "wave", "paddle", "oar", "boat", "ship", "drum", "wood", "knock",
                "bubble", "fish", "creak", "squeak", "strain", "stress", "rope", "bend", "door", "chest", "crack" };
            StringBuilder log = new StringBuilder("Sound candidates for rowing (Sounds.SplashSound, ClashSound, CreakSound):");
            int count = 0;
            foreach (GameObject prefab in AllPrefabs())
            {
                if (prefab == null)
                {
                    continue;
                }
                string name = prefab.name.ToLowerInvariant();
                if (!name.Contains("sfx") || !ContainsAny(name, keywords))
                {
                    continue;
                }
                AppendSound(log, prefab, null);
                count++;
            }
            foreach (KeyValuePair<string, GameObject> extra in s_extraPrefabs)
            {
                AppendSound(log, extra.Value, s_extraSources[extra.Key]);
                count++;
            }
            log.Append($"\n  ({count} found)");
            RowingPlugin.Log.LogInfo(log.ToString());
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

        private static void AppendSound(StringBuilder log, GameObject prefab, string source)
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
            string from = source != null ? $" [{source}]" : "";
            log.Append($"\n  {prefab.name}{from}: {string.Join(", ", clips.ToArray())}");
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
            int position = 0;
            return AudioClip.Create(name, samples.Length, 1, SampleRate, false,
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
