using System.Collections.Generic;
using BattleRoyale.GameModes;
using BattleRoyale.Maps;
using UnityEngine;

namespace BattleRoyale.GameModes.Elimination
{
    public static class EliminationRules
    {
        public const int MinimumTeamCount = 2;
        public const int DefaultTeamSize = 5;
        public const int DefaultRoundWinLimit = 13;
        public const float DefaultRoundSeconds = 115f;
        public const float DefaultIntermissionSeconds = 7f;
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

        public static bool IsRoundOver(IReadOnlyList<int> livingCounts)
        {
            return LivingSideRules.IsDecided(livingCounts);
        }

        public static int SoleSurvivor(IReadOnlyList<int> livingCounts)
        {
            return LivingSideRules.SoleSurvivor(livingCounts);
        }

        public static int TimeExpiryWinner(IReadOnlyList<int> livingCounts)
        {
            if (livingCounts == null)
            {
                return -1;
            }

            var winner = -1;
            var best = 0;
            var tied = false;
            for (var team = 0; team < livingCounts.Count; team++)
            {
                var living = livingCounts[team];
                if (living <= 0)
                {
                    continue;
                }

                if (winner < 0 || living > best)
                {
                    winner = team;
                    best = living;
                    tied = false;
                }
                else if (living == best)
                {
                    tied = true;
                }
            }

            return tied ? -1 : winner;
        }

        public static void AddRoundWin(IList<int> roundWins, int team)
        {
            if (roundWins == null || team < 0 || team >= roundWins.Count)
            {
                return;
            }

            roundWins[team]++;
        }

        public static int FindMatchWinner(IReadOnlyList<int> roundWins, int roundWinLimit)
        {
            if (roundWins == null || roundWinLimit <= 0)
            {
                return -1;
            }

            var winner = -1;
            for (var team = 0; team < roundWins.Count; team++)
            {
                if (roundWins[team] < roundWinLimit)
                {
                    continue;
                }

                if (winner < 0 || roundWins[team] > roundWins[winner])
                {
                    winner = team;
                }
            }

            return winner;
        }

        public static bool EnsureTeamSpawns(EliminationModeConfiguration configuration)
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
