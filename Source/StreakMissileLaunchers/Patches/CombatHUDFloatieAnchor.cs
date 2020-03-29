using System;
using BattleTech;
using BattleTech.UI;
using Harmony;
using UnityEngine;

namespace StreakMissileLaunchers.Patches
{
    // Utilizing "FloatieMessage.MessageNature.Suppression" and "FloatieMessage.MessageNature.Dodge" as they don't seem to be used by vanilla code
    [HarmonyPatch(typeof(CombatHUDFloatieAnchor), "GetFloatieColor")]
    public static class CombatHUDFloatieAnchor_GetFloatieColor_Patch
    {
        public static void Postfix(CombatHUDFloatieAnchor __instance, ref Color __result, FloatieMessage.MessageNature nature, UIManager ___uiManager)
        {
            try
            {
                // Streak locked-on
                if (nature == FloatieMessage.MessageNature.Suppression)
                {
                    //__result = Fields.TargetingLaserColor;
                    __result = ___uiManager.UIColorRefs.white;

                    //__result = ___uiManager.UIColorRefs.green;
                }
                // Streak failed to connect
                else if (nature == FloatieMessage.MessageNature.Dodge)
                {
                    __result = ___uiManager.UIColorRefs.structureDamaged;
                    //__result = ___uiManager.UIColorRefs.orange;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }



    // Redirect "FloatieMessage.MessageNature.Suppression" to the "normal" floatie creation process (!stacked)
    [HarmonyPatch(typeof(CombatHUDInWorldElementMgr), "AddFloatieMessage")]
    public static class CombatHUDInWorldElementMgr_AddFloatieMessage_Patch
    {
        public static bool Prefix(CombatHUDInWorldElementMgr __instance, MessageCenterMessage message)
        {
            try
            {
                FloatieMessage floatieMessage = message as FloatieMessage;

                if (floatieMessage.nature == FloatieMessage.MessageNature.Suppression)
                {
                    Traverse ShowFloatie = Traverse.Create(__instance).Method("ShowFloatie", floatieMessage);
                    ShowFloatie.GetValue();

                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);

                return true;
            }
        }
    }
}
