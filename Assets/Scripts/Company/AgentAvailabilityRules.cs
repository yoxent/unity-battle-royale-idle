namespace BattleRoyale.Company
{
    public struct AgentAvailability
    {
        public int Id;
        public bool Busy;
    }

    public static class AgentAvailabilityRules
    {
        public static bool TryDeploy(
            AgentAvailability[] roster,
            int[] agentIds,
            out AgentAvailability[] next)
        {
            return TrySet(roster, agentIds, requireBusy: false, busy: true, out next);
        }

        public static bool TryAbort(
            AgentAvailability[] roster,
            int[] agentIds,
            out AgentAvailability[] next)
        {
            return TrySet(roster, agentIds, requireBusy: true, busy: false, out next);
        }

        public static bool TryFinish(
            AgentAvailability[] roster,
            int[] agentIds,
            out AgentAvailability[] next)
        {
            return TrySet(roster, agentIds, requireBusy: true, busy: false, out next);
        }

        static bool TrySet(
            AgentAvailability[] roster,
            int[] agentIds,
            bool requireBusy,
            bool busy,
            out AgentAvailability[] next)
        {
            next = null;
            if (roster == null || agentIds == null || agentIds.Length == 0)
            {
                return false;
            }

            if (HasDuplicateRosterIds(roster) || HasDuplicateIds(agentIds))
            {
                return false;
            }

            var indexes = new int[agentIds.Length];
            for (var i = 0; i < agentIds.Length; i++)
            {
                var index = IndexOf(roster, agentIds[i]);
                if (index < 0 || roster[index].Busy != requireBusy)
                {
                    return false;
                }

                indexes[i] = index;
            }

            next = Copy(roster);
            for (var i = 0; i < indexes.Length; i++)
            {
                next[indexes[i]].Busy = busy;
            }

            return true;
        }

        static bool HasDuplicateRosterIds(AgentAvailability[] roster)
        {
            for (var i = 0; i < roster.Length; i++)
            {
                for (var j = i + 1; j < roster.Length; j++)
                {
                    if (roster[i].Id == roster[j].Id)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static bool HasDuplicateIds(int[] agentIds)
        {
            for (var i = 0; i < agentIds.Length; i++)
            {
                for (var j = i + 1; j < agentIds.Length; j++)
                {
                    if (agentIds[i] == agentIds[j])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static int IndexOf(AgentAvailability[] roster, int id)
        {
            for (var i = 0; i < roster.Length; i++)
            {
                if (roster[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        static AgentAvailability[] Copy(AgentAvailability[] roster)
        {
            var copy = new AgentAvailability[roster.Length];
            for (var i = 0; i < roster.Length; i++)
            {
                copy[i] = roster[i];
            }

            return copy;
        }
    }
}
