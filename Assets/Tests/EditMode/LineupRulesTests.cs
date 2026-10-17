using BattleRoyale.Company;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class LineupRulesTests
    {
        [Test]
        public void AutoFillTakesTheHighestFreeScores()
        {
            var scores = new[] { 10, 80, 40, 90, 20, 70 };
            var busy = new[] { false, true, false, false, false, false };

            Assert.IsTrue(LineupRules.TryAutoFill(scores, busy, 3, out var indexes));

            Assert.AreEqual(3, indexes[0]);
            Assert.AreEqual(5, indexes[1]);
            Assert.AreEqual(2, indexes[2]);
        }

        [Test]
        public void AutoFillKeepsTheEarlierAgentWhenScoresMatch()
        {
            var scores = new[] { 50, 50, 10 };
            var busy = new[] { false, false, false };

            Assert.IsTrue(LineupRules.TryAutoFill(scores, busy, 2, out var indexes));

            Assert.AreEqual(0, indexes[0]);
            Assert.AreEqual(1, indexes[1]);
        }

        [Test]
        public void ToggleFillsTheNextEmptySlotAndClearsAChosenAgent()
        {
            var slots = new[] { 1, -1 };

            Assert.IsTrue(LineupRules.TryToggle(slots, 0, false, 2, out var filled));
            Assert.AreEqual(1, filled[0]);
            Assert.AreEqual(0, filled[1]);

            Assert.IsTrue(LineupRules.TryToggle(filled, 0, false, 2, out var cleared));
            Assert.AreEqual(1, cleared[0]);
            Assert.AreEqual(-1, cleared[1]);
        }

        [Test]
        public void AFullLineupRefusesAnotherAgent()
        {
            var slots = new[] { 0, 1 };

            Assert.IsFalse(LineupRules.TryToggle(slots, 2, false, 2, out _));
        }

        [Test]
        public void ACompleteLineupNeedsEverySlotFreeAndDistinct()
        {
            var busy = new[] { false, false, true };

            Assert.IsTrue(LineupRules.IsComplete(new[] { 0, 1 }, 3, busy));
            Assert.IsFalse(LineupRules.IsComplete(new[] { 0, -1 }, 3, busy));
            Assert.IsFalse(LineupRules.IsComplete(new[] { 0, 2 }, 3, busy));
            Assert.IsFalse(LineupRules.IsComplete(new[] { 0, 0 }, 3, busy));
        }

        [Test]
        public void ReleaseShiftsLaterSlotsAndClearsTheRemovedAgent()
        {
            var slots = new[] { 0, 2, 4 };

            LineupRules.RemapRelease(slots, 2);

            Assert.AreEqual(0, slots[0]);
            Assert.AreEqual(-1, slots[1]);
            Assert.AreEqual(3, slots[2]);
        }
    }
}
