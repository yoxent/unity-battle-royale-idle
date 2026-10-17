using BattleRoyale.Company;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class ContractRulesTests
    {
        [Test]
        public void IllustrationMatchesThePriceTable()
        {
            var tuning = EconomyTuning.Illustration();

            AssertPrice(tuning, AgentRarity.Common, 6, 1, 1);
            AssertPrice(tuning, AgentRarity.Uncommon, 12, 2, 1);
            AssertPrice(tuning, AgentRarity.Rare, 24, 4, 0);
            AssertPrice(tuning, AgentRarity.Epic, 36, 6, 0);
            AssertPrice(tuning, AgentRarity.Legendary, 48, 8, 0);
        }

        [Test]
        public void EpicAndLegendaryOweUpkeepOnTheFirstTournament()
        {
            var tuning = EconomyTuning.Illustration();

            Assert.IsTrue(ContractRules.TryPrice(
                AgentRarity.Epic,
                tuning,
                out var epic));
            Assert.IsTrue(ContractRules.TryChargeUpkeep(
                AgentRarity.Epic,
                epic.WaivedAppearances,
                tuning,
                out var epicCharge,
                out var epicRemaining));
            Assert.AreEqual(0, epic.WaivedAppearances);
            Assert.AreEqual(epic.Upkeep, epicCharge);
            Assert.AreEqual(0, epicRemaining);

            Assert.IsTrue(ContractRules.TryPrice(
                AgentRarity.Legendary,
                tuning,
                out var legendary));
            Assert.IsTrue(ContractRules.TryChargeUpkeep(
                AgentRarity.Legendary,
                legendary.WaivedAppearances,
                tuning,
                out var legendaryCharge,
                out _));
            Assert.AreEqual(legendary.Upkeep, legendaryCharge);
        }

        [Test]
        public void AWaivedAppearanceSkipsOneUpkeepThenTheNextTournamentChargesIt()
        {
            var tuning = EconomyTuning.Illustration();
            Assert.IsTrue(ContractRules.TryPrice(
                AgentRarity.Common,
                tuning,
                out var price));

            Assert.IsTrue(ContractRules.TryChargeUpkeep(
                AgentRarity.Common,
                price.WaivedAppearances,
                tuning,
                out var firstCharge,
                out var remaining));
            Assert.AreEqual(0, firstCharge);
            Assert.AreEqual(0, remaining);

            Assert.IsTrue(ContractRules.TryChargeUpkeep(
                AgentRarity.Common,
                remaining,
                tuning,
                out var secondCharge,
                out var afterSecond));
            Assert.AreEqual(price.Upkeep, secondCharge);
            Assert.AreEqual(0, afterSecond);
        }

        [Test]
        public void PricesComeFromTheTuningObject()
        {
            var tuning = EconomyTuning.Illustration();
            tuning.ContractByRarity[(int)AgentRarity.Common] = 9;
            tuning.UpkeepByRarity[(int)AgentRarity.Common] = 3;
            tuning.WaivedAppearancesByRarity[(int)AgentRarity.Common] = 4;

            Assert.IsTrue(ContractRules.TryPrice(
                AgentRarity.Common,
                tuning,
                out var price));
            Assert.AreEqual(9, price.Contract);
            Assert.AreEqual(3, price.Upkeep);
            Assert.AreEqual(4, price.WaivedAppearances);
        }

        [Test]
        public void MissingTuningIsRejected()
        {
            Assert.IsFalse(ContractRules.TryPrice(
                AgentRarity.Common,
                null,
                out _));
            Assert.IsFalse(ContractRules.TryChargeUpkeep(
                AgentRarity.Common,
                1,
                null,
                out _,
                out _));
        }

        [Test]
        public void ANegativeWaiverCountIsRejected()
        {
            Assert.IsFalse(ContractRules.TryChargeUpkeep(
                AgentRarity.Common,
                -1,
                EconomyTuning.Illustration(),
                out _,
                out _));
        }

        [Test]
        public void IllustrationKeepsTheCompanyFloor()
        {
            var tuning = EconomyTuning.Illustration();
            Assert.IsTrue(ContractRules.TryPrice(
                AgentRarity.Common,
                tuning,
                out var common));
            Assert.IsTrue(ContractRules.TryPrice(
                AgentRarity.Legendary,
                tuning,
                out var legendary));

            Assert.AreEqual(100, tuning.StartingGold);
            Assert.GreaterOrEqual(
                tuning.StartingGold,
                common.Contract + tuning.CheapEntranceFee);
            Assert.GreaterOrEqual(tuning.StartingGold, legendary.Contract);
            Assert.GreaterOrEqual(
                tuning.Salary + tuning.FanPayCap,
                tuning.CheapEntranceFee + 5 * common.Upkeep);
            Assert.Less(
                tuning.Salary + tuning.FanPayCap,
                5 * legendary.Upkeep);
            Assert.Greater(tuning.CheapFirstPrize, tuning.CheapEntranceFee);
            Assert.AreEqual(0, tuning.CheapLastPrize);
        }

        static void AssertPrice(
            EconomyTuning tuning,
            AgentRarity rarity,
            int contract,
            int upkeep,
            int waivedAppearances)
        {
            Assert.IsTrue(ContractRules.TryPrice(rarity, tuning, out var price));
            Assert.AreEqual(contract, price.Contract);
            Assert.AreEqual(upkeep, price.Upkeep);
            Assert.AreEqual(waivedAppearances, price.WaivedAppearances);
        }
    }
}
