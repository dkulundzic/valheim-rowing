using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace RowingMod
{
    /// <summary>
    /// Lives on every ship. The client that owns the ship keeps the ship's beat and turns strokes into force,
    /// weaker the closer the ship is to its top sail speed. The owner is a client on board (the server
    /// hands it to whoever arrives first, and it only moves when the owner leaves), not necessarily the helmsman.
    /// Strokes are broadcast to every client, so all of them can show sync and clashes.
    /// </summary>
    public class ShipRowing : MonoBehaviour
    {
        public const string StrokeRpc = "RowingMod_Stroke2";
        // The 1.0 stroke (quality only, sent to the owner). Still accepted from rowers who haven't updated.
        public const string LegacyStrokeRpc = "RowingMod_Stroke";
        public const string BoostKey = "RowingMod_Boost";
        // Session ID of the last owner that ran this mod. If it differs from the ZDO's owner, the owner is vanilla.
        public const string ModdedOwnerKey = "RowingMod_Owner";
        // The ship's beat: time of the latest beat and the time to the next one, both in network-clock milliseconds.
        public const string BeatTimeKey = "RowingMod_BeatTime";
        public const string BeatPeriodKey = "RowingMod_BeatPeriod";

        private const float BoostSyncInterval = 0.5f;
        // How long to remember a beat's strokes; late strokes for it can still arrive over the network.
        private const double BeatMemorySeconds = 6.0;

        /// <summary>Who stroked on one beat and how well, plus what the owner has already applied for it.</summary>
        private class BeatStrokes
        {
            public readonly Dictionary<long, bool> StrongBySender = new Dictionary<long, bool>();
            public float AppliedBoost;
            public float AppliedBrake;

            public int StrongCount
            {
                get
                {
                    int count = 0;
                    foreach (bool strong in StrongBySender.Values)
                    {
                        if (strong)
                        {
                            count++;
                        }
                    }
                    return count;
                }
            }
        }

        private Ship m_ship;
        private ShipOars m_oars;
        private ZNetView m_nview;
        private Rigidbody m_body;
        private WaterVolume m_waterVolume;
        private float m_boost;
        private float m_brake;
        private float m_boostSyncTimer;
        private float m_topSpeed;
        private float m_lastDirection = 1f;
        private readonly Dictionary<long, BeatStrokes> m_beats = new Dictionary<long, BeatStrokes>();
        private readonly List<long> m_staleBeats = new List<long>();
        // Ship types whose seat spots have been logged, so each type is described once per session.
        private static readonly HashSet<string> s_loggedShipTypes = new HashSet<string>();

        private void Awake()
        {
            m_ship = GetComponent<Ship>();
            m_oars = GetComponent<ShipOars>();
            m_nview = GetComponent<ZNetView>();
            m_body = GetComponent<Rigidbody>();
            if (m_nview == null || m_nview.GetZDO() == null)
            {
                return;
            }

            m_nview.Register<float, long>(StrokeRpc, RPC_Stroke);
            m_nview.Register<float>(LegacyStrokeRpc, RPC_LegacyStroke);
            m_topSpeed = EstimateTopSailSpeed(m_ship);
            LogSeats();
        }

        /// <summary>
        /// Whether a seat spot on a ship is for rowing. Ships also use Chair for standing spots such as
        /// "Hold fast" (m_name "$ship_holdfast"), where you brace yourself rather than sit, so those are left out.
        /// </summary>
        public static bool IsRowingSeat(Chair chair)
        {
            return chair != null && (chair.m_name ?? "").IndexOf("holdfast", System.StringComparison.OrdinalIgnoreCase) < 0;
        }

        private void LogSeats()
        {
            Chair[] chairs = GetComponentsInChildren<Chair>(includeInactive: true);
            int rowingSeats = 0;
            foreach (Chair chair in chairs)
            {
                if (IsRowingSeat(chair))
                {
                    rowingSeats++;
                }
            }
            RowingPlugin.Log.LogInfo($"{name}: {rowingSeats} rowing seat(s) of {chairs.Length} spot(s), top sail speed {m_topSpeed:0.0} m/s");
            if (s_loggedShipTypes.Add(name))
            {
                foreach (Chair chair in chairs)
                {
                    string kind = IsRowingSeat(chair) ? "rowing seat" : "not for rowing";
                    RowingPlugin.Log.LogInfo($"  {chair.name}: name {chair.m_name}, animation {chair.m_attachAnimation}, {kind}");
                }
            }
        }

        /// <summary>
        /// The speed this ship settles at with full sail, the strongest wind and the best wind angle.
        /// The game has no top speed setting; it comes from Ship.CustomFixedUpdate, where each physics step
        /// the sail adds a velocity change and forward drag takes away v² · m_dampingForward · submersion.
        /// Smaller effects (waves, steering) are ignored, so this is an estimate; the log shows it per ship.
        /// </summary>
        private static float EstimateTopSailSpeed(Ship ship)
        {
            // Best forward push over all wind angles, mirroring Ship.GetSailForce and GetWindAngleFactor
            // at full wind intensity. The sail pushes along normalize(wind + forward), whose forward part is cos(θ/2).
            float bestPush = 0f;
            for (int degrees = 0; degrees <= 180; degrees++)
            {
                float angle = degrees * Mathf.Deg2Rad;
                float headwind = -Mathf.Cos(angle);
                float angleFactor = Mathf.Lerp(0.7f, 1f, 1f - Mathf.Abs(headwind)) * (1f - Mathf.Clamp01((headwind - 0.75f) / 0.05f));
                bestPush = Mathf.Max(bestPush, angleFactor * Mathf.Cos(angle / 2f));
            }
            float sailPush = ship.m_sailForceFactor * bestPush;

            // How deep a floating ship sits, as the game's 0..1 submersion factor: buoyancy (m_force) balances gravity.
            float submersion = Mathf.Clamp01(-Physics.gravity.y / (50f * Mathf.Max(0.01f, ship.m_force)));
            float drag = Mathf.Max(0.0001f, ship.m_dampingForward * submersion);
            return Mathf.Sqrt(sailPush / drag);
        }

        private float TopSpeed()
        {
            return m_topSpeed * Mathf.Max(0f, RowingPlugin.TopSpeedMultiplier.Value);
        }

        /// <summary>
        /// How much of a stroke's push gets through at the ship's current speed: all of it when still,
        /// 1 - (v / top)² as it speeds up, and none at or above top sail speed. Speed counts in the rowing direction.
        /// </summary>
        private float SpeedFactor(float direction)
        {
            float topSpeed = TopSpeed();
            if (topSpeed <= 0.01f)
            {
                return 0f;
            }
            float speed = Mathf.Max(0f, m_ship.GetSpeed() * direction);
            float ratio = speed / topSpeed;
            return Mathf.Clamp01(1f - ratio * ratio);
        }

        /// <summary>Which way rowing pushes: -1 while the ship is backing, otherwise +1 (forward), including at Stop.</summary>
        public static float RowDirection(Ship ship)
        {
            return ship.GetSpeedSetting() == Ship.Speed.Back ? -1f : 1f;
        }

        /// <summary>The crew's current boost, readable on every client.</summary>
        public float GetSyncedBoost()
        {
            if (m_nview == null || !m_nview.IsValid())
            {
                return 0f;
            }
            return m_nview.IsOwner() ? m_boost : m_nview.GetZDO().GetFloat(BoostKey);
        }

        /// <summary>
        /// Whether the client that owns the ship runs this mod, so strokes count. Readable on every client;
        /// right after ownership changes it can be false until the new owner's first sync arrives.
        /// </summary>
        public bool HasModdedOwner()
        {
            if (m_nview == null || !m_nview.IsValid())
            {
                return false;
            }
            ZDO zdo = m_nview.GetZDO();
            return zdo.GetLong(ModdedOwnerKey) == zdo.GetOwner();
        }

        /// <summary>
        /// The ship's beat around the given network-clock time (ms): the latest beat at or before it and the
        /// period to the next. Every client reads the owner's schedule from the ZDO and extends it with the same
        /// period, so they all agree on beat times. Without a schedule (no modded owner) it falls back to a fixed
        /// beat on the network clock, so the bar still works.
        /// </summary>
        public void GetBeat(long nowMs, out long beatMs, out long periodMs)
        {
            beatMs = 0;
            periodMs = 0;
            if (m_nview != null && m_nview.IsValid())
            {
                beatMs = m_nview.GetZDO().GetLong(BeatTimeKey);
                periodMs = m_nview.GetZDO().GetLong(BeatPeriodKey);
            }
            if (beatMs <= 0 || periodMs <= 0)
            {
                periodMs = SecondsToMs(RowingPlugin.StrokeCycleStill.Value);
                beatMs = nowMs - Mod(nowMs, periodMs);
                return;
            }
            // Extend the schedule over any beats the owner hasn't announced yet (or that are still in flight).
            if (nowMs >= beatMs)
            {
                beatMs += (nowMs - beatMs) / periodMs * periodMs;
            }
            else
            {
                beatMs -= ((beatMs - nowMs + periodMs - 1) / periodMs) * periodMs;
            }
        }

        /// <summary>How many rowers hit the given beat in the green zone, as seen by this client.</summary>
        public int GetStrongCount(long beatMs)
        {
            return m_beats.TryGetValue(beatMs, out BeatStrokes strokes) ? strokes.StrongCount : 0;
        }

        public static long NowMs()
        {
            return ZNet.instance != null ? (long)(ZNet.instance.GetTimeSeconds() * 1000.0) : 0;
        }

        public static long SecondsToMs(float seconds)
        {
            return (long)Mathf.Max(100f, seconds * 1000f);
        }

        private static long Mod(long value, long divisor)
        {
            long result = value % divisor;
            return result < 0 ? result + divisor : result;
        }

        /// <summary>
        /// A stroke from any rower, broadcast to every client. Each client records it (for "In sync" and "Clash"
        /// messages and, later, oar animation); only the owner turns it into force.
        /// </summary>
        private void RPC_Stroke(long sender, float quality, long beatMs)
        {
            if (!m_beats.TryGetValue(beatMs, out BeatStrokes strokes))
            {
                strokes = new BeatStrokes();
                m_beats[beatMs] = strokes;
            }
            // One stroke per rower per beat; the rower's client already enforces this.
            if (strokes.StrongBySender.ContainsKey(sender))
            {
                return;
            }
            strokes.StrongBySender[sender] = quality >= 0.999f;
            m_oars?.OnStroke(sender, quality >= 0.999f);

            if (m_nview.IsOwner())
            {
                ApplyBeat(strokes);
            }
        }

        /// <summary>
        /// Brings the force from one beat's strokes up to date. Strokes arrive one by one, so a stroke can count as
        /// weak at first and turn into a clash when someone else's well-timed stroke for that beat arrives; the
        /// owner adds or removes the difference from what it already applied for the beat.
        /// </summary>
        private void ApplyBeat(BeatStrokes strokes)
        {
            int strong = strokes.StrongCount;
            int weak = strokes.StrongBySender.Count - strong;
            float strength = RowingPlugin.StrokeStrength.Value;

            float bonus = strong >= 2
                ? Mathf.Min(RowingPlugin.SyncBonusPerRower.Value * (strong - 1), RowingPlugin.MaxSyncBonus.Value)
                : 0f;
            float boost = strong * strength * (1f + bonus);
            float brake = 0f;
            if (strong > 0)
            {
                // Off-beat strokes clash with the crew's rhythm: no push, a little braking.
                brake = weak * RowingPlugin.ClashBrake.Value;
            }
            else
            {
                // Nobody hit the beat, so there's no rhythm to clash with; off-beat strokes are just weak.
                boost += weak * strength * RowingPlugin.WeakStrokeFactor.Value;
            }

            m_boost = Mathf.Clamp(m_boost + boost - strokes.AppliedBoost, 0f, RowingPlugin.MaxBoost.Value);
            m_brake = Mathf.Max(0f, m_brake + brake - strokes.AppliedBrake);
            strokes.AppliedBoost = boost;
            strokes.AppliedBrake = brake;
        }

        private void RPC_LegacyStroke(long sender, float quality)
        {
            if (!m_nview.IsOwner())
            {
                return;
            }
            // A 1.0 rower doesn't follow the beat, so it gets neither sync bonus nor clash.
            quality = Mathf.Clamp01(quality);
            m_boost = Mathf.Min(m_boost + quality * RowingPlugin.StrokeStrength.Value, RowingPlugin.MaxBoost.Value);
        }

        internal void ApplyBoost(float dt)
        {
            if (m_nview == null || !m_nview.IsValid())
            {
                return;
            }
            ForgetOldBeats();
            if (!m_nview.IsOwner())
            {
                return;
            }

            float direction = RowDirection(m_ship);
            // Switching between forward and back drops the old push, so it can't shove the ship the wrong way.
            if (direction != m_lastDirection)
            {
                m_lastDirection = direction;
                m_boost = 0f;
                m_brake = 0f;
                m_beats.Clear();
            }

            UpdateBeat(direction);

            if ((m_boost > 0.001f || m_brake > 0.001f) && IsInWater())
            {
                // Same units as the game's paddle force (m_backwardForce): a velocity change per second.
                // Pushed through the centre of mass so rowing doesn't turn the ship.
                float speedChange = m_ship.m_backwardForce * m_boost * SpeedFactor(direction) * dt;
                // Clashes only slow the ship down; they never push it the other way.
                float speed = Mathf.Max(0f, m_ship.GetSpeed() * direction);
                speedChange -= Mathf.Min(m_ship.m_backwardForce * m_brake * dt, speed);
                m_body.AddForceAtPosition(transform.forward * (direction * speedChange * m_body.mass), m_body.worldCenterOfMass, ForceMode.Impulse);
            }

            float fade = Mathf.Exp(-dt / Mathf.Max(0.05f, RowingPlugin.StrokeFade.Value));
            m_boost = m_boost * fade < 0.001f ? 0f : m_boost * fade;
            m_brake = m_brake * fade < 0.001f ? 0f : m_brake * fade;

            m_boostSyncTimer -= dt;
            if (m_boostSyncTimer <= 0f)
            {
                m_boostSyncTimer = BoostSyncInterval;
                m_nview.GetZDO().Set(BoostKey, m_boost);
            }
            // Unchanged values aren't resent, so this costs nothing after the first frame as owner.
            m_nview.GetZDO().Set(ModdedOwnerKey, ZDOMan.GetSessionID());
        }

        /// <summary>
        /// Keeps the ship's beat while anyone is aboard. At each beat the owner picks the time to the next one
        /// from the ship's speed (slow beat when still, quicker near top speed) and publishes it, so the tempo
        /// only changes between beats. A new owner carries on from the published schedule.
        /// </summary>
        private void UpdateBeat(float direction)
        {
            if (m_ship.CanBeRemoved())
            {
                return; // nobody aboard
            }
            ZDO zdo = m_nview.GetZDO();
            long nowMs = NowMs();
            long beatMs = zdo.GetLong(BeatTimeKey);
            long periodMs = zdo.GetLong(BeatPeriodKey);

            // No schedule yet, or a stale one (the clock jumped after sleeping, or the ship sat unwatched): start fresh.
            if (beatMs <= 0 || periodMs <= 0 || nowMs - beatMs > periodMs + 2000)
            {
                zdo.Set(BeatTimeKey, nowMs);
                zdo.Set(BeatPeriodKey, TempoMs(direction));
                return;
            }
            if (nowMs >= beatMs + periodMs)
            {
                zdo.Set(BeatTimeKey, beatMs + periodMs);
                zdo.Set(BeatPeriodKey, TempoMs(direction));
            }
        }

        /// <summary>Time between beats: StrokeCycleStill when still, down to StrokeCycleTopSpeed at top sail speed.</summary>
        private long TempoMs(float direction)
        {
            float topSpeed = TopSpeed();
            float speedRatio = topSpeed > 0.01f ? Mathf.Clamp01(Mathf.Abs(m_ship.GetSpeed()) / topSpeed) : 0f;
            return SecondsToMs(Mathf.Lerp(RowingPlugin.StrokeCycleStill.Value, RowingPlugin.StrokeCycleTopSpeed.Value, speedRatio));
        }

        private void ForgetOldBeats()
        {
            if (m_beats.Count == 0)
            {
                return;
            }
            long cutoffMs = NowMs() - (long)(BeatMemorySeconds * 1000.0);
            m_staleBeats.Clear();
            foreach (long beatMs in m_beats.Keys)
            {
                if (beatMs < cutoffMs)
                {
                    m_staleBeats.Add(beatMs);
                }
            }
            foreach (long beatMs in m_staleBeats)
            {
                m_beats.Remove(beatMs);
            }
        }

        private bool IsInWater()
        {
            // Mirrors the check in Ship.CustomFixedUpdate, using the centre point only.
            Vector3 center = m_body.worldCenterOfMass;
            float waterLevel = Floating.GetWaterLevel(center, ref m_waterVolume);
            float depth = center.y - waterLevel - m_ship.m_waterLevelOffset;
            return depth <= m_ship.m_disableLevel;
        }
    }

    [HarmonyPatch(typeof(Ship), "Awake")]
    internal static class Ship_Awake_Patch
    {
        private static void Postfix(Ship __instance)
        {
            // Oars first, so ShipRowing finds them in its Awake.
            __instance.gameObject.AddComponent<ShipOars>();
            __instance.gameObject.AddComponent<ShipRowing>();
        }
    }

    [HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
    internal static class Ship_CustomFixedUpdate_Patch
    {
        private static void Postfix(Ship __instance, float fixedDeltaTime)
        {
            ShipRowing rowing = __instance.GetComponent<ShipRowing>();
            if (rowing != null)
            {
                rowing.ApplyBoost(fixedDeltaTime);
            }
        }
    }
}
