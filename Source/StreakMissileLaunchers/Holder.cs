using UnityEngine;

namespace StreakMissileLaunchers
{
    public static class Fields
    {
        internal static string StreakTargetingLaserId = "Weapon_Streak_Targeting_Laser";

        internal static bool AdjustTargetingLaserVFX = true;

        // Note that this color most likely "mixes" with the color of the projectile prefab
        // So in the case of the small pulse laser effect (almost pure red) a pure blue will end up as magenta
        internal static Color TargetingLaserColor = new Color(0.2f, 0.0f, 0.8f, 1.0f);
    }
}
