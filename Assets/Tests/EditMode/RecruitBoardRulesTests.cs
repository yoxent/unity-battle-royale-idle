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

            Assert.AreEqual(AgentRarity.Legendary, cards[0].Rarity);
            Assert.AreEqual(48, cards[0].Contract);
            Assert.LessOrEqual(cards[0].Contract, tuning.StartingGold);

            Assert.IsTrue(RecruitBoardRules.TryRoll(
                new[] { "Scout" },
                tuning,
                new System.Random(1),
                true,
                cards[0].Contract - 1,
                out var forced));

            Assert.AreEqual(AgentRarity.Common, forced[0].Rarity);
            Assert.AreEqual(6, forced[0].Contract);
            Assert.AreEqual(AgentRarity.Legendary, forced[1].Rarity);
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
        public void EightPaidRefreshesQuoteTwentySevenAndANewUtcDayQuotesFree()
        {
            var tuning = EconomyTuning.Illustration();
            const long accepted = 1_700_000_000;

            Assert.AreEqual(3, tuning.BaseEarlyRefreshFee);
            Assert.AreEqual(3, tuning.EarlyRefreshRaise);
            Assert.AreEqual(3, tuning.FreeRefreshesPerDay);
            Assert.AreEqual(14400f, tuning.RecruitRefreshSeconds);
            Assert.AreEqual(27, RecruitBoardRules.QuoteFee(tuning, 3, 8));
            Assert.AreEqual("Refresh · 27g", RecruitBoardRules.FormatRefreshCost(27));
            Assert.AreEqual("Refresh · Free", RecruitBoardRules.FormatRefreshCost(0));

            Assert.IsTrue(RecruitBoardRules.TryAcceptWorldTime(
                3, 8, (int)(accepted / RecruitBoardRules.UnixDaySeconds), accepted, accepted + 86400,
                out var freeUsed, out var paidRetries, out _, out _, out var reset));
            Assert.IsTrue(reset);
            Assert.AreEqual(0, freeUsed);
            Assert.AreEqual(0, paidRetries);
            Assert.AreEqual(0, RecruitBoardRules.QuoteFee(tuning, freeUsed, paidRetries));
            Assert.AreEqual(3, RecruitBoardRules.QuoteFee(tuning, 3, 0));
        }

        [Test]
        public void AnEarlierWorldTimeDoesNotResetTheRaisedFee()
        {
            const long accepted = 1_700_000_000;

            Assert.IsFalse(RecruitBoardRules.TryAcceptWorldTime(
                3, 4, 19675, accepted, accepted - 86400,
                out var freeUsed, out var paidRetries, out var day, out var nextAccepted, out var reset));

            Assert.IsFalse(reset);
            Assert.AreEqual(3, freeUsed);
            Assert.AreEqual(4, paidRetries);
            Assert.AreEqual(19675, day);
            Assert.AreEqual(accepted, nextAccepted);
            Assert.AreEqual(0f, RecruitBoardRules.OfflineSeconds(accepted, accepted - 3600, 0f));

            Assert.IsTrue(RecruitBoardRules.TryAcceptWorldTime(
                3, 4, (int)(accepted / RecruitBoardRules.UnixDaySeconds), accepted, accepted + 3600,
                out freeUsed, out paidRetries, out day, out nextAccepted, out reset));
            Assert.IsFalse(reset);
            Assert.AreEqual(3, freeUsed);
            Assert.AreEqual(4, paidRetries);
            Assert.AreEqual((int)(accepted / RecruitBoardRules.UnixDaySeconds), day);
            Assert.AreEqual(accepted + 3600, nextAccepted);
        }

        [Test]
        public void TheFirstThreeRefreshesAreFreeAndTheNextTwoRaiseTheFee()
        {
            var tuning = EconomyTuning.Illustration();
            var balance = 30;
            var freeUsed = 0;
            var paidRetries = 0;

            for (var i = 0; i < 3; i++)
            {
                Assert.AreEqual(0, RecruitBoardRules.QuoteFee(tuning, freeUsed, paidRetries));
                Assert.IsTrue(RecruitBoardRules.TryPayRefresh(
                    tuning, balance, freeUsed, paidRetries,
                    out balance, out freeUsed, out paidRetries, out var paid));
                Assert.IsFalse(paid);
            }

            Assert.AreEqual(30, balance);
            Assert.AreEqual(3, freeUsed);
            Assert.AreEqual(0, paidRetries);
            Assert.AreEqual("Refresh · 3g", RecruitBoardRules.FormatRefreshCost(
                RecruitBoardRules.QuoteFee(tuning, freeUsed, paidRetries)));

            Assert.IsTrue(RecruitBoardRules.TryPayRefresh(
                tuning, balance, freeUsed, paidRetries,
                out balance, out freeUsed, out paidRetries, out var firstPaid));
            Assert.IsTrue(firstPaid);
            Assert.AreEqual(27, balance);
            Assert.AreEqual(1, paidRetries);

            Assert.IsTrue(RecruitBoardRules.TryPayRefresh(
                tuning, balance, freeUsed, paidRetries,
                out balance, out freeUsed, out paidRetries, out var secondPaid));
            Assert.IsTrue(secondPaid);
            Assert.AreEqual(21, balance);
            Assert.AreEqual(2, paidRetries);
            Assert.AreEqual("Refresh · 9g", RecruitBoardRules.FormatRefreshCost(
                RecruitBoardRules.QuoteFee(tuning, freeUsed, paidRetries)));
        }

        [Test]
        public void APaidConfirmSkipLastsForThatRefreshDay()
        {
            Assert.IsFalse(RecruitBoardRules.SkipPaidConfirm(0, 20000));
            Assert.IsTrue(RecruitBoardRules.SkipPaidConfirm(20000, 20000));
            Assert.IsFalse(RecruitBoardRules.SkipPaidConfirm(20000, 20001));
        }

        [Test]
        public void AShortBalanceDoesNotBuyARefresh()
        {
            var tuning = EconomyTuning.Illustration();

            Assert.IsFalse(RecruitBoardRules.TryPayRefresh(
                tuning, 2, 3, 0,
                out var balance, out var freeUsed, out var paidRetries, out var paid));

            Assert.IsFalse(paid);
            Assert.AreEqual(2, balance);
            Assert.AreEqual(3, freeUsed);
            Assert.AreEqual(0, paidRetries);
        }

        [Test]
        public void FourHoursCatchesUpOneBoardAndKeepsTheRemainder()
        {
            Assert.IsTrue(RecruitBoardRules.TryCatchUp(
                100f, 40f, 14400f, out var waiting, out var notDue));
            Assert.IsFalse(notDue);
            Assert.AreEqual(60f, waiting);

            Assert.IsTrue(RecruitBoardRules.TryCatchUp(
                100f, 14400f, 14400f, out var landed, out var due));
            Assert.IsTrue(due);
            Assert.AreEqual(100f, landed);

            Assert.IsTrue(RecruitBoardRules.TryCatchUp(
                100f, 14400f + 50f, 14400f, out var into, out var later));
            Assert.IsTrue(later);
            Assert.AreEqual(50f, into);

            Assert.IsFalse(RecruitBoardRules.TryCatchUp(100f, -1f, 14400f, out _, out _));
            Assert.AreEqual(40f, RecruitBoardRules.OfflineSeconds(1_000, 1_050, 10f));
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
