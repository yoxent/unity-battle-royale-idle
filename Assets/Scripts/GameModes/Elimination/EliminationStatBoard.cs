using System.Collections.Generic;
using BattleRoyale.GameModes;
using BattleRoyale.GameModes.Deathmatch;

namespace BattleRoyale.GameModes.Elimination
{
    public static class EliminationStatTracker
    {
        public static void Write(
            MatchStatBoard board,
            DeathmatchCombatStats stats,
            bool started,
            IReadOnlyList<int> roundWins,
            float clockSeconds,
            bool intermission,
            IReadOnlyList<bool> alive)
        {
            if (board == null)
            {
                return;
            }

            board.Clear();
            if (!started || stats == null || roundWins == null || roundWins.Count != 2)
            {
                if (started && stats != null && roundWins != null && roundWins.Count != 2)
                {
                    board.RejectTeams();
                }

                return;
            }

            stats.NoteMatchClock(clockSeconds);
            NoteLiving(stats, alive);
            var clock = intermission
                ? "INT " + DeathmatchCombatStats.FormatClock(clockSeconds)
                : DeathmatchCombatStats.FormatClock(clockSeconds);
            board.ShowTeams(
                stats.TeamName(0),
                roundWins[0],
                stats.TeamName(1),
                roundWins[1],
                clock);
            var latest = stats.LatestEvent;
            board.SetEvent(
                latest.HasEvent,
                latest.HasActor,
                latest.ActorName,
                latest.ActorTeam,
                latest.SubjectName,
                latest.SubjectTeam);
            WriteRows(board, stats);
        }

        static void NoteLiving(DeathmatchCombatStats stats, IReadOnlyList<bool> alive)
        {
            for (var index = 0; index < stats.AgentCount; index++)
            {
                var living = alive != null && index < alive.Count && alive[index];
                stats.NoteAlive(index, living);
            }
        }

        static void WriteRows(MatchStatBoard board, DeathmatchCombatStats stats)
        {
            var lines = new List<DeathmatchAgentLine>();
            stats.CopyAgents(lines);
            for (var pass = 0; pass < 2; pass++)
            {
                for (var index = 0; index < lines.Count; index++)
                {
                    var line = lines[index];
                    if (line.TeamIndex != pass)
                    {
                        continue;
                    }

                    board.AddRow(new MatchStatRow(
                        line.DisplayName,
                        line.TeamIndex,
                        line.Alive,
                        true,
                        line.Kills,
                        line.Deaths,
                        line.Assists,
                        DeathmatchCombatStats.RoundHalfUp(line.DamageDealt),
                        DeathmatchCombatStats.AccuracyPercent(line.Hits, line.ShotsFired),
                        0));
                }
            }
        }
    }
}
