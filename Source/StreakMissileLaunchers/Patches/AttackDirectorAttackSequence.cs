using System;
using System.Collections.Generic;
using System.Reflection;
using BattleTech;
using Harmony;
using StreakMissileLaunchers.Extensions;
using UnityEngine;

namespace StreakMissileLaunchers.Patches
{
    [HarmonyPatch(typeof(AttackDirector.AttackSequence), "GetIndividualHits")]
    public static class AttackDirector_AttackSequence_GetIndividualHits_Patch
    {
        private static FastInvokeHandler AttackSequenceGetClusteredHits;

        private static void BuildAttackSequenceGetClusteredHits()
        {
            MethodInfo mi = AccessTools.Method(typeof(AttackDirector.AttackSequence), "GetClusteredHits");
            AttackSequenceGetClusteredHits = MethodInvoker.GetHandler(mi);
        }

        public static bool Prepare()
        {
            BuildAttackSequenceGetClusteredHits();
            return true;
        }

        public static bool Prefix(AttackDirector.AttackSequence __instance, ref WeaponHitInfo hitInfo, int groupIdx, int weaponIdx, Weapon weapon, ref float toHitChance, float prevDodgedDamage)
        {
            try
            {
                if (weapon.Type == WeaponType.SRM && weapon.AmmoCategoryValue.Name == "SRMStreak")
                {
                    Logger.Debug($"[AttackDirector.AttackSequence_GetIndividualHits_PREFIX] ---");
                    Logger.Debug($"[AttackSequence_GetIndividualHits_PREFIX] ({weapon.parent.DisplayName}) PREPARE AttackSequence: {__instance.id}, WeaponGroup: {groupIdx}, Weapon: {weapon.Name}({weaponIdx})");
                    Logger.Info($"[AttackDirector.AttackSequence_GetIndividualHits_PREFIX] ({weapon.Name}) toHitChance: {toHitChance}");

                    float hitChance = __instance.Director.Combat.ToHit.GetToHitChance(__instance.attacker, weapon, __instance.chosenTarget, __instance.attackPosition, __instance.chosenTarget.CurrentPosition, __instance.numTargets, __instance.meleeAttackType, __instance.isMoraleAttack);
                    Logger.Info($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] ({weapon.Name}) hitChance: {hitChance}");

                    float hitRoll = UnityEngine.Random.Range(0f, 1f);
                    Logger.Info($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] ({weapon.Name}) hitRoll: {hitRoll}");

                    bool streakWillHit = hitRoll <= hitChance;
                    Logger.Debug($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] ({weapon.Name}) streakWillHit: {streakWillHit}");


                    if (streakWillHit)
                    {
                        toHitChance = 1f;
                    }
                    else
                    {
                        toHitChance = 0f;
                    }
                    Logger.Info($"[AttackDirector.AttackSequence_GetIndividualHits_PREFIX] ({weapon.Name}) toHitChance: {toHitChance}");


                    // Redirecting to hit determination method for clustered hits (LRMs only by default)
                    AttackSequenceGetClusteredHits.Invoke(__instance, new object[] { hitInfo, groupIdx, weaponIdx, weapon, toHitChance, prevDodgedDamage });
                    Logger.Info($"[AttackDirector.AttackSequence_GetIndividualHits_PREFIX] ({weapon.Name}) Fetched clustered hits...");

                    if (streakWillHit)
                    {
                        // Make absolutely sure ALL missiles hits (Needed because of roll correction in AttackDirector.AttackSequence.GetClusteredHits() which sometimes returns "corrected" rolls of > 1f)
                        // With the added Patch to AttackDirector.AttackSequence.GetCorrectedRoll() this should NEVER be called
                        for (int i = 0; i < hitInfo.numberOfShots; i++)
                        {
                            // 0 = no hit, 65536 = secondary target hit
                            if (hitInfo.hitLocations[i] == 0 || hitInfo.hitLocations[i] == 65536)
                            {
                                Logger.Debug($"[AttackDirector.AttackSequence_GetIndividualHits_PREFIX] WARNING: Missile[{i}] had a hit location of 0|65536 even though the Streak should hit. Recalculating!");
                                hitInfo.hitLocations[i] = __instance.chosenTarget.GetHitLocation(__instance.attacker, __instance.attackPosition, hitInfo.locationRolls[i], __instance.calledShotLocation, __instance.attacker.CalledShotBonusMultiplier);
                                hitInfo.hitPositions[i] = __instance.chosenTarget.GetImpactPosition(__instance.attacker, __instance.attackPosition, weapon, ref hitInfo.hitLocations[i], ref hitInfo.attackDirections[i], ref hitInfo.secondaryTargetIds[i], ref hitInfo.secondaryHitLocations[i]);
                            }
                        }
                    }
                    else
                    {
                        // Make absolutely sure NO (potential) missile would hit ANYTHING (They won't be fired anyway, but they NEED a valid hitInfo)
                        for (int i = 0; i < hitInfo.numberOfShots; i++)
                        {
                            if (hitInfo.hitLocations[i] != 0)
                            {
                                Logger.Debug($"[AttackDirector.AttackSequence_GetIndividualHits_PREFIX] WARNING: Missile[{i}] had a hit location != 0 even though the Streak should miss. Setting (imaginary) missVector manually!");
                                hitInfo.hitLocations[i] = 0;

                                Vector3 missVector = __instance.attackPosition + __instance.attacker.HighestLOSPosition;
                                Vector3 shotVector = __instance.chosenTarget.GameRep.GetMissPosition(missVector, weapon, __instance.Director.Combat.NetworkRandom);
                                Vector3 normalized = (shotVector - missVector).normalized;
                                shotVector = missVector + normalized * 500f;

                                hitInfo.hitPositions[i] = shotVector;
                            }
                        }
                    }
                    Utilities.LogHitLocations(hitInfo);

                    // SAFEGUARD: Clean hitInfo from potentially set secondary targets, there AREN'T secondary targets for Streaks
                    hitInfo.secondaryTargetIds = new string[hitInfo.numberOfShots];

                    // Hit locations are set, skipping original method...
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return true;
            }
        }
    }



    // Sanitize roll correction
    [HarmonyPatch(typeof(AttackDirector.AttackSequence), "GetCorrectedRoll")]
    public static class AttackDirector_AttackSequence_GetCorrectedRoll_Patch
    {
        public static void Postfix(AttackDirector.AttackSequence __instance, ref float __result, float roll)
        {
            try
            {
                __result = Mathf.Clamp(__result, 0.00000001f, 1f);
                Logger.Info($"[AttackDirector.AttackSequence_GetCorrectedRoll_POSTFIX] Forced result to be between 0.00000001f and 1f ({__result})");
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }



    // NOTE that this is triggered by OnAttackSequenceFire to iterate through the weapons
    [HarmonyPatch(typeof(AttackDirector.AttackSequence), "OnAttackSequenceWeaponPreFireComplete")]
    public static class AttackDirector_AttackSequence_OnAttackSequenceWeaponPreFireComplete_Patch
    {
        public static void Prefix(AttackDirector.AttackSequence __instance, MessageCenterMessage message, List<List<Weapon>> ___sortedWeapons, WeaponHitInfo?[][] ___weaponHitInfo)
        {
            try
            {
                AttackSequenceWeaponPreFireCompleteMessage attackSequenceWeaponPreFireCompleteMessage = (AttackSequenceWeaponPreFireCompleteMessage)message;
                if (attackSequenceWeaponPreFireCompleteMessage.sequenceId != __instance.id)
                {
                    return;
                }
                int groupIdx = attackSequenceWeaponPreFireCompleteMessage.groupIdx;
                int weaponIdx = attackSequenceWeaponPreFireCompleteMessage.weaponIdx;
                Weapon weapon = ___sortedWeapons[groupIdx][weaponIdx];

                Logger.Debug($"[AttackDirector.AttackSequence_OnAttackSequenceWeaponPreFireComplete_PREFIX] ({weapon.parent.DisplayName}) HANDLED AttackSequence: {__instance.id}, WeaponGroup: {groupIdx}, Weapon: {weapon.Name}({weaponIdx})");
                Logger.Debug($"---");
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }



    [HarmonyPatch(typeof(AttackDirector.AttackSequence), "OnAttackSequenceFire")]
    public static class AttackDirector_AttackSequence_OnAttackSequenceFire_Patch
    {
        public static bool Prefix(AttackDirector.AttackSequence __instance, MessageCenterMessage message, List<List<Weapon>> ___sortedWeapons, ref int[][] ___numberOfShots, ref WeaponHitInfo?[][] ___weaponHitInfo)
        {
            try
            {
                AttackSequenceFireMessage attackSequenceFireMessage = (AttackSequenceFireMessage)message;
                if (attackSequenceFireMessage.sequenceId != __instance.id)
                {
                    return false;
                }
                int groupIdx = attackSequenceFireMessage.groupIdx;
                int weaponIdx = attackSequenceFireMessage.weaponIdx;
                Weapon weapon = ___sortedWeapons[groupIdx][weaponIdx];

                Logger.Debug($"---");
                Logger.Debug($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] ({weapon.parent.DisplayName}) STARTED AttackSequence: {__instance.id}, WeaponGroup: {groupIdx}, Weapon: {weapon.Name}({weaponIdx})");

                //if(weapon.weaponDef.ComponentTags.Contains("component_type_srmstreak"))
                if (weapon.Type == WeaponType.SRM && weapon.AmmoCategoryValue.Name == "SRMStreak")
                {
                    WeaponHitInfo weaponHitInfo = ___weaponHitInfo[groupIdx][weaponIdx].Value;
                    bool streakWillHit = weaponHitInfo.DidShotHitChosenTarget(0); // If first missile hits/misses, all will hit/miss
                    Logger.Info($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] ({weapon.Name}) streakWillHit: {streakWillHit}");

                    // Fire targeting laser
                    Vector3 floatieVector = new Vector3();
                    Utilities.CreateAndFireStreakTargetingLaser(__instance, weapon, out floatieVector, streakWillHit);

                    if (streakWillHit)
                    {
                        // Only floaties, everything else is prepared at this point

                        // Big Floatie
                        //__instance.Director.Combat.MessageCenter.PublishMessage(new FloatieMessage(__instance.chosenTarget.GUID, __instance.chosenTarget.GUID, "STREAK LOCKED-ON", FloatieMessage.MessageNature.CriticalHit));

                        // Small Floatie
                        FloatieMessage hitFloatie = new FloatieMessage(__instance.attacker.GUID, __instance.chosenTarget.GUID, "STREAK LOCKED-ON", __instance.Director.Combat.Constants.CombatUIConstants.floatieSizeMedium, FloatieMessage.MessageNature.Suppression, floatieVector.x, floatieVector.y, floatieVector.z);
                        __instance.Director.Combat.MessageCenter.PublishMessage(hitFloatie);

                        return true;
                    }
                    else
                    {
                        // Cancel firing, see code of original method...

                        // Mark Streak SRMs as having fired nevertheless because a failed lock on should be handled like "fired"
                        new Traverse(weapon).Property("HasFired").SetValue(true);
                        weapon.CompleteFiring();
                        Logger.Info($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] ({weapon.Name}) HasFired: {weapon.HasFired}, RoundsSinceLastFire: {weapon.roundsSinceLastFire}");

                        // If weapon already prefired we would need to reincrement ammo (Note that Weapon.OffsetAmmo() is a custom extension method)
                        if (weapon.HasPreFired)
                        {
                            weapon.OffsetAmmo();
                        }

                        // Send out all necessary messages to keep the current AttackSequence in sync
                        AttackSequenceWeaponPreFireCompleteMessage weaponPreFireCompleteMessage = new AttackSequenceWeaponPreFireCompleteMessage(__instance.stackItemUID, __instance.id, groupIdx, weaponIdx);
                        __instance.Director.Combat.MessageCenter.PublishMessage(weaponPreFireCompleteMessage);

                        int numberOfShots = ___numberOfShots[groupIdx][weaponIdx];
                        for (int j = 0; j < numberOfShots; j++)
                        {
                            float hitDamage = weapon.DamagePerShotAdjusted(weapon.parent.occupiedDesignMask);
                            float structureDamage = weapon.StructureDamagePerShotAdjusted(weapon.parent.occupiedDesignMask);
                            AttackSequenceImpactMessage impactMessage = new AttackSequenceImpactMessage(weaponHitInfo, j, hitDamage, structureDamage);
                            __instance.Director.Combat.MessageCenter.PublishMessage(impactMessage);
                        }

                        AttackSequenceResolveDamageMessage resolveDamageMessage = new AttackSequenceResolveDamageMessage(weaponHitInfo);
                        __instance.Director.Combat.MessageCenter.PublishMessage(resolveDamageMessage);

                        AttackSequenceWeaponCompleteMessage weaponCompleteMessage = new AttackSequenceWeaponCompleteMessage(__instance.stackItemUID, __instance.id, groupIdx, weaponIdx);
                        __instance.Director.Combat.MessageCenter.PublishMessage(weaponCompleteMessage);

                        // Big Floaties
                        //__instance.Director.Combat.MessageCenter.PublishMessage(new FloatieMessage(weapon.parent.GUID, weapon.parent.GUID, "STREAK LOCK-ON FAILED", FloatieMessage.MessageNature.Debuff));
                        //__instance.Director.Combat.MessageCenter.PublishMessage(new FloatieMessage(__instance.chosenTarget.GUID, __instance.chosenTarget.GUID, "STREAK LOCK-ON AVOIDED", FloatieMessage.MessageNature.Buff));

                        // Small Floatie
                        FloatieMessage missFloatie = new FloatieMessage(__instance.attacker.GUID, __instance.chosenTarget.GUID, "STREAK LOCK-ON FAILED", __instance.Director.Combat.Constants.CombatUIConstants.floatieSizeMedium, FloatieMessage.MessageNature.Dodge, floatieVector.x, floatieVector.y, floatieVector.z);
                        __instance.Director.Combat.MessageCenter.PublishMessage(missFloatie);



                        // Skip original method!
                        return false;
                    }
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
