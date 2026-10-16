using BattleRoyale.Company;
using BattleRoyale.GameModes.BattleRoyale;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class BattleRoyaleDerivedStatTests
    {
        [Test]
        public void JuggernautMitigatesBulletsAndDoesNotRaiseHealth()
        {
            var juggernaut = BattleRoyaleAgentSpecialtyRules.GetModifiers(
                BattleRoyaleAgentSpecialty.Juggernaut);
            var balanced = BattleRoyaleAgentSpecialtyRules.GetModifiers(
                BattleRoyaleAgentSpecialty.Balanced);

            Assert.Less(juggernaut.damageTakenMultiplier, balanced.damageTakenMultiplier);
            Assert.Greater(juggernaut.damageTakenMultiplier, 0f);
            Assert.AreEqual(2f / 3f, juggernaut.damageTakenMultiplier, 0.0001f);
            Assert.Less(juggernaut.moveSpeedMultiplier, 1f);
            Assert.AreEqual(
                AgentStatRules.SharedHealth,
                BattleRoyaleCombatStats.MaxHealth(new BattleRoyaleAgentStats()));
        }

        [Test]
        public void HighResilienceIsJuggernautWithoutASeparateHealthPool()
        {
            var stats = new BattleRoyaleAgentStats
            {
                resilience = 1f,
                endurance = 0.2f
            };

            var evaluation = BattleRoyaleAgentSpecialtyRules.Evaluate(stats);

            Assert.AreEqual(
                BattleRoyaleAgentSpecialty.Juggernaut,
                evaluation.Specialty);
        }

        [Test]
        public void SpeedAndPerceptionComeFromTheRatings()
        {
            var stats = new BattleRoyaleAgentStats
            {
                athleticism = 1f,
                awareness = 1f
            };
            var modifiers = new BattleRoyaleSpecialtyModifiers();

            Assert.AreEqual(12f, BattleRoyaleCombatStats.MoveSpeed(stats, modifiers, false));
            Assert.AreEqual(120f, BattleRoyaleCombatStats.Perception(stats, modifiers));
        }

        [Test]
        public void BulletDamageUsesResilienceAndJuggernautDoesNotRemoveIt()
        {
            var stats = new BattleRoyaleAgentStats { resilience = 1f };
            var taken = BattleRoyaleCombatStats.BulletDamage(10f, stats, 2f / 3f);

            Assert.Greater(taken, 0f);
            Assert.Less(taken, 10f);
            Assert.AreEqual(
                AgentStatRules.BulletDamage(10f, 100) * (2f / 3f),
                taken,
                0.0001f);
        }

        [Test]
        public void HazardDamageUsesEnduranceAndIgnoresResilience()
        {
            var resilient = new BattleRoyaleAgentStats
            {
                resilience = 1f,
                endurance = 0f
            };
            var enduring = new BattleRoyaleAgentStats
            {
                resilience = 0f,
                endurance = 1f
            };

            Assert.AreEqual(
                10f,
                BattleRoyaleCombatStats.HazardDamage(10f, resilient, 1f));
            Assert.Less(
                BattleRoyaleCombatStats.HazardDamage(10f, enduring, 1f),
                10f);
            Assert.Greater(
                BattleRoyaleCombatStats.HazardDamage(10f, enduring, 1f),
                0f);
        }
    }
}
