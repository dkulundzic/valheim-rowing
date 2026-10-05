using HarmonyLib;
using UnityEngine;

namespace RowingMod
{
    /// <summary>
    /// Gamepad rowing uses the triggers (RT to row, LT to brake), which both of Valheim's gamepad layouts bind to
    /// attack and block. A seated player stands up on attack or block, so while the local player sits at an oar
    /// those inputs are dropped before the game sees them. Moving, jumping and crouching still stand them up.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    internal static class Player_SetControls_Patch
    {
        private static void Prefix(Player __instance, ref bool attack, ref bool attackHold, ref bool secondaryAttack,
            ref bool secondaryAttackHold, ref bool block, ref bool blockHold)
        {
            if (!RowingPlugin.Gamepad.Value || !Rower.SeatedAtOar || __instance != Player.m_localPlayer)
            {
                return;
            }
            attack = false;
            attackHold = false;
            secondaryAttack = false;
            secondaryAttackHold = false;
            block = false;
            blockHold = false;
        }
    }
}
