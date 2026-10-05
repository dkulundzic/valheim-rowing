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
        // A rower holding water (braking) or letting go; repeated every second while braking.
        public const string BrakeRpc = "RowingMod_Brake";
        // The helmsman turning the war drum on or off; sent to the owner, who stores it on the ship.
        public const string DrumRpc = "RowingMod_Drum";
        public const string DrumKey = "RowingMod_Drum";
        // The war drum's pattern (0..DrumPatterns.Count-1), picked by the helmsman and kept on the ship by the owner.
        public const string DrumPatternRpc = "RowingMod_DrumPattern";
        public const string DrumPatternKey = "RowingMod_DrumPattern";
        // The number of the latest beat, counted by the owner, so every client agrees which measures end a phrase.
        public const string BeatIndexKey = "RowingMod_BeatIndex";
        // The helmsman's calls: commands go to the owner, who keeps the state on the ship for everyone.
        public const string HelmRpc = "RowingMod_Helm";
        public const string TempoKey = "RowingMod_Tempo";                // -1 Easy, 0 Steady, 1 Hard
        public const string RammingReadyKey = "RowingMod_RammingReady";  // network-clock ms
        // A ramming-speed run, planned by the owner when the helmsman calls it: the beat it starts on (a speed-up
        // measure, then the lead-in, the ramming measures and the release), the beat length before it, how many
        // ramming measures, and a seed for the drum's random lead-in, ramming rhythm and release.
        public const string RamStartKey = "RowingMod_RamStart";        // beat index of the speed-up measure
        public const string RamBaseKey = "RowingMod_RamBase";          // ms
        public const string RamMeasuresKey = "RowingMod_RamMeasures";
        public const string RamSeedKey = "RowingMod_RamSeed";
        public const string HoldWaterCallKey = "RowingMod_HoldWater";    // network-clock ms of the latest call
        public const int HelmFaster = 1;
        public const int HelmSlower = 2;
        public const int HelmRamming = 3;
        public const int HelmHoldWater = 4;
        // A brake not repeated for this long is dropped, in case its "off" got lost.
        private const float BrakeTimeout = 2.5f;
        // Below this speed (m/s) braking also adds a small constant deceleration, so the ship comes to a halt.
        private const float BrakeStopSpeed = 0.3f;
        private const float BrakeStopDeceleration = 0.3f;
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
        // Rowers holding water, by sender, with when each was last heard.
        private readonly Dictionary<long, float> m_brakers = new Dictionary<long, float>();
        private readonly List<long> m_staleBrakers = new List<long>();
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
            m_nview.Register<bool>(BrakeRpc, RPC_Brake);
            m_nview.Register<bool>(DrumRpc, RPC_Drum);
            m_nview.Register<int>(DrumPatternRpc, RPC_DrumPattern);
            m_nview.Register<int>(HelmRpc, RPC_Helm);
            m_topSpeed = EstimateTopSailSpeed(m_ship);
            LogSeats();
        }

        /// <summary>
        /// Whether a seat spot on a ship is a rowing bench. Ships use Chair for every spot: the rowing benches
        /// (animation "attach_sitship"; Karve 2, Longship 4), a back seat on the centre line ("attach_chair") and
        /// standing "Hold fast" spots ("$ship_holdfast", "attach_mast" / "attach_dragon"). Only the benches row.
        /// </summary>
        public static bool IsRowingSeat(Chair chair)
        {
            return chair != null && string.Equals(chair.m_attachAnimation, "attach_sitship", System.StringComparison.OrdinalIgnoreCase);
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
            GetBeat(nowMs, out beatMs, out periodMs, out _);
        }

        /// <summary>
        /// GetBeat, plus the beat's number as the owner counts them (extended over beats not announced yet), so
        /// every client agrees which measures end a four-measure phrase.
        /// </summary>
        public void GetBeat(long nowMs, out long beatMs, out long periodMs, out long beatIndex)
        {
            beatMs = 0;
            periodMs = 0;
            beatIndex = 0;
            if (m_nview != null && m_nview.IsValid())
            {
                beatIndex = m_nview.GetZDO().GetLong(BeatIndexKey);
            }
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
                long ahead = (nowMs - beatMs) / periodMs;
                beatMs += ahead * periodMs;
                beatIndex += ahead;
            }
            else
            {
                long behind = (beatMs - nowMs + periodMs - 1) / periodMs;
                beatMs -= behind * periodMs;
                beatIndex -= behind;
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
            bool strong = quality >= 0.999f;
            strokes.StrongBySender[sender] = strong;
            // An off-beat stroke on a beat someone already hit is a clash (as far as this client knows yet).
            int strongOnBeat = strokes.StrongCount;
            m_oars?.OnStroke(sender, strong, clash: !strong && strongOnBeat > 0, strongOnBeat, beatMs);

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
            float strength = RowingPlugin.StrokeStrength.Value * (IsRamming() ? 1f + Mathf.Max(0f, RowingPlugin.RammingStrength.Value) : 1f);

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

        /// <summary>Whether the ship's war drum is playing (set by the helmsman; off by default). Readable on every client.</summary>
        public bool IsDrumOn()
        {
            return m_nview != null && m_nview.IsValid() && m_nview.GetZDO().GetBool(DrumKey);
        }

        /// <summary>The war drum's pattern (0..DrumPatterns.Count-1). Readable on every client.</summary>
        public int GetDrumPattern()
        {
            int pattern = m_nview != null && m_nview.IsValid() ? m_nview.GetZDO().GetInt(DrumPatternKey) : 0;
            return ((pattern % DrumPatterns.Count) + DrumPatterns.Count) % DrumPatterns.Count;
        }

        /// <summary>The helmsman asks the ship's owner to switch the drum's pattern.</summary>
        public void RequestDrumPattern(int pattern)
        {
            if (m_nview != null && m_nview.IsValid())
            {
                m_nview.InvokeRPC(DrumPatternRpc, pattern);
            }
        }

        private void RPC_DrumPattern(long sender, int pattern)
        {
            if (m_nview.IsOwner())
            {
                m_nview.GetZDO().Set(DrumPatternKey, ((pattern % DrumPatterns.Count) + DrumPatterns.Count) % DrumPatterns.Count);
            }
        }

        /// <summary>The helmsman's tempo call: -1 Easy, 0 Steady (automatic), 1 Hard. Readable on every client.</summary>
        public int GetTempo()
        {
            return m_nview != null && m_nview.IsValid() ? m_nview.GetZDO().GetInt(TempoKey) : 0;
        }

        /// <summary>The parts of a ramming-speed run, one or more measures each.</summary>
        public enum RamPhase
        {
            None,
            SpeedUp,   // one measure, the beat a third of the way to ramming's; the helmsman's rhythm
            LeadIn,    // one measure, two thirds of the way; a build, a double kick and a stop
            Ramming,   // RammingCycle beats for about RammingDuration; strokes stronger, stamina dearer
            Release,   // one measure, halfway back to the normal beat; a big hit
        }

        /// <summary>
        /// Where the beat with the given index falls in the ramming-speed run, and its measure number within that
        /// part (0 for the first).
        /// </summary>
        public RamPhase GetRamPhase(long beatIndex, out int measure)
        {
            measure = 0;
            if (m_nview == null || !m_nview.IsValid())
            {
                return RamPhase.None;
            }
            ZDO zdo = m_nview.GetZDO();
            long start = zdo.GetLong(RamStartKey);
            int ramming = zdo.GetInt(RamMeasuresKey);
            long offset = beatIndex - start;
            if (start <= 0 || ramming <= 0 || offset < 0 || offset > ramming + 2)
            {
                return RamPhase.None;
            }
            if (offset == 0)
            {
                return RamPhase.SpeedUp;
            }
            if (offset == 1)
            {
                return RamPhase.LeadIn;
            }
            if (offset <= ramming + 1)
            {
                measure = (int)offset - 2;
                return RamPhase.Ramming;
            }
            return RamPhase.Release;
        }

        /// <summary>Where the current beat falls in a ramming-speed run.</summary>
        public RamPhase GetRamPhase()
        {
            GetBeat(NowMs(), out _, out _, out long beatIndex);
            return GetRamPhase(beatIndex, out _);
        }

        /// <summary>Whether ramming speed is on right now (the quick beat itself, not its speed-up or release).</summary>
        public bool IsRamming()
        {
            return GetRamPhase() == RamPhase.Ramming;
        }

        /// <summary>Whether a ramming-speed run has been called and hasn't finished.</summary>
        public bool IsRamRunActive()
        {
            if (m_nview == null || !m_nview.IsValid())
            {
                return false;
            }
            GetBeat(NowMs(), out _, out _, out long beatIndex);
            long start = m_nview.GetZDO().GetLong(RamStartKey);
            return start > 0 && beatIndex <= start + m_nview.GetZDO().GetInt(RamMeasuresKey) + 2;
        }

        /// <summary>The run's random seed, for the drum's lead-in, ramming rhythm and release.</summary>
        public int GetRamSeed()
        {
            return m_nview != null && m_nview.IsValid() ? m_nview.GetZDO().GetInt(RamSeedKey) : 0;
        }

        /// <summary>
        /// The planned length (ms) of the beat with the given index, if a ramming-speed run fixes it in advance
        /// (speed-up, lead-in and ramming measures); 0 if it's decided when the beat starts.
        /// </summary>
        public long GetPlannedPeriodMs(long beatIndex)
        {
            switch (GetRamPhase(beatIndex, out _))
            {
                case RamPhase.SpeedUp:
                    return RamStepMs(SpeedUpStep);
                case RamPhase.LeadIn:
                    return RamStepMs(LeadInStep);
                case RamPhase.Ramming:
                    return SecondsToMs(RowingPlugin.RammingCycle.Value);
                default:
                    return 0;
            }
        }

        // The speed-up and lead-in beats step from the beat before the run toward ramming's: a third, then two thirds.
        private const float SpeedUpStep = 0.3f;
        private const float LeadInStep = 0.65f;

        private long RamStepMs(float step)
        {
            long baseMs = m_nview.GetZDO().GetLong(RamBaseKey);
            long rammingMs = SecondsToMs(RowingPlugin.RammingCycle.Value);
            if (baseMs <= 0)
            {
                baseMs = rammingMs;
            }
            return baseMs + (long)((rammingMs - baseMs) * step);
        }

        /// <summary>Seconds until ramming speed can be called again (0 when it can).</summary>
        public float RammingCooldown()
        {
            if (m_nview == null || !m_nview.IsValid())
            {
                return 0f;
            }
            return Mathf.Max(0f, (m_nview.GetZDO().GetLong(RammingReadyKey) - NowMs()) / 1000f);
        }

        /// <summary>When the helmsman last called "Hold water!" (network-clock ms), 0 if never.</summary>
        public long GetHoldWaterCall()
        {
            return m_nview != null && m_nview.IsValid() ? m_nview.GetZDO().GetLong(HoldWaterCallKey) : 0;
        }

        /// <summary>The helmsman sends a call (HelmFaster, HelmSlower, HelmRamming, HelmHoldWater) to the owner.</summary>
        public void SendHelmCommand(int command)
        {
            if (m_nview != null && m_nview.IsValid())
            {
                m_nview.InvokeRPC(HelmRpc, command);
            }
        }

        private void RPC_Helm(long sender, int command)
        {
            if (!m_nview.IsOwner())
            {
                return;
            }
            ZDO zdo = m_nview.GetZDO();
            long nowMs = NowMs();
            switch (command)
            {
                case HelmFaster:
                    zdo.Set(TempoKey, Mathf.Min(1, zdo.GetInt(TempoKey) + 1));
                    break;
                case HelmSlower:
                    zdo.Set(TempoKey, Mathf.Max(-1, zdo.GetInt(TempoKey) - 1));
                    break;
                case HelmRamming:
                    // Plan the run from the next beat. The cooldown starts when ramming ends (see UpdateBeat).
                    if (nowMs >= zdo.GetLong(RammingReadyKey) && !IsRamRunActive())
                    {
                        GetBeat(nowMs, out _, out long periodMs, out long beatIndex);
                        float cycle = Mathf.Max(0.3f, RowingPlugin.RammingCycle.Value);
                        zdo.Set(RamStartKey, beatIndex + 1);
                        zdo.Set(RamBaseKey, periodMs);
                        zdo.Set(RamMeasuresKey, Mathf.Max(1, Mathf.RoundToInt(RowingPlugin.RammingDuration.Value / cycle)));
                        zdo.Set(RamSeedKey, Random.Range(1, int.MaxValue));
                    }
                    break;
                case HelmHoldWater:
                    zdo.Set(HoldWaterCallKey, nowMs);
                    break;
            }
        }

        /// <summary>The helmsman asks the ship's owner to turn the drum on or off.</summary>
        public void RequestDrum(bool on)
        {
            if (m_nview != null && m_nview.IsValid())
            {
                m_nview.InvokeRPC(DrumRpc, on);
            }
        }

        private void RPC_Drum(long sender, bool on)
        {
            if (m_nview.IsOwner())
            {
                m_nview.GetZDO().Set(DrumKey, on);
            }
        }

        /// <summary>A rower starting or stopping to hold water. Every client tracks it (oars, sound, panel); the owner brakes.</summary>
        private void RPC_Brake(long sender, bool braking)
        {
            if (braking)
            {
                m_brakers[sender] = Time.time;
            }
            else
            {
                m_brakers.Remove(sender);
            }
            m_oars?.SetBraking(sender, braking);
        }

        private void ForgetStaleBrakes()
        {
            if (m_brakers.Count == 0)
            {
                return;
            }
            m_staleBrakers.Clear();
            foreach (KeyValuePair<long, float> braker in m_brakers)
            {
                if (Time.time - braker.Value > BrakeTimeout)
                {
                    m_staleBrakers.Add(braker.Key);
                }
            }
            foreach (long sender in m_staleBrakers)
            {
                m_brakers.Remove(sender);
                m_oars?.SetBraking(sender, false);
            }
        }

        /// <summary>
        /// Holding water: each braking rower decelerates the ship by BrakeStrength × speed per second (plus a little
        /// near a standstill to finish the stop), and together they never take more than the ship's speed, so
        /// braking never pushes it backward. With Brake.Turning the drag acts at the rower's oarlock, so braking on
        /// one side swings the bow toward that side.
        /// </summary>
        private void ApplyBrakes(float dt)
        {
            float speed = m_ship.GetSpeed();
            float remaining = Mathf.Abs(speed);
            if (remaining < 0.01f)
            {
                return;
            }
            float against = -Mathf.Sign(speed);
            foreach (long sender in m_brakers.Keys)
            {
                float deceleration = RowingPlugin.BrakeStrength.Value * Mathf.Abs(speed);
                if (Mathf.Abs(speed) < BrakeStopSpeed)
                {
                    deceleration += BrakeStopDeceleration;
                }
                float change = Mathf.Min(deceleration * dt, remaining);
                remaining -= change;
                Vector3 point = m_body.worldCenterOfMass;
                if (RowingPlugin.BrakeTurning.Value && m_oars != null && m_oars.TryGetOarlock(sender, out Vector3 oarlock))
                {
                    point = oarlock;
                }
                m_body.AddForceAtPosition(transform.forward * (against * change * m_body.mass), point, ForceMode.Impulse);
                if (remaining <= 0f)
                {
                    break;
                }
            }
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
            ForgetStaleBrakes();
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

            if (m_brakers.Count > 0 && IsInWater())
            {
                ApplyBrakes(dt);
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
                zdo.Set(BeatPeriodKey, TempoMs(direction, zdo.GetLong(BeatIndexKey)));
                return;
            }
            if (nowMs >= beatMs + periodMs)
            {
                long index = zdo.GetLong(BeatIndexKey) + 1;
                zdo.Set(BeatTimeKey, beatMs + periodMs);
                zdo.Set(BeatPeriodKey, TempoMs(direction, index));
                zdo.Set(BeatIndexKey, index);
                // Ramming ends as the release begins: the cooldown starts now.
                if (GetRamPhase(index, out _) == RamPhase.Release)
                {
                    zdo.Set(RammingReadyKey, nowMs + SecondsToMs(RowingPlugin.RammingCooldown.Value));
                }
            }
        }

        /// <summary>Time between beats: StrokeCycleStill when still, down to StrokeCycleTopSpeed at top sail speed.</summary>
        private long TempoMs(float direction, long beatIndex)
        {
            // A ramming-speed run: the speed-up, lead-in and ramming beats are planned; the release is halfway
            // between ramming's beat and the normal one.
            long planned = GetPlannedPeriodMs(beatIndex);
            if (planned > 0)
            {
                return planned;
            }
            if (GetRamPhase(beatIndex, out _) == RamPhase.Release)
            {
                return (SecondsToMs(RowingPlugin.RammingCycle.Value) + NormalTempoMs()) / 2;
            }
            return NormalTempoMs();
        }

        /// <summary>The beat from the ship's speed, scaled by the helmsman's Easy or Hard call.</summary>
        private long NormalTempoMs()
        {
            float topSpeed = TopSpeed();
            float speedRatio = topSpeed > 0.01f ? Mathf.Clamp01(Mathf.Abs(m_ship.GetSpeed()) / topSpeed) : 0f;
            float seconds = Mathf.Lerp(RowingPlugin.StrokeCycleStill.Value, RowingPlugin.StrokeCycleTopSpeed.Value, speedRatio);
            // The helmsman's call scales the automatic beat: Easy is slower, Hard quicker.
            int tempo = GetTempo();
            if (tempo < 0)
            {
                seconds *= RowingPlugin.EasyTempoFactor.Value;
            }
            else if (tempo > 0)
            {
                seconds *= RowingPlugin.HardTempoFactor.Value;
            }
            return SecondsToMs(seconds);
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
