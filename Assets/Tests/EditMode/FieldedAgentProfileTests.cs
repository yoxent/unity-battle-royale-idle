using BattleRoyale.GameModes.BattleRoyale;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class FieldedAgentProfileTests
    {
        [Test]
        public void RollsReplaceTheArchetypeChartWithoutWritingTheAsset()
        {
            var archetype = ScriptableObject.CreateInstance<BattleRoyaleAgentProfile>();
            archetype.stats = new BattleRoyaleAgentStats { accuracy = 0.9f };
            var before = archetype.stats.accuracy;

            var profile = FieldedAgentProfile.Create(
                archetype,
                new[] { 41, 12, 60, 3, 18, 7 });

            Assert.AreEqual(before, archetype.stats.accuracy);
            Assert.AreEqual(0.41f, profile.stats.accuracy);
            Assert.AreEqual(0.12f, profile.stats.awareness);
            Assert.AreEqual(0.6f, profile.stats.resilience);
            Assert.AreEqual(0.03f, profile.stats.athleticism);
            Assert.AreEqual(0.18f, profile.stats.endurance);
            Assert.AreEqual(0.07f, profile.stats.discipline);
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(archetype);
        }
    }
}
