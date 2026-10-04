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

        // Colour-blind mode (UI.ColorblindMode): the Okabe-Ito palette, which stays distinct for the common kinds of
        // colour blindness, plus a symbol on each stroke flash so its meaning never rests on colour alone.
        private static readonly Color SafeStrong = new Color(0f, 0.62f, 0.45f, 1f);      // bluish green
        private static readonly Color SafeWeak = new Color(0.94f, 0.89f, 0.26f, 1f);     // yellow
        private static readonly Color SafeClash = new Color(0.84f, 0.37f, 0f, 1f);       // vermillion
        private static readonly Color SafeSync = new Color(0.34f, 0.71f, 0.91f, 1f);     // sky blue
        private static readonly Color SafeBrake = new Color(0f, 0.45f, 0.7f, 1f);        // blue
        private static readonly Color GlyphColor = new Color(0f, 0f, 0f, 0.85f);
        private static readonly Color BoostColor = new Color(0.4f, 0.8f, 1f, 0.95f);

        private readonly List<ShipOars.Bench> m_benches = new List<ShipOars.Bench>();

        /// <summary>At the helm, the drum key turns the ship's war drum on or off.</summary>
        private void Update()
        {
            Player player = Player.m_localPlayer;
            Ship ship = player != null ? player.GetControlledShip() : null;
            if (ship == null || !ZInput.GetKeyDown(RowingPlugin.DrumKey.Value, logWarning: false))
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
            bool on = !rowing.IsDrumOn();
            rowing.RequestDrum(on);
            player.Message(MessageHud.MessageType.Center, on ? "War drum on" : "War drum off");
        }

        private void OnGUI()
        {
            if (!RowingPlugin.ShowCrewPanel.Value)
            {
                return;
            }
            Ship ship = FindShip();
            ShipOars oars = ship != null ? ship.GetComponent<ShipOars>() : null;
            ShipRowing rowing = ship != null ? ship.GetComponent<ShipRowing>() : null;
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
        private static Ship FindShip()
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
            foreach (Chair chair in ship.GetComponentsInChildren<Chair>(includeInactive: true))
            {
                if (chair.m_attachPoint == attachPoint)
                {
                    return ShipRowing.IsRowingSeat(chair) ? ship : null;
                }
            }
            return null;
        }

        private void Draw(Ship ship, ShipOars oars, ShipRowing rowing, ShipOars.HullOutline hull)
        {
            Rect panel = new Rect(RowingUI.Width - Margin - PanelWidth, RowingUI.Height - Margin - PanelHeight, PanelWidth, PanelHeight);
            RowingUI.DrawRect(panel, new Color(0f, 0f, 0f, 0.3f));

            Rect area = new Rect(panel.x + Padding, panel.y + Padding, panel.width - 2f * Padding, panel.height - 2f * Padding - FooterHeight);
            float length = hull.MaxZ - hull.MinZ;
            float halfSpan = hull.MaxHalfWidth + OarRoom;
            float scale = Mathf.Min(area.width / (2f * halfSpan), area.height / length);
            float centreX = area.x + area.width / 2f;
            float top = area.y + (area.height - length * scale) / 2f;
            Vector2 Map(Vector2 p) => new Vector2(centreX + p.x * scale, top + (hull.MaxZ - p.y) * scale);

            // The hull, pulsing on each of the ship's beats.
            long nowMs = ShipRowing.NowMs();
            rowing.GetBeat(nowMs, out long beatMs, out long periodMs);
            float pulse = Mathf.Exp(-(nowMs - beatMs) / 160f);
            Rect hullRect = new Rect(centreX - hull.MaxHalfWidth * scale, top, 2f * hull.MaxHalfWidth * scale, length * scale);
            RowingUI.DrawTexture(hullRect, hull.Texture, Color.white);
            if (pulse > 0.02f)
            {
                RowingUI.DrawTexture(hullRect, hull.Texture, new Color(1f, 0.85f, 0.5f, 0.6f * pulse));
            }

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

            GUIStyle nameStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = false };
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
                if (RowingPlugin.ColorblindMode.Value)
                {
                    DrawGlyph(seat, bench);
                }
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

            DrawHelm(ship, Map, nameStyle);

            // Footer: the ship's speed setting and the crew's boost and, right around a beat the crew hit together,
            // how many were in sync.
            GUIStyle footer = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false, fontSize = 11, clipping = TextClipping.Overflow };
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
            string drum = $"Drum: {(drumOn ? "on" : "off")}" + (atHelm ? $" ({RowingPlugin.DrumKey.Value} to turn {(drumOn ? "off" : "on")})" : "");
            RowingUI.Label(new Rect(line1.x, line1.yMax + line1.height, line1.width, line1.height), drum, footer);
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
            bool safe = RowingPlugin.ColorblindMode.Value;
            if (bench.Braking)
            {
                return safe ? SafeBrake : BrakeColor;
            }
            Color flash;
            switch (bench.Kind)
            {
                case ShipOars.StrokeKind.Strong:
                    flash = safe ? SafeStrong : StrongColor;
                    break;
                case ShipOars.StrokeKind.Weak:
                    flash = safe ? SafeWeak : WeakColor;
                    break;
                case ShipOars.StrokeKind.Clash:
                    flash = safe ? SafeClash : ClashColor;
                    break;
                case ShipOars.StrokeKind.Sync:
                    flash = safe ? SafeSync : SyncColor;
                    break;
                default:
                    return Idle;
            }
            return Color.Lerp(flash, Idle, Mathf.Clamp01(bench.KindAge / FlashTime));
        }

        /// <summary>
        /// Colour-blind mode: a symbol on the bench while its stroke flash shows (and while braking), so the result
        /// reads without colour: ✓ strong, — weak, ✕ clash, ✱ in sync, = braking.
        /// </summary>
        private static void DrawGlyph(Vector2 c, ShipOars.Bench bench)
        {
            const float t = 1.6f;
            if (bench.Braking)
            {
                RowingUI.DrawLine(c + new Vector2(-4f, -2f), c + new Vector2(4f, -2f), t, GlyphColor);
                RowingUI.DrawLine(c + new Vector2(-4f, 2f), c + new Vector2(4f, 2f), t, GlyphColor);
                return;
            }
            if (bench.Kind == ShipOars.StrokeKind.None || bench.KindAge > FlashTime)
            {
                return;
            }
            switch (bench.Kind)
            {
                case ShipOars.StrokeKind.Strong:
                    RowingUI.DrawLine(c + new Vector2(-4f, 0f), c + new Vector2(-1f, 3f), t, GlyphColor);
                    RowingUI.DrawLine(c + new Vector2(-1f, 3f), c + new Vector2(4f, -3f), t, GlyphColor);
                    break;
                case ShipOars.StrokeKind.Weak:
                    RowingUI.DrawLine(c + new Vector2(-4f, 0f), c + new Vector2(4f, 0f), t, GlyphColor);
                    break;
                case ShipOars.StrokeKind.Clash:
                    RowingUI.DrawLine(c + new Vector2(-3.5f, -3.5f), c + new Vector2(3.5f, 3.5f), t, GlyphColor);
                    RowingUI.DrawLine(c + new Vector2(-3.5f, 3.5f), c + new Vector2(3.5f, -3.5f), t, GlyphColor);
                    break;
                case ShipOars.StrokeKind.Sync:
                    RowingUI.DrawLine(c + new Vector2(-4f, 0f), c + new Vector2(4f, 0f), t, GlyphColor);
                    RowingUI.DrawLine(c + new Vector2(-2.5f, -3.5f), c + new Vector2(2.5f, 3.5f), t, GlyphColor);
                    RowingUI.DrawLine(c + new Vector2(-2.5f, 3.5f), c + new Vector2(2.5f, -3.5f), t, GlyphColor);
                    break;
            }
        }

        private static Rect Centred(Vector2 centre, float size)
        {
            return new Rect(centre.x - size / 2f, centre.y - size / 2f, size, size);
        }
    }
}
