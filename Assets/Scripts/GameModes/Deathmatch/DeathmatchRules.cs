using System.Collections.Generic;
using BattleRoyale.GameModes;
using BattleRoyale.Maps;
using UnityEngine;

namespace BattleRoyale.GameModes.Deathmatch
{
    public static class DeathmatchRules
    {
        public const int MinimumTeamCount = 2;
        public const int DefaultTeamSize = 4;
        public const int DefaultScoreLimit = 50;
        public const float DefaultRespawnSeconds = 3f;
        public const float DefaultSpawnRadius = 8f;
        public const float SpawnRingRadius = 36f;

        public static int RequiredTeamCount(int teamCount)
        {
            return SideRules.RequiredTeamCount(teamCount);
        }

        public static bool SharesTeam(int teamA, int teamB)
        {
            return SideRules.SharesTeam(teamA, teamB);
        }

        public static void AddKill(IList<int> teamScores, int killerTeam)
        {
            if (teamScores == null ||
                killerTeam < 0 ||
                killerTeam >= teamScores.Count)
            {
                return;
            }

            teamScores[killerTeam]++;
        }

        public static int FindWinner(IReadOnlyList<int> teamScores, int scoreLimit)
        {
            if (teamScores == null || scoreLimit <= 0)
            {
                return -1;
            }

            var winner = -1;
            for (var team = 0; team < teamScores.Count; team++)
            {
                if (teamScores[team] < scoreLimit)
                {
                    continue;
                }

                if (winner < 0 || teamScores[team] > teamScores[winner])
                {
                    winner = team;
                }
            }

            return winner;
        }

        public static bool EnsureTeamSpawns(DeathmatchModeConfiguration configuration)
        {
            if (configuration == null)
            {
                return false;
            }

            configuration.teams ??= new List<MapTeamSpawn>();
            var requiredCount = RequiredTeamCount(configuration.teamCount);
            var changed = configuration.teamCount != requiredCount;
            configuration.teamCount = requiredCount;
            var center = configuration.map != null
                ? configuration.map.WorldCenter
                : Vector3.zero;
            while (configuration.teams.Count < requiredCount)
            {
                var index = configuration.teams.Count;
                var angle = (index + 0.5f) / requiredCount * Mathf.PI * 2f;
                configuration.teams.Add(
                    new MapTeamSpawn
                    {
                        teamId = $"Team {index + 1}",
                        spawnPosition = center + new Vector3(
                            Mathf.Cos(angle) * SpawnRingRadius,
                            0f,
                            Mathf.Sin(angle) * SpawnRingRadius),
                        radius = DefaultSpawnRadius
                    });
                changed = true;
            }

            while (configuration.teams.Count > requiredCount)
            {
                configuration.teams.RemoveAt(
                    configuration.teams.Count - 1);
                changed = true;
            }

            return changed;
        }
    }
}
