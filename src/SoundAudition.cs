using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;

namespace RowingMod
{
    /// <summary>
    /// A temporary listening tool for choosing rowing voice sounds (Debug.Audition, off by default). In a world,
    /// N plays the next sample, B the previous one and L replays it; the top of the screen shows what's playing.
    /// The samples are the game's own voice candidates (at a few pitches) and any 16-bit PCM WAV files in
    /// BepInEx/plugins/RowingMod/audition/.
    /// </summary>
    public class SoundAudition : MonoBehaviour
    {
        private const KeyCode NextKey = KeyCode.N;
        private const KeyCode BackKey = KeyCode.B;
        private const KeyCode ReplayKey = KeyCode.L;

        private struct Sample
        {
            public string Label;
            public AudioClip Clip;
            public float Pitch;
        }

        // Game voice candidates: prefab, description, and the pitches to try each clip at.
        private static readonly (string Prefab, string Description, float[] Pitches)[] GameCandidates =
        {
            ("sfx_goblin_hit", "game male grunt", new[] { 1f, 0.92f, 1.3f }),
            ("sfx_GoblinShaman_hurt", "game male pain", new[] { 1f }),
            ("fx_drown", "game male underwater hurt", new[] { 1f }),
            ("sfx_draugr_hit", "game draugr grunt", new[] { 1f }),
        };

        private List<Sample> m_samples;
        private int m_index = -1;
        private string m_status;

        private void Update()
        {
            if (!RowingPlugin.Audition.Value || Player.m_localPlayer == null)
            {
                return;
            }
            if (Console.IsVisible() || (Chat.instance != null && Chat.instance.HasFocus()) || TextInput.IsVisible() || Menu.IsVisible())
            {
                return;
            }
            int step = ZInput.GetKeyDown(NextKey, logWarning: false) ? 1 : ZInput.GetKeyDown(BackKey, logWarning: false) ? -1 : 0;
            bool replay = ZInput.GetKeyDown(ReplayKey, logWarning: false);
            if (step == 0 && !replay)
            {
                return;
            }
            if (m_samples == null)
            {
                Load();
            }
            if (m_samples.Count == 0)
            {
                return;
            }
            if (m_index < 0)
            {
                m_index = 0; // the first key press starts at the first sample
            }
            else if (step != 0)
            {
                m_index = (m_index + step + m_samples.Count) % m_samples.Count;
            }
            Sample sample = m_samples[m_index];
            RowingSounds.Preview(sample.Clip, sample.Pitch, 0.9f);
        }

        private void Load()
        {
            m_samples = new List<Sample>();
            foreach ((string prefab, string description, float[] pitches) in GameCandidates)
            {
                AudioClip[] clips = RowingSounds.FindGameClips(prefab);
                if (clips == null)
                {
                    continue;
                }
                foreach (float pitch in pitches)
                {
                    foreach (AudioClip clip in clips)
                    {
                        if (clip == null)
                        {
                            continue;
                        }
                        string pitchNote = Mathf.Approximately(pitch, 1f) ? "" : $", pitch {pitch:0.00}";
                        m_samples.Add(new Sample { Label = $"{description}: {clip.name}{pitchNote}", Clip = clip, Pitch = pitch });
                    }
                }
            }

            string folder = Path.Combine(Paths.PluginPath, "RowingMod", "audition");
            int files = 0;
            if (Directory.Exists(folder))
            {
                List<string> paths = new List<string>(Directory.GetFiles(folder, "*.wav"));
                paths.Sort(System.StringComparer.OrdinalIgnoreCase);
                foreach (string path in paths)
                {
                    if (Path.GetFileName(path).StartsWith("._"))
                    {
                        continue; // macOS metadata files on the exFAT drive
                    }
                    AudioClip clip = LoadWav(path);
                    if (clip != null)
                    {
                        m_samples.Add(new Sample { Label = $"file: {Path.GetFileNameWithoutExtension(path)}", Clip = clip, Pitch = 1f });
                        files++;
                    }
                }
            }
            m_status = $"{m_samples.Count} samples ({files} from {folder})";
            RowingPlugin.Log.LogInfo($"Audition: {m_status}");
        }

        /// <summary>Reads a PCM WAV file (8, 16, 24 or 32-bit integer), mixed down to mono.</summary>
        private static AudioClip LoadWav(string path)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                if (bytes.Length < 44 || System.Text.Encoding.ASCII.GetString(bytes, 0, 4) != "RIFF" || System.Text.Encoding.ASCII.GetString(bytes, 8, 4) != "WAVE")
                {
                    RowingPlugin.Log.LogWarning($"Audition: {path} isn't a WAV file");
                    return null;
                }
                int channels = 0;
                int sampleRate = 0;
                int bits = 0;
                int format = 0;
                int position = 12;
                while (position + 8 <= bytes.Length)
                {
                    string id = System.Text.Encoding.ASCII.GetString(bytes, position, 4);
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
                            RowingPlugin.Log.LogWarning($"Audition: {path} is format {format}, {bits}-bit; only integer PCM is supported");
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
                                int offset = body + (frame * channels + channel) * bytesPerSample;
                                sum += ReadSample(bytes, offset, bits);
                            }
                            samples[frame] = sum / channels;
                        }
                        return RowingSounds.MakeClip(Path.GetFileNameWithoutExtension(path), samples, sampleRate);
                    }
                    position = body + size + (size & 1);
                }
                RowingPlugin.Log.LogWarning($"Audition: {path} has no audio data");
            }
            catch (System.Exception e)
            {
                RowingPlugin.Log.LogWarning($"Audition: couldn't read {path}: {e.Message}");
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
                    int value = bytes[offset] | (bytes[offset + 1] << 8) | ((sbyte)bytes[offset + 2] << 16);
                    return value / 8388608f;
                default:
                    return System.BitConverter.ToInt32(bytes, offset) / 2147483648f;
            }
        }

        private void OnGUI()
        {
            if (!RowingPlugin.Audition.Value || Player.m_localPlayer == null)
            {
                return;
            }
            Matrix4x4 previous = RowingUI.BeginScaled();
            try
            {
                string line;
                if (m_samples == null)
                {
                    line = "Sound audition: press N to start (N next, B back, L replay)";
                }
                else if (m_samples.Count == 0)
                {
                    line = "Sound audition: no samples found";
                }
                else
                {
                    string current = m_index >= 0 ? m_samples[m_index].Label : "-";
                    line = $"Sound audition {m_index + 1}/{m_samples.Count}: {current}   (N next, B back, L replay)";
                }
                GUIStyle style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false };
                Rect rect = new Rect(0f, 70f, RowingUI.Width, 24f);
                RowingUI.DrawRect(new Rect(RowingUI.Width / 2f - 420f, rect.y - 4f, 840f, rect.height + 8f), new Color(0f, 0f, 0f, 0.5f));
                RowingUI.Label(rect, line, style);
            }
            finally
            {
                GUI.matrix = previous;
            }
        }
    }
}
