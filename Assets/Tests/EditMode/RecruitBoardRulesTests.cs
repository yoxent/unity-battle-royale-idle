using BattleRoyale.Company;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class RecruitBoardRulesTests
    {
        [Test]
        public void TheFirstBoardIncludesACardTheStartingGoldCanAfford()
        {
            var tuning = EconomyTuning.Illustration();
            tuning.RarityWeights = new[] { 0, 0, 0, 0, 2 };

            Assert.IsTrue(RecruitBoardRules.TryRoll(
                new[] { "Scout" },
                tuning,
                new System.Random(1),
                true,
                tuning.StartingGold,
                out var cards));

            Assert.AreEqual(AgentRarity.Common, cards[0].Rarity);
            Assert.AreEqual(6, cards[0].Contract);
            Assert.AreEqual(AgentRarity.Legendary, cards[1].Rarity);
        }

        [Test]
        public void ALaterRollIsNotForced()
        {
            var tuning = EconomyTuning.Illustration();
            tuning.RarityWeights = new[] { 0, 0, 0, 0, 2 };

            Assert.IsTrue(RecruitBoardRules.TryRoll(
                new[] { "Scout" },
                tuning,
                new System.Random(1),
                false,
                tuning.StartingGold,
                out var cards));

            Assert.AreEqual(AgentRarity.Legendary, cards[0].Rarity);
        }

        [Test]
        public void HireDeductsTheContractAndClearsTheSlot()
        {
            var board = Board(contract: 6);

            Assert.IsTrue(RecruitBoardRules.TryHire(board, 0, 20, 5, out var balance, out var hired));

            Assert.AreEqual(14, balance);
            Assert.AreEqual(6, hired.Contract);
            Assert.IsFalse(board.Slots[0].Filled);
        }

        [Test]
        public void HireDoesNothingWhenTheRosterIsFullOrTheBalanceIsShort()
        {
            var full = Board(contract: 6);
            Assert.IsFalse(RecruitBoardRules.TryHire(full, 0, 20, 8, out _, out _));
            Assert.IsTrue(full.Slots[0].Filled);

            var shortBoard = Board(contract: 48);
            Assert.IsFalse(RecruitBoardRules.TryHire(shortBoard, 0, 20, 1, out _, out _));
            Assert.IsTrue(shortBoard.Slots[0].Filled);
        }

        [Test]
        public void ReleaseRemovesTheAgentAndPaysNothing()
        {
            var roster = new[]
            {
                Agent("A"),
                Agent("B"),
                Agent("C"),
            };

            Assert.IsTrue(RecruitBoardRules.TryRelease(roster, 1, false, out var next));

            Assert.AreEqual(2, next.Length);
            Assert.AreEqual("A", next[0].ArchetypeId);
            Assert.AreEqual("C", next[1].ArchetypeId);
        }

        [Test]
        public void ReleaseRefusesABusyAgent()
        {
            var roster = new[] { Agent("A") };
            Assert.IsFalse(RecruitBoardRules.TryRelease(roster, 0, true, out _));
        }

        [Test]
        public void EightPaidRefreshesQuoteTwentySevenAndANewDateQuotesTheBase()
        {
            var tuning = EconomyTuning.Illustration();

            Assert.AreEqual(3, tuning.BaseEarlyRefreshFee);
            Assert.AreEqual(3, tuning.EarlyRefreshRaise);
            Assert.AreEqual(600f, tuning.RecruitRefreshSeconds);
            Assert.AreEqual(27, RecruitBoardRules.QuoteFee(tuning, 8, 1, 1));
            Assert.AreEqual(3, RecruitBoardRules.QuoteFee(tuning, 8, 1, 2));
        }

        [Test]
        public void APaidRefreshRaisesTheNextFeeAndAFreeRefreshDoesNot()
        {
            var tuning = EconomyTuning.Illustration();

            Assert.IsTrue(RecruitBoardRules.TryPayRefresh(
                tuning, 20, 10f, 0, 1, 1,
                out var balance, out var retries, out var date, out var paid));
            Assert.IsTrue(paid);
            Assert.AreEqual(17, balance);
            Assert.AreEqual(1, retries);
            Assert.AreEqual(1, date);

            Assert.IsTrue(RecruitBoardRules.TryPayRefresh(
                tuning, 20, 0f, 4, 1, 1,
                out var freeBalance, out var freeRetries, out _, out var freePaid));
            Assert.IsFalse(freePaid);
            Assert.AreEqual(20, freeBalance);
            Assert.AreEqual(4, freeRetries);
        }

        static RecruitBoard Board(int contract)
        {
            return new RecruitBoard
            {
                Slots = new[]
                {
                    new RecruitCard
                    {
                        Filled = true,
                        ArchetypeId = "Scout",
                        Contract = contract,
                        Rarity = AgentRarity.Common,
                        Rolls = new[] { 1, 1, 1, 1, 1, 1 },
                    },
                },
            };
        }

        static CompanySaveAgent Agent(string id)
        {
            return new CompanySaveAgent
            {
                ArchetypeId = id,
                Rolls = new[] { 1, 1, 1, 1, 1, 1 },
            };
        }
    }
}
