namespace BattleRoyale.Company
{
    public static class LineupRules
    {
        public static int Score(int[] rolls)
        {
            if (rolls == null)
            {
                return 0;
            }

            var total = 0;
            for (var i = 0; i < rolls.Length; i++)
            {
                total += rolls[i];
            }

            return total;
        }

        public static bool TryAutoFill(int[] scores, bool[] busy, int count, out int[] indexes)
        {
            indexes = null;
            if (scores == null || busy == null || scores.Length != busy.Length || count <= 0)
            {
                return false;
            }

            var order = new int[scores.Length];
            var available = 0;
            for (var i = 0; i < scores.Length; i++)
            {
                if (busy[i])
                {
                    continue;
                }

                order[available] = i;
                available++;
            }

            if (available < count)
            {
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                var best = i;
                for (var j = i + 1; j < available; j++)
                {
                    var candidate = order[j];
                    var current = order[best];
                    if (scores[candidate] > scores[current])
                    {
                        best = j;
                    }
                }

                var swap = order[i];
                order[i] = order[best];
                order[best] = swap;
            }

            indexes = new int[count];
            for (var i = 0; i < count; i++)
            {
                indexes[i] = order[i];
            }

            return true;
        }

        public static bool TryToggle(
            int[] current,
            int rosterIndex,
            bool busy,
            int count,
            out int[] next)
        {
            next = null;
            if (current == null || current.Length != count || count <= 0 || rosterIndex < 0)
            {
                return false;
            }

            next = (int[])current.Clone();
            for (var i = 0; i < next.Length; i++)
            {
                if (next[i] != rosterIndex)
                {
                    continue;
                }

                next[i] = -1;
                return true;
            }

            if (busy)
            {
                next = null;
                return false;
            }

            for (var i = 0; i < next.Length; i++)
            {
                if (next[i] >= 0)
                {
                    continue;
                }

                next[i] = rosterIndex;
                return true;
            }

            next = null;
            return false;
        }

        public static bool IsComplete(int[] indexes, int rosterLength, bool[] busy)
        {
            if (indexes == null || indexes.Length == 0 ||
                busy == null || busy.Length != rosterLength || rosterLength <= 0)
            {
                return false;
            }

            var seen = new bool[rosterLength];
            for (var i = 0; i < indexes.Length; i++)
            {
                var index = indexes[i];
                if (index < 0 || index >= rosterLength || busy[index] || seen[index])
                {
                    return false;
                }

                seen[index] = true;
            }

            return true;
        }

        public static void RemapRelease(int[] indexes, int removed)
        {
            if (indexes == null || removed < 0)
            {
                return;
            }

            for (var i = 0; i < indexes.Length; i++)
            {
                if (indexes[i] == removed)
                {
                    indexes[i] = -1;
                }
                else if (indexes[i] > removed)
                {
                    indexes[i]--;
                }
            }
        }
    }
}
