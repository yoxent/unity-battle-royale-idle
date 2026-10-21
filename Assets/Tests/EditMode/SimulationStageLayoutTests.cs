using BattleRoyale.Maps;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class SimulationStageLayoutTests
    {
        [Test]
        public void Centers_SeparateByWidthPlusGap()
        {
            var centers = SimulationStageLayout.Centers(200f, 40f);

            Assert.AreEqual(3, centers.Length);
            Assert.AreEqual(240f, centers[0].x, 0.001f);
            Assert.AreEqual(0f, centers[0].y, 0.001f);
            Assert.AreEqual(0f, centers[0].z, 0.001f);
            Assert.AreEqual(240f, centers[1].x - centers[0].x, 0.001f);
            Assert.AreEqual(240f, centers[2].x - centers[1].x, 0.001f);
        }

        [Test]
        public void PreviewSitsOffTheSlotRow()
        {
            var preview = SimulationStageLayout.PreviewCenter(200f, 40f);

            Assert.AreEqual(0f, preview.x, 0.001f);
            Assert.AreEqual(0f, preview.y, 0.001f);
            Assert.AreEqual(-240f, preview.z, 0.001f);
        }
    }
}
