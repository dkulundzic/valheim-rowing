using System.Collections.Generic;
using UnityEngine;

namespace RowingMod
{
    /// <summary>
    /// The crew panel: a top-down view of the ship in the bottom-right corner, for rowers and the helmsman. It
    /// shows each rowing bench (empty, occupied, and a flash for each stroke: strong, weak, clash or in sync), the
    /// oars swinging as they really do, the helmsman (a diamond at the helm, no oar), your own place, the ship's
    /// speed setting, the crew's boost and a pulse on each of the ship's beats.
    /// Everything comes from data every client already has (broadcast strokes and seat occupancy).
    /// </summary>
    public class CrewPanel : MonoBehaviour
    {
        private const float PanelWidth = 170f;
        private const float PanelHeight = 270f;
        private const float Margin = 24f;
        private const float Padding = 10f;
        private const float FooterHeight = 56f;
        // The speed gauge between the ship and the footer.
        private const float GaugeHeight = 20f;
        private static readonly Color GaugeColor = new Color(0.55f, 0.85f, 0.55f, 0.95f);
        // Room left beside the hull for oars, in metres. Oars reaching further are clipped at the panel's edge,
        // which keeps the ship itself big in the panel.
        private const float OarRoom = 1.2f;
        // How long a stroke's colour lingers on its bench.
        private const float FlashTime = 0.9f;
        private const float BenchSize = 12f;
        private const float LocalRingSize = 20f;
        private const float HelmSize = 13f;
        private const float LocalHelmRingSize = 21f;

        private static readonly Color Idle = new Color(0.85f, 0.85f, 0.85f, 0.95f);
        private static readonly Color Empty = new Color(0.8f, 0.8f, 0.8f, 0.45f);
        private static readonly Color StrongColor = new Color(0.35f, 0.9f, 0.35f, 1f);
        private static readonly Color WeakColor = new Color(0.95f, 0.8f, 0.25f, 1f);
        private static readonly Color ClashColor = new Color(0.95f, 0.3f, 0.25f, 1f);
        private static readonly Color SyncColor = new Color(1f, 0.72f, 0.1f, 1f);
        private static readonly Color OarColor = new Color(0.86f, 0.66f, 0.4f, 0.95f);
        private static readonly Color BrakeColor = new Color(0.35f, 0.6f, 1f, 1f);
        private static readonly Color BoostColor = new Color(0.4f, 0.8f, 1f, 0.95f);

        private readonly List<ShipOars.Bench> m_benches = new List<ShipOars.Bench>();

        // The panel's ship and its components, found once per frame in Update (OnGUI runs several times a frame).
        private Ship m_ship;
        private ShipOars m_oars;
        private ShipRowing m_rowing;
        // The last seat looked up, and whether it's a rowing bench, so the ship's chairs are searched only when the
        // local player moves to another seat.
        private Transform m_seatChecked;
        private bool m_seatIsBench;
        private static GUIStyle s_nameStyle;
        private static GUIStyle s_footerStyle;
        private static GUIStyle s_gaugeStyle;
        private static GUIStyle s_gaugeTopStyle;
        // The ship last steered, to remind the helmsman of the keys when they take the helm.
        private Ship m_lastHelm;

        private void Awake()
        {
            // Everything is drawn with GUI (not GUILayout), so skip IMGUI's layout pass.
            useGUILayout = false;
        }

        /// <summary>
        /// Finds the panel's ship. At the helm: the war drum, the beat (quicker, slower) and the "Hold water!"
        /// call. Taking the helm shows the keys once.
        /// </summary>
        private void Update()
        {
            double started = HitchLog.Begin();
            try
            {
                UpdateTimed();
            }
            finally
            {
                HitchLog.End("crew panel keys", started);
            }
        }

        private void UpdateTimed()
        {
            Ship panelShip = FindShip();
            if (panelShip != m_ship)
            {
                m_ship = panelShip;
                m_oars = panelShip != null ? panelShip.GetComponent<ShipOars>() : null;
                m_rowing = panelShip != null ? panelShip.GetComponent<ShipRowing>() : null;
            }

            Player player = Player.m_localPlayer;
            Ship ship = player != null ? player.GetControlledShip() : null;
            if (ship != m_lastHelm)
            {
                m_lastHelm = ship;
                if (ship != null)
                {
                    player.Message(MessageHud.MessageType.TopLeft,
                        $"Helm: {RowingPlugin.DrumKey.Value} war drum, {RowingPlugin.TempoUpKey.Value}/{RowingPlugin.TempoDownKey.Value} quicker/slower beat, " +
                        $"{RowingPlugin.HoldWaterCallKey.Value} \"Hold water!\"");
                }
            }
            if (ship == null)
            {
                return;
            }
            if (Console.IsVisible() || (Chat.instance != null && Chat.instance.HasFocus()) || TextInput.IsVisible()
                || Menu.IsVisible() || InventoryGui.IsVisible())
            {
                return;
            }
            ShipRowing rowing = ship.GetComponent<ShipRowing>();
            if (rowing == null)
            {
                return;
            }
            if (ZInput.GetKeyDown(RowingPlugin.DrumKey.Value, logWarning: false))
            {
                bool on = !rowing.IsDrumOn();
                rowing.RequestDrum(on);
                player.Message(MessageHud.MessageType.Center, on ? "War drum on" : "War drum off");
            }
            // Work out the new beat before sending: when the helmsman owns the ship, the call takes effect at once.
            if (ZInput.GetKeyDown(RowingPlugin.TempoUpKey.Value, logWarning: false))
            {
                int tempo = Mathf.Min(1, rowing.GetTempo() + 1);
                rowing.SendHelmCommand(ShipRowing.HelmFaster);
                player.Message(MessageHud.MessageType.Center, $"Beat: {TempoName(tempo)}");
            }
            if (ZInput.GetKeyDown(RowingPlugin.TempoDownKey.Value, logWarning: false))
            {
                int tempo = Mathf.Max(-1, rowing.GetTempo() - 1);
                rowing.SendHelmCommand(ShipRowing.HelmSlower);
                player.Message(MessageHud.MessageType.Center, $"Beat: {TempoName(tempo)}");
            }
            if (ZInput.GetKeyDown(RowingPlugin.HoldWaterCallKey.Value, logWarning: false))
            {
                rowing.SendHelmCommand(ShipRowing.HelmHoldWater);
                player.Message(MessageHud.MessageType.Center, "Hold water!");
            }
        }

        public static string TempoName(int tempo)
        {
            return tempo < 0 ? "Easy" : tempo > 0 ? "Hard" : "Steady";
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
                HitchLog.End("crew panel", started);
            }
        }

        private void OnGUITimed()
        {
            if (!RowingUI.IsRepaint)
            {
                return;
            }
            // The UI's first-time work, once, as soon as the player exists (under the loading fade).
            if (Player.m_localPlayer != null)
            {
                RowingUI.WarmUp();
            }
            if (!RowingPlugin.ShowCrewPanel.Value)
            {
                return;
            }
            Ship ship = m_ship;
            ShipOars oars = m_oars;
            ShipRowing rowing = m_rowing;
            if (ship == null)
            {
                return;
            }
            ShipOars.HullOutline hull = oars != null ? oars.GetHull() : null;
            if (hull == null || rowing == null || hull.Texture == null)
            {
                return;
            }
            Matrix4x4 previous = RowingUI.BeginScaled();
            try
            {
                Draw(ship, oars, rowing, hull);
            }
            finally
            {
                GUI.matrix = previous;
            }
        }

        /// <summary>The ship the local player steers, or rows on (seated on a rowing bench).</summary>
        private Ship FindShip()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return null;
            }
            Ship steered = player.GetControlledShip();
            if (steered != null)
            {
                return steered;
            }
            Transform attachPoint = player.IsAttachedToShip() ? player.GetAttachPoint() : null;
            Ship ship = attachPoint != null ? attachPoint.GetComponentInParent<Ship>() : null;
            if (ship == null)
            {
                return null;
            }
            if (attachPoint != m_seatChecked)
            {
                m_seatChecked = attachPoint;
                m_seatIsBench = false;
                foreach (Chair chair in ship.GetComponentsInChildren<Chair>(includeInactive: true))
                {
                    if (chair.m_attachPoint == attachPoint)
                    {
                        m_seatIsBench = ShipRowing.IsRowingSeat(chair);
                        break;
                    }
                }
            }
            return m_seatIsBench ? ship : null;
        }

        private void Draw(Ship ship, ShipOars oars, ShipRowing rowing, ShipOars.HullOutline hull)
        {
            Rect panel = new Rect(RowingUI.Width - Margin - PanelWidth, RowingUI.Height - Margin - PanelHeight, PanelWidth, PanelHeight);
            RowingUI.DrawRect(panel, new Color(0f, 0f, 0f, 0.3f));

            Rect area = new Rect(panel.x + Padding, panel.y + Padding, panel.width - 2f * Padding, panel.height - 2f * Padding - FooterHeight - GaugeHeight);
            float length = hull.MaxZ - hull.MinZ;
            float halfSpan = hull.MaxHalfWidth + OarRoom;
            float scale = Mathf.Min(area.width / (2f * halfSpan), area.height / length);
            float centreX = area.x + area.width / 2f;
            float top = area.y + (area.height - length * scale) / 2f;
            Vector2 Map(Vector2 p) => new Vector2(centreX + p.x * scale, top + (hull.MaxZ - p.y) * scale);

            // The hull, pulsing on each of the ship's beats.
            double section = HitchLog.Begin();
            long nowMs = ShipRowing.NowMs();
            rowing.GetBeat(nowMs, out long beatMs, out long periodMs);
            float pulse = Mathf.Exp(-(nowMs - beatMs) / 160f);
            Rect hullRect = new Rect(centreX - hull.MaxHalfWidth * scale, top, 2f * hull.MaxHalfWidth * scale, length * scale);
            RowingUI.DrawTexture(hullRect, hull.Texture, Color.white);
            if (pulse > 0.02f)
            {
                RowingUI.DrawTexture(hullRect, hull.Texture, new Color(1f, 0.85f, 0.5f, 0.6f * pulse));
            }

            HitchLog.End("crew panel: hull", section);
            section = HitchLog.Begin();
            oars.GetBenches(m_benches);

            // The crew's boost, along the centre line between the benches.
            if (m_benches.Count > 0)
            {
                float minZ = float.MaxValue;
                float maxZ = float.MinValue;
                foreach (ShipOars.Bench bench in m_benches)
                {
                    minZ = Mathf.Min(minZ, bench.Seat.y);
                    maxZ = Mathf.Max(maxZ, bench.Seat.y);
                }
                if (maxZ - minZ < 1f)
                {
                    minZ -= 0.75f;
                    maxZ += 0.75f;
                }
                Vector2 barTop = Map(new Vector2(0f, maxZ));
                Vector2 barBottom = Map(new Vector2(0f, minZ));
                Rect bar = new Rect(barTop.x - 3f, barTop.y, 6f, barBottom.y - barTop.y);
                RowingUI.DrawRect(bar, new Color(0f, 0f, 0f, 0.5f));
                RowingUI.DrawOutline(new Rect(bar.x - 1f, bar.y - 1f, bar.width + 2f, bar.height + 2f), 1f, new Color(BoostColor.r, BoostColor.g, BoostColor.b, 0.5f));
                float fill = Mathf.Clamp01(rowing.GetSyncedBoost() / Mathf.Max(0.1f, RowingPlugin.MaxBoost.Value));
                RowingUI.DrawRect(new Rect(bar.x, bar.yMax - bar.height * fill, bar.width, bar.height * fill), BoostColor);
            }

            GUIStyle nameStyle = s_nameStyle ?? (s_nameStyle = RowingUI.LabelStyle(TextAnchor.UpperLeft, FontStyle.Normal, false, 11));
            foreach (ShipOars.Bench bench in m_benches)
            {
                bool occupied = bench.Occupant != null;
                // Oars out in the water stand out; stowed ones are thin and faint, so empty benches read as rings.
                Vector2 oarFrom = Map(bench.OarFrom);
                Vector2 oarTo = Map(bench.OarTo);
                if (RowingUI.ClipLine(panel, ref oarFrom, ref oarTo))
                {
                    float stowed = Mathf.Clamp01(bench.Stowed);
                    Color oarColor = new Color(OarColor.r, OarColor.g, OarColor.b, Mathf.Lerp(OarColor.a, 0.25f, stowed));
                    RowingUI.DrawLine(oarFrom, oarTo, Mathf.Lerp(2.5f, 1.2f, stowed), oarColor);
                }

                Vector2 seat = Map(bench.Seat);
                if (!occupied)
                {
                    RowingUI.DrawTexture(Centred(seat, BenchSize), RowingUI.Ring, Empty);
                    continue;
                }
                RowingUI.DrawTexture(Centred(seat, BenchSize), RowingUI.Disc, BenchColor(bench));
                if (bench.Occupant == Player.m_localPlayer)
                {
                    RowingUI.DrawTexture(Centred(seat, LocalRingSize), RowingUI.Ring, Color.white);
                }
                if (RowingPlugin.CrewNames.Value)
                {
                    string playerName = bench.Occupant.GetPlayerName();
                    bool left = bench.Seat.x < 0f;
                    nameStyle.alignment = left ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
                    Rect label = left
                        ? new Rect(seat.x - 12f - 120f, seat.y - 9f, 120f, 18f)
                        : new Rect(seat.x + 12f, seat.y - 9f, 120f, 18f);
                    RowingUI.Label(label, playerName, nameStyle);
                }
            }

            HitchLog.End("crew panel: benches", section);
            section = HitchLog.Begin();
            DrawHelm(ship, Map, nameStyle);
            DrawSpeedGauge(new Rect(area.x, area.yMax + 2f, area.width, GaugeHeight - 4f), oars.Speed, rowing.GetTopSpeed());
            HitchLog.End("crew panel: helm and gauge", section);
            section = HitchLog.Begin();

            // Footer: the ship's speed setting and the crew's boost and, right around a beat the crew hit together,
            // how many were in sync.
            if (s_footerStyle == null)
            {
                s_footerStyle = RowingUI.LabelStyle(TextAnchor.MiddleCenter, FontStyle.Bold, false, 11);
                s_footerStyle.clipping = TextClipping.Overflow;
            }
            GUIStyle footer = s_footerStyle;
            float boost = rowing.GetSyncedBoost();
            Rect line1 = new Rect(panel.x, panel.yMax - Padding - FooterHeight, panel.width, FooterHeight / 3f);
            RowingUI.Label(line1, $"{SpeedSettingName(ship.GetSpeedSetting())} · Crew boost {boost * 100f:0}%", footer);
            long nearestMs = nowMs - beatMs <= periodMs / 2 ? beatMs : beatMs + periodMs;
            int inSync = rowing.GetStrongCount(nearestMs);
            if (inSync >= 2)
            {
                Color previousColor = GUI.color;
                GUI.color = SyncColor;
                RowingUI.Label(new Rect(line1.x, line1.yMax, line1.width, line1.height), $"In sync ×{inSync}", footer);
                GUI.color = previousColor;
            }

            // The war drum, and for the helmsman how to change it.
            bool drumOn = rowing.IsDrumOn();
            bool atHelm = Player.m_localPlayer != null && Player.m_localPlayer.GetControlledShip() == ship;
            string beat = TempoName(rowing.GetTempo());
            string drum = $"Drum: {(drumOn ? "on" : "off")}" + (atHelm ? $" ({RowingPlugin.DrumKey.Value})" : "") + $" · Beat: {beat}";
            RowingUI.Label(new Rect(line1.x, line1.yMax + line1.height, line1.width, line1.height), drum, footer);
            HitchLog.End("crew panel: footer", section);
        }

        /// <summary>
        /// The helm: a diamond where the helmsman stands, without an oar. A dim outline when nobody steers, filled
        /// when someone does, ringed when it's you. Who steers is synced by the game (ShipControlls.GetUser).
        /// </summary>
        private static void DrawHelm(Ship ship, System.Func<Vector2, Vector2> map, GUIStyle nameStyle)
        {
            ShipControlls helm = ship.GetComponentInChildren<ShipControlls>();
            if (helm == null)
            {
                return;
            }
            Transform spot = helm.m_attachPoint != null ? helm.m_attachPoint : helm.transform;
            Vector3 local = ship.transform.InverseTransformPoint(spot.position);
            Vector2 position = map(new Vector2(local.x, local.z));

            Player helmsman = helm.HaveValidUser() ? Player.GetPlayer(helm.GetUser()) : null;
            if (helmsman == null)
            {
                RowingUI.DrawTexture(Centred(position, HelmSize), RowingUI.DiamondRing, Empty);
                return;
            }
            RowingUI.DrawTexture(Centred(position, HelmSize), RowingUI.Diamond, Idle);
            if (helmsman == Player.m_localPlayer)
            {
                RowingUI.DrawTexture(Centred(position, LocalHelmRingSize), RowingUI.DiamondRing, Color.white);
            }
            if (RowingPlugin.CrewNames.Value)
            {
                nameStyle.alignment = TextAnchor.MiddleLeft;
                RowingUI.Label(new Rect(position.x + 12f, position.y - 9f, 120f, 18f), helmsman.GetPlayerName(), nameStyle);
            }
        }

        /// <summary>
        /// The ship's speed against its top sail speed (the most rowing can push it to): "5.2 m/s" and a bar that
        /// fills toward the top speed, which is marked at the right end.
        /// </summary>
        private static void DrawSpeedGauge(Rect rect, float speed, float topSpeed)
        {
            if (s_gaugeStyle == null)
            {
                s_gaugeStyle = RowingUI.LabelStyle(TextAnchor.MiddleLeft, FontStyle.Bold, false, 11);
                s_gaugeStyle.clipping = TextClipping.Overflow;
                s_gaugeTopStyle = new GUIStyle(s_gaugeStyle) { alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Normal };
            }
            GUIStyle text = s_gaugeStyle;
            const float textWidth = 56f;
            RowingUI.Label(new Rect(rect.x, rect.y, textWidth, rect.height), $"{Mathf.Abs(speed):0.0} m/s", text);

            Rect bar = new Rect(rect.x + textWidth, rect.y + rect.height / 2f - 3f, rect.width - textWidth - 28f, 6f);
            RowingUI.DrawRect(bar, new Color(0f, 0f, 0f, 0.5f));
            float fill = topSpeed > 0.01f ? Mathf.Clamp01(Mathf.Abs(speed) / topSpeed) : 0f;
            RowingUI.DrawRect(new Rect(bar.x, bar.y, bar.width * fill, bar.height), GaugeColor);
            RowingUI.DrawRect(new Rect(bar.xMax - 1f, bar.y - 3f, 2f, bar.height + 6f), new Color(1f, 1f, 1f, 0.8f));

            RowingUI.Label(new Rect(bar.xMax, rect.y, rect.xMax - bar.xMax, rect.height), $"{topSpeed:0.0}", s_gaugeTopStyle);
        }

        private static string SpeedSettingName(Ship.Speed speed)
        {
            switch (speed)
            {
                case Ship.Speed.Stop:
                    return "Stopped";
                case Ship.Speed.Back:
                    return "Backing";
                case Ship.Speed.Slow:
                    return "Paddling";
                case Ship.Speed.Half:
                    return "Half sail";
                case Ship.Speed.Full:
                    return "Full sail";
                default:
                    return speed.ToString();
            }
        }

        private static Color BenchColor(ShipOars.Bench bench)
        {
            if (bench.Braking)
            {
                return BrakeColor;
            }
            Color flash;
            switch (bench.Kind)
            {
                case ShipOars.StrokeKind.Strong:
                    flash = StrongColor;
                    break;
                case ShipOars.StrokeKind.Weak:
                    flash = WeakColor;
                    break;
                case ShipOars.StrokeKind.Clash:
                    flash = ClashColor;
                    break;
                case ShipOars.StrokeKind.Sync:
                    flash = SyncColor;
                    break;
                default:
                    return Idle;
            }
            return Color.Lerp(flash, Idle, Mathf.Clamp01(bench.KindAge / FlashTime));
        }

        private static Rect Centred(Vector2 centre, float size)
        {
            return new Rect(centre.x - size / 2f, centre.y - size / 2f, size, size);
        }
    }
}
