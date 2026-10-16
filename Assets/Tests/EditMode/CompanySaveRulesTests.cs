using BattleRoyale.Company;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class CompanySaveRulesTests
    {
        [Test]
        public void ACompanyRoundTripsAndKeepsTheCheckpointEmpty()
        {
            var company = Sample();

            Assert.IsTrue(CompanySaveRules.TryWrite(company, out var json));
            Assert.IsTrue(CompanySaveRules.TryRead(json, out var loaded));

            Assert.AreEqual("Ash Court", loaded.CompanyName);
            Assert.AreEqual(20, loaded.Balance);
            Assert.AreEqual(1, loaded.Roster.Length);
            Assert.AreEqual("Scout", loaded.Roster[0].ArchetypeId);
            Assert.IsFalse(loaded.HasCheckpoint);
        }

        [Test]
        public void AClosedDayPaysTwoSalaryAndTwoFans()
        {
            var tuning = EconomyTuning.Illustration();
            var company = Sample();
            company.Fans = tuning.FanPayCap;

            Assert.IsTrue(CompanySaveRules.TrySettleOffline(
                company,
                tuning.SessionDaySeconds,
                tuning,
                out var next,
                out var salary,
                out var fans));

            Assert.AreEqual(2, salary);
            Assert.AreEqual(2, fans);
            Assert.AreEqual(24, next.Balance);
            Assert.AreEqual(company.CompanyName, next.CompanyName);
        }

        [Test]
        public void ABackwardStepIsRejected()
        {
            Assert.IsFalse(CompanySaveRules.TrySettleOffline(
                Sample(),
                -1f,
                EconomyTuning.Illustration(),
                out _,
                out _,
                out _));
        }

        [Test]
        public void ABlankFileIsRejected()
        {
            Assert.IsFalse(CompanySaveRules.TryRead("", out _));
            Assert.IsFalse(CompanySaveRules.TryRead("{ }", out _));
        }

        static CompanySaveRecord Sample()
        {
            return new CompanySaveRecord
            {
                CompanyName = "Ash Court",
                Balance = 20,
                Fans = 5,
                CompanyDate = 1,
                Roster = new[]
                {
                    new CompanySaveAgent
                    {
                        ArchetypeId = "Scout",
                        Rarity = (int)AgentRarity.Common,
                        Rolls = new[] { 1, 2, 3, 4, 5, 6 },
                        Waived = 1,
                    },
                },
            };
        }
    }
}
