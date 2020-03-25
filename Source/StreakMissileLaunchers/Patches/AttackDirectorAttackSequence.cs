using System;
using System.Collections.Generic;
using System.Reflection;
using BattleTech;
using Harmony;
using StreakMissileLaunchers.Extensions;

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
                //Logger.Info($"[AttackDirector.AttackSequence_GetIndividualHits_PREFIX] Streaks.streaksScopeEnabled: {Streaks.streaksScopeEnabled}");
                if (weapon.Type == WeaponType.SRM && weapon.AmmoCategoryValue.Name == "SRMStreak")
                {
                    Logger.Info($"[AttackDirector.AttackSequence_GetIndividualHits_PREFIX] ---");
                    Logger.Info($"[AttackDirector.AttackSequence_GetIndividualHits_PREFIX] ({weapon.Name}) toHitChance: {toHitChance}");
                    Logger.Info($"[AttackDirector.AttackSequence_GetIndividualHits_PREFIX] ({weapon.Name}) Fields.StreakWillHit: {Fields.StreakWillHit}");

                    if (Fields.StreakWillHit)
                    {
                        toHitChance = 1f;
                    }
                    Logger.Info($"[AttackDirector.AttackSequence_GetIndividualHits_PREFIX] ({weapon.Name}) toHitChance: {toHitChance}");

                    // Redirecting to hit determination method for clustered hits (LRMs only by default)
                    AttackSequenceGetClusteredHits.Invoke(__instance, new object[] { hitInfo, groupIdx, weaponIdx, weapon, toHitChance, prevDodgedDamage });

                    Logger.Info($"[AttackDirector.AttackSequence_GetIndividualHits_PREFIX] ({weapon.Name}) Determining clustered hits...");
                    Utilities.LogHitLocations(hitInfo);

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
        public static void Prefix(AttackDirector.AttackSequence __instance, MessageCenterMessage message, List<List<Weapon>> ___sortedWeapons, ref int[][] ___numberOfShots, ref WeaponHitInfo?[][] ___weaponHitInfo)
        {
            try
            {
                AttackSequenceFireMessage attackSequenceFireMessage = (AttackSequenceFireMessage)message;
                if (attackSequenceFireMessage.sequenceId != __instance.id)
                {
                    return;
                }
                int groupIdx = attackSequenceFireMessage.groupIdx;
                int weaponIdx = attackSequenceFireMessage.weaponIdx;
                Weapon weapon = ___sortedWeapons[groupIdx][weaponIdx];

                Logger.Debug($"---");
                Logger.Debug($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] ({weapon.parent.DisplayName}) STARTED AttackSequence: {__instance.id}, WeaponGroup: {groupIdx}, Weapon: {weapon.Name}({weaponIdx})");

                //if(weapon.weaponDef.ComponentTags.Contains("component_type_srmstreak"))
                if (weapon.Type == WeaponType.SRM && weapon.AmmoCategoryValue.Name == "SRMStreak")
                {
                    // Reset hit indicator
                    Fields.StreakWillHit = false;

                    float hitChance = __instance.Director.Combat.ToHit.GetToHitChance(__instance.attacker, weapon, __instance.chosenTarget, __instance.attackPosition, __instance.chosenTarget.CurrentPosition, __instance.numTargets, __instance.meleeAttackType, __instance.isMoraleAttack);
                    Logger.Info($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] ({weapon.Name}) hitChance: {hitChance}");

                    float hitRoll = UnityEngine.Random.Range(0f, 1f);
                    Logger.Info($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] ({weapon.Name}) hitRoll: {hitRoll}");

                    Fields.StreakWillHit = hitRoll <= hitChance;
                    Logger.Debug($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] ({weapon.Name}) Fields.StreaksWillHit: {Fields.StreakWillHit}");



                    //Utilities.FireStreakTargetingLaser(__instance, weapon.parent, Fields.StreakWillHit);
                    Utilities.CreateAndFireStreakTargetingLaser(__instance, weapon, Fields.StreakWillHit);



                    if (Fields.StreakWillHit)
                    {
                        // Force recalculation of HitOnfo by setting it to null here, triggering AttackDirector.AttackSequence.GenerateHitInfo() => AttackDirector.AttackSequence.GetIndividualHits()
                        // There the hit chance of all missiles is set to 1f -> All missiles hit!

                        ___weaponHitInfo[groupIdx][weaponIdx] = null;
                        return;
                    }
                    else
                    {
                        Logger.Info($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] ({weapon.Name}) Lock on failed, set ___numberOfShots to -1");

                        // This should cancel firing, see code of original method...
                        ___numberOfShots[groupIdx][weaponIdx] = -1;

                        // Mark Streak SRMs as having fired nevertheless because a failed lock on should be handled like "fired"
                        new Traverse(weapon).Property("HasFired").SetValue(true);
                        weapon.CompleteFiring();
                        Logger.Info($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] ({weapon.Name}) HasFired: {weapon.HasFired}, RoundsSinceLastFire: {weapon.roundsSinceLastFire}");

                        // Floaties
                        __instance.Director.Combat.MessageCenter.PublishMessage(new FloatieMessage(weapon.parent.GUID, weapon.parent.GUID, "STREAK LOCK-ON FAILED", FloatieMessage.MessageNature.Debuff));
                        __instance.Director.Combat.MessageCenter.PublishMessage(new FloatieMessage(__instance.chosenTarget.GUID, __instance.chosenTarget.GUID, "STREAK LOCK-ON AVOIDED", FloatieMessage.MessageNature.Buff));

                        // If weapon already prefired we would need to reincrement ammo
                        // Note that Weapon.OffsetAmmo() is a custom extension method
                        if (weapon.HasPreFired)
                        {
                            weapon.OffsetAmmo();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        /*
        public static void Postfix(AttackDirector.AttackSequence __instance, MessageCenterMessage message, List<List<Weapon>> ___sortedWeapons, ref int[][] ___numberOfShots, ref WeaponHitInfo?[][] ___weaponHitInfo)
        {
            if (Fields.StreakScopeEnabled)
            {
                Fields.StreakScopeEnabled = false;
                Logger.Info($"[AttackDirector.AttackSequence_OnAttackSequenceFire_POSTFIX] Fields.StreaksScopeEnabled: {Fields.StreakScopeEnabled}");
                Logger.Info($"---");
            }
        }
        */
    }
}
