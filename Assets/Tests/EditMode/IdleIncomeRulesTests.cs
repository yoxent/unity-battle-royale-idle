using BattleRoyale.Company;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class IdleIncomeRulesTests
    {
        [Test]
        public void OneOnlineDayPaysTheSalaryAndTheCappedFanTick()
        {
            var tuning = EconomyTuning.Illustration();
            var company = Company(tuning.StartingGold, fans: 40);

            Assert.IsTrue(IdleIncomeRules.TryAdvance(
                company,
                tuning,
                tuning.SessionDaySeconds,
                offline: false,
                out var paid));

            Assert.AreEqual(5, paid.SalaryPaid);
            Assert.AreEqual(5, paid.FanPaid);
            Assert.AreEqual(tuning.StartingGold + 10, paid.Balance);
            Assert.AreEqual(0f, paid.DayProgress);
            Assert.AreEqual(40, paid.Fans);
        }

        [Test]
        public void ThatDayRestoresACommonLastPlace()
        {
            var tuning = EconomyTuning.Illustration();
            Assert.IsTrue(IdleIncomeRules.TryAdvance(
                Company(10, fans: tuning.FanPayCap),
                tuning,
                tuning.SessionDaySeconds,
                offline: false,
                out var paid));

            Assert.AreEqual(
                tuning.CheapEntranceFee + (5 * tuning.UpkeepByRarity[0]),
                paid.SalaryPaid + paid.FanPaid);
            Assert.AreEqual(10, paid.SalaryPaid + paid.FanPaid);
        }

        [Test]
        public void ADayWithNoFansStillPaysTheSalary()
        {
            var tuning = EconomyTuning.Illustration();
            Assert.IsTrue(IdleIncomeRules.TryAdvance(
                Company(tuning.StartingGold, fans: 0),
                tuning,
                tuning.SessionDaySeconds,
                offline: false,
                out var paid));

            Assert.AreEqual(5, paid.SalaryPaid);
            Assert.AreEqual(0, paid.FanPaid);
            Assert.AreEqual(tuning.StartingGold + 5, paid.Balance);
        }

        [Test]
        public void FanPayBelowTheCapPaysTheFanCount()
        {
            var tuning = EconomyTuning.Illustration();
            Assert.IsTrue(IdleIncomeRules.TryAdvance(
                Company(0, fans: 2),
                tuning,
                tuning.SessionDaySeconds,
                offline: false,
                out var paid));

            Assert.AreEqual(2, paid.FanPaid);
        }

        [Test]
        public void AClosedDayPaysHalfRoundedDown()
        {
            var tuning = EconomyTuning.Illustration();
            Assert.IsTrue(IdleIncomeRules.TryAdvance(
                Company(tuning.StartingGold, fans: 40),
                tuning,
                tuning.SessionDaySeconds,
                offline: true,
                out var paid));

            Assert.AreEqual(2, paid.SalaryPaid);
            Assert.AreEqual(2, paid.FanPaid);
            Assert.AreEqual(tuning.StartingGold + 4, paid.Balance);
        }

        [Test]
        public void ACustomOfflineRateFloorsEachPart()
        {
            var tuning = EconomyTuning.Illustration();
            tuning.Salary = 9;
            tuning.FanPayCap = 9;
            tuning.OfflineEarningRate = 0.25f;
            tuning.SessionDaySeconds = 10f;

            Assert.IsTrue(IdleIncomeRules.TryAdvance(
                Company(0, fans: 9),
                tuning,
                10f,
                offline: true,
                out var paid));

            Assert.AreEqual(2, paid.SalaryPaid);
            Assert.AreEqual(2, paid.FanPaid);
        }

        [Test]
        public void TwoClosedDaysPayTwice()
        {
            var tuning = EconomyTuning.Illustration();
            Assert.IsTrue(IdleIncomeRules.TryAdvance(
                Company(0, fans: 40),
                tuning,
                tuning.SessionDaySeconds * 2f,
                offline: true,
                out var paid));

            Assert.AreEqual(4, paid.SalaryPaid);
            Assert.AreEqual(4, paid.FanPaid);
            Assert.AreEqual(0f, paid.DayProgress);
        }

        [Test]
        public void APartialDayPaysNothingAndKeepsTheRemainder()
        {
            var tuning = EconomyTuning.Illustration();
            tuning.SessionDaySeconds = 10f;
            var company = Company(7, fans: 40);

            Assert.IsTrue(IdleIncomeRules.TryAdvance(
                company,
                tuning,
                6f,
                offline: false,
                out var partial));
            Assert.AreEqual(0, partial.SalaryPaid);
            Assert.AreEqual(0, partial.FanPaid);
            Assert.AreEqual(7, partial.Balance);
            Assert.AreEqual(6f, partial.DayProgress);
            Assert.AreEqual(7, company.Balance);

            Assert.IsTrue(IdleIncomeRules.TryAdvance(
                partial,
                tuning,
                4f,
                offline: false,
                out var completed));
            Assert.AreEqual(5, completed.SalaryPaid);
            Assert.AreEqual(5, completed.FanPaid);
            Assert.AreEqual(17, completed.Balance);
            Assert.AreEqual(0f, completed.DayProgress);
        }

        [Test]
        public void TwoAndAHalfDaysPayTwiceAndKeepTheHalf()
        {
            var tuning = EconomyTuning.Illustration();
            tuning.SessionDaySeconds = 10f;

            Assert.IsTrue(IdleIncomeRules.TryAdvance(
                Company(0, fans: 40),
                tuning,
                25f,
                offline: false,
                out var paid));

            Assert.AreEqual(10, paid.SalaryPaid);
            Assert.AreEqual(10, paid.FanPaid);
            Assert.AreEqual(5f, paid.DayProgress);
        }

        [Test]
        public void AFullOfflineRateIsRejected()
        {
            var tuning = EconomyTuning.Illustration();
            tuning.OfflineEarningRate = 1f;
            var company = Company(tuning.StartingGold, fans: 4);

            Assert.IsFalse(IdleIncomeRules.TryAdvance(
                company,
                tuning,
                tuning.SessionDaySeconds,
                offline: true,
                out _));
            Assert.AreEqual(tuning.StartingGold, company.Balance);
            Assert.AreEqual(0f, company.DayProgress);
        }

        static IdleIncomeState Company(int balance, int fans)
        {
            return new IdleIncomeState
            {
                Balance = balance,
                Fans = fans,
            };
        }
    }
}
