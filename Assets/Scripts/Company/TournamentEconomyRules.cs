namespace BattleRoyale.Company
{
    public enum TournamentEntryPhase
    {
        Open = 0,
        Window = 1,
        Running = 2,
        Resolved = 3,
    }

    public enum TournamentResult
    {
        None = 0,
        First = 1,
        Last = 2,
        Loss = 3,
    }

    public struct FieldedAgent
    {
        public AgentRarity Rarity;
        public int WaivedAppearances;
        public bool Benched;
    }

    public sealed class TournamentEntryState
    {
        public int Balance;
        public int Reserved;
        public int CompanyDate;
        public TournamentEntryPhase Phase;
        public TournamentResult Result;
        public float WindowRemaining;
        public FieldedAgent[] Agents;

        public int Spendable => Balance - Reserved;
    }

    public static class TournamentEconomyRules
    {
        public static bool TryEnter(
            TournamentEntryState state,
            EconomyTuning tuning,
            int entranceFee,
            FieldedAgent[] agents,
            out TournamentEntryState next)
        {
            next = null;
            if (state == null ||
                tuning == null ||
                agents == null ||
                entranceFee < 0 ||
                tuning.AbortWindowSeconds <= 0f ||
                state.Phase != TournamentEntryPhase.Open)
            {
                return false;
            }

            var cost = entranceFee;
            var fielded = 0;
            for (var i = 0; i < agents.Length; i++)
            {
                if (agents[i].Benched)
                {
                    continue;
                }

                fielded++;
                if (!ContractRules.TryChargeUpkeep(
                        agents[i].Rarity,
                        agents[i].WaivedAppearances,
                        tuning,
                        out var charge,
                        out _))
                {
                    return false;
                }

                cost += charge;
            }

            if (fielded == 0 || state.Balance - state.Reserved < cost)
            {
                return false;
            }

            next = Copy(state);
            next.Reserved = state.Reserved + cost;
            next.Phase = TournamentEntryPhase.Window;
            next.WindowRemaining = tuning.AbortWindowSeconds;
            next.Result = TournamentResult.None;
            next.Agents = CopyAgents(agents);
            return true;
        }

        public static bool TryAdvance(
            TournamentEntryState state,
            float elapsedSeconds,
            out TournamentEntryState next)
        {
            next = null;
            if (state == null ||
                state.Phase != TournamentEntryPhase.Window ||
                elapsedSeconds < 0f)
            {
                return false;
            }

            next = Copy(state);
            if (elapsedSeconds < state.WindowRemaining)
            {
                next.WindowRemaining = state.WindowRemaining - elapsedSeconds;
                return true;
            }

            next.Balance = state.Balance - state.Reserved;
            next.Reserved = 0;
            next.Phase = TournamentEntryPhase.Running;
            next.WindowRemaining = 0f;
            next.Result = TournamentResult.None;
            ConsumeWaivers(next.Agents);
            return true;
        }

        public static bool TryAbort(
            TournamentEntryState state,
            out TournamentEntryState next)
        {
            next = null;
            if (state == null || state.Phase != TournamentEntryPhase.Window)
            {
                return false;
            }

            next = Copy(state);
            next.Phase = TournamentEntryPhase.Open;
            next.Reserved = 0;
            next.WindowRemaining = 0f;
            next.Result = TournamentResult.None;
            return true;
        }

        public static bool TryApplyResult(
            TournamentEntryState state,
            TournamentResult result,
            int prize,
            out TournamentEntryState next)
        {
            next = null;
            if (state == null ||
                state.Phase != TournamentEntryPhase.Running ||
                prize < 0 ||
                (result != TournamentResult.First &&
                 result != TournamentResult.Last &&
                 result != TournamentResult.Loss))
            {
                return false;
            }

            next = Copy(state);
            next.Phase = TournamentEntryPhase.Resolved;
            next.Result = result;
            next.CompanyDate = state.CompanyDate + 1;
            if (result == TournamentResult.First)
            {
                next.Balance = state.Balance + prize;
            }

            return true;
        }

        static void ConsumeWaivers(FieldedAgent[] agents)
        {
            if (agents == null)
            {
                return;
            }

            for (var i = 0; i < agents.Length; i++)
            {
                if (agents[i].Benched || agents[i].WaivedAppearances <= 0)
                {
                    continue;
                }

                agents[i].WaivedAppearances -= 1;
            }
        }

        static TournamentEntryState Copy(TournamentEntryState state)
        {
            return new TournamentEntryState
            {
                Balance = state.Balance,
                Reserved = state.Reserved,
                CompanyDate = state.CompanyDate,
                Phase = state.Phase,
                Result = state.Result,
                WindowRemaining = state.WindowRemaining,
                Agents = CopyAgents(state.Agents),
            };
        }

        static FieldedAgent[] CopyAgents(FieldedAgent[] agents)
        {
            if (agents == null)
            {
                return null;
            }

            var copy = new FieldedAgent[agents.Length];
            for (var i = 0; i < agents.Length; i++)
            {
                copy[i] = agents[i];
            }

            return copy;
        }
    }
}
