using System;
using BattleTech;

namespace StreakMissileLaunchers
{
    class Utilities
    {
        public static void LogHitLocations(WeaponHitInfo hitInfo)
        {
            try
            {
                string output = "[Utilities_LogHitLocations] ---\n";
                output += $"[Utilities_LogHitLocations] Clustered hits: {hitInfo.hitLocations.Length}\n";
                for (int i = 0; i < hitInfo.hitLocations.Length; i++)
                {
                    int location = hitInfo.hitLocations[i];
                    var chassisLocationFromArmorLocation =
                        MechStructureRules.GetChassisLocationFromArmorLocation((ArmorLocation)location);

                    if (location == 0 || location == 65536)
                    {
                        output += $"[Utilities_LogHitLocations] HitLocation {i}: NONE/INVALID\n";
                    }
                    else
                    {
                        output += $"[Utilities_LogHitLocations] HitLocation {i}: {chassisLocationFromArmorLocation} ({location})\n";
                    }
                }
                Logger.Info(output);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
