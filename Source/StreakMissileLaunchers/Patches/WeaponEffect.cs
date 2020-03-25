using System;
using BattleTech;
using BattleTech.Rendering;
using Harmony;
using UnityEngine;

namespace StreakMissileLaunchers.Patches
{
    [HarmonyPatch(typeof(MissileLauncherEffect), "Init")]
    public static class MissileLauncherEffect_Init_Patch
    {
        public static void Prefix(MissileLauncherEffect __instance, Weapon weapon)
        {
            try
            {
                Logger.Info($"[MissileLauncherEffect_Init_PREFIX] weapon.parent.DisplayName: {weapon.parent.DisplayName}");
                Logger.Info($"[MissileLauncherEffect_Init_PREFIX] weapon.Name: {weapon.Name}");
                Logger.Info($"[MissileLauncherEffect_Init_PREFIX] BEFORE MissileLauncherEffect.preFireDuration: {__instance.preFireDuration}");

                if (weapon.AmmoCategoryValue.Name == "SRMStreak")
                {
                    __instance.preFireDuration = 0.9f;
                    Logger.Debug($"[MissileLauncherEffect_Init_PREFIX] ({weapon.parent.DisplayName}) Raised preFireDuration for {weapon.Name} to {__instance.preFireDuration}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }

    // Get Info
    [HarmonyPatch(typeof(WeaponEffect), "Init")]
    public static class WeaponEffect_Init_Patch
    {
        public static void Prefix(WeaponEffect __instance, Weapon weapon, float ___duration)
        {
            try
            {
                //Logger.Info($"[WeaponEffect_Init_PREFIX] actor: {weapon.parent.DisplayName}, weapon: {weapon.Name}, weaponEffect: {__instance.GetType()}, preFireDuration: {__instance.preFireDuration}");
                //Logger.Info($"[WeaponEffect_Init_PREFIX] ___duration: {___duration}");
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }

    // Get Info
    [HarmonyPatch(typeof(LaserEffect), "SetupLaser")]
    public static class LaserEffect_SetupLaser_Patch
    {
        public static void Postfix(LaserEffect __instance, BTLight ___laserLight, float ___laserAlpha, Color[] ___laserColor)
        {
            try
            {
                //Logger.Info($"[LaserEffect_SetupLaser_POSTFIX] weapon: {__instance.weapon.Name}, ___laserLight.LightColor: {___laserLight.LightColor}, ___laserAlpha: {___laserAlpha}, ___laserColor: {___laserColor[0]},{___laserColor[1]}");
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }



    // This is VERY IMPORTANT! Sending this message for pseudo-weapons will bring the underlying AttackSequence out of sync! (AttackDirector.AttackSequence.OnAttackSequenceWeaponPreFireComplete)
    [HarmonyPatch(typeof(WeaponEffect), "PublishNextWeaponMessage")]
    public static class WeaponEffect_PublishNextWeaponMessage_Patch
    {
        public static bool Prefix(WeaponEffect __instance)
        {
            try
            {
                if (__instance.weapon.weaponDef.Description.Id == Fields.StreakTargetingLaserId)
                {
                    Logger.Debug($"[WeaponEffect_PublishNextWeaponMessage_PREFIX] ({__instance.weapon.parent.DisplayName}) Supressing message for Weapon: {__instance.weapon.weaponDef.Description.Id}(WeaponEffect: {__instance.name})");
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

    // Prevents processing of AttackDirector.AttackSequence.OnAttackSequenceResolveDamage
    [HarmonyPatch(typeof(WeaponEffect), "PublishWeaponCompleteMessage")]
    public static class WeaponEffect_PublishWeaponCompleteMessage_Patch
    {
        public static bool Prefix(WeaponEffect __instance)
        {
            try
            {
                if (__instance.weapon.weaponDef.Description.Id == Fields.StreakTargetingLaserId)
                {
                    Logger.Debug($"[WeaponEffect_PublishWeaponCompleteMessage_PREFIX] ({__instance.weapon.parent.DisplayName}) Supressing message for Weapon: {__instance.weapon.weaponDef.Description.Id}(WeaponEffect: {__instance.name})");
                    new Traverse(__instance).Property("FiringComplete").SetValue(true);

                    Logger.Info($"[WeaponEffect_PublishWeaponCompleteMessage_PREFIX] __instance.FiringComplete: {__instance.FiringComplete}");

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

    // Suppressing this message will disable the "1" dmg floatie on impact ((AttackDirector.AttackSequence.OnAttackSequenceImpact))
    [HarmonyPatch(typeof(WeaponEffect), "OnImpact")]
    public static class WeaponEffect_OnImpact_Patch
    {
        public static bool Prefix(WeaponEffect __instance)
        {
            try
            {
                if (__instance.weapon.weaponDef.Description.Id == Fields.StreakTargetingLaserId)
                {
                    Logger.Debug($"[WeaponEffect_OnImpact_PREFIX] ({__instance.weapon.parent.DisplayName}) Supressing message for Weapon: {__instance.weapon.weaponDef.Description.Id}(WeaponEffect: {__instance.name})");

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

    // Will just prevent processing unnecessary code (AttackDirector.AttackSequence.OnAttackSequenceResolveDamage)
    [HarmonyPatch(typeof(WeaponEffect), "OnComplete")]
    public static class WeaponEffect_OnComplete_Patch
    {
        public static bool Prefix(WeaponEffect __instance, string ___activeProjectileName)
        {
            try
            {
                if (__instance.weapon.weaponDef.Description.Id == Fields.StreakTargetingLaserId)
                {
                    Logger.Debug($"[WeaponEffect_OnComplete_PREFIX] ({__instance.weapon.parent.DisplayName}) Supressing message for Weapon: {__instance.weapon.weaponDef.Description.Id}(WeaponEffect: {__instance.name})");

                    if (__instance.currentState == WeaponEffect.WeaponEffectState.Complete)
                    {
                        return false;
                    }
                    __instance.currentState = WeaponEffect.WeaponEffectState.Complete;

                    /* This has to be prevented...
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
