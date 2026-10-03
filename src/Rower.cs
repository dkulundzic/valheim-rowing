using UnityEngine;

namespace RowingMod
{
    /// <summary>
    /// Runs on each client for the local player. While they sit on a ship seat it reads the row key,
    /// judges the stroke timing, spends stamina, sends the stroke to the ship's owner and draws the stroke bar.
    /// </summary>
    public class Rower : MonoBehaviour
    {
        // A press before this point in the cycle is mashing: it costs stamina and does nothing.
        private const float MinStrokePhase = 0.5f;
        // How far past the sweet spot the bar runs before the marker stops at the end.
        private const float BarEndPhase = 1.5f;
        private const float MessageTime = 1.2f;
        // Space between the bottom of the rowing UI and the top of the game's stamina bars.
        private const float BarGap = 12f;
        // Vertical space between the stacked pieces: snackbar, title, bar and message.
        private const float StackGap = 6f;
        // Width of the text lines above and below the bar; wider than the bar so lines don't wrap.
        private const float TextWidth = 640f;
        private const float ToastWidth = 520f;
        private const float ToastPadding = 8f;

        // Snackbar timing in seconds.
        private const float ToastFadeIn = 0.25f;
        private const float ToastHold = 3.5f;
        private const float ToastFadeOut = 0.6f;
        // How long the ship's owner may go without announcing the mod before rowers are warned.
        private const float OwnerGraceTime = 3f;

        private static readonly Vector3[] s_corners = new Vector3[4];

        private Transform m_seat;
        private Ship m_ship;
        private ShipRowing m_shipRowing;
        private float m_lastStrokeTime;
        private string m_message;
        private float m_messageUntil;
        private float m_ownerMissingSince = -1f;
        private bool m_ownerWarned;
        private bool m_couldRow;
        private string m_toastTitle;
        private string m_toastBody;
        private float m_toastStart;

        private void Update()
        {
            Player player = Player.m_localPlayer;
            if (!UpdateSeat(player))
            {
                return;
            }
            UpdateNotices();

            if (!ZInput.GetKeyDown(RowingPlugin.RowKey.Value, logWarning: false) || IsTyping())
            {
                return;
            }

            if (!ShipRowing.CanRow(m_ship))
            {
                Show(BlockedReason(m_ship));
                return;
            }

            float cost = RowingPlugin.StaminaPerStroke.Value * StaminaMultiplier(m_ship);
            if (!player.HaveStamina(cost))
            {
                Show("Too tired to row");
                return;
            }
            player.UseStamina(cost);

            float phase = GetPhase();
            m_lastStrokeTime = Time.time;
            if (phase < MinStrokePhase)
            {
                Show("Too fast!");
                return;
            }

            bool inSweetSpot = Mathf.Abs(phase - 1f) <= RowingPlugin.SweetSpotWidth.Value / 2f;
            float quality = inSweetSpot ? 1f : RowingPlugin.WeakStrokeFactor.Value;
            Show(inSweetSpot ? "Strong stroke!" : (phase < 1f ? "Early" : "Late"));
            m_ship.GetComponent<ZNetView>().InvokeRPC(ShipRowing.StrokeRpc, quality);
        }

        /// <summary>Tracks whether the local player sits on a ship seat (a Chair, not the helm).</summary>
        private bool UpdateSeat(Player player)
        {
            Transform attachPoint = (player != null && player.IsAttachedToShip()) ? player.GetAttachPoint() : null;
            if (attachPoint == m_seat && m_seat != null)
            {
                return m_ship != null;
            }

            m_seat = attachPoint;
            m_ship = null;
            m_shipRowing = null;
            if (attachPoint == null)
            {
                return false;
            }

            Ship ship = attachPoint.GetComponentInParent<Ship>();
            if (ship == null || !IsShipSeat(ship, attachPoint))
            {
                return false;
            }

            m_ship = ship;
            m_shipRowing = ship.GetComponent<ShipRowing>();
            // Start the cycle when they sit down, so the first stroke can be well timed.
            m_lastStrokeTime = Time.time;

            m_couldRow = ShipRowing.CanRow(ship);
            m_ownerMissingSince = -1f;
            m_ownerWarned = false;
            Toast("Rowing ready", m_couldRow ? RowHint() : BlockedReason(ship));
            return true;
        }

        /// <summary>Shows a snackbar when rowing becomes possible or blocked, or when strokes won't count.</summary>
        private void UpdateNotices()
        {
            bool canRow = ShipRowing.CanRow(m_ship);
            if (canRow != m_couldRow)
            {
                m_couldRow = canRow;
                if (canRow)
                {
                    Toast("You can row now", RowHint());
                }
                else
                {
                    Toast("Rowing paused", BlockedReason(m_ship));
                }
            }

            // Strokes go to the ship's owner, so they do nothing if that player doesn't run the mod.
            // Wait a moment before warning, since a new owner takes a sync or two to announce itself.
            if (m_shipRowing != null && m_shipRowing.HasModdedOwner())
            {
                if (m_ownerWarned)
                {
                    Toast("Your strokes count again", "The ship's owner now has the Rowing mod");
                }
                m_ownerMissingSince = -1f;
                m_ownerWarned = false;
            }
            else if (m_ownerMissingSince < 0f)
            {
                m_ownerMissingSince = Time.time;
            }
            else if (!m_ownerWarned && Time.time - m_ownerMissingSince > OwnerGraceTime)
            {
                m_ownerWarned = true;
                Toast("Your strokes won't count", "The ship's owner doesn't have the Rowing mod");
            }
        }

        /// <summary>
        /// How much more a stroke costs when rowing into the wind: 1 with no headwind, up to
        /// 1 + HeadwindStaminaFactor straight into a full-strength wind. A tailwind costs no less than normal.
        /// </summary>
        private static float StaminaMultiplier(Ship ship)
        {
            EnvMan env = EnvMan.instance;
            if (env == null)
            {
                return 1f;
            }
            // GetWindDir is where the wind blows to, so rowing into it means the wind points against the rowing direction.
            Vector3 rowDir = ship.transform.forward * ShipRowing.RowDirection(ship);
            Vector3 wind = env.GetWindDir();
            wind.y = 0f;
            rowDir.y = 0f;
            float headwind = Mathf.Max(0f, Vector3.Dot(wind.normalized, -rowDir.normalized));
            return 1f + Mathf.Max(0f, RowingPlugin.HeadwindStaminaFactor.Value) * headwind * Mathf.Clamp01(env.GetWindIntensity());
        }

        private static string RowHint()
        {
            return $"Press {RowingPlugin.RowKey.Value} when the marker reaches the green zone";
        }

        private static string BlockedReason(Ship ship)
        {
            return "The ship is stopped. Paddle or open the sail to row";
        }

        private void Toast(string title, string body)
        {
            m_toastTitle = title;
            m_toastBody = body;
            m_toastStart = Time.time;
        }

        private static bool IsShipSeat(Ship ship, Transform attachPoint)
        {
            foreach (Chair chair in ship.GetComponentsInChildren<Chair>(includeInactive: true))
            {
                if (chair.m_attachPoint == attachPoint)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsTyping()
        {
            return Console.IsVisible() || (Chat.instance != null && Chat.instance.HasFocus()) || TextInput.IsVisible()
                || Menu.IsVisible() || InventoryGui.IsVisible();
        }

        private float GetPhase()
        {
            return (Time.time - m_lastStrokeTime) / Mathf.Max(0.1f, RowingPlugin.StrokeCycle.Value);
        }

        private void Show(string message)
        {
            m_message = message;
            m_messageUntil = Time.time + MessageTime;
        }

        private void OnGUI()
        {
            if (m_ship == null || m_seat == null)
            {
                return;
            }

            const float width = 320f;
            const float height = 16f;
            float x = (Screen.width - width) / 2f;
            float textX = (Screen.width - TextWidth) / 2f;
            GUIStyle style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false };

            // Stack from the bottom up, measuring each text line, so nothing overlaps whatever the font size:
            // message line (space kept even when empty, so the bar doesn't jump), bar, title, snackbar.
            float messageHeight = style.CalcHeight(new GUIContent("Ag"), TextWidth);
            float messageY = GetHudBarsTop() - BarGap - RowingPlugin.BarOffset.Value - messageHeight;
            // The marker sticks out 4 px above and below the bar.
            float y = messageY - StackGap - 4f - height;
            bool canRow = ShipRowing.CanRow(m_ship);
            float alpha = canRow ? 1f : 0.35f;

            // Background
            DrawRect(new Rect(x - 2f, y - 2f, width + 4f, height + 4f), new Color(0f, 0f, 0f, 0.6f * alpha));

            // Sweet spot
            float sweetWidth = RowingPlugin.SweetSpotWidth.Value;
            float sweetStart = (1f - sweetWidth / 2f) / BarEndPhase;
            float sweetEnd = (1f + sweetWidth / 2f) / BarEndPhase;
            DrawRect(new Rect(x + width * sweetStart, y, width * (sweetEnd - sweetStart), height), new Color(0.3f, 0.8f, 0.3f, 0.8f * alpha));

            // Too-fast zone
            DrawRect(new Rect(x, y, width * (MinStrokePhase / BarEndPhase), height), new Color(0.8f, 0.25f, 0.2f, 0.5f * alpha));

            // Marker
            float markerPhase = canRow ? Mathf.Min(GetPhase(), BarEndPhase) : 0f;
            DrawRect(new Rect(x + width * (markerPhase / BarEndPhase) - 2f, y - 4f, 4f, height + 8f), new Color(1f, 1f, 1f, alpha));

            // Labels
            string title;
            if (!canRow)
            {
                title = "Stopped";
            }
            else
            {
                title = ShipRowing.RowDirection(m_ship) < 0f ? $"Row back [{RowingPlugin.RowKey.Value}]" : $"Row [{RowingPlugin.RowKey.Value}]";
            }
            float staminaMultiplier = StaminaMultiplier(m_ship);
            if (canRow && staminaMultiplier > 1.05f)
            {
                title += $"   Headwind: +{(staminaMultiplier - 1f) * 100f:0}% stamina";
            }
            float boost = m_shipRowing != null ? m_shipRowing.GetSyncedBoost() : 0f;
            if (boost > 0.01f)
            {
                title += $"   Crew boost {boost * 100f:0}%";
            }
            float titleHeight = style.CalcHeight(new GUIContent(title), TextWidth);
            float titleY = y - 4f - StackGap - titleHeight;
            GUI.Label(new Rect(textX, titleY, TextWidth, titleHeight), title, style);

            if (m_message != null && Time.time < m_messageUntil)
            {
                GUI.Label(new Rect(textX, messageY, TextWidth, messageHeight), m_message, style);
            }

            DrawToast(titleY - StackGap);
        }

        /// <summary>Draws the snackbar just above the bar's title: fades in while sliding up, holds, then fades out.</summary>
        private void DrawToast(float bottom)
        {
            if (m_toastTitle == null)
            {
                return;
            }

            float t = Time.time - m_toastStart;
            if (t > ToastFadeIn + ToastHold + ToastFadeOut)
            {
                m_toastTitle = null;
                return;
            }
            float fadeIn = Mathf.Clamp01(t / ToastFadeIn);
            float fadeOut = Mathf.Clamp01((ToastFadeIn + ToastHold + ToastFadeOut - t) / ToastFadeOut);
            float alpha = Mathf.Min(fadeIn, fadeOut);
            // Slides down into place from above, so it never covers the bar's title below it.
            float slide = (1f - fadeIn) * (1f - fadeIn) * 12f;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true };
            GUIStyle bodyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            float textWidth = ToastWidth - 2f * ToastPadding;
            float titleHeight = titleStyle.CalcHeight(new GUIContent(m_toastTitle), textWidth);
            float bodyHeight = bodyStyle.CalcHeight(new GUIContent(m_toastBody), textWidth);
            float height = ToastPadding + titleHeight + bodyHeight + ToastPadding;

            Rect panel = new Rect((Screen.width - ToastWidth) / 2f, bottom - height - slide, ToastWidth, height);
            DrawRect(panel, new Color(0f, 0f, 0f, 0.7f * alpha));
            DrawRect(new Rect(panel.x, panel.y, 4f, panel.height), new Color(0.3f, 0.8f, 0.3f, alpha));

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            float textX = panel.x + ToastPadding;
            GUI.Label(new Rect(textX, panel.y + ToastPadding, textWidth, titleHeight), m_toastTitle, titleStyle);
            GUI.Label(new Rect(textX, panel.y + ToastPadding + titleHeight, textWidth, bodyHeight), m_toastBody, bodyStyle);
            GUI.color = previous;
        }

        /// <summary>
        /// Top edge, in GUI coordinates (y down), of the game's stamina, eitr and adrenaline bars,
        /// which sit at the bottom centre and stack when shown. Falls back to a fixed height without a HUD.
        /// </summary>
        private static float GetHudBarsTop()
        {
            Hud hud = Hud.instance;
            float top = Screen.height - 190f;
            if (hud == null)
            {
                return top;
            }

            foreach (RectTransform bar in new[] { hud.m_staminaBar2Root, hud.m_eitrBarRoot, hud.m_adrenalineBarRoot })
            {
                if (bar == null || !bar.gameObject.activeInHierarchy)
                {
                    continue;
                }
                Canvas canvas = bar.GetComponentInParent<Canvas>();
                Camera camera = (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;
                bar.GetWorldCorners(s_corners);
                for (int i = 0; i < s_corners.Length; i++)
                {
                    // Screen space has y up; IMGUI has y down.
                    float guiY = Screen.height - RectTransformUtility.WorldToScreenPoint(camera, s_corners[i]).y;
                    top = Mathf.Min(top, guiY);
                }
            }
            return top;
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
