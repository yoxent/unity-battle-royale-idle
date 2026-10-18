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

        [Test]
        public void ABandNamesTheNextTierAndThePointsRemaining()
        {
            var start = CompanyRankRules.Band(0);
            Assert.AreEqual("Initiate", start.Tier);
            Assert.AreEqual("Contender", start.NextTier);
            Assert.AreEqual(100, start.Remaining);
            Assert.AreEqual(0f, start.Fill, 0.0001f);

            var edge = CompanyRankRules.Band(99);
            Assert.AreEqual("Initiate", edge.Tier);
            Assert.AreEqual(1, edge.Remaining);

            var contender = CompanyRankRules.Band(100);
            Assert.AreEqual("Contender", contender.Tier);
            Assert.AreEqual("Rival", contender.NextTier);
            Assert.AreEqual(0f, contender.Fill, 0.0001f);

            Assert.AreEqual("Rival", CompanyRankRules.Band(250).Tier);
            Assert.AreEqual("Elite", CompanyRankRules.Band(250).NextTier);
            Assert.AreEqual("Elite", CompanyRankRules.Band(450).Tier);
            Assert.AreEqual("Legend", CompanyRankRules.Band(699).NextTier);

            var legend = CompanyRankRules.Band(700);
            Assert.AreEqual("Legend", legend.Tier);
            Assert.IsFalse(legend.HasNext);
            Assert.AreEqual(0, legend.Remaining);
            Assert.AreEqual(1f, legend.Fill, 0.0001f);

            var below = CompanyRankRules.Band(-4);
            Assert.AreEqual(0, below.Points);
            Assert.AreEqual("Initiate", below.Tier);
            Assert.AreEqual(100, below.Remaining);
        }
    }
}
