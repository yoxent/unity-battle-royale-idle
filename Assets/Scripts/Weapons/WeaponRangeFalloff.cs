using UnityEngine;

namespace BattleRoyale.Weapons
{
    public static class WeaponRangeFalloff
    {
        public static float Scale(float damage, float hitDistance, float weaponRange)
        {
            if (damage <= 0f)
            {
                return 0f;
            }

            return damage * Multiplier(hitDistance, weaponRange);
        }

        public static float Multiplier(float hitDistance, float weaponRange)
        {
            var distance = Mathf.Max(0f, hitDistance);
            var range = Mathf.Max(0f, weaponRange);
            if (distance <= range)
            {
                return 1f;
            }

            if (range <= 0.0001f)
            {
                return 0f;
            }

            var excess = (distance - range) / range;
            return 1f / Mathf.Pow(1f + excess, 4f);
        }
    }
}
