using System;
using BattleTech;
using UnityEngine;

namespace StreakMissileLaunchers
{
    class Utilities
    {
        public static void FireStreakTargetingLaser(AttackDirector.AttackSequence sequence, AbstractActor actor, bool hitsTarget = true)
        {
            foreach (Weapon weapon in actor.supportComponents)
            {
                if (weapon.weaponDef.Description.Id == Fields.StreakTargetingLaserId)
                {
                    Logger.Info($"[Utilities_FireStreakTargetingLaser] {actor.DisplayName} has a targeting laser({Fields.StreakTargetingLaserId}) prepared");
                    Logger.Info($"[Utilities_FireStreakTargetingLaser] hitsTarget: {hitsTarget}");

                    // Generate WeaponHitInfo for targeting laser
                    float randomFloat = UnityEngine.Random.Range(0f, 1f);

                    WeaponHitInfo targetingLaserHitInfo = default(WeaponHitInfo);
                    targetingLaserHitInfo.attackerId = sequence.attacker.GUID;
                    targetingLaserHitInfo.targetId = sequence.chosenTarget.GUID;
                    targetingLaserHitInfo.numberOfShots = 1;
                    targetingLaserHitInfo.stackItemUID = sequence.stackItemUID;
                    targetingLaserHitInfo.attackSequenceId = sequence.id;
                    targetingLaserHitInfo.attackGroupIndex = 0;
                    targetingLaserHitInfo.attackWeaponIndex = 0;
                    targetingLaserHitInfo.toHitRolls = new float[1];
                    targetingLaserHitInfo.locationRolls = new float[] { randomFloat };
                    targetingLaserHitInfo.dodgeRolls = new float[1];
                    targetingLaserHitInfo.dodgeSuccesses = new bool[1];
                    targetingLaserHitInfo.hitLocations = new int[1];
                    targetingLaserHitInfo.hitPositions = new Vector3[1];
                    targetingLaserHitInfo.hitVariance = new int[1];
                    targetingLaserHitInfo.hitQualities = new AttackImpactQuality[1];
                    targetingLaserHitInfo.secondaryTargetIds = new string[1];
                    targetingLaserHitInfo.secondaryHitLocations = new int[1];
                    targetingLaserHitInfo.attackDirections = new AttackDirection[1];

                    float calledShotBonusMultiplier = sequence.attacker.CalledShotBonusMultiplier;
                    if (hitsTarget)
                    {
                        Logger.Info($"[Utilities_FireStreakTargetingLaser] targetingLaserHitInfo.locationRolls[0]: {targetingLaserHitInfo.locationRolls[0]}");
                        targetingLaserHitInfo.hitLocations[0] = sequence.chosenTarget.GetHitLocation(sequence.attacker, sequence.attackPosition, targetingLaserHitInfo.locationRolls[0], sequence.calledShotLocation, calledShotBonusMultiplier);
                        //hitInfo.hitQualities[i] = this.Director.Combat.ToHit.GetBlowQuality(this.attacker, this.attackPosition, weapon, this.chosenTarget, this.meleeAttackType, this.IsBreachingShot);
                    }
                    else
                    {
                        targetingLaserHitInfo.hitLocations[0] = 0; // None
                    }
                    Logger.Info($"[Utilities_FireStreakTargetingLaser] targetingLaserHitInfo.hitLocations[0]: {targetingLaserHitInfo.hitLocations[0]}");
                    targetingLaserHitInfo.hitPositions[0] = sequence.chosenTarget.GetImpactPosition(sequence.attacker, sequence.attackPosition, weapon, ref targetingLaserHitInfo.hitLocations[0], ref targetingLaserHitInfo.attackDirections[0], ref targetingLaserHitInfo.secondaryTargetIds[0], ref targetingLaserHitInfo.secondaryHitLocations[0]);



                    // Check existence and adjust effects
                    if (weapon.weaponRep != null && weapon.weaponRep.HasWeaponEffect)
                    {
                        if (weapon.weaponRep.WeaponEffect is LaserEffect LaserEffect)
                        {
                            // Disable sound for this effect
                            LaserEffect.beamStartSFX = "";
                            LaserEffect.beamStopSFX = "";
                            LaserEffect.pulseSFX = "";

                            //LaserEffect.pulseDelay = 0.25f;
                            LaserEffect.lightIntensity = 3500000f; 
                            LaserEffect.lightRadius = 100;
                        }

                        // Fire
                        weapon.weaponRep.PlayWeaponEffect(targetingLaserHitInfo);
                        Logger.Info($"[Utilities_FireStreakTargetingLaser] {actor.DisplayName} fired its targeting laser({Fields.StreakTargetingLaserId})!");

                        // Sometimes the vfx of this laser persist UNTIL some targeting laser (can also be on another actor) fires and somehow clears the old "trail"
                        // Probably need to mark as complete later to prevent vfx to persist?
                    }
                }
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



        /*
        public static void CreateAndFireStreakTargetingLaser(Weapon baseWeapon, WeaponHitInfo? baseWeaponHitInfo)
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
            mech.GameRep.weaponReps.Remove(fakeTargetingLaser.weaponRep);
            mech.supportComponents.Remove(fakeTargetingLaser);
            fakeTargetingLaser = null;
        }
        */
    }
}
