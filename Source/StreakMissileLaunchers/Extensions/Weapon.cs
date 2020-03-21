using BattleTech;

namespace StreakMissileLaunchers.Extensions
{
    public static class WeaponExtensions
    {
        public static void OffsetAmmo(this Weapon weapon)
        {
            Logger.Debug($"[WeaponExtensions_OffsetAmmo] ({weapon.Name}) Regain ammunition if Mech has failed to lock-on Streaks");

            if (weapon.AmmoCategoryValue.Is_NotSet || weapon.parent.UnitType != UnitType.Mech)
            {
                return;
            }

            int stackItemUID = -1;
            int shotsToAdd = weapon.ShotsWhenFired;

            for (int i = weapon.ammoBoxes.Count - 1; i >= 0; i--)
            {
                AmmunitionBox ammunitionBox = weapon.ammoBoxes[i];
                Logger.Info($"[WeaponExtensions_OffsetAmmo] ({weapon.Name}) Current AmmoBox[{i}]'s Ammo: {ammunitionBox.CurrentAmmo}");

                int spaceLeft = ammunitionBox.AmmoCapacity - ammunitionBox.CurrentAmmo;
                if (shotsToAdd >= spaceLeft)
                {
                    ammunitionBox.StatCollection.ModifyStat<int>(weapon.uid, stackItemUID, "CurrentAmmo", StatCollection.StatOperation.Set, ammunitionBox.AmmoCapacity, -1, true);
                    shotsToAdd -= spaceLeft;
                }
                else
                {
                    ammunitionBox.StatCollection.ModifyStat<int>(weapon.uid, stackItemUID, "CurrentAmmo", StatCollection.StatOperation.Int_Add, shotsToAdd, -1, true);
                    shotsToAdd = 0;
                }
                Logger.Info($"[WeaponExtensions_OffsetAmmo] ({weapon.Name}) Updated AmmoBox[{i}]'s Ammo: {ammunitionBox.CurrentAmmo}");

                if (shotsToAdd <= 0)
                {
                    break;
                }
            }
        }
    }
}
