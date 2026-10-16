using BattleRoyale.GameModes;

namespace BattleRoyale.GameModes.BattleRoyale
{
    public static class BattleRoyaleStatTracker
    {
        public static void Write(
            MatchStatBoard board,
            bool started,
            int aliveCount,
            BattleRoyaleRosterOrder order)
        {
            if (board == null)
            {
                return;
            }

            board.Clear();
            if (!started)
            {
                return;
            }

            var count = aliveCount < 0 ? 0 : aliveCount;
            board.ShowAlive(count + " alive");
            if (order == null)
            {
                return;
            }

            for (var index = 0; index < order.Count; index++)
            {
                var entry = order.EntryAt(index);
                board.AddRow(new MatchStatRow(
                    entry.DisplayName,
                    entry.TeamIndex,
                    entry.Alive,
                    false,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0));
            }
        }
    }
}
