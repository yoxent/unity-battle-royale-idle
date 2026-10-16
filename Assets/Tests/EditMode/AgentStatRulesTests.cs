using BattleRoyale.Company;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class AgentStatRulesTests
    {
        [Test]
        public void HealthIsOneSharedPool()
        {
            Assert.AreEqual(100f, AgentStatRules.SharedHealth);
        }

        [Test]
        public void AthleticismMapsMoveSpeedFromFiveToTwelve()
        {
            Assert.AreEqual(5f, AgentStatRules.MoveSpeed(0));
            Assert.AreEqual(8.5f, AgentStatRules.MoveSpeed(50), 0.0001f);
            Assert.AreEqual(12f, AgentStatRules.MoveSpeed(100));
            Assert.AreEqual(5f, AgentStatRules.MoveSpeed(-10));
            Assert.AreEqual(12f, AgentStatRules.MoveSpeed(150));
        }

        [Test]
        public void AwarenessMapsPerceptionFromFiftyToOneTwenty()
        {
            Assert.AreEqual(50f, AgentStatRules.Perception(0));
            Assert.AreEqual(85f, AgentStatRules.Perception(50), 0.0001f);
            Assert.AreEqual(120f, AgentStatRules.Perception(100));
            Assert.AreEqual(50f, AgentStatRules.Perception(-10));
            Assert.AreEqual(120f, AgentStatRules.Perception(150));
        }

        [Test]
        public void AccuracyEnduranceAndDisciplineAreTheRollDividedByOneHundred()
        {
            Assert.AreEqual(0f, AgentStatRules.Rating(0));
            Assert.AreEqual(0.5f, AgentStatRules.Rating(50), 0.0001f);
            Assert.AreEqual(1f, AgentStatRules.Rating(100));
            Assert.AreEqual(0f, AgentStatRules.Rating(-10));
            Assert.AreEqual(1f, AgentStatRules.Rating(150));
        }

        [Test]
        public void HigherResilienceTakesLessBulletDamageAndStillTakesSome()
        {
            var low = AgentStatRules.BulletDamage(10f, 0);
            var high = AgentStatRules.BulletDamage(10f, 100);

            Assert.AreEqual(10f, low);
            Assert.Less(high, low);
            Assert.Greater(high, 0f);
            Assert.AreEqual(0f, AgentStatRules.BulletDamage(0f, 100));
            Assert.AreEqual(0f, AgentStatRules.BulletDamage(-5f, 0));
        }

        [Test]
        public void ResilienceDoesNotReduceHazardOrSafeZoneDamage()
        {
            Assert.AreEqual(10f, AgentStatRules.HazardDamage(10f, 0));
            Assert.Less(AgentStatRules.BulletDamage(10f, 100), 10f);
        }

        [Test]
        public void HigherEnduranceTakesLessHazardDamageAndStillTakesSome()
        {
            var low = AgentStatRules.HazardDamage(10f, 0);
            var high = AgentStatRules.HazardDamage(10f, 100);

            Assert.AreEqual(10f, low);
            Assert.Less(high, low);
            Assert.Greater(high, 0f);
        }

        [Test]
        public void HigherEnduranceShortensHoldSettleTimeWithoutRemovingIt()
        {
            var low = AgentStatRules.HoldSettleSeconds(6f, 0);
            var high = AgentStatRules.HoldSettleSeconds(6f, 100);

            Assert.AreEqual(6f, low);
            Assert.Less(high, low);
            Assert.Greater(high, 0f);
            Assert.AreEqual(0f, AgentStatRules.HoldSettleSeconds(0f, 100));
        }

        [Test]
        public void DisciplineScalesTheStanceOverrideFromNoneToFull()
        {
            Assert.AreEqual(0f, AgentStatRules.StanceWeight(0));
            Assert.AreEqual(0.5f, AgentStatRules.StanceWeight(50), 0.0001f);
            Assert.AreEqual(1f, AgentStatRules.StanceWeight(100));
            Assert.Greater(
                AgentStatRules.StanceWeight(80),
                AgentStatRules.StanceWeight(20));
        }
    }
}
