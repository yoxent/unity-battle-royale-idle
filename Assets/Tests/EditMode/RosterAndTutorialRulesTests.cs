using BattleRoyale.Company;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class RosterAndTutorialRulesTests
    {
        [Test]
        public void FieldingTakesTheFirstFiveFreeAgents()
        {
            var busy = new[] { true, true, false, false, false, false, false, false };

            Assert.IsTrue(RosterFieldRules.TryTakeFree(busy, 5, out var indexes));

            Assert.AreEqual(new[] { 2, 3, 4, 5, 6 }, indexes);
        }

        [Test]
        public void FieldingFailsBelowFiveFreeAgents()
        {
            var busy = new[] { false, false, false, false, true };
            Assert.IsFalse(RosterFieldRules.TryTakeFree(busy, 5, out _));
        }

        [Test]
        public void DeployEndsTheTourEvenWhenHireWasSkipped()
        {
            Assert.AreEqual(
                TutorialStep.Done,
                TutorialRules.Advance(TutorialStep.Recruit, TutorialAction.Deploy));
        }

        [Test]
        public void OpeningRecruitThenHiringAdvancesTheTour()
        {
            var hire = TutorialRules.Advance(TutorialStep.Recruit, TutorialAction.OpenRecruit);
            var deploy = TutorialRules.Advance(hire, TutorialAction.HireSucceeded);

            Assert.AreEqual(TutorialStep.Hire, hire);
            Assert.AreEqual(TutorialStep.Deploy, deploy);
        }
    }
}
