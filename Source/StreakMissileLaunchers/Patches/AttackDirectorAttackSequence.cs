using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using BattleTech;
using Harmony;
using StreakMissileLaunchers.Extensions;
using UnityEngine;

namespace StreakMissileLaunchers.Patches
{
    internal static class AttackDirectorAttackSequence
    {
        internal static bool StreakScopeEnabled = false;
        internal static bool StreakWillHit = false;


        // Cancel triggering the next weapon in an AttackDirector.AttackSequence if it's a special system (ie streak targeting laser)
        [HarmonyPatch(typeof(WeaponEffect), "PublishNextWeaponMessage")]
        public static class WeaponEffect_PublishNextWeaponMessage_Patch
        {
            public static bool Prefix(WeaponEffect __instance)
            {
                try
                {
                    Logger.Debug($"[WeaponEffect_PublishNextWeaponMessage_PREFIX] WeaponEffect: {__instance.name}, Weapon: {__instance.weapon.weaponDef.Description.Id}");
                    if (__instance.weapon.weaponDef.Description.Id == "Weapon_TAG_Standard_0-STOCK")
                    {
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


        [HarmonyPatch(typeof(Mech), "InitGameRep")]
        public static class Mech_InitStats_Patch
        {
            public static void Prefix(Mech __instance)
            {
                try
                {
                    Logger.Debug($"[Mech_InitGameRep_PREFIX] ({__instance.DisplayName}) Add streak targetting lasers if needed...");

                    bool hasStreaksMounted = false;
                    foreach(Weapon w in __instance.Weapons)
                    {
                        if (w.Type == WeaponType.SRM)
                        {
                            hasStreaksMounted = true;
                        }
                    }
                    Logger.Info($"[Mech_InitGameRep_PREFIX] ({__instance.DisplayName}) hasStreaksMounted: {hasStreaksMounted}");

                    if (hasStreaksMounted)
                    {
                        AbstractActor actor = __instance as AbstractActor;
                        WeaponDef weaponDef = actor.Combat.DataManager.WeaponDefs.Get("Weapon_TAG_Standard_0-STOCK");
                        MechComponentRef mechComponentRef = new MechComponentRef(weaponDef.Description.Id, weaponDef.Description.Id + "_StreakTargettingLaser", ComponentType.Weapon, ChassisLocations.CenterTorso, -1, ComponentDamageLevel.Functional, false);
                        mechComponentRef.SetComponentDef(weaponDef);
                        mechComponentRef.DataManager = actor.Combat.DataManager;
                        mechComponentRef.RefreshComponentDef();
                        Weapon fakeTargetingLaser = new Weapon(__instance, actor.Combat, mechComponentRef, actor.Combat.GUID + "_StreakTargettingLaser");
                        fakeTargetingLaser.Init();
                        fakeTargetingLaser.InitStats();

                        // Add to supportComponents which will get their Representation ready in original method...
                        __instance.supportComponents.Add(fakeTargetingLaser);

                        Logger.Info($"[Mech_InitGameRep_PREFIX] ({__instance.DisplayName}) fakeTargetingLaser.uid: {fakeTargetingLaser.uid}");
                        Logger.Info($"[Mech_InitGameRep_PREFIX] ({__instance.DisplayName}) fakeTargetingLaser.weaponDef.WeaponEffectID: {fakeTargetingLaser.weaponDef.WeaponEffectID}");


                    }

                    

                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }



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
                    if (StreakScopeEnabled && weapon.Type == WeaponType.SRM)
                    {
                        Logger.Info($"[AttackDirector.AttackSequence_GetIndividualHits_PREFIX] ---");
                        Logger.Info($"[AttackDirector.AttackSequence_GetIndividualHits_PREFIX] ({weapon.Name}) toHitChance: {toHitChance}");
                        Logger.Info($"[AttackDirector.AttackSequence_GetIndividualHits_PREFIX] ({weapon.Name}) StreaksScopeEnabled: {StreakScopeEnabled}");

                        if (StreakWillHit)
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

                    Logger.Info($"[AttackDirector.AttackSequence_OnAttackSequenceWeaponPreFireComplete_PREFIX] ({weapon.parent.DisplayName}) HANDLED AttackSequence: {__instance.id}, WeaponGroup: {groupIdx}, Weapon: {weapon.Name}({weaponIdx})");
                    Logger.Info($"---");
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

                    Logger.Info($"---");
                    Logger.Info($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] ({weapon.parent.DisplayName}) AttackSequence: {__instance.id}, WeaponGroup: {groupIdx}, Weapon: {weapon.Name}({weaponIdx})");

                    //if(weapon.weaponDef.ComponentTags.Contains("component_type_streak"))

                    // Test
                    if (weapon.Type == WeaponType.SRM)
                    {
                        // Enable scope for depending methods and reset hit indicator
                        StreakScopeEnabled = true;
                        StreakWillHit = false;
                        Logger.Info($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] StreaksScopeEnabled: {StreakScopeEnabled}");

                        float hitChance = __instance.Director.Combat.ToHit.GetToHitChance(__instance.attacker, weapon, __instance.chosenTarget, __instance.attackPosition, __instance.chosenTarget.CurrentPosition, __instance.numTargets, __instance.meleeAttackType, __instance.isMoraleAttack);
                        Logger.Info($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] ({weapon.Name}) hitChance: {hitChance}");

                        float hitRoll = UnityEngine.Random.Range(0f, 1f);
                        Logger.Info($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] ({weapon.Name}) hitRoll: {hitRoll}");

                        StreakWillHit = hitRoll <= hitChance;
                        Logger.Info($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] ({weapon.Name}) StreaksWillHit: {StreakWillHit}");



                        //
                        // @ToDo: Try to play some weapon effect to imitate targeting lasers
                        //

                        //WeaponHitInfo? whiClone = ___weaponHitInfo[groupIdx][weaponIdx];
                        //Utilities.CreateAndFireStreakTargetingLaser(weapon, whiClone);

                        foreach (Weapon w in weapon.parent.supportComponents)
                        {
                            if (w.weaponDef.Description.Id == "Weapon_TAG_Standard_0-STOCK")
                            {
                                Logger.Info($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] Found fakeTargetingLaser");
                                //WeaponHitInfo? weaponHitInfo = ___weaponHitInfo[groupIdx][weaponIdx];
                                //WeaponHitInfo whi;
                                //if (weaponHitInfo != null)
                                //{
                                //    whi = weaponHitInfo.Value;
                                //    if (w.weaponRep != null && w.weaponRep.HasWeaponEffect)
                                //    {
                                //        w.weaponRep.PlayWeaponEffect(whi);
                                //    }
                                //}

                                Vector3 fakeTargetingHitPosition = __instance.chosenTarget.GameRep.GetHitPosition(8) + UnityEngine.Random.insideUnitSphere * 5f;
                                WeaponHitInfo fakeTargetingHitInfo = new WeaponHitInfo(__instance.stackItemUID, __instance.id, 0, 0, __instance.attacker.GUID, __instance.chosenTarget.GUID, 1, new float[1], new float[1], new float[1], new bool[1], new int[] { 8 }, new int[1], new AttackImpactQuality[1], new AttackDirection[1], new Vector3[] { fakeTargetingHitPosition }, new string[1], new int[1]);
                                Logger.Info($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] fakeTargetingHitInfo: {fakeTargetingHitInfo}");

                                if (w.weaponRep != null && w.weaponRep.HasWeaponEffect)
                                {
                                    w.weaponRep.PlayWeaponEffect(fakeTargetingHitInfo);
                                }
                            }
                        }
                        



                        if (StreakWillHit)
                        {
                            // Force recalculation of HitOnfo by setting it to null here, triggering AttackDirector.AttackSequence.GenerateHitInfo() => AttackDirector.AttackSequence.GetIndividualHits()
                            // There the hit chance of all missiles is set to 1f -> All missiles hit!


                            //___weaponHitInfo[groupIdx][weaponIdx] = null;
                            //return true;

                            //Rebuild orignal method
                            int num = ___numberOfShots[groupIdx][weaponIdx];

                            weapon.FireWeapon();

                            WeaponHitInfo? weaponHitInfo = ___weaponHitInfo[groupIdx][weaponIdx];
                            WeaponHitInfo weaponHitInfo2;
                            if (weaponHitInfo != null)
                            {
                                weaponHitInfo2 = weaponHitInfo.Value;
                                __instance.AddAllAffectedTargets(weaponHitInfo2);
                            }
                            else
                            {
                                AttackDirector.AttackSequence.logger.LogError("[OnAttackSequenceFire] had to generate hit info because pre-calculated hit info was not available!");
                                //weaponHitInfo2 = __instance.GenerateHitInfo(weapon, groupIdx, weaponIdx, num, __instance.indirectFire, 0f);

                                //Traverse GenerateHitInfo = Traverse.Create(__instance).Method("GenerateHitInfo");
                                //var weaponHitInfo2 = GenerateHitInfo.GetValue(new object[] { weapon, groupIdx, weaponIdx, num, __instance.indirectFire, 0f });

                                FastInvokeHandler AttackSequenceGenerateHitInfo;
                                MethodInfo mi = AccessTools.Method(typeof(AttackDirector.AttackSequence), "GenerateHitInfo");
                                AttackSequenceGenerateHitInfo = MethodInvoker.GetHandler(mi);
                                weaponHitInfo2 = (WeaponHitInfo)AttackSequenceGenerateHitInfo.Invoke(__instance, new object[] { weapon, groupIdx, weaponIdx, num, __instance.indirectFire, 0f });

                                __instance.AddAllAffectedTargets(weaponHitInfo2);
                            }

                            weapon.CompleteFiring();

                            foreach (EffectData effectData in weapon.weaponDef.statusEffects)
                            {
                                if (effectData.targetingData.effectTriggerType == EffectTriggerType.OnActivation)
                                {
                                    string effectID = string.Format("{0}Effect_{1}_{2}", effectData.targetingData.effectTriggerType.ToString(), weapon.parent.GUID, weaponHitInfo2.attackSequenceId);
                                    foreach (ICombatant combatant in __instance.Director.Combat.EffectManager.GetTargetCombatantForEffect(effectData, weapon.parent, __instance.chosenTarget))
                                    {
                                        __instance.Director.Combat.EffectManager.CreateEffect(effectData, effectID, __instance.stackItemUID, weapon.parent, combatant, weaponHitInfo2, weaponIdx, false);
                                        if (!effectData.targetingData.hideApplicationFloatie)
                                        {
                                            __instance.Director.Combat.MessageCenter.PublishMessage(new FloatieMessage(weapon.parent.GUID, weapon.parent.GUID, effectData.Description.Name, FloatieMessage.MessageNature.Buff));
                                        }
                                        if (!effectData.targetingData.hideApplicationFloatie)
                                        {
                                            __instance.Director.Combat.MessageCenter.PublishMessage(new FloatieMessage(weapon.parent.GUID, combatant.GUID, effectData.Description.Name, FloatieMessage.MessageNature.Buff));
                                        }
                                    }
                                }
                            }

                            bool flag = weapon.weaponRep != null && weapon.weaponRep.HasWeaponEffect;
                            if (DebugBridge.TestToolsEnabled)
                            {
                                flag = (flag && !DebugBridge.DisableWeaponEffectDrivenAttacks);
                            }
                            if (flag)
                            {
                                weapon.weaponRep.PlayWeaponEffect(weaponHitInfo2);
                            }

                            return false;
                        }
                        else
                        {
                            // This should cancel firing, see code of original method...
                            //___numberOfShots[groupIdx][weaponIdx] = -1;

                            Logger.Info($"[AttackDirector.AttackSequence_OnAttackSequenceFire_PREFIX] ({weapon.Name}) Lock on failed, setting ___numberOfShots to -1");

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

                            // Cancel firing, send messages to signal completion of handling this weapon
                            // BEWARE: If these messages are sent too early the order of handled weapons will be disturbed with chaotic consequences for the sequence!!!
                            AttackSequenceWeaponPreFireCompleteMessage messageWeaponPreFireComplete = new AttackSequenceWeaponPreFireCompleteMessage(__instance.stackItemUID, __instance.id, groupIdx, weaponIdx);
                            __instance.Director.Combat.MessageCenter.PublishMessage(messageWeaponPreFireComplete);
                            AttackSequenceWeaponCompleteMessage messageWeaponComplete = new AttackSequenceWeaponCompleteMessage(__instance.stackItemUID, __instance.id, groupIdx, weaponIdx);
                            __instance.Director.Combat.MessageCenter.PublishMessage(messageWeaponComplete);

                            return false;
                        }
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

            public static void Postfix(AttackDirector.AttackSequence __instance, MessageCenterMessage message, List<List<Weapon>> ___sortedWeapons, ref int[][] ___numberOfShots, ref WeaponHitInfo?[][] ___weaponHitInfo)
            {
                if (StreakScopeEnabled)
                {
                    StreakScopeEnabled = false;
                    Logger.Info($"[AttackDirector.AttackSequence_OnAttackSequenceFire_POSTFIX] StreaksScopeEnabled: {StreakScopeEnabled}");
                    Logger.Info($"---");
                }
            }
        }
    }
}
