using BattleRoyale.CameraSystems;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class SimulationCameraFramingTests
    {
        [Test]
        public void OrthographicSize_GrowsForAWiderMap()
        {
            var narrow = SimulationCameraFraming.OrthographicSize(
                100f, 80f, 1f, 0f, 6f, 2000f);
            var wide = SimulationCameraFraming.OrthographicSize(
                300f, 80f, 1f, 0f, 6f, 2000f);

            Assert.Greater(wide, narrow);
        }

        [Test]
        public void OrthographicSize_Clamps()
        {
            var size = SimulationCameraFraming.OrthographicSize(
                10f, 10f, 1f, 0f, 6f, 2000f);

            Assert.AreEqual(6f, size);
        }

        [Test]
        public void CameraHeight_UsesBaseScaleAndOffset()
        {
            Assert.AreEqual(
                41f,
                SimulationCameraFraming.CameraHeight(5f, 20f, 16f));
        }
    }
}
