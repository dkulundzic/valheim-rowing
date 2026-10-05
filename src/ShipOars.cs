using System.Collections.Generic;
using UnityEngine;

namespace RowingMod
{
    /// <summary>
    /// Draws an oar beside every rowing bench on a ship. An occupied bench's oar rests in the water and swings on each
    /// stroke; an empty bench's oar is pulled in and stowed inside the hull, as a real crew would. Runs on every client
    /// with the mod; strokes reach it through the broadcast stroke RPC, so everyone sees the whole crew's oars.
    /// The oars are plain shapes built in code with the ship's own material and have no colliders, so they never
    /// touch the physics.
    /// </summary>
    public class ShipOars : MonoBehaviour
    {
        // A remote player counts as sitting on a seat when this close to its attach point. Only positions are
        // synced over the network, not who's sitting where.
        private const float SeatRadius = 0.5f;

        // Shape, in metres.
        private const float ShaftRadius = 0.045f;
        private const float Inboard = 0.8f;
        private const float BladeLength = 0.8f;
        private const float BladeWidth = 0.22f;
        private const float BladeThickness = 0.04f;

        // Stroke animation: the drive sweeps the blade through the water from the bow side to the stern side,
        // the recovery lifts it and swings it forward again, then it settles back to rest.
        private const float DriveTime = 0.45f;
        private const float RecoveryTime = 0.55f;
        private const float SettleTime = 1.2f;
        private const float SweepAngle = 30f;
        private const float WeakSweepFactor = 0.6f;
        private const float LiftAngle = 14f;
        private const float RestPitchMin = 4f;
        private const float RestPitchMax = 55f;

        // Stowed (empty bench): the oar lies flat inside the hull, this far in from the gunwale and a little below
        // its top, parallel to the side, centred on its bench with the blade toward the stern.
        private const float StowInset = 0.35f;
        private const float StowDrop = 0.08f;
        private static readonly Quaternion StowRotation =
            Quaternion.AngleAxis(90f, Vector3.up) * Quaternion.AngleAxis(90f, Vector3.right);

        // Probing for the gunwale: step outward from the seat, casting down from above.
        private const float ProbeStep = 0.05f;
        private const float ProbeReach = 3f;
        private const float ProbeHeight = 4f;
        // Seconds to swing the oar out when someone sits down, or in when they leave.
        private const float StowTime = 0.8f;

        // Holding water (braking): the oar swings square to the hull, blade dug deeper and held still.
        private const float BrakeTime = 0.3f;
        private const float BrakeExtraPitch = 8f;
        // While braking at speed: water rushing past the blade, and some spray.
        private const float GurgleMinSpeed = 0.15f;
        private const float SprayMinSpeed = 1.5f;

        private class Oar
        {
            public Chair Seat;
            public float Side; // +1 starboard (right), -1 port (left)
            public Transform Root;
            public float Outboard;
            // Ship-space poses: the oarlock on the gunwale (rowing pivot) and the stowed spot inside the hull.
            public Vector3 RowPosition;
            public Vector3 StowPosition;
            public Player Occupant;
            public WaterVolume WaterVolume;
            // Animation state, in "bow-ward" degrees: positive swings the blade toward the bow.
            public float StrokeStart = -100f;
            public float Amplitude;
            public float Direction = 1f;
            public float SweepAtStrokeStart;
            public float Sweep;
            // 0 = out in the water, 1 = stowed. Starts stowed so a freshly loaded ship doesn't animate.
            public float Stowed = 1f;
            // The latest stroke, for the crew panel: its beat, what kind it turned out to be, and when.
            public long StrokeBeat;
            public StrokeKind Kind;
            public float KindTime = -100f;
            // Holding water, and how far into the braking pose the oar is (0..1).
            public bool Braking;
            public float BrakeBlend;
            public float NextGurgle;
            public float NextSpray;
            public float NextWake;
            // How many wakes this stroke has left so far.
            public int Wakes = WakesPerStroke;
        }

        public enum StrokeKind
        {
            None,
            Strong,
            Weak,
            Clash,
            Sync,
        }

        /// <summary>One rowing bench as the crew panel draws it, in ship space seen from above (x right, y = z forward).</summary>
        public struct Bench
        {
            public Vector2 Seat;
            public Vector2 OarFrom;
            public Vector2 OarTo;
            public Player Occupant;
            public StrokeKind Kind;
            public float KindAge;
            // 0 = out in the water, 1 = stowed inside the hull.
            public float Stowed;
            public bool Braking;
        }

        // Hull outlines seen from above, per ship type: half-width of the gunwale every HullStep metres along the ship.
        private const float HullStep = 0.25f;
        private const float HullSearch = 15f;
        private static readonly Dictionary<string, HullOutline> s_hulls = new Dictionary<string, HullOutline>();

        public class HullOutline
        {
            public float MinZ;
            public float MaxZ;
            public float MaxHalfWidth;
            public float[] HalfWidths; // at MinZ + i * HullStep
            public Texture2D Texture;
        }

        private HullOutline m_hull;

        // Creaks come at most this often per ship, so a full crew doesn't creak on every oar at once.
        private const float CreakInterval = 0.7f;

        // Wakes: small ripples along each stroke's path through the water.
        private const int WakesPerStroke = 3;
        private const float WakeScale = 0.5f;

        private Ship m_ship;
        private ShipRowing m_rowing;
        private float m_lastCreak = -100f;
        // The war drum: the beat it last played, and a count for accenting every fourth beat (generated drum only).
        private long m_lastDrumBeat;
        private int m_drumCount;
        // Pattern drumming: hits are scheduled this far ahead so they land on time, and everything up to
        // m_drumScheduledUntil (network-clock ms) has been scheduled already.
        private const long DrumLookaheadMs = 200;
        private long m_drumScheduledUntil;
        private readonly DrumMeasure[] m_drumMeasures = { new DrumMeasure(), new DrumMeasure() };
        // The ship's forward speed, from how far it moved since the last frame, so it works on every client
        // (only the owner simulates the ship's physics).
        private float m_speed;
        private Vector3 m_lastPosition;
        private bool m_hasLastPosition;
        private readonly List<Oar> m_oars = new List<Oar>();
        // Beyond ActiveDistance from the local player, a ship's oars stop updating (the drum carries 70 m).
        // Within CrewDistance of the ship's centre, a player may be on a bench (a Longship is about 20 m long).
        private const float ActiveDistance = 90f;
        private const float CrewDistance = 15f;
        private bool m_idle;
        private bool m_built;

        private void Awake()
        {
            m_ship = GetComponent<Ship>();
        }

        private void Start()
        {
            m_rowing = GetComponent<ShipRowing>();
        }

        /// <summary>Called for every stroke the ship receives, from any rower including the local one.</summary>
        public void OnStroke(long sender, bool strong, bool clash, int strongOnBeat, long beatMs)
        {
            // A well-timed stroke on a beat others already hit makes all of them a sync, and turns any earlier
            // off-beat stroke on that beat into a clash. The crew panel shows the upgrade.
            if (strong && strongOnBeat >= 1)
            {
                foreach (Oar other in m_oars)
                {
                    if (other.StrokeBeat != beatMs)
                    {
                        continue;
                    }
                    if (other.Kind == StrokeKind.Weak)
                    {
                        other.Kind = StrokeKind.Clash;
                    }
                    else if (other.Kind == StrokeKind.Strong && strongOnBeat >= 2)
                    {
                        other.Kind = StrokeKind.Sync;
                    }
                }
            }
            foreach (Oar oar in m_oars)
            {
                if (oar.Occupant != null && OwnerOf(oar.Occupant) == sender)
                {
                    oar.StrokeBeat = beatMs;
                    oar.Kind = clash ? StrokeKind.Clash : !strong ? StrokeKind.Weak : strongOnBeat >= 2 ? StrokeKind.Sync : StrokeKind.Strong;
                    oar.KindTime = Time.time;
                    oar.SweepAtStrokeStart = oar.Sweep;
                    oar.StrokeStart = Time.time;
                    oar.Amplitude = strong ? 1f : WeakSweepFactor;
                    oar.Wakes = 0;
                    oar.Direction = ShipRowing.RowDirection(m_ship);
                    Vector3 blade = oar.Root.TransformPoint(new Vector3(oar.Outboard - BladeLength / 2f, 0f, 0f));
                    Vector3 oarlock = oar.Root.position;
                    if (clash)
                    {
                        RowingSounds.PlayClash(blade);
                        return;
                    }
                    // At most one creak chance per CreakInterval per ship, so a full crew doesn't creak on every oar.
                    bool creak = strong && Time.time - m_lastCreak > CreakInterval;
                    if (creak)
                    {
                        m_lastCreak = Time.time;
                    }
                    ShipRowing rowing = GetComponent<ShipRowing>();
                    float load = rowing != null ? rowing.GetSyncedBoost() / Mathf.Max(0.1f, RowingPlugin.MaxBoost.Value) : 0.5f;
                    RowingSounds.PlayStroke(blade, oarlock, strong, strongOnBeat, DriveTime, creak, load);
                    return;
                }
            }
        }

        /// <summary>The ship's rowing benches for the crew panel, with each oar projected from its real position.</summary>
        public void GetBenches(List<Bench> benches)
        {
            benches.Clear();
            foreach (Oar oar in m_oars)
            {
                Transform seat = oar.Seat.m_attachPoint != null ? oar.Seat.m_attachPoint : oar.Seat.transform;
                Vector3 seatLocal = transform.InverseTransformPoint(seat.position);
                Vector3 from = oar.Root.localPosition;
                Vector3 to = transform.InverseTransformPoint(oar.Root.TransformPoint(new Vector3(oar.Outboard, 0f, 0f)));
                benches.Add(new Bench
                {
                    Seat = new Vector2(seatLocal.x, seatLocal.z),
                    OarFrom = new Vector2(from.x, from.z),
                    OarTo = new Vector2(to.x, to.z),
                    Occupant = oar.Occupant,
                    Kind = oar.Kind,
                    KindAge = Time.time - oar.KindTime,
                    Stowed = oar.Stowed,
                    Braking = oar.Braking,
                });
            }
        }

        /// <summary>
        /// The hull seen from above, traced from the gunwale (shared per ship type), or null before the oars are
        /// built. Includes a texture: dark translucent deck with a light outline, bow at the top.
        /// </summary>
        public HullOutline GetHull()
        {
            return m_hull;
        }

        private HullOutline TraceHull(Collider[] hullColliders, float probeY)
        {
            string key = name;
            if (s_hulls.TryGetValue(key, out HullOutline cached))
            {
                return cached;
            }
            List<float> widths = new List<float>();
            float minZ = float.NaN;
            float lastZ = float.NaN;
            for (float z = -HullSearch; z <= HullSearch; z += HullStep)
            {
                bool found = TryProbeGunwale(0f, z, 1f, probeY, hullColliders, out Vector3 gunwale);
                if (!found)
                {
                    if (!float.IsNaN(minZ))
                    {
                        break; // past the bow
                    }
                    continue;
                }
                if (float.IsNaN(minZ))
                {
                    minZ = z;
                }
                widths.Add(Mathf.Abs(gunwale.x));
                lastZ = z;
            }
            if (widths.Count < 2)
            {
                return null;
            }
            HullOutline hull = new HullOutline { MinZ = minZ, MaxZ = lastZ, HalfWidths = widths.ToArray() };
            foreach (float width in widths)
            {
                hull.MaxHalfWidth = Mathf.Max(hull.MaxHalfWidth, width);
            }
            hull.Texture = DrawHull(hull);
            s_hulls[key] = hull;
            return hull;
        }

        public static float HalfWidthAt(HullOutline hull, float z)
        {
            float index = (z - hull.MinZ) / HullStep;
            if (index < 0f || index > hull.HalfWidths.Length - 1)
            {
                return 0f;
            }
            int i = Mathf.Min((int)index, hull.HalfWidths.Length - 2);
            return Mathf.Lerp(hull.HalfWidths[i], hull.HalfWidths[i + 1], index - i);
        }

        /// <summary>Renders the outline into a texture once: 32 pixels per metre, anti-aliased edges.</summary>
        private static Texture2D DrawHull(HullOutline hull)
        {
            const float pixelsPerMetre = 32f;
            const float outlinePixels = 2.5f;
            int width = Mathf.CeilToInt(hull.MaxHalfWidth * 2f * pixelsPerMetre) + 4;
            int height = Mathf.CeilToInt((hull.MaxZ - hull.MinZ) * pixelsPerMetre) + 4;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int py = 0; py < height; py++)
            {
                // Texture rows go up, so the bottom row is the stern (MinZ) and the top the bow.
                float z = hull.MinZ + (py - 2 + 0.5f) / pixelsPerMetre;
                float halfWidth = HalfWidthAt(hull, z) * pixelsPerMetre;
                for (int px = 0; px < width; px++)
                {
                    float x = Mathf.Abs(px + 0.5f - width / 2f);
                    float inside = halfWidth - x; // pixels from the edge, positive inside
                    if (inside <= -1f || halfWidth <= 0f)
                    {
                        texture.SetPixel(px, py, clear);
                        continue;
                    }
                    float coverage = Mathf.Clamp01(inside + 0.5f);
                    float edge = Mathf.Clamp01(outlinePixels - inside);
                    Color fill = new Color(0.05f, 0.05f, 0.05f, 0.55f);
                    Color line = new Color(1f, 1f, 1f, 0.9f);
                    Color color = Color.Lerp(fill, line, edge);
                    color.a *= coverage;
                    texture.SetPixel(px, py, color);
                }
            }
            texture.Apply();
            return texture;
        }

        /// <summary>A rower starting or stopping to hold water (from the broadcast brake RPC).</summary>
        public void SetBraking(long sender, bool braking)
        {
            foreach (Oar oar in m_oars)
            {
                if (oar.Occupant != null && OwnerOf(oar.Occupant) == sender)
                {
                    // The blade digs in: a splash at any speed, so braking always sounds. Repeats ("still braking")
                    // don't splash again.
                    if (braking && !oar.Braking)
                    {
                        RowingSounds.PlayBrakeCatch(oar.Root.TransformPoint(new Vector3(oar.Outboard - BladeLength / 2f, 0f, 0f)));
                        oar.NextGurgle = Time.time + 0.4f;
                    }
                    oar.Braking = braking;
                    return;
                }
            }
        }

        /// <summary>Where a rower's oar meets the hull (world space), for applying their braking drag.</summary>
        public bool TryGetOarlock(long sender, out Vector3 position)
        {
            foreach (Oar oar in m_oars)
            {
                if (oar.Occupant != null && OwnerOf(oar.Occupant) == sender)
                {
                    position = transform.TransformPoint(oar.RowPosition);
                    return true;
                }
            }
            position = Vector3.zero;
            return false;
        }

        private static long OwnerOf(Player player)
        {
            ZNetView nview = player.GetComponent<ZNetView>();
            ZDO zdo = nview != null ? nview.GetZDO() : null;
            return zdo != null ? zdo.GetOwner() : 0;
        }

        private void Update()
        {
            if (!RowingPlugin.ShowOars.Value)
            {
                foreach (Oar oar in m_oars)
                {
                    oar.Root.gameObject.SetActive(false);
                }
                return;
            }
            // Ships far from the local player (e.g. parked at a harbour) skip their oars, wakes and drum: nobody
            // here would see or hear them. They pick up again on approach.
            Player local = Player.m_localPlayer;
            if (local == null || (local.transform.position - transform.position).sqrMagnitude > ActiveDistance * ActiveDistance)
            {
                if (!m_idle)
                {
                    m_idle = true;
                    m_hasLastPosition = false;
                    m_lastDrumBeat = 0;
                    m_drumCount = 0;
                    m_drumScheduledUntil = 0;
                    foreach (Oar oar in m_oars)
                    {
                        oar.Occupant = null;
                        oar.Braking = false;
                    }
                }
                return;
            }
            m_idle = false;
            if (!m_built)
            {
                Build();
            }

            UpdateSpeed();
            UpdateDrum();
            // Only look for each bench's occupant when some player is on or right by the ship.
            bool anyoneClose = AnyPlayerWithin(CrewDistance);
            foreach (Oar oar in m_oars)
            {
                if (!oar.Root.gameObject.activeSelf)
                {
                    oar.Root.gameObject.SetActive(true);
                }
                // Who sits here decides whose strokes swing this oar. An empty bench can't be braking.
                oar.Occupant = anyoneClose ? FindOccupant(oar.Seat) : null;
                if (oar.Occupant == null)
                {
                    oar.Braking = false;
                }
                Animate(oar);
                StrokeWakes(oar);
                if (oar.Braking && oar.BrakeBlend > 0.8f)
                {
                    BrakeEffects(oar);
                }
            }
        }

        /// <summary>
        /// The war drum: while the helmsman has it on and someone is aboard, every client plays it on each of the
        /// ship's beats (from the shared beat schedule, so everyone hears it in time), accenting every fourth beat.
        /// </summary>
        private void UpdateDrum()
        {
            bool drumOn = m_rowing != null && m_rowing.IsDrumOn();
            bool kit = RowingSounds.HasDrumKit();
            // A ramming-speed run plays the drum even when it's off, from the measure before the run (so the
            // speed-up's first stroke can be scheduled ahead) to the release.
            bool ramRun = false;
            if (m_rowing != null && kit && !drumOn)
            {
                m_rowing.GetBeat(ShipRowing.NowMs(), out _, out _, out long beatIndex);
                ramRun = m_rowing.GetRamPhase(beatIndex, out _) != ShipRowing.RamPhase.None
                    || m_rowing.GetRamPhase(beatIndex + 1, out _) != ShipRowing.RamPhase.None;
            }
            if (m_rowing == null || !(drumOn || ramRun) || !AnyPlayerAboard())
            {
                m_lastDrumBeat = 0;
                m_drumCount = 0;
                m_drumScheduledUntil = 0;
                return;
            }
            if (kit)
            {
                UpdateDrumPattern(drumOn);
                return;
            }
            m_rowing.GetBeat(ShipRowing.NowMs(), out long beatMs, out _);
            if (beatMs == m_lastDrumBeat)
            {
                return;
            }
            // Start on the next beat rather than the one already under way when the drum was turned on.
            if (m_lastDrumBeat != 0)
            {
                RowingSounds.PlayDrum(transform.position + transform.up * 1.5f, m_drumCount % 4 == 0);
                m_drumCount++;
            }
            m_lastDrumBeat = beatMs;
        }

        /// <summary>
        /// Plays the helmsman's drum pattern: one measure per beat, built from the shared beat schedule and seeded with
        /// the beat's time so every client plays the same hits. Hits are scheduled up to 200 ms ahead with a delay,
        /// so they land on time regardless of frame rate. When the drum is turned on, it starts at the next stroke.
        ///
        /// A ramming-speed run replaces the rhythm for its lead-in, ramming and release measures with the ones its
        /// seed picks, and plays them even with the drum off (<paramref name="drumOn"/>). Their beat lengths are
        /// planned, so the next measure's first hits land right even when the tempo jumps.
        /// </summary>
        private void UpdateDrumPattern(bool drumOn)
        {
            long nowMs = ShipRowing.NowMs();
            m_rowing.GetBeat(nowMs, out long beatMs, out long periodMs, out long beatIndex);
            if (m_drumScheduledUntil == 0)
            {
                m_drumScheduledUntil = beatMs + periodMs - 1;
            }
            long from = System.Math.Max(m_drumScheduledUntil, nowMs);
            long until = nowMs + DrumLookaheadMs;
            if (until <= from)
            {
                return;
            }
            int pattern = m_rowing.GetDrumPattern();
            int seed = m_rowing.GetRamSeed() & int.MaxValue;
            Vector3 position = transform.position + transform.up * 1.5f;
            // This measure and the next (whose ONE may fall inside the lookahead). A measure's length is planned
            // during a ramming-speed run; otherwise the next one is assumed to last as long as this one, which only
            // matters for hits in its first 200 ms.
            long measureMs = beatMs;
            long lengthMs = periodMs;
            for (int k = 0; k < 2; k++)
            {
                long index = beatIndex + k;
                long planned = m_rowing.GetPlannedPeriodMs(index);
                if (planned > 0)
                {
                    lengthMs = planned;
                }
                if (measureMs > until)
                {
                    break;
                }
                DrumPatterns.Kind kind = DrumPatterns.Kind.Normal;
                int variant = pattern;
                switch (m_rowing.GetRamPhase(index, out _))
                {
                    case ShipRowing.RamPhase.LeadIn:
                        kind = DrumPatterns.Kind.LeadIn;
                        variant = seed % DrumPatterns.LeadInNames.Length;
                        break;
                    case ShipRowing.RamPhase.Ramming:
                        kind = DrumPatterns.Kind.Ramming;
                        variant = seed / 3 % DrumPatterns.RammingNames.Length;
                        break;
                    case ShipRowing.RamPhase.Release:
                        kind = DrumPatterns.Kind.Release;
                        variant = seed / 24 % DrumPatterns.ReleaseNames.Length;
                        break;
                    case ShipRowing.RamPhase.None:
                        if (!drumOn)
                        {
                            measureMs += lengthMs;
                            continue;
                        }
                        break;
                }
                float period = lengthMs / 1000f;
                foreach (DrumPatterns.Hit hit in MeasureHits(kind, variant, index, period, measureMs))
                {
                    long hitMs = measureMs + (long)(hit.Fraction * lengthMs);
                    if (hitMs > from && hitMs <= until)
                    {
                        RowingSounds.PlayDrumHit(hit.Drum, position, hit.Gain, hit.Pitch, (hitMs - nowMs) / 1000f, hit.Length);
                    }
                }
                measureMs += lengthMs;
            }
            m_drumScheduledUntil = until;
        }

        /// <summary>
        /// A measure's hits, built once and kept while the lookahead reaches into it (the current and next measure),
        /// so the scheduler doesn't rebuild them every frame. Rebuilt if the rhythm or the beat's length changes.
        /// </summary>
        private List<DrumPatterns.Hit> MeasureHits(DrumPatterns.Kind kind, int variant, long index, float period, long measureMs)
        {
            int pattern = (int)kind * 100 + variant;
            DrumMeasure oldest = m_drumMeasures[0];
            foreach (DrumMeasure measure in m_drumMeasures)
            {
                if (measure.Ms == measureMs && measure.Pattern == pattern && measure.Period == period)
                {
                    return measure.Hits;
                }
                if (measure.Ms < oldest.Ms)
                {
                    oldest = measure;
                }
            }
            oldest.Ms = measureMs;
            oldest.Pattern = pattern;
            oldest.Period = period;
            oldest.Hits = DrumPatterns.Build(kind, variant, (int)index, period, measureMs);
            return oldest.Hits;
        }

        private class DrumMeasure
        {
            public long Ms = long.MinValue;
            public int Pattern;
            public float Period;
            public List<DrumPatterns.Hit> Hits;
        }

        private bool AnyPlayerWithin(float distance)
        {
            Vector3 position = transform.position;
            foreach (Player player in Player.GetAllPlayers())
            {
                if (player != null && (player.transform.position - position).sqrMagnitude < distance * distance)
                {
                    return true;
                }
            }
            return false;
        }

        private bool AnyPlayerAboard()
        {
            foreach (Player player in Player.GetAllPlayers())
            {
                if (player != null && m_ship.IsPlayerInBoat(player))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Leaves a few small ripples on the water along the blade's path during a stroke's drive.</summary>
        private void StrokeWakes(Oar oar)
        {
            if (oar.Wakes >= WakesPerStroke || oar.Occupant == null || oar.Braking)
            {
                return;
            }
            float t = Time.time - oar.StrokeStart;
            if (t > DriveTime)
            {
                oar.Wakes = WakesPerStroke;
                return;
            }
            // Spread across the drive: at about 20%, 50% and 80% of it.
            if (t < DriveTime * (0.2f + 0.3f * oar.Wakes))
            {
                return;
            }
            oar.Wakes++;
            RowingSounds.ShowWake(BladeOnWater(oar), WakeScale * oar.Amplitude);
        }

        private Vector3 BladeOnWater(Oar oar)
        {
            Vector3 blade = oar.Root.TransformPoint(new Vector3(oar.Outboard - BladeLength / 2f, 0f, 0f));
            blade.y = Floating.GetWaterLevel(blade, ref oar.WaterVolume);
            return blade;
        }

        private void UpdateSpeed()
        {
            Vector3 position = transform.position;
            if (m_hasLastPosition && Time.deltaTime > 0f)
            {
                float speed = Vector3.Dot(position - m_lastPosition, transform.forward) / Time.deltaTime;
                m_speed = Mathf.Lerp(m_speed, speed, Mathf.Clamp01(Time.deltaTime * 5f));
            }
            m_lastPosition = position;
            m_hasLastPosition = true;
        }

        /// <summary>While a blade is held in the water at speed: rushing, gurgling water, and spray now and then.</summary>
        private void BrakeEffects(Oar oar)
        {
            float speed = Mathf.Abs(m_speed);
            if (speed < GurgleMinSpeed)
            {
                return;
            }
            Vector3 blade = oar.Root.TransformPoint(new Vector3(oar.Outboard - BladeLength / 2f, 0f, 0f));
            if (Time.time >= oar.NextGurgle)
            {
                oar.NextGurgle = Time.time + Random.Range(0.3f, 0.55f);
                RowingSounds.PlayGurgle(blade, Mathf.Clamp01(speed / 4f));
            }
            if (speed >= 1f && Time.time >= oar.NextWake)
            {
                oar.NextWake = Time.time + Random.Range(0.4f, 0.6f);
                RowingSounds.ShowWake(BladeOnWater(oar), WakeScale * 1.2f);
            }
            if (speed >= SprayMinSpeed && Time.time >= oar.NextSpray)
            {
                oar.NextSpray = Time.time + Random.Range(0.6f, 1f);
                RowingSounds.ShowSpray(blade, speed > 4f);
            }
        }

        private static Player FindOccupant(Chair seat)
        {
            Transform attachPoint = seat.m_attachPoint != null ? seat.m_attachPoint : seat.transform;
            foreach (Player player in Player.GetAllPlayers())
            {
                if (player == null)
                {
                    continue;
                }
                if (player == Player.m_localPlayer)
                {
                    if (player.IsAttached() && player.GetAttachPoint() == attachPoint)
                    {
                        return player;
                    }
                }
                else if ((player.transform.position - attachPoint.position).sqrMagnitude < SeatRadius * SeatRadius)
                {
                    return player;
                }
            }
            return null;
        }

        private void Build()
        {
            m_built = true;
            Renderer hull = FindHullRenderer();
            Material material = hull != null ? hull.sharedMaterial : null;
            int layer = hull != null ? hull.gameObject.layer : gameObject.layer;
            Collider[] hullColliders = GetComponentsInChildren<Collider>(includeInactive: false);

            int index = 0;
            foreach (Chair chair in GetComponentsInChildren<Chair>(includeInactive: true))
            {
                if (!ShipRowing.IsRowingSeat(chair))
                {
                    continue;
                }
                Transform seat = chair.m_attachPoint != null ? chair.m_attachPoint : chair.transform;
                Vector3 local = transform.InverseTransformPoint(seat.position);
                // Seats on the centre line alternate sides.
                float side = Mathf.Abs(local.x) > 0.15f ? Mathf.Sign(local.x) : (index % 2 == 0 ? 1f : -1f);
                index++;

                Vector3 oarlock = FindGunwale(local, side, hullColliders);
                float halfWidth = Mathf.Abs(oarlock.x);
                Oar oar = new Oar
                {
                    Seat = chair,
                    Side = side,
                    Outboard = Mathf.Clamp(1.6f + halfWidth * 0.7f, 1.8f, 3.2f),
                    RowPosition = oarlock,
                };
                oar.StowPosition = FindStowPosition(local, side, Inboard + oar.Outboard, oar.Outboard, hullColliders);
                oar.Root = BuildOar(chair.name, oarlock, oar.Outboard, material, layer);
                m_oars.Add(oar);
                RowingPlugin.Log.LogInfo($"  {chair.name}: seat {local.x:0.00}, {local.y:0.00}, {local.z:0.00}; oarlock {oarlock.x:0.00}, {oarlock.y:0.00}, {oarlock.z:0.00}; stowed pivot {oar.StowPosition.x:0.00}, {oar.StowPosition.y:0.00}, {oar.StowPosition.z:0.00}; oar {Inboard + oar.Outboard:0.0} m");
            }
            float probeY = 0f;
            foreach (Chair chair in GetComponentsInChildren<Chair>(includeInactive: true))
            {
                if (ShipRowing.IsRowingSeat(chair))
                {
                    probeY = transform.InverseTransformPoint(chair.transform.position).y;
                    break;
                }
            }
            m_hull = TraceHull(hullColliders, probeY);
            string materialName = material != null ? material.name : "none";
            RowingPlugin.Log.LogInfo($"{name}: built {m_oars.Count} oar(s) with material {materialName}");
        }

        /// <summary>The ship's hull renderer: one whose material looks like wood, else the biggest one.</summary>
        private Renderer FindHullRenderer()
        {
            Renderer biggest = null;
            float biggestSize = 0f;
            foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(includeInactive: false))
            {
                Material material = renderer.sharedMaterial;
                if (material == null)
                {
                    continue;
                }
                if (material.name.IndexOf("wood", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return renderer;
                }
                float size = renderer.bounds.size.sqrMagnitude;
                if (size > biggestSize)
                {
                    biggestSize = size;
                    biggest = renderer;
                }
            }
            return biggest;
        }

        /// <summary>
        /// The top of the hull's side (the gunwale) beside a seat, in ship space. Steps outward from the seat,
        /// casting straight down from above at each step; the outermost spot that still hits the ship is the
        /// gunwale, and the hit height is its top.
        /// </summary>
        private Vector3 FindGunwale(Vector3 seatLocal, float side, Collider[] hullColliders)
        {
            return TryProbeGunwale(seatLocal.x, seatLocal.z, side, seatLocal.y, hullColliders, out Vector3 gunwale)
                ? gunwale
                : new Vector3(side * 1.2f, seatLocal.y + 0.3f, seatLocal.z);
        }

        /// <summary>
        /// Steps outward from <paramref name="startX"/> at <paramref name="z"/> (ship space), casting straight down
        /// from above; the outermost spot that still hits the ship is the gunwale, and the hit height is its top.
        /// </summary>
        private bool TryProbeGunwale(float startX, float z, float side, float seatY, Collider[] hullColliders, out Vector3 gunwale)
        {
            Vector3 down = transform.TransformDirection(Vector3.down);
            bool found = false;
            float gunwaleX = 0f;
            float gunwaleY = 0f;
            for (float d = 0f; d <= ProbeReach; d += ProbeStep)
            {
                float x = startX + side * d;
                Ray ray = new Ray(transform.TransformPoint(new Vector3(x, seatY + ProbeHeight, z)), down);
                if (TryRaycast(ray, ProbeHeight * 2f, hullColliders, out RaycastHit hit))
                {
                    found = true;
                    gunwaleX = x;
                    gunwaleY = transform.InverseTransformPoint(hit.point).y;
                }
            }
            gunwale = new Vector3(gunwaleX, gunwaleY + 0.02f, z);
            return found;
        }

        /// <summary>
        /// Where a stowed oar's pivot goes, in ship space. The oar lies flat along the inside of the hull, blade
        /// toward the stern, ideally centred on its bench. Hulls narrow toward the bow and stern, so on a short
        /// ship (the Karve) an oar centred on a bench near one end would poke through the side. The oar slides
        /// toward the middle of the ship until the hull is wide enough at both of its ends and its middle.
        /// </summary>
        private Vector3 FindStowPosition(Vector3 seatLocal, float side, float length, float outboard, Collider[] hullColliders)
        {
            float half = length / 2f;
            float towardMiddle = -Mathf.Sign(seatLocal.z);
            for (float shift = 0f; shift <= Mathf.Abs(seatLocal.z) + 0.01f; shift += 0.1f)
            {
                float centre = seatLocal.z + towardMiddle * shift;
                if (TryFitStowed(centre, half, side, seatLocal.y, hullColliders, out float x, out float y))
                {
                    return StowPivot(x, y, centre, side, outboard, half);
                }
            }
            // Nothing fits (a very small hull): lie along the centre line, just above the seat.
            return StowPivot(0f, seatLocal.y + 0.3f, 0f, side, outboard, half);
        }

        /// <summary>
        /// Whether an oar centred at this point along the ship fits inside the hull: the gunwale must be found at
        /// both ends and the middle. Gives the inset sideways position and the height, from the narrowest point.
        /// </summary>
        private bool TryFitStowed(float centre, float half, float side, float seatY, Collider[] hullColliders, out float x, out float y)
        {
            x = 0f;
            y = float.MaxValue;
            float narrowest = float.MaxValue;
            foreach (float z in new[] { centre - half, centre, centre + half })
            {
                if (!TryProbeGunwale(0f, z, side, seatY, hullColliders, out Vector3 gunwale))
                {
                    return false;
                }
                narrowest = Mathf.Min(narrowest, Mathf.Abs(gunwale.x));
                y = Mathf.Min(y, gunwale.y);
            }
            if (narrowest < StowInset + 0.15f)
            {
                return false;
            }
            x = side * (narrowest - StowInset);
            y -= StowDrop;
            return true;
        }

        /// <summary>The pivot for a stowed oar whose middle is at <paramref name="centre"/>.</summary>
        private static Vector3 StowPivot(float x, float y, float centre, float side, float outboard, float half)
        {
            // Stowed, the oar's +X points to the stern and runs from -Inboard to +outboard about the pivot,
            // so the pivot sits toward the bow of the oar's middle by (outboard - half).
            return new Vector3(x, y, centre + (outboard - half));
        }

        private static bool TryRaycast(Ray ray, float distance, Collider[] colliders, out RaycastHit closest)
        {
            closest = default;
            bool any = false;
            foreach (Collider collider in colliders)
            {
                if (collider == null || collider.isTrigger)
                {
                    continue;
                }
                if (collider.Raycast(ray, out RaycastHit hit, distance) && (!any || hit.distance < closest.distance))
                {
                    closest = hit;
                    any = true;
                }
            }
            return any;
        }

        /// <summary>
        /// Builds one oar: a root at the oarlock with the shaft along its +X (outward), the handle reaching
        /// Inboard toward the rower and the blade at the outboard end.
        /// </summary>
        private Transform BuildOar(string seatName, Vector3 oarlock, float outboard, Material material, int layer)
        {
            GameObject root = new GameObject("RowingMod_Oar_" + seatName);
            root.layer = layer;
            root.transform.SetParent(transform, worldPositionStays: false);
            root.transform.localPosition = oarlock;

            // A cylinder primitive is 2 m tall along Y with radius 0.5; lay it along X.
            float length = Inboard + outboard;
            GameObject shaft = MakePart(PrimitiveType.Cylinder, root.transform, material, layer);
            shaft.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            shaft.transform.localPosition = new Vector3((outboard - Inboard) / 2f, 0f, 0f);
            shaft.transform.localScale = new Vector3(ShaftRadius * 2f, length / 2f, ShaftRadius * 2f);

            GameObject blade = MakePart(PrimitiveType.Cube, root.transform, material, layer);
            blade.transform.localPosition = new Vector3(outboard - BladeLength / 2f, 0f, 0f);
            blade.transform.localScale = new Vector3(BladeLength, BladeWidth, BladeThickness);
            return root.transform;
        }

        private static GameObject MakePart(PrimitiveType type, Transform parent, Material material, int layer)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            // Remove the collider straight away so it never collides with the ship, even for one frame.
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.layer = layer;
            part.transform.SetParent(parent, worldPositionStays: false);
            if (material != null)
            {
                part.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
            return part;
        }

        private void Animate(Oar oar)
        {
            float t = Time.time - oar.StrokeStart;
            float sweep;
            float lift = 0f;
            float catchAngle = SweepAngle * oar.Amplitude * oar.Direction;
            if (t < DriveTime)
            {
                // Drive: blade in the water, swept from the catch (bow side) to the finish (stern side).
                // Backing reverses it. Starts from wherever the oar was, so a quick stroke doesn't snap.
                float u = Mathf.SmoothStep(0f, 1f, t / DriveTime);
                sweep = Mathf.Lerp(oar.SweepAtStrokeStart, -catchAngle, u);
            }
            else if (t < DriveTime + RecoveryTime)
            {
                // Recovery: blade lifted out of the water and swung forward to the catch, ready for the next stroke.
                float u = Mathf.SmoothStep(0f, 1f, (t - DriveTime) / RecoveryTime);
                sweep = Mathf.Lerp(-catchAngle, catchAngle * 0.7f, u);
                lift = Mathf.Sin(u * Mathf.PI) * LiftAngle;
            }
            else
            {
                // Settle back to rest, blade resting in the water.
                float u = Mathf.SmoothStep(0f, 1f, (t - DriveTime - RecoveryTime) / SettleTime);
                sweep = Mathf.Lerp(catchAngle * 0.7f, 0f, u);
            }
            oar.Sweep = sweep;

            // Swing between the water and the stowed pose as the bench fills or empties.
            float stowTarget = oar.Occupant != null ? 0f : 1f;
            oar.Stowed = Mathf.MoveTowards(oar.Stowed, stowTarget, Time.deltaTime / StowTime);
            float stow = Mathf.SmoothStep(0f, 1f, oar.Stowed);

            // Tilt down just enough for the blade to sit in the water, following the waves.
            Vector3 oarlockWorld = transform.TransformPoint(oar.RowPosition);
            float waterLevel = Floating.GetWaterLevel(oarlockWorld, ref oar.WaterVolume);
            float drop = oarlockWorld.y - waterLevel;
            float reach = oar.Outboard - BladeLength / 2f;
            float waterPitch = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(drop / reach, -1f, 1f)) * Mathf.Rad2Deg, RestPitchMin, RestPitchMax);
            float pitch = waterPitch - lift;

            // Holding water: square to the hull, blade dug in deeper, held still.
            oar.BrakeBlend = Mathf.MoveTowards(oar.BrakeBlend, oar.Braking ? 1f : 0f, Time.deltaTime / BrakeTime);
            float brake = Mathf.SmoothStep(0f, 1f, oar.BrakeBlend);
            sweep = Mathf.Lerp(sweep, 0f, brake);
            pitch = Mathf.Lerp(pitch, Mathf.Min(waterPitch + BrakeExtraPitch, RestPitchMax), brake);

            // Root axes: +X outward. Starboard oars use the ship's axes; port oars are turned 180° so +X points left.
            // Turning about Y by a negative angle moves +X toward +Z, so "toward the bow" is -sweep on starboard
            // and +sweep on port (whose local +Z faces the stern). Rotating about Z by a negative angle dips +X.
            Quaternion facing = oar.Side > 0f ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            float yaw = oar.Side > 0f ? -sweep : sweep;
            Quaternion rowing = facing * Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(-pitch, Vector3.forward);

            // Stowed: +X to the stern (a quarter turn about Y), blade turned flat (a quarter turn about the shaft).
            oar.Root.localRotation = Quaternion.Slerp(rowing, StowRotation, stow);
            oar.Root.localPosition = Vector3.Lerp(oar.RowPosition, oar.StowPosition, stow);
        }
    }
}
