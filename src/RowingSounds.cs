using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;

namespace RowingMod
{
    /// <summary>
    /// Rowing sounds: a splash where an oar's blade enters the water on each stroke (heard by everyone nearby),
    /// a wooden knock for a clash, and a soft beat tick only the seated rower hears.
    ///
    /// The splash reuses audio clips from one of the game's own sound prefabs (config Sounds.SplashSound), played
    /// through our own AudioSource so its volume can follow stroke strength. The tick and knock are generated in
    /// code. Everything goes through the game's SFX mixer group, so the game's SFX volume applies.
    /// </summary>
    public static class RowingSounds
    {
        private const int SampleRate = 44100;
        private const float SplashMaxDistance = 40f;

        private static bool s_initialized;
        private static AudioMixerGroup s_sfxGroup;
        private static AudioClip[] s_splashClips;
        private static AudioClip s_tickClip;
        private static AudioClip s_knockClip;
        private static AudioClip s_fallbackSplashClip;
        private static AudioSource s_tickSource;

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

            LogSoundCandidates();

            string splashName = RowingPlugin.SplashSound.Value;
            if (!string.IsNullOrEmpty(splashName))
            {
                GameObject prefab = ZNetScene.instance.GetPrefab(splashName);
                ZSFX sfx = prefab != null ? prefab.GetComponentInChildren<ZSFX>(includeInactive: true) : null;
                if (sfx != null && sfx.m_audioClips != null && sfx.m_audioClips.Length > 0)
                {
                    s_splashClips = sfx.m_audioClips;
                }
                else
                {
                    RowingPlugin.Log.LogWarning($"Splash sound '{splashName}' not found or has no clips; using the built-in splash");
                }
            }
            s_sfxGroup = FindSfxGroup(splashName);

            s_tickClip = MakeKnock("RowingMod_Tick", 1250f, 0.035f, 0.25f);
            s_knockClip = MakeKnock("RowingMod_Knock", 420f, 0.09f, 0.55f);
            s_fallbackSplashClip = MakeSplash("RowingMod_Splash");

            GameObject tickObject = new GameObject("RowingMod_Tick");
            Object.DontDestroyOnLoad(tickObject);
            s_tickSource = tickObject.AddComponent<AudioSource>();
            s_tickSource.spatialBlend = 0f;
            s_tickSource.playOnAwake = false;
            s_tickSource.outputAudioMixerGroup = s_sfxGroup;
            return true;
        }

        /// <summary>A splash at the blade, louder for a strong stroke. Everyone nearby with the mod hears it.</summary>
        public static void PlaySplash(Vector3 position, bool strong)
        {
            if (!EnsureInitialized())
            {
                return;
            }
            AudioClip clip = s_splashClips != null && s_splashClips.Length > 0
                ? s_splashClips[Random.Range(0, s_splashClips.Length)]
                : s_fallbackSplashClip;
            float volume = RowingPlugin.SplashVolume.Value * (strong ? 1f : 0.55f);
            PlayAt(clip, position, volume, Random.Range(0.92f, 1.08f));
        }

        /// <summary>A wooden knock: an oar clashing with the crew's rhythm.</summary>
        public static void PlayClash(Vector3 position)
        {
            if (!EnsureInitialized())
            {
                return;
            }
            PlayAt(s_knockClip, position, RowingPlugin.SplashVolume.Value, Random.Range(0.95f, 1.05f));
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

        private static void PlayAt(AudioClip clip, Vector3 position, float volume, float pitch)
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
            source.maxDistance = SplashMaxDistance;
            source.outputAudioMixerGroup = s_sfxGroup;
            source.Play();
            Object.Destroy(sound, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
        }

        /// <summary>
        /// The game's SFX mixer group, taken from the splash prefab's AudioSource, or else from any sound prefab,
        /// so the game's SFX volume slider applies to our sounds too.
        /// </summary>
        private static AudioMixerGroup FindSfxGroup(string preferredPrefab)
        {
            if (!string.IsNullOrEmpty(preferredPrefab))
            {
                GameObject prefab = ZNetScene.instance.GetPrefab(preferredPrefab);
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
        /// Lists the game's sound prefabs that might suit rowing (water, splash, wood...), with their clips, once
        /// per session, so a fitting splash can be chosen for Sounds.SplashSound.
        /// </summary>
        private static void LogSoundCandidates()
        {
            if (!RowingPlugin.LogSoundCandidates.Value)
            {
                return;
            }
            string[] keywords = { "water", "splash", "swim", "wave", "paddle", "oar", "row", "boat", "ship", "drum", "wood", "knock", "bubble", "fish",
                "creak", "squeak", "strain", "stress", "rope", "bend", "tree", "door", "chest", "crack" };
            StringBuilder log = new StringBuilder("Sound candidates for rowing (Sounds.SplashSound):");
            int count = 0;
            foreach (GameObject prefab in AllPrefabs())
            {
                if (prefab == null)
                {
                    continue;
                }
                string name = prefab.name.ToLowerInvariant();
                if (!name.Contains("sfx"))
                {
                    continue;
                }
                bool match = false;
                foreach (string keyword in keywords)
                {
                    if (name.Contains(keyword))
                    {
                        match = true;
                        break;
                    }
                }
                if (!match)
                {
                    continue;
                }
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
                log.Append($"\n  {prefab.name}: {string.Join(", ", clips.ToArray())}");
                count++;
            }
            log.Append($"\n  ({count} found)");
            RowingPlugin.Log.LogInfo(log.ToString());
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

        /// <summary>A stand-in splash until a game sound is chosen: a soft, low-passed burst of noise.</summary>
        private static AudioClip MakeSplash(string name)
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
            return MakeClip(name, samples);
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
