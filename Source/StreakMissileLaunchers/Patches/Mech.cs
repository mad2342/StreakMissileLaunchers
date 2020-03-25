using System;
using BattleTech;
using Harmony;

namespace StreakMissileLaunchers.Patches
{
    [HarmonyPatch(typeof(Mech), "InitGameRep")]
    public static class Mech_InitGameRep_Patch
    {
        public static void Prefix(Mech __instance)
        {
            try
            {
                bool hasStreaksMounted = false;
                foreach (Weapon w in __instance.Weapons)
                {
                    //Logger.Info($"[Mech_InitGameRep_PREFIX] ({__instance.DisplayName}) w.AmmoCategoryValue.Name: {w.AmmoCategoryValue.Name}");
                    if (w.Type == WeaponType.SRM && w.AmmoCategoryValue.Name == "SRMStreak")
                    {
                        hasStreaksMounted = true;
                    }
                }
                Logger.Info($"[Mech_InitGameRep_PREFIX] ({__instance.DisplayName}) hasStreaksMounted: {hasStreaksMounted}");

                if (hasStreaksMounted)
                {
                    Logger.Debug($"[Mech_InitGameRep_PREFIX] ({__instance.DisplayName}) Adding streak targeting laser");

                    // @ToDo: Also handle vehicles and turrets?
                    WeaponDef weaponDef = __instance.Combat.DataManager.WeaponDefs.Get(Fields.StreakTargetingLaserId);
                    MechComponentRef mechComponentRef = new MechComponentRef(weaponDef.Description.Id, weaponDef.Description.Id + "_StreakTargetingLaser", ComponentType.Weapon, ChassisLocations.CenterTorso, -1, ComponentDamageLevel.Functional, false);
                    mechComponentRef.SetComponentDef(weaponDef);
                    mechComponentRef.DataManager = __instance.Combat.DataManager;
                    mechComponentRef.RefreshComponentDef();
                    Weapon TargetingLaser = new Weapon(__instance, __instance.Combat, mechComponentRef, __instance.GUID + "_StreakTargetingLaser");
                    TargetingLaser.Init();
                    TargetingLaser.InitStats();

                    // Add to supportComponents which will get their representation ready in original method...
                    __instance.supportComponents.Add(TargetingLaser);

                    //Logger.Info($"[Mech_InitGameRep_PREFIX] ({__instance.DisplayName}) TargetingLaser.mechComponentRef.SimGameUID: {TargetingLaser.mechComponentRef.SimGameUID}");
                    //Logger.Info($"[Mech_InitGameRep_PREFIX] ({__instance.DisplayName}) TargetingLaser.mechComponentRef.ComponentDefID: {TargetingLaser.mechComponentRef.ComponentDefID}");
                    //Logger.Info($"[Mech_InitGameRep_PREFIX] ({__instance.DisplayName}) TargetingLaser.uid: {TargetingLaser.uid}");
                    //Logger.Info($"[Mech_InitGameRep_PREFIX] ({__instance.DisplayName}) TargetingLaser.weaponDef.Description.Id: {TargetingLaser.weaponDef.Description.Id}");
                    //Logger.Info($"[Mech_InitGameRep_PREFIX] ({__instance.DisplayName}) TargetingLaser.weaponDef.WeaponEffectID: {TargetingLaser.weaponDef.WeaponEffectID}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
