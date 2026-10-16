namespace BattleRoyale.Company
{
    public static class RosterFieldRules
    {
        public static bool TryTakeFree(bool[] busy, int count, out int[] indexes)
        {
            indexes = null;
            if (busy == null || count <= 0)
            {
                return false;
            }

            var free = 0;
            for (var i = 0; i < busy.Length; i++)
            {
                if (!busy[i])
                {
                    free++;
                }
            }

            if (free < count)
            {
                return false;
            }

            indexes = new int[count];
            var write = 0;
            for (var i = 0; i < busy.Length && write < count; i++)
            {
                if (busy[i])
                {
                    continue;
                }

                indexes[write] = i;
                write++;
            }

            return true;
        }
    }
}
