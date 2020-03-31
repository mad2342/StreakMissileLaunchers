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



    // Minimize the preFireDuration for targeting lasers to adjust the effect to their role
    [HarmonyPatch(typeof(WeaponEffect), "Init")]
    public static class WeaponEffect_Init_Patch
    {
        public static void Prefix(WeaponEffect __instance, Weapon weapon)
        {
            try
            {
                if (weapon.weaponDef.Description.Id == Fields.StreakTargetingLaserId)
                {
                    float oldPFD = __instance.preFireDuration;
                    // MUST be > 0f or it's gonna be overridden with some fallback
                    __instance.preFireDuration = 0.1f;
                    Logger.Debug($"[WeaponEffect_Init_PREFIX] ({weapon.parent.DisplayName}) Changed preFireDuration for {weapon.Name} from {oldPFD} to {__instance.preFireDuration}");

                    return;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }



    // Modify VFX for targeting laser
    [HarmonyPatch(typeof(LaserEffect), "SetupLaser")]
    public static class LaserEffect_SetupLaser_Patch
    {
        public static bool Prepare()
        {
            return Fields.AdjustTargetingLaserVFX;
        }

        public static void Prefix(LaserEffect __instance, ref Color[] ___laserColor, ref BTLight ___laserLight, ref LineRenderer ___beamRenderer)
        {
            try
            {
                Weapon weapon = __instance.weapon;
                if (weapon.weaponDef.Description.Id == Fields.StreakTargetingLaserId)
                {
                    Logger.Debug($"[LaserEffect_SetupLaser_PREFIX] ({weapon.parent.DisplayName}) lightIntensity: {__instance.lightIntensity}");
                    Logger.Debug($"[LaserEffect_SetupLaser_PREFIX] ({weapon.parent.DisplayName}) lightRadius: {__instance.lightRadius}");
                    Logger.Debug($"[LaserEffect_SetupLaser_PREFIX] ({weapon.parent.DisplayName}) pulseDelay: {__instance.pulseDelay}");

                    __instance.lightIntensity = 300000f; // Default: 3500000f (SmallLaserPulse: 50000)
                    //__instance.lightRadius = 120; // Default: 100 (SmallLaserPulse: 60)
                    //__instance.pulseDelay = 0.2f; // Default: ? (SmallLaserPulse: -1)

                    Color overrideColor = Fields.TargetingLaserColor;

                    ___beamRenderer = __instance.projectile.GetComponent<LineRenderer>();
                    Logger.Debug($"[LaserEffect_SetupLaser_PREFIX] ({weapon.parent.DisplayName}) __beamRenderer: {___beamRenderer.startColor}, {___beamRenderer.endColor}, {___beamRenderer.startWidth}, {___beamRenderer.endWidth}");
                    ___beamRenderer.startColor = overrideColor;
                    ___beamRenderer.endColor = overrideColor;

                    ___beamRenderer.startWidth = 1.9f;
                    ___beamRenderer.endWidth = 2.5f;

                    Logger.Debug($"[LaserEffect_SetupLaser_PREFIX] ({weapon.parent.DisplayName}) __beamRenderer: {___beamRenderer.startColor}, {___beamRenderer.endColor}, {___beamRenderer.startWidth}, {___beamRenderer.endWidth}");

                    // Used by LaserEffect.Update() for ___beamRenderer too;
                    ___laserColor[0] = overrideColor;
                    ___laserColor[1] = overrideColor;

                    ___laserLight = ___beamRenderer.GetComponentInChildren<BTLight>(true);
                    ___laserLight.lightColor = overrideColor;
                    Logger.Debug($"[LaserEffect_SetupLaser_PREFIX] ({weapon.parent.DisplayName}) ___laserLight.lightColor: {___laserLight.lightColor}");

                    // Called in original method, not necessary here...
                    //___laserLight.RefreshLightSettings(true);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public static void Postfix(LaserEffect __instance, ref Color[] ___laserColor, ref BTLight ___laserLight, ref LineRenderer ___beamRenderer)
        {
            try
            {
                Weapon weapon = __instance.weapon;
                if (weapon.weaponDef.Description.Id == Fields.StreakTargetingLaserId)
                {
                    Logger.Debug($"[LaserEffect_SetupLaser_POSTFIX] ({weapon.parent.DisplayName}) lightIntensity: {__instance.lightIntensity}");
                    Logger.Debug($"[LaserEffect_SetupLaser_POSTFIX] ({weapon.parent.DisplayName}) lightRadius: {__instance.lightRadius}");
                    Logger.Debug($"[LaserEffect_SetupLaser_POSTFIX] ({weapon.parent.DisplayName}) pulseDelay: {__instance.pulseDelay}");

                    Logger.Debug($"[LaserEffect_SetupLaser_POSTFIX] ({weapon.parent.DisplayName}) __beamRenderer: {___beamRenderer.startColor}, {___beamRenderer.endColor}, {___beamRenderer.startWidth}, {___beamRenderer.endWidth}");

                    Logger.Debug($"[LaserEffect_SetupLaser_POSTFIX] ({weapon.parent.DisplayName}) ___laserLight.lightColor: {___laserLight.lightColor}");

                    Vector4 tempColor = (Vector4)AccessTools.Field(typeof(BTLight), "tempColor").GetValue(___laserLight);
                    Vector4 linearColor = (Vector4)AccessTools.Field(typeof(BTLight), "linearColor").GetValue(___laserLight);
                    Logger.Info($"[LaserEffect_SetupLaser_POSTFIX] ({weapon.parent.DisplayName}) ___laserLight.tempColor: {tempColor}");
                    Logger.Info($"[LaserEffect_SetupLaser_POSTFIX] ({weapon.parent.DisplayName}) ___laserLight.linearColor: {linearColor}");
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
