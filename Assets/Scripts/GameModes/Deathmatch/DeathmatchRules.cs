using System.Collections.Generic;
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
            return Mathf.Max(MinimumTeamCount, teamCount);
        }

        public static bool SharesTeam(int teamA, int teamB)
        {
            return teamA >= 0 && teamA == teamB;
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

        public static bool EnsureTeamSpawns(MapModeConfiguration configuration)
        {
            if (configuration == null || !configuration.IsDeathmatch)
            {
                return false;
            }

            configuration.deathmatchTeams ??= new List<MapModeTeamSpawn>();
            var requiredCount = RequiredTeamCount(configuration.teamCount);
            var changed = configuration.teamCount != requiredCount;
            configuration.teamCount = requiredCount;
            var center = configuration.map != null
                ? configuration.map.WorldCenter
                : Vector3.zero;
            while (configuration.deathmatchTeams.Count < requiredCount)
            {
                var index = configuration.deathmatchTeams.Count;
                var angle = (index + 0.5f) / requiredCount * Mathf.PI * 2f;
                configuration.deathmatchTeams.Add(
                    new MapModeTeamSpawn
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

            while (configuration.deathmatchTeams.Count > requiredCount)
            {
                configuration.deathmatchTeams.RemoveAt(
                    configuration.deathmatchTeams.Count - 1);
                changed = true;
            }

            return changed;
        }
    }
}
