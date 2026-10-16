using System.Collections.Generic;

namespace BattleRoyale.GameModes
{
    public static class LivingSideRules
    {
        public static bool IsDecided(IReadOnlyList<int> livingCounts)
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
