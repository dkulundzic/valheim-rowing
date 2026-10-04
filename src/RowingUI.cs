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
