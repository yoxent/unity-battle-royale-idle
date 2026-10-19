using BattleRoyale.Company;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class TournamentEconomyRulesTests
    {
        [Test]
        public void EnterReservesTheFeeAndUpkeepWithoutStartingTheMatch()
        {
            var tuning = EconomyTuning.Illustration();
            var treasury = Treasury(tuning.StartingGold);

            Assert.IsTrue(TournamentEconomyRules.TryEnter(
                treasury,
                tuning,
                tuning.CheapEntranceFee,
                new[] { Agent(AgentRarity.Common, 0) },
                out var entered));

            Assert.AreEqual(tuning.StartingGold, entered.Balance);
            Assert.AreEqual(6, entered.Reserved);
            Assert.AreEqual(tuning.StartingGold - 6, entered.Spendable);
            Assert.AreEqual(TournamentEntryPhase.Window, entered.Phase);
            Assert.AreEqual(tuning.AbortWindowSeconds, entered.WindowRemaining);
            Assert.AreEqual(4, entered.CompanyDate);
            Assert.AreEqual(0, entered.Agents[0].WaivedAppearances);
        }

        [Test]
        public void AShortAdvanceDoesNotStartTheMatch()
        {
            var tuning = EconomyTuning.Illustration();
            Assert.IsTrue(TournamentEconomyRules.TryEnter(
                Treasury(tuning.StartingGold),
                tuning,
                tuning.CheapEntranceFee,
                new[] { Agent(AgentRarity.Common, 0) },
                out var entered));

            Assert.IsTrue(TournamentEconomyRules.TryAdvance(
                entered,
                5f,
                out var stillOpen));

            Assert.AreEqual(TournamentEntryPhase.Window, stillOpen.Phase);
            Assert.AreEqual(10f, stillOpen.WindowRemaining);
            Assert.AreEqual(6, stillOpen.Reserved);
            Assert.AreEqual(tuning.StartingGold, stillOpen.Balance);
        }

        [Test]
        public void TheWindowEndSpendsTheReservationAndStartsTheMatch()
        {
            var tuning = EconomyTuning.Illustration();
            Assert.IsTrue(TournamentEconomyRules.TryEnter(
                Treasury(tuning.StartingGold),
                tuning,
                tuning.CheapEntranceFee,
                new[] { Agent(AgentRarity.Common, 0) },
                out var entered));

            Assert.IsTrue(TournamentEconomyRules.TryAdvance(
                entered,
                tuning.AbortWindowSeconds,
                out var started));

            Assert.AreEqual(TournamentEntryPhase.Running, started.Phase);
            Assert.AreEqual(0, started.Reserved);
            Assert.AreEqual(tuning.StartingGold - 6, started.Balance);
            Assert.AreEqual(4, started.CompanyDate);
            Assert.AreEqual(TournamentResult.None, started.Result);
        }

        [Test]
        public void AbortReturnsTheReservationAndRestoresTheWaiver()
        {
            var tuning = EconomyTuning.Illustration();
            Assert.IsTrue(TournamentEconomyRules.TryEnter(
                Treasury(tuning.StartingGold),
                tuning,
                tuning.CheapEntranceFee,
                new[] { Agent(AgentRarity.Common, 1) },
                out var entered));

            Assert.AreEqual(tuning.CheapEntranceFee, entered.Reserved);
            Assert.IsTrue(TournamentEconomyRules.TryAbort(entered, out var aborted));

            Assert.AreEqual(TournamentEntryPhase.Open, aborted.Phase);
            Assert.AreEqual(0, aborted.Reserved);
            Assert.AreEqual(tuning.StartingGold, aborted.Balance);
            Assert.AreEqual(1, aborted.Agents[0].WaivedAppearances);
            Assert.AreEqual(4, aborted.CompanyDate);
            Assert.AreEqual(TournamentResult.None, aborted.Result);
        }

        [Test]
        public void AWaivedCommonIsConsumedWhenTheWindowEnds()
        {
            var tuning = EconomyTuning.Illustration();
            Assert.IsTrue(TournamentEconomyRules.TryEnter(
                Treasury(tuning.StartingGold),
                tuning,
                tuning.CheapEntranceFee,
                new[] { Agent(AgentRarity.Common, 1) },
                out var entered));
            Assert.IsTrue(TournamentEconomyRules.TryAdvance(
                entered,
                tuning.AbortWindowSeconds,
                out var started));

            Assert.AreEqual(tuning.StartingGold - tuning.CheapEntranceFee, started.Balance);
            Assert.AreEqual(0, started.Agents[0].WaivedAppearances);
        }

        [Test]
        public void UnclaimedPrizeStaysOutOfSpendableGold()
        {
            Assert.AreEqual(80, TournamentEconomyRules.Spendable(100, 20));
            Assert.AreEqual(100, TournamentEconomyRules.Spendable(100, 0));
            Assert.AreEqual(40, TournamentEconomyRules.Spendable(40, -5));
            Assert.AreEqual(0, TournamentEconomyRules.Spendable(10, 25));
        }

        [Test]
        public void FirstPlaceAddsThePrize()
        {
            var started = StartedCommon();

            Assert.IsTrue(TournamentEconomyRules.TryApplyResult(
                started,
                TournamentResult.First,
                24,
                out var resolved));

            Assert.AreEqual(TournamentEntryPhase.Resolved, resolved.Phase);
            Assert.AreEqual(TournamentResult.First, resolved.Result);
            Assert.AreEqual(started.Balance + 24, resolved.Balance);
            Assert.AreEqual(5, resolved.CompanyDate);
        }

        [Test]
        public void LastPlaceAddsNoPrizeAndDoesNotChargeAgain()
        {
            var started = StartedCommon();

            Assert.IsTrue(TournamentEconomyRules.TryApplyResult(
                started,
                TournamentResult.Last,
                24,
                out var resolved));

            Assert.AreEqual(TournamentResult.Last, resolved.Result);
            Assert.AreEqual(started.Balance, resolved.Balance);
            Assert.AreEqual(5, resolved.CompanyDate);
        }

        [Test]
        public void ALossAddsNoPrizeAndIsNotAFinish()
        {
            var started = StartedCommon();

            Assert.IsTrue(TournamentEconomyRules.TryApplyResult(
                started,
                TournamentResult.Loss,
                24,
                out var resolved));

            Assert.AreEqual(TournamentResult.Loss, resolved.Result);
            Assert.AreNotEqual(TournamentResult.Last, resolved.Result);
            Assert.AreEqual(started.Balance, resolved.Balance);
            Assert.AreEqual(5, resolved.CompanyDate);
        }

        [Test]
        public void ABenchedAgentIsNotReserved()
        {
            var tuning = EconomyTuning.Illustration();
            var agents = new[]
            {
                Agent(AgentRarity.Common, 0),
                Agent(AgentRarity.Legendary, 0, benched: true),
            };

            Assert.IsTrue(TournamentEconomyRules.TryEnter(
                Treasury(tuning.StartingGold),
                tuning,
                tuning.CheapEntranceFee,
                agents,
                out var entered));

            Assert.AreEqual(6, entered.Reserved);
            Assert.IsTrue(TournamentEconomyRules.TryAdvance(
                entered,
                tuning.AbortWindowSeconds,
                out var started));
            Assert.AreEqual(0, started.Agents[1].WaivedAppearances);
            Assert.IsTrue(started.Agents[1].Benched);
        }

        [Test]
        public void ALegendarySideThatCannotPayIsRefused()
        {
            var tuning = EconomyTuning.Illustration();
            var floor = tuning.Salary + tuning.FanPayCap;
            var treasury = Treasury(floor);
            var side = new[]
            {
                Agent(AgentRarity.Legendary, 0),
                Agent(AgentRarity.Legendary, 0),
                Agent(AgentRarity.Legendary, 0),
                Agent(AgentRarity.Legendary, 0),
                Agent(AgentRarity.Legendary, 0),
            };

            Assert.IsFalse(TournamentEconomyRules.TryEnter(
                treasury,
                tuning,
                tuning.CheapEntranceFee,
                side,
                out _));
            Assert.AreEqual(floor, treasury.Balance);
            Assert.AreEqual(0, treasury.Reserved);
        }

        [Test]
        public void ARosterWithNoFieldedAgentIsRefused()
        {
            var tuning = EconomyTuning.Illustration();

            Assert.IsFalse(TournamentEconomyRules.TryEnter(
                Treasury(tuning.StartingGold),
                tuning,
                tuning.CheapEntranceFee,
                new[] { Agent(AgentRarity.Common, 1, benched: true) },
                out _));
        }

        static TournamentEntryState StartedCommon()
        {
            var tuning = EconomyTuning.Illustration();
            Assert.IsTrue(TournamentEconomyRules.TryEnter(
                Treasury(tuning.StartingGold),
                tuning,
                tuning.CheapEntranceFee,
                new[] { Agent(AgentRarity.Common, 0) },
                out var entered));
            Assert.IsTrue(TournamentEconomyRules.TryAdvance(
                entered,
                tuning.AbortWindowSeconds,
                out var started));
            return started;
        }

        static TournamentEntryState Treasury(int balance)
        {
            return new TournamentEntryState
            {
                Balance = balance,
                CompanyDate = 4,
                Phase = TournamentEntryPhase.Open,
            };
        }

        static FieldedAgent Agent(AgentRarity rarity, int waived, bool benched = false)
        {
            return new FieldedAgent
            {
                Rarity = rarity,
                WaivedAppearances = waived,
                Benched = benched,
            };
        }
    }
}
