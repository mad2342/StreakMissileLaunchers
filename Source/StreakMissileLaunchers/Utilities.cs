using System;
using System.Collections.Generic;
using BattleTech;
using UnityEngine;

namespace StreakMissileLaunchers
{
    class Utilities
    {
        public static void CreateAndFireStreakTargetingLaser(AttackDirector.AttackSequence sequence, Weapon baseWeapon, out Vector3 floatieVector, bool hitsTarget = true)
        {
            //@ToDo: Make this work for turrets and vehicles too

            floatieVector = sequence.chosenTarget.CurrentPosition;
            AbstractActor actor = baseWeapon.parent;

            if (!(actor is Mech mech))
            {
                return;
            }
            Logger.Debug($"[Utilities_CreateAndFireStreakTargetingLaser] {actor.DisplayName} prepares its targeting laser for weapon: {baseWeapon.Name}");

            

            // Borrowed from AbstractActor.InitAbilities()
            WeaponDef weaponDef = actor.Combat.DataManager.WeaponDefs.Get(Fields.StreakTargetingLaserId);
            MechComponentRef mechComponentRef = new MechComponentRef(weaponDef.Description.Id, weaponDef.Description.Id + "_Reference", ComponentType.Weapon, (ChassisLocations)baseWeapon.Location, -1, ComponentDamageLevel.Functional, false);
            mechComponentRef.SetComponentDef(weaponDef);
            mechComponentRef.DataManager = actor.Combat.DataManager;
            mechComponentRef.RefreshComponentDef();
            Weapon TargetingLaser = new Weapon(mech, actor.Combat, mechComponentRef, weaponDef.Description.Id + baseWeapon.GUID);
            TargetingLaser.Init();
            TargetingLaser.InitStats();
            //Logger.Debug($"[Utilities_CreateAndFireStreakTargetingLaser] TargetingLaser.Name: {TargetingLaser.Name}, TargetingLaser.uid: {TargetingLaser.uid}, TargetingLaser.GUID: {TargetingLaser.GUID}");



            // Borrowed from Mech.InitGameRep()
            List<string> usedPrefabNames = new List<string>();
            TargetingLaser.baseComponentRef.prefabName = MechHardpointRules.GetComponentPrefabName(mech.MechDef.Chassis.HardpointDataDef, TargetingLaser.baseComponentRef, mech.MechDef.Chassis.PrefabBase, TargetingLaser.mechComponentRef.MountedLocation.ToString().ToLower(), ref usedPrefabNames);
            TargetingLaser.baseComponentRef.hasPrefabName = true;
            if (!string.IsNullOrEmpty(TargetingLaser.baseComponentRef.prefabName))
            {
                Transform attachTransform = mech.GetAttachTransform(TargetingLaser.mechComponentRef.MountedLocation);
                TargetingLaser.InitGameRep(TargetingLaser.baseComponentRef.prefabName, attachTransform, actor.LogDisplayName);
                mech.GameRep.weaponReps.Add(TargetingLaser.weaponRep);

                // Needed? (Probably only the visuals on the mech, we only need the "effect origin")
                string componentMountingPointPrefabName = MechHardpointRules.GetComponentMountingPointPrefabName(mech.MechDef, TargetingLaser.mechComponentRef);
                if (!string.IsNullOrEmpty(componentMountingPointPrefabName))
                {
                    WeaponRepresentation component = actor.Combat.DataManager.PooledInstantiate(componentMountingPointPrefabName, BattleTechResourceType.Prefab, null, null, null).GetComponent<WeaponRepresentation>();
                    component.Init(mech, attachTransform, true, actor.LogDisplayName, TargetingLaser.Location);
                    mech.GameRep.weaponReps.Add(component);
                }
            }
            // Hide targeting laser visually (Otherwise could disturb other weaponReps in the same ChassisLocation)
            TargetingLaser.weaponRep.OnPlayerVisibilityChanged(VisibilityLevel.None);



            // Generate WeaponHitInfo for targeting laser
            float randomFloat = UnityEngine.Random.Range(0f, 1f);

            WeaponHitInfo targetingLaserHitInfo = default(WeaponHitInfo);
            targetingLaserHitInfo.attackerId = sequence.attacker.GUID;
            targetingLaserHitInfo.targetId = sequence.chosenTarget.GUID;
            targetingLaserHitInfo.numberOfShots = 1;
            //targetingLaserHitInfo.stackItemUID = sequence.stackItemUID;
            //targetingLaserHitInfo.attackSequenceId = sequence.id;
            //targetingLaserHitInfo.attackGroupIndex = 0;
            //targetingLaserHitInfo.attackWeaponIndex = 0;
            targetingLaserHitInfo.stackItemUID = -1;
            targetingLaserHitInfo.attackSequenceId = -1;
            targetingLaserHitInfo.attackGroupIndex = -1;
            targetingLaserHitInfo.attackWeaponIndex = -1;
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
                Logger.Info($"[Utilities_CreateAndFireStreakTargetingLaser] targetingLaserHitInfo.locationRolls[0]: {targetingLaserHitInfo.locationRolls[0]}");
                targetingLaserHitInfo.hitLocations[0] = sequence.chosenTarget.GetHitLocation(sequence.attacker, sequence.attackPosition, targetingLaserHitInfo.locationRolls[0], sequence.calledShotLocation, calledShotBonusMultiplier);
                targetingLaserHitInfo.hitPositions[0] = sequence.chosenTarget.GetImpactPosition(sequence.attacker, sequence.attackPosition, TargetingLaser, ref targetingLaserHitInfo.hitLocations[0], ref targetingLaserHitInfo.attackDirections[0], ref targetingLaserHitInfo.secondaryTargetIds[0], ref targetingLaserHitInfo.secondaryHitLocations[0]);
                floatieVector = targetingLaserHitInfo.hitPositions[0];
            }
            else
            {
                targetingLaserHitInfo.hitLocations[0] = 0; // None
                targetingLaserHitInfo.hitPositions[0] = sequence.chosenTarget.GetImpactPosition(sequence.attacker, sequence.attackPosition, TargetingLaser, ref targetingLaserHitInfo.hitLocations[0], ref targetingLaserHitInfo.attackDirections[0], ref targetingLaserHitInfo.secondaryTargetIds[0], ref targetingLaserHitInfo.secondaryHitLocations[0]);
                floatieVector = sequence.chosenTarget.TargetPosition + UnityEngine.Random.insideUnitSphere * 5f;
            }
            Logger.Info($"[Utilities_CreateAndFireStreakTargetingLaser] targetingLaserHitInfo.hitLocations[0]: {targetingLaserHitInfo.hitLocations[0]}");
            Logger.Info($"[Utilities_CreateAndFireStreakTargetingLaser] floatieVector: {floatieVector}");



            // Check existence, adjust effects and fire
            if (TargetingLaser.weaponRep != null && TargetingLaser.weaponRep.HasWeaponEffect)
            {
                if (TargetingLaser.weaponRep.WeaponEffect is LaserEffect LaserEffect)
                {
                    // Disable sound for this effect
                    LaserEffect.beamStartSFX = "";
                    LaserEffect.beamStopSFX = "";
                    LaserEffect.pulseSFX = "";
                    LaserEffect.weaponImpactType = AudioSwitch_weapon_type.machinegun; // Not noticable
                }

                // Ping (Could be used for Streak LRMs)
                //RadarPingIndicator.Instance.Ping(sequence.chosenTarget.CurrentPosition);

                // Fire
                TargetingLaser.weaponRep.PlayWeaponEffect(targetingLaserHitInfo);
                Logger.Debug($"[Utilities_CreateAndFireStreakTargetingLaser] {actor.DisplayName} fired its targeting laser({Fields.StreakTargetingLaserId})");
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
                        output += $"[Utilities_LogHitLocations] HitLocation {i}: NONE/INVALID({location})";
                    }
                    else
                    {
                        output += $"[Utilities_LogHitLocations] HitLocation {i}: {chassisLocationFromArmorLocation}({location})";
                    }

                    output += (i < hitInfo.hitLocations.Length - 1) ? "\n" : "\n---";
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
