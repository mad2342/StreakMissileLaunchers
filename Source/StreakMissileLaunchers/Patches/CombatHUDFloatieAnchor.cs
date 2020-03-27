using System;
using BattleTech;
using BattleTech.UI;
using Harmony;
using HBS;
using UnityEngine;

namespace StreakMissileLaunchers.Patches
{
    [HarmonyPatch(typeof(CombatHUDFloatieAnchor), "GetFloatieColor")]
    public static class CombatHUDFloatieAnchor_GetFloatieColor_Patch
    {
        public static void Postfix(CombatHUDFloatieAnchor __instance, ref Color __result, FloatieMessage.MessageNature nature, UIManager ___uiManager)
        {
            try
            {
                if (nature == FloatieMessage.MessageNature.Dodge)
                {
                    Color overrideColor = ___uiManager.UIColorRefs.gold;
                    __result = overrideColor;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
