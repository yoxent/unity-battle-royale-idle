using System;
using System.Collections.Generic;

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
            if (livingCounts == null)
            {
                return false;
            }

            var teamsAlive = 0;
            for (var team = 0; team < livingCounts.Count; team++)
            {
                if (livingCounts[team] > 0)
                {
                    teamsAlive++;
                }
            }

            return teamsAlive < 2;
        }

        public static int SoleSurvivor(IReadOnlyList<int> livingCounts)
        {
            if (livingCounts == null)
            {
                return -1;
            }

            var survivor = -1;
            var teamsAlive = 0;
            for (var team = 0; team < livingCounts.Count; team++)
            {
                if (livingCounts[team] <= 0)
                {
                    continue;
                }

                teamsAlive++;
                survivor = team;
            }

            return teamsAlive == 1 ? survivor : -1;
        }
    }
}
