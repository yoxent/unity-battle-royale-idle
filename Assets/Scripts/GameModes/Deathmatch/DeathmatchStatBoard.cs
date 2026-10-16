using System.Collections.Generic;
using BattleRoyale.GameModes;

namespace BattleRoyale.GameModes.Deathmatch
{
    public static class DeathmatchStatTracker
    {
        public static void Write(
            MatchStatBoard board,
            DeathmatchCombatStats stats,
            bool started,
            IReadOnlyList<int> teamScores,
            float matchSeconds,
            IReadOnlyList<bool> alive,
            IReadOnlyList<int> respawnSeconds)
        {
            if (board == null)
            {
                return;
            }

            board.Clear();
            if (!started || stats == null || teamScores == null || teamScores.Count != 2)
            {
                if (started && stats != null && teamScores != null && teamScores.Count != 2)
                {
                    board.RejectTeams();
                }

                return;
            }

            stats.NoteMatchClock(matchSeconds);
            stats.NoteTeamScores(teamScores);
            NoteLiving(stats, alive);
            board.ShowTeams(
                stats.TeamName(0),
                stats.TeamScoreAt(0),
                stats.TeamName(1),
                stats.TeamScoreAt(1),
                DeathmatchCombatStats.FormatClock(stats.MatchClock));
            var latest = stats.LatestEvent;
            board.SetEvent(
                latest.HasEvent,
                latest.HasActor,
                latest.ActorName,
                latest.ActorTeam,
                latest.SubjectName,
                latest.SubjectTeam);
            WriteRows(board, stats, respawnSeconds);
        }

        static void NoteLiving(DeathmatchCombatStats stats, IReadOnlyList<bool> alive)
        {
            for (var index = 0; index < stats.AgentCount; index++)
            {
                var living = alive != null && index < alive.Count && alive[index];
                stats.NoteAlive(index, living);
            }
        }

        static void WriteRows(
            MatchStatBoard board,
            DeathmatchCombatStats stats,
            IReadOnlyList<int> respawnSeconds)
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

                    var respawn = 0;
                    if (!line.Alive &&
                        respawnSeconds != null &&
                        line.RosterIndex >= 0 &&
                        line.RosterIndex < respawnSeconds.Count)
                    {
                        respawn = respawnSeconds[line.RosterIndex];
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
                        respawn));
                }
            }
        }
    }
}
