using UnityEngine;

namespace RowingMod
{
    /// <summary>
    /// Shared IMGUI helpers: UI scaling and the few textures the mod draws with. IMGUI draws in screen pixels, so
    /// on a large screen (e.g. 5K) everything would be tiny; drawing happens in "virtual" pixels instead, scaled by
    /// UI.Scale (automatic by default: the screen height over 1080).
    /// </summary>
    public static class RowingUI
    {
        private static Texture2D s_disc;
        private static Texture2D s_ring;
        // ClipLine's scratch space, reused so drawing the panel doesn't allocate (IMGUI runs on the main thread only).
        private static readonly float[] s_clipP = new float[4];
        private static readonly float[] s_clipQ = new float[4];

        /// <summary>
        /// Whether an OnGUI call should draw. The mod's UI only draws (no buttons or layout), so it skips every event
        /// but Repaint: OnGUI runs several times a frame, and the other passes would only redo the same work.
        /// </summary>
        public static bool IsRepaint => Event.current.type == EventType.Repaint;

        private static bool s_warm;

        // Every character the mod's UI shows, so the font has them ready at each size and style.
        private const string Glyphs = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 .,:;!?'\"()[]/%+-×·…";

        /// <summary>
        /// Does the UI's first-time work ahead of time: the shared textures, and the font's characters at every size
        /// and style the mod uses. Unity prepares both on first use, which froze the game for ~0.7 s the first time
        /// the crew panel appeared (2026-10-05). Call it inside OnGUI, under the loading fade; it runs once.
        /// </summary>
        public static void WarmUp()
        {
            if (s_warm)
            {
                return;
            }
            s_warm = true;
            System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
            _ = Disc;
            _ = Ring;
            _ = Diamond;
            _ = DiamondRing;
            GUIContent content = new GUIContent(Glyphs);
            foreach (int size in new[] { 0, 11 })
            {
                foreach (FontStyle style in new[] { FontStyle.Normal, FontStyle.Bold })
                {
                    LabelStyle(TextAnchor.MiddleLeft, style, false, size).CalcSize(content);
                }
            }
            RowingPlugin.Log.LogInfo($"UI ready in {watch.ElapsedMilliseconds} ms");
        }

        /// <summary>A copy of the skin's label style with changes, for caching: GUI.skin is only readable inside OnGUI.</summary>
        public static GUIStyle LabelStyle(TextAnchor alignment, FontStyle fontStyle, bool wordWrap, int fontSize = 0)
        {
            return new GUIStyle(GUI.skin.label) { alignment = alignment, fontStyle = fontStyle, wordWrap = wordWrap, fontSize = fontSize };
        }

        public static float Scale
        {
            get
            {
                float setting = RowingPlugin.UIScale.Value;
                return setting > 0.01f ? setting : Mathf.Max(1f, Screen.height / 1080f);
            }
        }

        /// <summary>Screen width and height in virtual pixels.</summary>
        public static float Width => Screen.width / Scale;
        public static float Height => Screen.height / Scale;

        /// <summary>Starts drawing in virtual pixels. Returns the previous matrix, to restore with GUI.matrix.</summary>
        public static Matrix4x4 BeginScaled()
        {
            Matrix4x4 previous = GUI.matrix;
            float scale = Scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            return previous;
        }

        /// <summary>A label with a dark drop shadow, so it stays readable over bright sand, water or sky.</summary>
        public static void Label(Rect rect, string text, GUIStyle style)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.8f * previous.a);
            GUI.Label(new Rect(rect.x + 1.2f, rect.y + 1.2f, rect.width, rect.height), text, style);
            GUI.color = previous;
            GUI.Label(rect, text, style);
        }

        /// <summary>A rectangle's outline, <paramref name="thickness"/> wide.</summary>
        public static void DrawOutline(Rect rect, float thickness, Color color)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        /// <summary>
        /// Clips the line a-b to a rectangle (Liang-Barsky). Returns false when it lies entirely outside.
        /// </summary>
        public static bool ClipLine(Rect rect, ref Vector2 a, ref Vector2 b)
        {
            float t0 = 0f;
            float t1 = 1f;
            Vector2 d = b - a;
            float[] p = s_clipP;
            float[] q = s_clipQ;
            p[0] = -d.x; p[1] = d.x; p[2] = -d.y; p[3] = d.y;
            q[0] = a.x - rect.xMin; q[1] = rect.xMax - a.x; q[2] = a.y - rect.yMin; q[3] = rect.yMax - a.y;
            for (int i = 0; i < 4; i++)
            {
                if (Mathf.Approximately(p[i], 0f))
                {
                    if (q[i] < 0f)
                    {
                        return false;
                    }
                    continue;
                }
                float t = q[i] / p[i];
                if (p[i] < 0f)
                {
                    t0 = Mathf.Max(t0, t);
                }
                else
                {
                    t1 = Mathf.Min(t1, t);
                }
            }
            if (t0 > t1)
            {
                return false;
            }
            Vector2 start = a;
            a = start + d * t0;
            b = start + d * t1;
            return true;
        }

        public static void DrawRect(Rect rect, Color color)
        {
            DrawTexture(rect, Texture2D.whiteTexture, color);
        }

        public static void DrawTexture(Rect rect, Texture texture, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texture);
            GUI.color = previous;
        }

        /// <summary>A line from a to b, <paramref name="thickness"/> wide, drawn as a rotated rectangle.</summary>
        public static void DrawLine(Vector2 a, Vector2 b, float thickness, Color color)
        {
            Vector2 delta = b - a;
            float length = delta.magnitude;
            if (length < 0.01f)
            {
                return;
            }
            Matrix4x4 previous = GUI.matrix;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            // Rotate about a in the current (scaled) coordinates. GUIUtility.RotateAroundPivot would take the pivot
            // in unscaled screen pixels, which puts it in the wrong place once the UI is scaled.
            Vector3 pivot = new Vector3(a.x, a.y, 0f);
            GUI.matrix = previous * Matrix4x4.TRS(pivot, Quaternion.Euler(0f, 0f, angle), Vector3.one) * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
            DrawRect(new Rect(a.x, a.y - thickness / 2f, length, thickness), color);
            GUI.matrix = previous;
        }

        /// <summary>A filled, anti-aliased white diamond (a square on its corner), to tint with GUI.color.</summary>
        public static Texture2D Diamond => s_diamond ?? (s_diamond = MakeDiamond(64, 0f));

        /// <summary>An anti-aliased white diamond outline, to tint with GUI.color.</summary>
        public static Texture2D DiamondRing => s_diamondRing ?? (s_diamondRing = MakeDiamond(64, 0.22f));

        private static Texture2D s_diamond;
        private static Texture2D s_diamondRing;

        private static Texture2D MakeDiamond(int size, float holeFraction)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            float radius = size / 2f;
            float hole = radius * (1f - holeFraction);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Distance in the diamond's own metric (|x| + |y|), scaled so the edge is anti-aliased by a pixel.
                    float distance = (Mathf.Abs(x + 0.5f - radius) + Mathf.Abs(y + 0.5f - radius)) / 1.4142f;
                    float edge = radius / 1.4142f;
                    float alpha = Mathf.Clamp01(edge - distance);
                    if (holeFraction > 0f)
                    {
                        alpha *= Mathf.Clamp01(distance - hole / 1.4142f);
                    }
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return texture;
        }

        /// <summary>A filled, anti-aliased white disc, to tint with GUI.color.</summary>
        public static Texture2D Disc => s_disc ?? (s_disc = MakeCircle(64, 0f));

        /// <summary>An anti-aliased white ring, to tint with GUI.color.</summary>
        public static Texture2D Ring => s_ring ?? (s_ring = MakeCircle(64, 0.22f));

        private static Texture2D MakeCircle(int size, float holeFraction)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            float radius = size / 2f;
            float hole = radius * (1f - holeFraction);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                    float alpha = Mathf.Clamp01(radius - distance);
                    if (holeFraction > 0f)
                    {
                        alpha *= Mathf.Clamp01(distance - hole);
                    }
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return texture;
        }
    }
}
