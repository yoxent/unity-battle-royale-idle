using BattleRoyale.Company;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class ActiveMatchListRulesTests
    {
        [Test]
        public void NoEnteredCardLeavesEveryCardInTheOpenGroup()
        {
            var order = new int[4];

            var active = ActiveMatchListRules.Arrange(
                TournamentEntryPhase.Running,
                -1,
                4,
                order);

            Assert.AreEqual(0, active);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, order);
            Assert.AreEqual(MatchCardState.None, ActiveMatchListRules.StateOf(
                TournamentEntryPhase.Running,
                -1,
                0));
            Assert.IsFalse(ActiveMatchListRules.IsActive(MatchCardState.None));
        }

        [Test]
        public void AnAbortedEntryLeavesNoActiveCard()
        {
            var order = new int[4];

            var active = ActiveMatchListRules.Arrange(
                TournamentEntryPhase.Open,
                -1,
                4,
                order);

            Assert.AreEqual(0, active);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, order);
        }

        [Test]
        public void OnlyTheEnteredCardIsActive()
        {
            Assert.AreEqual(
                MatchCardState.Live,
                ActiveMatchListRules.StateOf(TournamentEntryPhase.Running, 2, 2));
            Assert.AreEqual(
                MatchCardState.None,
                ActiveMatchListRules.StateOf(TournamentEntryPhase.Running, 2, 0));
            Assert.AreEqual(
                MatchCardState.None,
                ActiveMatchListRules.StateOf(TournamentEntryPhase.Running, 2, 3));
        }

        [Test]
        public void TheEntryCardReportsEachPhase()
        {
            Assert.AreEqual(
                MatchCardState.Deploying,
                ActiveMatchListRules.StateOf(TournamentEntryPhase.Window, 1, 1));
            Assert.AreEqual(
                MatchCardState.Live,
                ActiveMatchListRules.StateOf(TournamentEntryPhase.Running, 1, 1));
            Assert.AreEqual(
                MatchCardState.Finished,
                ActiveMatchListRules.StateOf(TournamentEntryPhase.Resolved, 1, 1));
            Assert.AreEqual(
                MatchCardState.None,
                ActiveMatchListRules.StateOf(TournamentEntryPhase.Open, 1, 1));
        }

        [Test]
        public void TheActiveCardMovesToTheTop()
        {
            var order = new int[4];

            var active = ActiveMatchListRules.Arrange(
                TournamentEntryPhase.Running,
                2,
                4,
                order);

            Assert.AreEqual(1, active, "The divider sits before the second card.");
            CollectionAssert.AreEqual(new[] { 2, 0, 1, 3 }, order);
        }

        [Test]
        public void AnActiveCardAlreadyFirstKeepsTheOrder()
        {
            var order = new int[4];

            var active = ActiveMatchListRules.Arrange(
                TournamentEntryPhase.Window,
                0,
                4,
                order);

            Assert.AreEqual(1, active);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, order);
        }

        [Test]
        public void ArrangeRefusesAPoolThatCannotHoldEveryCard()
        {
            Assert.AreEqual(
                -1,
                ActiveMatchListRules.Arrange(
                    TournamentEntryPhase.Running,
                    2,
                    4,
                    new int[3]));
            Assert.AreEqual(
                -1,
                ActiveMatchListRules.Arrange(
                    TournamentEntryPhase.Running,
                    2,
                    4,
                    null));
        }

        [Test]
        public void ArrangeAcceptsNoCards()
        {
            var order = new int[1];

            var active = ActiveMatchListRules.Arrange(
                TournamentEntryPhase.Running,
                0,
                0,
                order);

            Assert.AreEqual(0, active);
        }

        [Test]
        public void OnlyLiveAndFinishedCardsUseTheMatchColumn()
        {
            Assert.IsTrue(ActiveMatchListRules.ShowsLiveMatch(MatchCardState.Live));
            Assert.IsTrue(ActiveMatchListRules.ShowsLiveMatch(MatchCardState.Finished));
            Assert.IsFalse(ActiveMatchListRules.ShowsLiveMatch(MatchCardState.Deploying));
            Assert.IsFalse(ActiveMatchListRules.ShowsLiveMatch(MatchCardState.None));
        }

        [Test]
        public void TheLabelNamesTheState()
        {
            Assert.AreEqual(
                "Deploying \u00b7 2s",
                ActiveMatchListRules.Label(MatchCardState.Deploying, 2));
            Assert.AreEqual(
                "Deploying",
                ActiveMatchListRules.Label(MatchCardState.Deploying, 0));
            Assert.AreEqual("Live", ActiveMatchListRules.Label(MatchCardState.Live, 0));
            Assert.AreEqual("Result", ActiveMatchListRules.Label(MatchCardState.Finished, 0));
            Assert.AreEqual("", ActiveMatchListRules.Label(MatchCardState.None, 0));
        }
    }
}
