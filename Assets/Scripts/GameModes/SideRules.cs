using UnityEngine;

namespace BattleRoyale.GameModes
{
    public static class SideRules
    {
        public const int MinimumTeamCount = 2;

        public static int RequiredTeamCount(int teamCount)
        {
            return Mathf.Max(MinimumTeamCount, teamCount);
        }

        public static bool SharesTeam(int teamA, int teamB)
        {
            return teamA >= 0 && teamA == teamB;
        }
    }
}
