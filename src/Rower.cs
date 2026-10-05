using System.Collections.Generic;
using UnityEngine;

namespace RowingMod
{
    /// <summary>
    /// Runs on each client for the local player. While they sit on a ship seat it reads the row key,
    /// judges the stroke against the ship's beat, spends stamina, broadcasts the stroke and draws the stroke bar.
    /// </summary>
    public class Rower : MonoBehaviour
    {
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
        // Snackbars stay up long enough to read: at least ToastHold, plus time for each character (ReadingSpeed per second).
        private const float ToastHold = 3.5f;
        private const float ReadingSpeed = 15f;
        private const float ToastFadeOut = 0.6f;
        // How long the ship's owner may go without announcing the mod before rowers are warned.
        private const float OwnerGraceTime = 3f;

        private static readonly Vector3[] s_corners = new Vector3[4];

        private Transform m_seat;
        private Ship m_ship;
        private ShipRowing m_shipRowing;
        // The beat (network-clock ms) of this rower's latest stroke; one stroke per beat.
        private long m_lastStrokeBeat;
        private bool m_lastStrokeStrong;
        private bool m_lastStrokeEarly;
        private string m_message;
        private bool m_messageIsStroke;
        private float m_messageUntil;
        private float m_ownerMissingSince = -1f;
        private bool m_ownerWarned;
        // The helmsman's calls as last seen, so each new one shows a snackbar.
        private int m_lastTempo;
        private long m_lastHoldWaterCall;
        // This stint at the oar, summarized when the rower stands up.
        private readonly VoyageStats m_voyage = new VoyageStats();
        // Holding water: braking with the oar while the brake key is held.
        private bool m_braking;
        private float m_brakeHeartbeat;
        // How often a braking rower repeats "still braking", so the owner can drop a brake whose "off" got lost.
        private const float BrakeHeartbeatInterval = 1f;
        // Text styles, made once on first draw, and a reusable content for measuring text.
        private static GUIStyle s_labelStyle;
        private static GUIStyle s_toastTitleStyle;
        private static GUIStyle s_toastBodyStyle;
        private static readonly GUIContent s_content = new GUIContent();
        private string m_toastTitle;
        private string m_toastBody;
        private float m_toastStart;
        private float m_toastHold = ToastHold;
        // What the snackbar on screen is: the helmsman's calls always show at once, other notices too, and tips
        // only when nothing else is up. A tip that something else replaces comes back afterwards.
        private enum ToastKind
        {
            Notice,
            Helm,
            Tip,
        }
        private ToastKind m_toastKind;
        private RowingTips.Tip m_toastTip;
        // Tips waiting their turn, in order; they wait TipGap after the last one, and triggers are checked once a second.
        private const float TipGap = 1.5f;
        private readonly List<RowingTips.Tip> m_tips = new List<RowingTips.Tip>();
        private float m_nextTipTime;
        private float m_nextTipCheck;
        private ShipOars m_oars;
        private readonly List<ShipOars.Bench> m_benches = new List<ShipOars.Bench>();

        private void Update()
        {
            double started = HitchLog.Begin();
            try
            {
                UpdateTimed();
            }
            finally
            {
                HitchLog.End("rower", started);
            }
        }

        private void UpdateTimed()
        {
            Player player = Player.m_localPlayer;
            if (!UpdateSeat(player))
            {
                return;
            }
            UpdateNotices();
            UpdateTips(player);
            m_voyage.Update();
            UpdateBrake(player);

            if (!ZInput.GetKeyDown(RowingPlugin.RowKey.Value, logWarning: false) || IsTyping())
            {
                return;
            }
            if (m_braking)
            {
                Show($"Holding water. Let go of {RowingPlugin.BrakeKey.Value} to row");
                return;
            }

            float cost = RowingPlugin.StaminaPerStroke.Value * StaminaCost.Multiplier(player, m_ship, StaminaCost.Use.Stroke);
            if (!player.HaveStamina(cost))
            {
                Show("Too tired to row");
                TriggerTip(RowingTips.Tip.Tired);
                return;
            }
            player.UseStamina(cost);

            // A press belongs to the nearest beat; the green zone sits on the beat.
            long nowMs = ShipRowing.NowMs();
            m_shipRowing.GetBeat(nowMs, out long beatMs, out long periodMs);
            long nearestMs = nowMs - beatMs <= beatMs + periodMs - nowMs ? beatMs : beatMs + periodMs;
            if (nearestMs == m_lastStrokeBeat)
            {
                Show("Too fast! One stroke per beat");
                return;
            }

            float offset = (nowMs - nearestMs) / (float)periodMs;
            m_lastStrokeBeat = nearestMs;
            // The Rowing skill widens the green zone.
            m_lastStrokeStrong = Mathf.Abs(offset) <= RowingSkill.SweetSpotWidth(player) / 2f;
            m_lastStrokeEarly = offset < 0f;
            // The stroke's strength goes to the owner as its quality, raised by the Rowing skill.
            float quality = (m_lastStrokeStrong ? 1f : RowingPlugin.WeakStrokeFactor.Value) * RowingSkill.StrengthMultiplier(player);
            RowingSkill.Practice(player, m_lastStrokeStrong);
            m_ship.GetComponent<ZNetView>().InvokeRPC(ZNetView.Everybody, ShipRowing.StrokeRpc, quality, nearestMs);
            m_voyage.OnStroke(nearestMs, m_lastStrokeStrong);
            TriggerTip(RowingTips.Tip.Panel);
            m_messageIsStroke = true;
            m_messageUntil = Time.time + MessageTime;
        }

        /// <summary>
        /// Holding water: while the brake key is held (and there's stamina), the rower brakes instead of rowing.
        /// The change is broadcast, and repeated every second while braking so the owner can drop a brake whose
        /// "off" was lost.
        /// </summary>
        private void UpdateBrake(Player player)
        {
            bool wanted = ZInput.GetKey(RowingPlugin.BrakeKey.Value, logWarning: false) && !IsTyping();
            float cost = RowingPlugin.BrakeStaminaPerSecond.Value * StaminaCost.Multiplier(player, m_ship, StaminaCost.Use.Brake) * Time.deltaTime;
            if (wanted && cost > 0f && !player.HaveStamina(cost))
            {
                wanted = false;
                if (m_braking || ZInput.GetKeyDown(RowingPlugin.BrakeKey.Value, logWarning: false))
                {
                    Show("Too tired to brake");
                }
            }
            if (wanted && cost > 0f)
            {
                player.UseStamina(cost);
            }

            if (wanted != m_braking)
            {
                SetBraking(wanted);
                return;
            }
            if (m_braking)
            {
                m_brakeHeartbeat += Time.deltaTime;
                if (m_brakeHeartbeat >= BrakeHeartbeatInterval)
                {
                    m_brakeHeartbeat = 0f;
                    m_ship.GetComponent<ZNetView>().InvokeRPC(ZNetView.Everybody, ShipRowing.BrakeRpc, true);
                }
            }
        }

        private void SetBraking(bool braking)
        {
            m_braking = braking;
            m_brakeHeartbeat = 0f;
            ZNetView nview = m_ship != null ? m_ship.GetComponent<ZNetView>() : null;
            if (nview != null && nview.IsValid())
            {
                nview.InvokeRPC(ZNetView.Everybody, ShipRowing.BrakeRpc, braking);
            }
        }

        /// <summary>
        /// The message for this rower's latest stroke. It's worked out every frame because other rowers' strokes
        /// for the same beat arrive over the network a moment later and can turn it into a sync or a clash.
        /// </summary>
        private string StrokeMessage()
        {
            int strong = m_shipRowing != null ? m_shipRowing.GetStrongCount(m_lastStrokeBeat) : 0;
            if (m_lastStrokeStrong)
            {
                return strong >= 2 ? $"In sync ×{strong}!" : "Strong stroke!";
            }
            if (strong >= 1)
            {
                return "Clash!";
            }
            return m_lastStrokeEarly ? "Early" : "Late";
        }

        /// <summary>Tracks whether the local player sits on a ship seat (a Chair, not the helm).</summary>
        private bool UpdateSeat(Player player)
        {
            Transform attachPoint = (player != null && player.IsAttachedToShip()) ? player.GetAttachPoint() : null;
            if (attachPoint == m_seat && m_seat != null)
            {
                return m_ship != null;
            }

            // Leaving the bench (or switching seats) drops waiting tips (they show again when they next matter), ends
            // this stint's stats and any braking on the old ship.
            m_tips.Clear();
            if (m_toastTitle != null && m_toastKind == ToastKind.Tip)
            {
                m_toastTitle = null;
            }
            if (m_voyage.Finish(player, out string voyageTitle, out string voyageBody))
            {
                // Shown as a snackbar, which stays up (see OnGUI) after standing up.
                Toast(voyageTitle, voyageBody);
            }
            if (m_braking)
            {
                SetBraking(false);
            }
            m_seat = attachPoint;
            m_ship = null;
            m_shipRowing = null;
            if (attachPoint == null)
            {
                return false;
            }

            Ship ship = attachPoint.GetComponentInParent<Ship>();
            ShipRowing shipRowing = ship != null ? ship.GetComponent<ShipRowing>() : null;
            if (shipRowing == null || !IsShipSeat(ship, attachPoint))
            {
                return false;
            }

            m_ship = ship;
            m_shipRowing = shipRowing;
            m_oars = ship.GetComponent<ShipOars>();
            m_lastStrokeBeat = 0;
            m_lastTempo = shipRowing.GetTempo();
            m_lastHoldWaterCall = shipRowing.GetHoldWaterCall();
            m_voyage.Start(ship, shipRowing);

            m_ownerMissingSince = -1f;
            m_ownerWarned = false;
            m_nextTipTime = Time.time;
            if (RowingTips.IsSeen(RowingTips.Tip.Beat))
            {
                Toast("Rowing ready", RowHint());
            }
            else
            {
                TriggerTip(RowingTips.Tip.Beat);
            }
            return true;
        }

        /// <summary>Shows a snackbar for the helmsman's calls, when strokes won't count, and again once they do.</summary>
        private void UpdateNotices()
        {
            if (m_shipRowing != null)
            {
                int tempo = m_shipRowing.GetTempo();
                if (tempo != m_lastTempo)
                {
                    m_lastTempo = tempo;
                    string detail = tempo < 0 ? "A slower beat: easier on stamina" : tempo > 0 ? "A quicker beat: more push, more stamina" : "The beat follows the ship's speed";
                    Toast($"Helmsman: {CrewPanel.TempoName(tempo)}!", detail, ToastKind.Helm);
                }
                long holdWater = m_shipRowing.GetHoldWaterCall();
                if (holdWater != m_lastHoldWaterCall)
                {
                    m_lastHoldWaterCall = holdWater;
                    Toast("Helmsman: Hold water!", $"Hold {RowingPlugin.BrakeKey.Value} to brake", ToastKind.Helm);
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

        private static string RowHint()
        {
            return $"Press {RowingPlugin.RowKey.Value} when the marker reaches the green zone. Hold {RowingPlugin.BrakeKey.Value} to brake.";
        }

        private void Toast(string title, string body, ToastKind kind = ToastKind.Notice)
        {
            // A tip that something else replaces comes back once it's gone.
            if (m_toastTitle != null && m_toastKind == ToastKind.Tip && kind != ToastKind.Tip && !m_tips.Contains(m_toastTip))
            {
                m_tips.Insert(0, m_toastTip);
            }
            m_toastTitle = title;
            m_toastBody = body;
            m_toastStart = Time.time;
            m_toastHold = ReadingTime(title, body);
            m_toastKind = kind;
        }

        /// <summary>How long a snackbar stays up: time to read it, and never less than ToastHold.</summary>
        private static float ReadingTime(string title, string body)
        {
            int characters = (title?.Length ?? 0) + (body?.Length ?? 0);
            return Mathf.Max(ToastHold, 1.5f + characters / ReadingSpeed);
        }

        /// <summary>Queues a tip because it just became relevant, unless it's been seen, is waiting or is showing.</summary>
        private void TriggerTip(RowingTips.Tip tip)
        {
            bool showing = m_toastTitle != null && m_toastKind == ToastKind.Tip && m_toastTip == tip;
            if (!showing && !m_tips.Contains(tip) && !RowingTips.IsSeen(tip))
            {
                m_tips.Add(tip);
            }
        }

        /// <summary>
        /// Watches for the moments a tip becomes relevant, and shows the next waiting tip when no other snackbar is up.
        /// </summary>
        private void UpdateTips(Player player)
        {
            if (m_messageIsStroke && Time.time < m_messageUntil && StrokeMessage() == "Clash!")
            {
                TriggerTip(RowingTips.Tip.Clash);
            }
            if (Time.time >= m_nextTipCheck)
            {
                m_nextTipCheck = Time.time + 1f;
                if (m_oars != null)
                {
                    if (Mathf.Abs(m_oars.Speed) >= RowingTips.BrakeSpeed)
                    {
                        TriggerTip(RowingTips.Tip.Brake);
                    }
                    m_oars.GetBenches(m_benches);
                    foreach (ShipOars.Bench bench in m_benches)
                    {
                        if (bench.Occupant != null && bench.Occupant != player)
                        {
                            TriggerTip(RowingTips.Tip.Together);
                        }
                    }
                }
                if (StaminaCost.Describe(player, m_ship) != null)
                {
                    TriggerTip(RowingTips.Tip.Stamina);
                }
            }
            if (m_toastTitle == null && m_tips.Count > 0 && Time.time >= m_nextTipTime)
            {
                RowingTips.Tip tip = m_tips[0];
                m_tips.RemoveAt(0);
                RowingTips.Text(tip, out string title, out string body);
                Toast(title, body, ToastKind.Tip);
                m_toastTip = tip;
                RowingPlugin.Log.LogInfo($"Tip: showing {tip} for {m_toastHold:0.0} s");
            }
        }

        private static bool IsShipSeat(Ship ship, Transform attachPoint)
        {
            foreach (Chair chair in ship.GetComponentsInChildren<Chair>(includeInactive: true))
            {
                if (chair.m_attachPoint == attachPoint)
                {
                    return ShipRowing.IsRowingSeat(chair);
                }
            }
            return false;
        }

        private static bool IsTyping()
        {
            return Console.IsVisible() || (Chat.instance != null && Chat.instance.HasFocus()) || TextInput.IsVisible()
                || Menu.IsVisible() || InventoryGui.IsVisible();
        }

        private void Show(string message)
        {
            m_message = message;
            m_messageIsStroke = false;
            m_messageUntil = Time.time + MessageTime;
        }

        private void Awake()
        {
            // Everything is drawn with GUI (not GUILayout), so skip IMGUI's layout pass.
            useGUILayout = false;
        }

        private void OnGUI()
        {
            double started = HitchLog.Begin();
            try
            {
                OnGUITimed();
            }
            finally
            {
                HitchLog.End("stroke bar", started);
            }
        }

        private void OnGUITimed()
        {
            bool seated = m_ship != null && m_seat != null;
            if ((!seated && m_toastTitle == null) || !RowingUI.IsRepaint)
            {
                return;
            }
            Matrix4x4 previousMatrix = RowingUI.BeginScaled();
            try
            {
                if (seated)
                {
                    DrawStrokeUI();
                }
                else
                {
                    // Off the bench (e.g. the voyage summary): the snackbar sits where the stroke bar was.
                    DrawToast(GetHudBarsTop() / RowingUI.Scale - BarGap - RowingPlugin.BarOffset.Value);
                }
            }
            finally
            {
                GUI.matrix = previousMatrix;
            }
        }

        /// <summary>The stroke bar, its labels and the snackbar, in virtual pixels (see RowingUI).</summary>
        private void DrawStrokeUI()
        {
            const float width = 320f;
            const float height = 16f;
            float x = (RowingUI.Width - width) / 2f;
            float textX = (RowingUI.Width - TextWidth) / 2f;
            GUIStyle style = s_labelStyle ?? (s_labelStyle = RowingUI.LabelStyle(TextAnchor.MiddleCenter, FontStyle.Bold, false));

            // Stack from the bottom up, measuring each text line, so nothing overlaps whatever the font size:
            // message line (space kept even when empty, so the bar doesn't jump), bar, title, snackbar.
            s_content.text = "Ag";
            float messageHeight = style.CalcHeight(s_content, TextWidth);
            float messageY = GetHudBarsTop() / RowingUI.Scale - BarGap - RowingPlugin.BarOffset.Value - messageHeight;
            // The marker sticks out 4 px above and below the bar.
            float y = messageY - StackGap - 4f - height;

            // Background
            DrawRect(new Rect(x - 2f, y - 2f, width + 4f, height + 4f), new Color(0f, 0f, 0f, 0.6f));

            // The bar spans one beat, centred on the nearest beat: the marker sweeps through the green zone
            // as the beat passes, then jumps back to the left edge halfway to the next beat.
            long nowMs = ShipRowing.NowMs();
            m_shipRowing.GetBeat(nowMs, out long beatMs, out long periodMs);
            long nearestMs = nowMs - beatMs <= beatMs + periodMs - nowMs ? beatMs : beatMs + periodMs;
            float markerPos = Mathf.Clamp01((nowMs - nearestMs) / (float)periodMs + 0.5f);

            // Green zone, with a line on the beat itself
            float sweetWidth = RowingSkill.SweetSpotWidth(Player.m_localPlayer);
            DrawRect(new Rect(x + width * (0.5f - sweetWidth / 2f), y, width * sweetWidth, height), new Color(0.3f, 0.8f, 0.3f, 0.8f));
            DrawRect(new Rect(x + width * 0.5f - 1f, y, 2f, height), new Color(1f, 1f, 1f, 0.35f));

            // Marker, greyed once you've stroked on this beat
            Color markerColor = nearestMs == m_lastStrokeBeat ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            DrawRect(new Rect(x + width * markerPos - 2f, y - 4f, 4f, height + 8f), markerColor);

            // Labels
            string title = m_braking
                ? $"Holding water [{RowingPlugin.BrakeKey.Value}]"
                : ShipRowing.RowDirection(m_ship) < 0f ? $"Row back [{RowingPlugin.RowKey.Value}]" : $"Row [{RowingPlugin.RowKey.Value}]";
            float boost = m_shipRowing != null ? m_shipRowing.GetSyncedBoost() : 0f;
            if (boost > 0.01f)
            {
                title += $"   Crew boost {boost * 100f:0}%";
            }
            s_content.text = title;
            float titleHeight = style.CalcHeight(s_content, TextWidth);
            float titleY = y - 4f - StackGap - titleHeight;
            RowingUI.Label(new Rect(textX, titleY, TextWidth, titleHeight), title, style);

            // What strokes cost right now and why, on its own line above the title (it can be long).
            float top = titleY;
            string stamina = StaminaCost.Describe(Player.m_localPlayer, m_ship);
            if (stamina != null)
            {
                s_content.text = stamina;
                float staminaHeight = style.CalcHeight(s_content, TextWidth);
                top = titleY - staminaHeight;
                RowingUI.Label(new Rect(textX, top, TextWidth, staminaHeight), stamina, style);
            }

            if (Time.time < m_messageUntil)
            {
                string message = m_messageIsStroke ? StrokeMessage() : m_message;
                RowingUI.Label(new Rect(textX, messageY, TextWidth, messageHeight), message, style);
            }

            DrawToast(top - StackGap);
        }

        /// <summary>Draws the snackbar just above the bar's title: fades in while sliding up, holds, then fades out.</summary>
        private void DrawToast(float bottom)
        {
            if (m_toastTitle == null)
            {
                return;
            }

            float t = Time.time - m_toastStart;
            if (t > ToastFadeIn + m_toastHold + ToastFadeOut)
            {
                m_toastTitle = null;
                // A tip counts as seen once it has shown in full. The next tip waits a moment after any snackbar.
                if (m_toastKind == ToastKind.Tip)
                {
                    RowingTips.MarkSeen(m_toastTip);
                }
                m_nextTipTime = Time.time + TipGap;
                return;
            }
            float fadeIn = Mathf.Clamp01(t / ToastFadeIn);
            float fadeOut = Mathf.Clamp01((ToastFadeIn + m_toastHold + ToastFadeOut - t) / ToastFadeOut);
            float alpha = Mathf.Min(fadeIn, fadeOut);
            // Slides down into place from above, so it never covers the bar's title below it.
            float slide = (1f - fadeIn) * (1f - fadeIn) * 12f;

            GUIStyle titleStyle = s_toastTitleStyle ?? (s_toastTitleStyle = RowingUI.LabelStyle(TextAnchor.MiddleCenter, FontStyle.Bold, true));
            GUIStyle bodyStyle = s_toastBodyStyle ?? (s_toastBodyStyle = RowingUI.LabelStyle(TextAnchor.MiddleCenter, FontStyle.Normal, true));
            float textWidth = ToastWidth - 2f * ToastPadding;
            s_content.text = m_toastTitle;
            float titleHeight = titleStyle.CalcHeight(s_content, textWidth);
            s_content.text = m_toastBody;
            float bodyHeight = bodyStyle.CalcHeight(s_content, textWidth);
            float height = ToastPadding + titleHeight + bodyHeight + ToastPadding;

            Rect panel = new Rect((RowingUI.Width - ToastWidth) / 2f, bottom - height - slide, ToastWidth, height);
            DrawRect(panel, new Color(0f, 0f, 0f, 0.7f * alpha));
            DrawRect(new Rect(panel.x, panel.y, 4f, panel.height), new Color(0.3f, 0.8f, 0.3f, alpha));

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            float textX = panel.x + ToastPadding;
            RowingUI.Label(new Rect(textX, panel.y + ToastPadding, textWidth, titleHeight), m_toastTitle, titleStyle);
            RowingUI.Label(new Rect(textX, panel.y + ToastPadding + titleHeight, textWidth, bodyHeight), m_toastBody, bodyStyle);
            GUI.color = previous;
        }

        /// <summary>
        /// Top edge, in screen pixels with y down (divide by RowingUI.Scale for virtual pixels), of the game's stamina, eitr and adrenaline bars,
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

    /// <summary>
    /// Tutorial.ResetOnLogout: logging out and quitting both shut the game down, so the tutorial is marked unseen then
    /// and plays again on the first sit of the next session.
    /// </summary>
    [HarmonyLib.HarmonyPatch(typeof(Game), "Shutdown")]
    internal static class Game_Shutdown_Patch
    {
        private static void Postfix()
        {
            if (RowingPlugin.TutorialResetOnLogout.Value)
            {
                RowingTips.ForgetAll();
            }
        }
    }
}
