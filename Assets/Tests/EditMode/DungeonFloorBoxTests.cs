using BattleRoyale.Maps;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class DungeonFloorBoxTests
    {
        [Test]
        public void BoxCoversTheVisualCornersAndKeepsItsTopAtTheFloor()
        {
            DungeonFloorBox.GetCenterAndSize(
                new Vector3(40f, 0f, 0f),
                new Vector3(0f, 3f, 10f),
                new Vector3(40f, 1f, 10f),
                new Vector3(0f, 0f, 0f),
                5f,
                DungeonFloorBox.Thickness,
                out var center,
                out var size);

            Assert.AreEqual(20f, center.x);
            Assert.AreEqual(5f, center.z);
            Assert.AreEqual(40f, size.x);
            Assert.AreEqual(10f, size.z);
            Assert.AreEqual(DungeonFloorBox.Thickness, size.y);
            Assert.AreEqual(5f, center.y + size.y * 0.5f, 0.0001f);
        }
    }
}
