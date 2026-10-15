namespace BattleRoyale.Company
{
    public sealed class IdleIncomeState
    {
        public int Balance;
        public int Fans;
        public float DayProgress;
        public int SalaryPaid;
        public int FanPaid;
    }

    public static class IdleIncomeRules
    {
        public static bool TryAdvance(
            IdleIncomeState state,
            EconomyTuning tuning,
            float elapsedSeconds,
            bool offline,
            out IdleIncomeState next)
        {
            next = null;
            if (state == null ||
                tuning == null ||
                elapsedSeconds < 0f ||
                tuning.SessionDaySeconds <= 0f ||
                tuning.OfflineEarningRate < 0f ||
                tuning.OfflineEarningRate >= 1f ||
                tuning.Salary < 0 ||
                tuning.FanPayCap < 0 ||
                state.Fans < 0)
            {
                return false;
            }

            var total = state.DayProgress + elapsedSeconds;
            var days = (int)System.Math.Floor(total / tuning.SessionDaySeconds);
            var progress = total - (days * tuning.SessionDaySeconds);
            var fanPerDay = state.Fans < tuning.FanPayCap ? state.Fans : tuning.FanPayCap;
            var salaryPerDay = Scale(tuning.Salary, offline, tuning.OfflineEarningRate);
            var fanPayPerDay = Scale(fanPerDay, offline, tuning.OfflineEarningRate);
            var salaryPaid = salaryPerDay * days;
            var fanPaid = fanPayPerDay * days;

            next = new IdleIncomeState
            {
                Balance = state.Balance + salaryPaid + fanPaid,
                Fans = state.Fans,
                DayProgress = progress,
                SalaryPaid = salaryPaid,
                FanPaid = fanPaid,
            };
            return true;
        }

        static int Scale(int amount, bool offline, float rate)
        {
            if (!offline)
            {
                return amount;
            }

            return (int)System.Math.Floor(amount * (double)rate);
        }
    }
}
