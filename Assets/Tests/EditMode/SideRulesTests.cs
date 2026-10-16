using BattleRoyale.GameModes;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class SideRulesTests
    {
        [Test]
        public void RequiredTeamCount_RaisesValuesBelowTwo()
        {
            Assert.AreEqual(2, SideRules.RequiredTeamCount(0));
            Assert.AreEqual(2, SideRules.RequiredTeamCount(1));
            Assert.AreEqual(4, SideRules.RequiredTeamCount(4));
        }

        [Test]
        public void SharesTeam_MatchesOnlyARealSharedIndex()
        {
            Assert.IsTrue(SideRules.SharesTeam(1, 1));
            Assert.IsFalse(SideRules.SharesTeam(0, 1));
            Assert.IsFalse(SideRules.SharesTeam(-1, -1));
        }
    }
}
