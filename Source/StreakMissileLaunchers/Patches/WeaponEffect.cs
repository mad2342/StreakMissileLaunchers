using System;
using BattleTech;
using Harmony;

namespace StreakMissileLaunchers.Patches
{
    [HarmonyPatch(typeof(MissileLauncherEffect), "Init")]
    public static class MissileLauncherEffect_Init_Patch
    {
        // Raising the preFireDuration for Steak SRM launchers to give their targeting laser enough time to display their vfx
        public static void Prefix(MissileLauncherEffect __instance, Weapon weapon)
        {
            try
            {
                if (weapon.Type == WeaponType.SRM && weapon.AmmoCategoryValue.Name == "SRMStreak")
                {
                    float oldPFD = __instance.preFireDuration;
                    __instance.preFireDuration = 0.9f;
                    Logger.Debug($"[MissileLauncherEffect_Init_PREFIX] ({weapon.parent.DisplayName}) Raised preFireDuration for {weapon.Name} from {oldPFD} to {__instance.preFireDuration}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }



    /**
     * These are VERY IMPORTANT! Sending these message for pseudo-weapons will bring the underlying AttackSequence out of sync!
     * - AttackDirector.AttackSequence.OnAttackSequenceWeaponPreFireComplete()
     * - AttackDirector.AttackSequence.OnAttackSequenceImpact()
     * - AttackDirector.AttackSequence.OnAttackSequenceResolveDamage()
     * - AttackDirector.AttackSequence.OnAttackSequenceWeaponComplete()
     * 
     * Everything apart from suppressing the messages is rebuilt just to be sure (even though it's probably not necessary for pseudo-weapons without a sequence)
    **/

    // Suppressing this message will prevent an early skip to the next weapon of the sequence at AttackDirector.AttackSequence.OnAttackSequenceWeaponPreFireComplete()
    [HarmonyPatch(typeof(WeaponEffect), "PublishNextWeaponMessage")]
    public static class WeaponEffect_PublishNextWeaponMessage_Patch
    {
        public static bool Prefix(WeaponEffect __instance)
        {
            try
            {
                if (__instance.weapon.weaponDef.Description.Id == Fields.StreakTargetingLaserId)
                {
                    Logger.Debug($"[WeaponEffect_PublishNextWeaponMessage_PREFIX] ({__instance.weapon.parent.DisplayName}) Supressing message for Weapon: {__instance.weapon.weaponDef.Description.Id} (WeaponEffect: {__instance.name})");

                    new Traverse(__instance).Field("attackSequenceNextDelayTimer").SetValue(-1f);
                    new Traverse(__instance).Field("hasSentNextWeaponMessage").SetValue(true);

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

    // Prevents processing of AttackDirector.AttackSequence.OnAttackSequenceResolveDamage()
    [HarmonyPatch(typeof(WeaponEffect), "PublishWeaponCompleteMessage")]
    public static class WeaponEffect_PublishWeaponCompleteMessage_Patch
    {
        public static bool Prefix(WeaponEffect __instance)
        {
            try
            {
                if (__instance.weapon.weaponDef.Description.Id == Fields.StreakTargetingLaserId)
                {
                    Logger.Debug($"[WeaponEffect_PublishWeaponCompleteMessage_PREFIX] ({__instance.weapon.parent.DisplayName}) Supressing message for Weapon: {__instance.weapon.weaponDef.Description.Id} (WeaponEffect: {__instance.name})");

                    new Traverse(__instance).Property("FiringComplete").SetValue(true);

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

    // Suppressing this message will disable the "1" dmg floatie on impact in AttackDirector.AttackSequence.OnAttackSequenceImpact()
    [HarmonyPatch(typeof(WeaponEffect), "OnImpact")]
    public static class WeaponEffect_OnImpact_Patch
    {
        public static bool Prefix(WeaponEffect __instance, ref float hitDamage)
        {
            try
            {
                if (__instance.weapon.weaponDef.Description.Id == Fields.StreakTargetingLaserId)
                {
                    Logger.Debug($"[WeaponEffect_OnImpact_PREFIX] ({__instance.weapon.parent.DisplayName}) Supressing message for Weapon: {__instance.weapon.weaponDef.Description.Id} (WeaponEffect: {__instance.name})");

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

    // Will just prevent processing unnecessary code of AttackDirector.AttackSequence.OnAttackSequenceResolveDamage()
    [HarmonyPatch(typeof(WeaponEffect), "OnComplete")]
    public static class WeaponEffect_OnComplete_Patch
    {
        public static bool Prefix(WeaponEffect __instance, string ___activeProjectileName)
        {
            try
            {
                if (__instance.weapon.weaponDef.Description.Id == Fields.StreakTargetingLaserId)
                {
                    Logger.Debug($"[WeaponEffect_OnComplete_PREFIX] ({__instance.weapon.parent.DisplayName}) Supressing message for Weapon: {__instance.weapon.weaponDef.Description.Id} (WeaponEffect: {__instance.name})");

                    if (__instance.currentState == WeaponEffect.WeaponEffectState.Complete)
                    {
                        return false;
                    }
                    __instance.currentState = WeaponEffect.WeaponEffectState.Complete;

                    /* This is to be prevented...
                    if (!__instance.subEffect)
                    {
                        AttackSequenceResolveDamageMessage message = new AttackSequenceResolveDamageMessage(__instance.hitInfo);
                        __instance.Combat.MessageCenter.PublishMessage(message);
                    }
                    __instance.PublishNextWeaponMessage();
                    __instance.PublishWeaponCompleteMessage();
                    */

                    if (__instance.projectilePrefab != null)
                    {
                        AutoPoolObject autoPoolObject = __instance.projectile.GetComponent<AutoPoolObject>();
                        if (autoPoolObject == null)
                        {
                            autoPoolObject = __instance.projectile.AddComponent<AutoPoolObject>();
                        }
                        autoPoolObject.Init(__instance.weapon.parent.Combat.DataManager, ___activeProjectileName, 4f);
                        __instance.projectile = null;
                    }

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
