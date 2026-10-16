using BattleRoyale.Company;
using BattleRoyale.GameModes;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class SideStanceRulesTests
    {
        [Test]
        public void AnOpponentKeepsTheStanceOnItsSnapshot()
        {
            var stance = SideStanceRules.Resolve(
                hasSidePlaystyle: true,
                TeamPlaystyle.Defensive,
                TeamPlaystyle.Aggressive);

            Assert.AreEqual(TeamPlaystyle.Defensive, stance);
        }

        [Test]
        public void APrototypeAgentUsesTheMatchStance()
        {
            var stance = SideStanceRules.Resolve(
                hasSidePlaystyle: false,
                TeamPlaystyle.Defensive,
                TeamPlaystyle.Aggressive);

            Assert.AreEqual(TeamPlaystyle.Aggressive, stance);
        }

        [Test]
        public void TheCoachDirectiveReachesOnlyThePlayerSide()
        {
            Assert.IsTrue(SideStanceRules.ReceivesCoachDirective(
                true,
                teamIndex: 0,
                playerTeamIndex: 0));
            Assert.IsFalse(SideStanceRules.ReceivesCoachDirective(
                true,
                teamIndex: 1,
                playerTeamIndex: 0));
            Assert.IsTrue(SideStanceRules.ReceivesCoachDirective(
                false,
                teamIndex: 1,
                playerTeamIndex: 0));
        }
    }
}
