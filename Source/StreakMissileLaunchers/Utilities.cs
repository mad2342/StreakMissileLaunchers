using System;
using System.Collections.Generic;
using BattleTech;
using UnityEngine;

namespace StreakMissileLaunchers
{
    class Utilities
    {
        public static void CreateAndFireStreakTargetingLaser(Weapon baseWeapon, WeaponHitInfo? baseWeaponHitInfo)
        {
            try
            {
                AbstractActor actor = baseWeapon.parent;
                if (!(actor is Mech mech))
                {
                    return;
                }

                // Borrowed from AbstractActor.InitAbilities()
                WeaponDef weaponDef = actor.Combat.DataManager.WeaponDefs.Get("Weapon_Laser_SmallLaserPulse_0-STOCK");
                MechComponentRef mechComponentRef = new MechComponentRef(weaponDef.Description.Id, weaponDef.Description.Id + "_StreakTargettingLaser", ComponentType.Weapon, (ChassisLocations)baseWeapon.Location, -1, baseWeapon.DamageLevel, false);
                mechComponentRef.SetComponentDef(weaponDef);
                mechComponentRef.DataManager = actor.Combat.DataManager;
                mechComponentRef.RefreshComponentDef();
                Weapon fakeTargetingLaser = new Weapon(mech, actor.Combat, mechComponentRef, actor.Combat.GUID + "_StreakTargettingLaser");
                fakeTargetingLaser.Init();
                fakeTargetingLaser.InitStats();
                mech.supportComponents.Add(fakeTargetingLaser);
                Logger.Info($"[TEST] fakeTargetingLaser.weaponDef.WeaponEffectID: {fakeTargetingLaser.weaponDef.WeaponEffectID}");


                // Borrowed from Mech.InitGameRep()
                List<string> usedPrefabNames = new List<string>();
                fakeTargetingLaser.baseComponentRef.prefabName = MechHardpointRules.GetComponentPrefabName(mech.MechDef.Chassis.HardpointDataDef, fakeTargetingLaser.baseComponentRef, mech.MechDef.Chassis.PrefabBase, fakeTargetingLaser.mechComponentRef.MountedLocation.ToString().ToLower(), ref usedPrefabNames);
                fakeTargetingLaser.baseComponentRef.hasPrefabName = true;
                if (!string.IsNullOrEmpty(fakeTargetingLaser.baseComponentRef.prefabName))
                {
                    Transform attachTransform = mech.GetAttachTransform(fakeTargetingLaser.mechComponentRef.MountedLocation);
                    fakeTargetingLaser.InitGameRep(fakeTargetingLaser.baseComponentRef.prefabName, attachTransform, actor.LogDisplayName);

                    mech.GameRep.weaponReps.Add(fakeTargetingLaser.weaponRep);
                    string componentMountingPointPrefabName = MechHardpointRules.GetComponentMountingPointPrefabName(mech.MechDef, fakeTargetingLaser.mechComponentRef);
                    if (!string.IsNullOrEmpty(componentMountingPointPrefabName))
                    {
                        WeaponRepresentation component = actor.Combat.DataManager.PooledInstantiate(componentMountingPointPrefabName, BattleTechResourceType.Prefab, null, null, null).GetComponent<WeaponRepresentation>();
                        component.Init(mech, attachTransform, true, actor.LogDisplayName, fakeTargetingLaser.Location);
                        mech.GameRep.weaponReps.Add(component);
                    }
                }


                // Borrowed from AttackDirector.AttackSequence.OnAttackSequenceFire()
                WeaponHitInfo? weaponHitInfo = baseWeaponHitInfo;
                Logger.Info($"[TEST] weaponHitInfo: {weaponHitInfo}");
                if (weaponHitInfo != null)
                {
                    WeaponHitInfo whi;
                    whi = weaponHitInfo.Value;
                    Logger.Info($"[TEST] fakeTargetingLaser.weaponRep: {fakeTargetingLaser.weaponRep}");
                    Logger.Info($"[TEST] fakeTargetingLaser.weaponRep.HasWeaponEffect: {fakeTargetingLaser.weaponRep.HasWeaponEffect}");
                    if (fakeTargetingLaser.weaponRep != null && fakeTargetingLaser.weaponRep.HasWeaponEffect)
                    {
                        // ToDo: Disable sound for this effect
                        fakeTargetingLaser.weaponRep.PlayWeaponEffect(whi);
                    }
                }


                // Cleanup
                /*
                foreach (Weapon w in mech.Weapons)
                {
                    Logger.Info($"[TEST] mech.Weapons: {w.Name}({w.uid})");
                }
                foreach (MechComponent c in mech.supportComponents)
                {
                    Logger.Info($"[TEST] mech.supportComponents: {c.Name}({c.GUID})");
                }
                */

                mech.GameRep.weaponReps.Remove(fakeTargetingLaser.weaponRep);
                mech.supportComponents.Remove(fakeTargetingLaser);
                fakeTargetingLaser = null;



            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public static void LogHitLocations(WeaponHitInfo hitInfo)
        {
            try
            {
                string output = "---\n";
                output += $"[Utilities_LogHitLocations] Clustered hits: {hitInfo.hitLocations.Length}\n";
                for (int i = 0; i < hitInfo.hitLocations.Length; i++)
                {
                    int location = hitInfo.hitLocations[i];
                    var chassisLocationFromArmorLocation = MechStructureRules.GetChassisLocationFromArmorLocation((ArmorLocation)location);

                    if (location == 0 || location == 65536)
                    {
                        output += $"[Utilities_LogHitLocations] HitLocation {i}: NONE/INVALID";
                    }
                    else
                    {
                        output += $"[Utilities_LogHitLocations] HitLocation {i}: {chassisLocationFromArmorLocation}({location})";
                    }

                    output += (i < hitInfo.hitLocations.Length -1) ? "\n" : "\n---";
                }
                Logger.Info(output, false);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
