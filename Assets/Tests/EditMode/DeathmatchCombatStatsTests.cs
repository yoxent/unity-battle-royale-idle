using NUnit.Framework;
using BattleRoyale.GameModes.Deathmatch;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class DeathmatchCombatStatsTests
    {
        [Test]
        public void DamagingHitSetsShotsHitsAndAccuracy()
        {
            var stats = TwoAgents();
            stats.RecordShot(0);
            stats.RecordHit(0, 1, 20f, 1f);

            var shooter = stats.AgentAt(0);
            Assert.AreEqual(1, shooter.ShotsFired);
            Assert.AreEqual(1, shooter.Hits);
            Assert.AreEqual(1f, shooter.Accuracy, 0.001f);
            Assert.AreEqual(20f, shooter.DamageDealt, 0.001f);
        }

        [Test]
        public void AMissLeavesAccuracyAtZero()
        {
            var stats = TwoAgents();
            stats.RecordShot(0);

            var shooter = stats.AgentAt(0);
            Assert.AreEqual(1, shooter.ShotsFired);
            Assert.AreEqual(0, shooter.Hits);
            Assert.AreEqual(0f, shooter.Accuracy, 0.001f);
            Assert.AreEqual(0, DeathmatchCombatStats.AccuracyPercent(0, 1));
        }

        [Test]
        public void HealthRemovedIsCappedAtTheHealthLeft()
        {
            Assert.AreEqual(
                25f,
                DeathmatchCombatStats.HealthRemoved(25f, 40f),
                0.001f);
            Assert.AreEqual(
                10f,
                DeathmatchCombatStats.HealthRemoved(40f, 10f),
                0.001f);
            Assert.AreEqual(
                0f,
                DeathmatchCombatStats.HealthRemoved(0f, 10f),
                0.001f);
        }

        [Test]
        public void EliminationAddsOneKillAndOneDeath()
        {
            var stats = TwoAgents();
            stats.RecordElimination(1, 0, 3f);

            Assert.AreEqual(1, stats.AgentAt(0).Kills);
            Assert.AreEqual(1, stats.AgentAt(1).Deaths);
            Assert.AreEqual("Gunner eliminated Scout", stats.LatestEvent.Phrase);
        }

        [Test]
        public void AssistAtFiveSecondsCountsOnce()
        {
            var stats = ThreeAgents();
            stats.RecordHit(2, 1, 10f, 0f);
            stats.RecordHit(2, 1, 5f, 1f);
            stats.RecordElimination(1, 0, 5f);

            Assert.AreEqual(1, stats.AgentAt(2).Assists);
            Assert.AreEqual(0, stats.AgentAt(0).Assists);
        }

        [Test]
        public void KillerDoesNotGetAnAssist()
        {
            var stats = TwoAgents();
            stats.RecordHit(0, 1, 15f, 1f);
            stats.RecordElimination(1, 0, 2f);

            Assert.AreEqual(1, stats.AgentAt(0).Kills);
            Assert.AreEqual(0, stats.AgentAt(0).Assists);
        }

        [Test]
        public void DamageOlderThanFiveSecondsDoesNotAssist()
        {
            var stats = ThreeAgents();
            stats.RecordHit(2, 1, 10f, 0f);
            stats.RecordElimination(1, 0, 5.01f);

            Assert.AreEqual(0, stats.AgentAt(2).Assists);
        }

        [Test]
        public void DeathWithoutAKillerAwardsNothingElse()
        {
            var stats = ThreeAgents();
            stats.RecordHit(2, 1, 10f, 4f);
            stats.RecordElimination(1, -1, 4.5f);

            Assert.AreEqual(1, stats.AgentAt(1).Deaths);
            Assert.AreEqual(0, stats.AgentAt(0).Kills);
            Assert.AreEqual(0, stats.AgentAt(2).Assists);
            Assert.AreEqual("Scout died", stats.LatestEvent.Phrase);
            Assert.IsFalse(stats.LatestEvent.HasActor);
        }

        [Test]
        public void BlankDisplayNameIsUnknown()
        {
            var stats = new DeathmatchCombatStats();
            stats.RegisterAgent(0, 0, "  ");

            Assert.AreEqual("Unknown", stats.AgentAt(0).DisplayName);
        }

        [Test]
        public void ClockFormatsMinutesAndSeconds()
        {
            Assert.AreEqual("04:12", DeathmatchCombatStats.FormatClock(252f));
        }

        static DeathmatchCombatStats TwoAgents()
        {
            var stats = new DeathmatchCombatStats();
            stats.RegisterAgent(0, 1, "Gunner");
            stats.RegisterAgent(1, 0, "Scout");
            return stats;
        }

        static DeathmatchCombatStats ThreeAgents()
        {
            var stats = TwoAgents();
            stats.RegisterAgent(2, 1, "Hunter");
            return stats;
        }
    }
}
