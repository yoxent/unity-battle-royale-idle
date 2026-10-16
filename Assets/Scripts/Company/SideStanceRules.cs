using BattleRoyale.GameModes;

namespace BattleRoyale.Company
{
    public static class SideStanceRules
    {
        public static TeamPlaystyle Resolve(
            bool hasSidePlaystyle,
            TeamPlaystyle sidePlaystyle,
            TeamPlaystyle matchPlaystyle)
        {
            return hasSidePlaystyle ? sidePlaystyle : matchPlaystyle;
        }

        public static bool ReceivesCoachDirective(
            bool hasSidePlaystyle,
            int teamIndex,
            int playerTeamIndex)
        {
            if (!hasSidePlaystyle)
            {
                return true;
            }

            return teamIndex == playerTeamIndex;
        }
    }
}
