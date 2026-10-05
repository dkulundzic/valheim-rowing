using System.Collections.Generic;
using UnityEngine;

namespace RowingMod
{
    /// <summary>
    /// Experimental: makes seated rowers' bodies follow their oar (UI.RowerLean). Valheim has no rowing animation, so
    /// after the game's own animation each frame the rower's spine and upper arms are turned a little on top of the
    /// sitting pose: leaning in and reaching as the blade swings to the catch, leaning back as it pulls through,
    /// braced while braking. The bones are found once per character (Unity's humanoid mapping if the rig has one,
    /// otherwise by name) and logged, since Valheim's skeleton isn't documented.
    /// </summary>
    public static class RowerLean
    {
        // At full sweep (the catch or the finish) the body leans this far, and the upper arms swing this far.
        private const float SpineLean = 12f;
        private const float ArmSwing = 22f;
        private const float BrakeLean = -8f;
        private const float FullSweep = 30f;

        private class Bones
        {
            public Transform Spine;
            public Transform Chest;
            public Transform LeftArm;
            public Transform RightArm;
        }

        private static readonly Dictionary<Player, Bones> s_bones = new Dictionary<Player, Bones>();
        private static bool s_logged;

        /// <summary>
        /// Poses one rower, called in LateUpdate after the game's animation. <paramref name="sweep"/> is the oar's
        /// sweep in degrees (positive toward the bow), <paramref name="active"/> how far the oar is out (0 stowed,
        /// 1 rowing) and <paramref name="brake"/> how far into the braking pose it is (0..1).
        /// </summary>
        public static void Apply(Player rower, Transform ship, float sweep, float active, float brake)
        {
            if (!RowingPlugin.RowerLean.Value || rower == null || active <= 0.01f)
            {
                return;
            }
            Bones bones = GetBones(rower);
            if (bones == null)
            {
                return;
            }
            // Lean about the ship's sideways axis: forward (toward the bow) at the catch, back at the finish.
            float reach = Mathf.Clamp(sweep / FullSweep, -1f, 1f) * active;
            float lean = Mathf.Lerp(reach * SpineLean, BrakeLean, brake);
            float arms = Mathf.Lerp(reach * ArmSwing, 0f, brake);
            Vector3 axis = ship.right;
            Rotate(bones.Spine, axis, lean * 0.5f);
            Rotate(bones.Chest, axis, lean * 0.5f);
            Rotate(bones.LeftArm, axis, arms);
            Rotate(bones.RightArm, axis, arms);
        }

        private static void Rotate(Transform bone, Vector3 axis, float degrees)
        {
            if (bone != null && Mathf.Abs(degrees) > 0.01f)
            {
                bone.rotation = Quaternion.AngleAxis(degrees, axis) * bone.rotation;
            }
        }

        private static Bones GetBones(Player player)
        {
            if (s_bones.TryGetValue(player, out Bones cached))
            {
                return cached;
            }
            Bones bones = new Bones();
            Animator animator = player.GetComponentInChildren<Animator>();
            if (animator != null && animator.isHuman)
            {
                bones.Spine = animator.GetBoneTransform(HumanBodyBones.Spine);
                bones.Chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                bones.LeftArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                bones.RightArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            }
            // Fall back to names, e.g. Spine, Spine1/Spine2 or Chest, LeftArm/RightArm or LeftUpperArm.
            Transform root = animator != null ? animator.transform : player.transform;
            bones.Spine = bones.Spine ?? Find(root, "Spine");
            bones.Chest = bones.Chest ?? Find(root, "Spine2") ?? Find(root, "Spine1") ?? Find(root, "Chest");
            bones.LeftArm = bones.LeftArm ?? Find(root, "LeftArm") ?? Find(root, "LeftUpperArm");
            bones.RightArm = bones.RightArm ?? Find(root, "RightArm") ?? Find(root, "RightUpperArm");
            if (!s_logged)
            {
                s_logged = true;
                RowingPlugin.Log.LogInfo($"Rower lean bones (humanoid rig: {animator != null && animator.isHuman}): spine {Name(bones.Spine)}, chest {Name(bones.Chest)}, " +
                    $"left arm {Name(bones.LeftArm)}, right arm {Name(bones.RightArm)}");
            }
            if (bones.Spine == null && bones.Chest == null && bones.LeftArm == null && bones.RightArm == null)
            {
                bones = null;
            }
            s_bones[player] = bones;
            return bones;
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (string.Equals(child.name, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
            return null;
        }

        private static string Name(Transform bone)
        {
            return bone != null ? bone.name : "not found";
        }

        /// <summary>Drops cached bones of players who left (their objects are destroyed).</summary>
        public static void Forget()
        {
            List<Player> gone = null;
            foreach (Player player in s_bones.Keys)
            {
                if (player == null)
                {
                    (gone ??= new List<Player>()).Add(player);
                }
            }
            if (gone != null)
            {
                foreach (Player player in gone)
                {
                    s_bones.Remove(player);
                }
            }
        }
    }
}
