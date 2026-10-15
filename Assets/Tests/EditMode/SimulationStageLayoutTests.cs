using BattleRoyale.Maps;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class SimulationStageLayoutTests
    {
        [Test]
        public void Centers_SeparateByWidthPlusGap()
        {
            var centers = SimulationStageLayout.Centers(200f, 40f);

            Assert.AreEqual(3, centers.Length);
            Assert.AreEqual(240f, centers[1].x - centers[0].x, 0.001f);
            Assert.AreEqual(240f, centers[2].x - centers[1].x, 0.001f);
        }

        [Test]
        public void PlaceBarrier_SitsInTheGapAndCoversDepth()
        {
            const float largestWidth = 200f;
            const float gap = 40f;
            var centers = SimulationStageLayout.Centers(largestWidth, gap);

            var host = new GameObject("Barrier");
            var barrier = host.AddComponent<BoxCollider>();
            barrier.isTrigger = true;

            SimulationStageLayout.PlaceBarrier(
                barrier,
                new Vector3(0f, 0f, 0f),
                new Vector3(240f, 0f, 0f),
                4f,
                180f,
                0f,
                8f);

            Assert.IsFalse(barrier.isTrigger);
            Assert.AreEqual(120f, barrier.transform.position.x, 0.001f);
            Assert.GreaterOrEqual(barrier.transform.localScale.z, 180f);
            Assert.GreaterOrEqual(barrier.transform.localScale.y, 8f);

            Physics.SyncTransforms();
            var origin = centers[0] + new Vector3(largestWidth * 0.5f, 1.5f, 0f);
            Assert.IsTrue(Physics.Raycast(origin, Vector3.right, out var hit));
            Assert.AreSame(barrier, hit.collider);
            Assert.Less(hit.point.x, centers[1].x - largestWidth * 0.5f);

            Object.DestroyImmediate(host);
        }
    }
}
