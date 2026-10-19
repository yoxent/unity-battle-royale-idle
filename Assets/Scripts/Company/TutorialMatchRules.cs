using BattleRoyale.Maps;

namespace BattleRoyale.Company
{
    public static class TutorialMatchRules
    {
        public const int SoloSides = 4;
        public const int SoloTeamSize = 1;

        public static bool TryOffer(
            MapModeConfiguration configuration,
            int spawnCount,
            int opponentAgents,
            out int teamCount,
            out int teamSize)
        {
            teamCount = 0;
            teamSize = 0;
            if (configuration == null ||
                configuration.mode != MapGameMode.BattleRoyale ||
                configuration.teamSize != SoloTeamSize ||
                configuration.teamCount < SoloSides ||
                spawnCount < SoloSides ||
                opponentAgents < (SoloSides - 1) * SoloTeamSize)
            {
                return false;
            }

            teamCount = SoloSides;
            teamSize = SoloTeamSize;
            return true;
        }
    }
}
