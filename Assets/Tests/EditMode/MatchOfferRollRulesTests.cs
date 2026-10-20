using System;
using BattleRoyale.Company;
using BattleRoyale.Maps;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class MatchOfferRollRulesTests
    {
        [Test]
        public void OnlyDeathmatchAndBattleRoyaleAreListed()
        {
            Assert.IsTrue(MatchOfferRollRules.CanList(MapGameMode.Deathmatch));
            Assert.IsTrue(MatchOfferRollRules.CanList(MapGameMode.BattleRoyale));
            Assert.IsFalse(MatchOfferRollRules.CanList(MapGameMode.Elimination));
            Assert.IsFalse(MatchOfferRollRules.CanList(MapGameMode.CaptureTheFlag));
        }

        [Test]
        public void EachSlotDrawsOneAvailableMode()
        {
            var slots = new int[4];
            Assert.IsTrue(MatchOfferRollRules.TryRoll(3, slots, new Random(5)));
            for (var i = 0; i < slots.Length; i++)
            {
                Assert.GreaterOrEqual(slots[i], 0);
                Assert.Less(slots[i], 3);
            }
        }

        [Test]
        public void TheSameSeedDrawsTheSameSlots()
        {
            var first = new int[4];
            var second = new int[4];
            Assert.IsTrue(MatchOfferRollRules.TryRoll(5, first, new Random(11)));
            Assert.IsTrue(MatchOfferRollRules.TryRoll(5, second, new Random(11)));
            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void AnEmptyPoolDoesNotFillSlots()
        {
            var slots = new[] { 2, 2 };
            Assert.IsFalse(MatchOfferRollRules.TryRoll(0, slots, new Random(1)));
            Assert.AreEqual(-1, slots[0]);
            Assert.AreEqual(-1, slots[1]);
            Assert.IsFalse(MatchOfferRollRules.TryRoll(2, slots, null));
            Assert.AreEqual(-1, slots[0]);
        }
    }
}
