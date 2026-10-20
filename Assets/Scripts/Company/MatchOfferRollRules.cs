using System;
using BattleRoyale.Maps;

namespace BattleRoyale.Company
{
    public static class MatchOfferRollRules
    {
        public static bool CanList(MapGameMode mode)
        {
            return mode == MapGameMode.Deathmatch || mode == MapGameMode.BattleRoyale;
        }

        public static bool TryRoll(int availableCount, int[] slots, Random random)
        {
            if (slots == null || slots.Length == 0 || availableCount <= 0 || random == null)
            {
                if (slots != null)
                {
                    for (var i = 0; i < slots.Length; i++)
                    {
                        slots[i] = -1;
                    }
                }

                return false;
            }

            for (var i = 0; i < slots.Length; i++)
            {
                slots[i] = random.Next(availableCount);
            }

            return true;
        }
    }
}
