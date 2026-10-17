using BattleRoyale.Company;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class CompanyRankRulesTests
    {
        [Test]
        public void AWinAddsPointsAndALossRemovesThem()
        {
            Assert.AreEqual(25, CompanyRankRules.Apply(0, true));
            Assert.AreEqual(10, CompanyRankRules.Apply(25, false));
        }

        [Test]
        public void PointsDoNotDropBelowZero()
        {
            Assert.AreEqual(0, CompanyRankRules.Apply(10, false));
        }

        [Test]
        public void TheTierChangesWhenThePointTotalCrossesABand()
        {
            Assert.AreEqual("Initiate", CompanyRankRules.Tier(0));
            Assert.AreEqual("Initiate", CompanyRankRules.Tier(99));
            Assert.AreEqual("Contender", CompanyRankRules.Tier(100));
            Assert.AreEqual("Rival", CompanyRankRules.Tier(250));
            Assert.AreEqual("Elite", CompanyRankRules.Tier(450));
            Assert.AreEqual("Legend", CompanyRankRules.Tier(700));
        }
    }
}
