using System;
using System.Collections.Generic;
using BattleRoyale.GameModes;

namespace BattleRoyale.GameModes.BattleRoyale
{
    public static class BattleRoyaleRules
    {
        public static int TeamIndexForSlot(int slot, int teamSize)
        {
            var size = Math.Max(1, teamSize);
            var index = Math.Max(0, slot);
            return index / size;
        }

        public static bool IsMatchOver(IReadOnlyList<int> livingCounts)
        {
            return LivingSideRules.IsDecided(livingCounts);
        }

        public static int SoleSurvivor(IReadOnlyList<int> livingCounts)
        {
            return LivingSideRules.SoleSurvivor(livingCounts);
        }
    }
}
