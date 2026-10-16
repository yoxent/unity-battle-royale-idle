using System.Collections.Generic;
using BattleRoyale.GameModes;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class LivingSideRulesTests
    {
        [Test]
        public void IsDecided_WhenFewerThanTwoSidesRemain()
        {
            Assert.IsFalse(LivingSideRules.IsDecided(new[] { 2, 1 }));
            Assert.IsTrue(LivingSideRules.IsDecided(new[] { 3, 0 }));
            Assert.IsTrue(LivingSideRules.IsDecided(new[] { 0, 0 }));
            Assert.IsFalse(LivingSideRules.IsDecided(null));
        }

        [Test]
        public void SoleSurvivor_ReturnsTheOnlyLivingSide()
        {
            Assert.AreEqual(0, LivingSideRules.SoleSurvivor(new[] { 2, 0, 0 }));
            Assert.AreEqual(-1, LivingSideRules.SoleSurvivor(new[] { 1, 1 }));
            Assert.AreEqual(-1, LivingSideRules.SoleSurvivor(new[] { 0, 0 }));
        }
    }
}
