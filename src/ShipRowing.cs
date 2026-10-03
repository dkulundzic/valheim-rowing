using HarmonyLib;
using UnityEngine;

namespace RowingMod
{
    /// <summary>
    /// Lives on every ship. Rowers send their strokes here; the client that owns the ship
    /// turns them into force, weaker the closer the ship is to its top sail speed. The owner is a client on board (the server
    /// hands it to whoever arrives first, and it only moves when the owner leaves), not necessarily the helmsman.
    /// </summary>
    public class ShipRowing : MonoBehaviour
    {
        public const string StrokeRpc = "RowingMod_Stroke";
        public const string BoostKey = "RowingMod_Boost";
        // Session ID of the last owner that ran this mod. If it differs from the ZDO's owner, the owner is vanilla.
        public const string ModdedOwnerKey = "RowingMod_Owner";

        private const float BoostSyncInterval = 0.5f;

        private Ship m_ship;
        private ZNetView m_nview;
        private Rigidbody m_body;
        private WaterVolume m_waterVolume;
        private float m_boost;
        private float m_boostSyncTimer;
        private float m_topSpeed;

        private void Awake()
        {
            m_ship = GetComponent<Ship>();
            m_nview = GetComponent<ZNetView>();
            m_body = GetComponent<Rigidbody>();
            if (m_nview == null || m_nview.GetZDO() == null)
            {
                return;
            }

            m_nview.Register<float>(StrokeRpc, RPC_Stroke);
            int seats = GetComponentsInChildren<Chair>(includeInactive: true).Length;
            m_topSpeed = EstimateTopSailSpeed(m_ship);
            RowingPlugin.Log.LogInfo($"{name}: {seats} seat(s) for rowers, top sail speed {m_topSpeed:0.0} m/s");
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

        /// <summary>
        /// How much of a stroke's push gets through at the ship's current speed: all of it when still,
        /// 1 - (v / top)² as it speeds up, and none at or above top sail speed. Speed counts in the rowing direction.
        /// </summary>
        private float SpeedFactor()
        {
            float topSpeed = m_topSpeed * Mathf.Max(0f, RowingPlugin.TopSpeedMultiplier.Value);
            if (topSpeed <= 0.01f)
            {
                return 0f;
            }
            float speed = Mathf.Max(0f, m_ship.GetSpeed() * RowDirection(m_ship));
            float ratio = speed / topSpeed;
            return Mathf.Clamp01(1f - ratio * ratio);
        }

        /// <summary>Whether rowing adds speed right now: any setting but Stop, including with the sail open.</summary>
        public static bool CanRow(Ship ship)
        {
            return ship.GetSpeedSetting() != Ship.Speed.Stop || RowingPlugin.AllowRowingWhenStopped.Value;
        }

        /// <summary>Which way rowing pushes: +1 forward, or -1 while the ship is backing.</summary>
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

        private void RPC_Stroke(long sender, float quality)
        {
            // Only the owner moves the ship. A stroke that arrives just after ownership changed is dropped.
            if (!m_nview.IsOwner() || !CanRow(m_ship))
            {
                return;
            }
            quality = Mathf.Clamp01(quality);
            m_boost = Mathf.Min(m_boost + quality * RowingPlugin.StrokeStrength.Value, RowingPlugin.MaxBoost.Value);
        }

        internal void ApplyBoost(float dt)
        {
            if (m_nview == null || !m_nview.IsValid() || !m_nview.IsOwner())
            {
                return;
            }

            if (!CanRow(m_ship))
            {
                m_boost = 0f;
            }

            if (m_boost > 0.001f && IsInWater())
            {
                // Same units as the game's paddle force (m_backwardForce), but pushed through the
                // centre of mass so rowing doesn't turn the ship.
                Vector3 force = transform.forward * (RowDirection(m_ship) * m_ship.m_backwardForce * m_boost * SpeedFactor());
                m_body.AddForceAtPosition(force * (m_body.mass * dt), m_body.worldCenterOfMass, ForceMode.Impulse);
            }

            m_boost *= Mathf.Exp(-dt / Mathf.Max(0.05f, RowingPlugin.StrokeFade.Value));
            if (m_boost < 0.001f)
            {
                m_boost = 0f;
            }

            m_boostSyncTimer -= dt;
            if (m_boostSyncTimer <= 0f)
            {
                m_boostSyncTimer = BoostSyncInterval;
                m_nview.GetZDO().Set(BoostKey, m_boost);
            }
            // Unchanged values aren't resent, so this costs nothing after the first frame as owner.
            m_nview.GetZDO().Set(ModdedOwnerKey, ZDOMan.GetSessionID());
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
