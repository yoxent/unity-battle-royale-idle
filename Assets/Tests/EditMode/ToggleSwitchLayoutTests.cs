using BattleRoyale.Settings;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class ToggleSwitchLayoutTests
    {
        [Test]
        public void OnPutsTheKnobOnTheRight()
        {
            float knobX = ToggleSwitchLayout.KnobX(128f, 46f, 4f, true);

            Assert.Greater(knobX, 0f);
        }

        [Test]
        public void OffPutsTheKnobOnTheLeft()
        {
            float knobX = ToggleSwitchLayout.KnobX(128f, 46f, 4f, false);

            Assert.Less(knobX, 0f);
        }

        [Test]
        public void OnAndOffMirrorEachOther()
        {
            float on = ToggleSwitchLayout.KnobX(128f, 46f, 4f, true);
            float off = ToggleSwitchLayout.KnobX(128f, 46f, 4f, false);

            Assert.AreEqual(on, -off, 0.001f);
        }
    }
}
