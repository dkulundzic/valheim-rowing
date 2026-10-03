using System.Collections.Generic;
using UnityEngine;

namespace RowingMod
{
    /// <summary>
    /// Draws an oar beside every rowing seat on a ship, resting in the water, and swings a rower's oar on each stroke. Runs on every client
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

        private class Oar
        {
            public Chair Seat;
            public float Side; // +1 starboard (right), -1 port (left)
            public Transform Root;
            public float Outboard;
            public Player Occupant;
            public WaterVolume WaterVolume;
            // Animation state, in "bow-ward" degrees: positive swings the blade toward the bow.
            public float StrokeStart = -100f;
            public float Amplitude;
            public float Direction = 1f;
            public float SweepAtStrokeStart;
            public float Sweep;
        }

        private Ship m_ship;
        private readonly List<Oar> m_oars = new List<Oar>();
        private bool m_built;

        private void Awake()
        {
            m_ship = GetComponent<Ship>();
        }

        /// <summary>Called for every stroke the ship receives, from any rower including the local one.</summary>
        public void OnStroke(long sender, bool strong)
        {
            foreach (Oar oar in m_oars)
            {
                if (oar.Occupant != null && OwnerOf(oar.Occupant) == sender)
                {
                    oar.SweepAtStrokeStart = oar.Sweep;
                    oar.StrokeStart = Time.time;
                    oar.Amplitude = strong ? 1f : WeakSweepFactor;
                    oar.Direction = ShipRowing.RowDirection(m_ship);
                    return;
                }
            }
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
            if (!m_built)
            {
                Build();
            }

            foreach (Oar oar in m_oars)
            {
                if (!oar.Root.gameObject.activeSelf)
                {
                    oar.Root.gameObject.SetActive(true);
                }
                // Who sits here decides whose strokes swing this oar.
                oar.Occupant = FindOccupant(oar.Seat);
                Animate(oar);
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

                Vector3 oarlock = FindOarlock(local, side, hullColliders);
                float halfWidth = Mathf.Abs(oarlock.x);
                Oar oar = new Oar
                {
                    Seat = chair,
                    Side = side,
                    Outboard = Mathf.Clamp(1.6f + halfWidth * 0.7f, 1.8f, 3.2f),
                };
                oar.Root = BuildOar(chair.name, oarlock, oar.Outboard, material, layer);
                m_oars.Add(oar);
            }
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
        /// Where the oar rests on the hull beside a seat, in ship space: cast a ray from outside the ship toward
        /// the seat, a little above seat height, and take where it meets the hull.
        /// </summary>
        private Vector3 FindOarlock(Vector3 seatLocal, float side, Collider[] hullColliders)
        {
            float height = seatLocal.y + 0.3f;
            Vector3 origin = transform.TransformPoint(new Vector3(side * 8f, height, seatLocal.z));
            Vector3 direction = transform.TransformDirection(new Vector3(-side, 0f, 0f));
            Ray ray = new Ray(origin, direction);
            float best = float.MaxValue;
            foreach (Collider collider in hullColliders)
            {
                if (collider.isTrigger)
                {
                    continue;
                }
                if (collider.Raycast(ray, out RaycastHit hit, 8f) && hit.distance < best)
                {
                    best = hit.distance;
                }
            }
            float x = best < float.MaxValue ? side * (8f - best + 0.05f) : side * 1.2f;
            return new Vector3(x, height, seatLocal.z);
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

            // Tilt down just enough for the blade to sit in the water, following the waves.
            Vector3 oarlockWorld = oar.Root.parent.TransformPoint(oar.Root.localPosition);
            float waterLevel = Floating.GetWaterLevel(oarlockWorld, ref oar.WaterVolume);
            float drop = oarlockWorld.y - waterLevel;
            float reach = oar.Outboard - BladeLength / 2f;
            float pitch = Mathf.Asin(Mathf.Clamp(drop / reach, -1f, 1f)) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, RestPitchMin, RestPitchMax) - lift;

            // Root axes: +X outward. Starboard oars use the ship's axes; port oars are turned 180° so +X points left.
            // Turning about Y by a negative angle moves +X toward +Z, so "toward the bow" is -sweep on starboard
            // and +sweep on port (whose local +Z faces the stern). Rotating about Z by a negative angle dips +X.
            Quaternion facing = oar.Side > 0f ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            float yaw = oar.Side > 0f ? -sweep : sweep;
            oar.Root.localRotation = facing * Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(-pitch, Vector3.forward);
        }
    }
}
