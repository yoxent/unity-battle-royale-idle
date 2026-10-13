using BattleRoyale.GameModes.BattleRoyale;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class BattleRoyaleRulesTests
    {
        [Test]
        public void SlotsShareATeamWhenTheSideHasMoreThanOnePlayer()
        {
            Assert.AreEqual(0, BattleRoyaleRules.TeamIndexForSlot(0, 1));
            Assert.AreEqual(3, BattleRoyaleRules.TeamIndexForSlot(3, 1));
            Assert.AreEqual(0, BattleRoyaleRules.TeamIndexForSlot(0, 3));
            Assert.AreEqual(0, BattleRoyaleRules.TeamIndexForSlot(2, 3));
            Assert.AreEqual(1, BattleRoyaleRules.TeamIndexForSlot(3, 3));
            Assert.AreEqual(1, BattleRoyaleRules.TeamIndexForSlot(5, 3));
        }

        [Test]
        public void TheMatchEndsWhenOneSideIsStillAlive()
        {
            Assert.IsFalse(BattleRoyaleRules.IsMatchOver(new[] { 1, 1, 1, 1 }));
            Assert.AreEqual(-1, BattleRoyaleRules.SoleSurvivor(new[] { 1, 1 }));

            Assert.IsTrue(BattleRoyaleRules.IsMatchOver(new[] { 0, 3, 0 }));
            Assert.AreEqual(1, BattleRoyaleRules.SoleSurvivor(new[] { 0, 3, 0 }));

            Assert.IsTrue(BattleRoyaleRules.IsMatchOver(new[] { 0, 0 }));
            Assert.AreEqual(-1, BattleRoyaleRules.SoleSurvivor(new[] { 0, 0 }));
            Assert.IsFalse(BattleRoyaleRules.IsMatchOver(null));
            Assert.AreEqual(-1, BattleRoyaleRules.SoleSurvivor(null));
        }
    }
}
