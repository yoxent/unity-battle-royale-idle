using BattleRoyale.Company;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class SideColorRulesTests
    {
        [Test]
        public void AnUnsetPrimaryKeepsThePalette()
        {
            var emblem = new FieldEmblem();

            Assert.IsFalse(SideColorRules.TryPrimary(emblem, out _));
            var side = SideColorRules.ForSide(emblem, 0);
            var other = SideColorRules.ForSide(emblem, 1);

            Assert.AreEqual(0.2f, side.Red, 0.0001f);
            Assert.AreEqual(0.45f, side.Green, 0.0001f);
            Assert.AreEqual(1f, side.Blue, 0.0001f);
            Assert.AreEqual(1f, other.Red, 0.0001f);
            Assert.AreEqual(0.45f, other.Green, 0.0001f);
            Assert.AreEqual(0.15f, other.Blue, 0.0001f);
        }

        [Test]
        public void APrimaryReplacesThePalette()
        {
            var emblem = new FieldEmblem
            {
                Primary = new FieldColor { Red = 0.72f, Green = 0.2f, Blue = 0.14f },
            };

            Assert.IsTrue(SideColorRules.TryPrimary(emblem, out var primary));
            var side = SideColorRules.ForSide(emblem, 1);

            Assert.AreEqual(0.72f, primary.Red, 0.0001f);
            Assert.AreEqual(side.Red, primary.Red, 0.0001f);
            Assert.AreEqual(side.Green, primary.Green, 0.0001f);
            Assert.AreEqual(side.Blue, primary.Blue, 0.0001f);
        }
    }
}
