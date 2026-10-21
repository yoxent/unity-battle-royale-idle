using System.Collections.Generic;
using BattleRoyale.Maps;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class DungeonColumnBoxesTests
    {
        [Test]
        public void KeptColumnUsesCellCenterAndWallHeight()
        {
            var boxes = new List<DungeonColumnBox>();

            DungeonColumnBoxes.Collect(
                new[] { new Vector2Int(2, 4) },
                columnSize: 1.5f,
                columnOffset: Vector2.zero,
                wallHeight: 20f,
                baseHeight: 0f,
                clipToMap: true,
                clipMin: new Vector2(-10f, -10f),
                clipMax: new Vector2(10f, 10f),
                GridToWorld,
                boxes);

            Assert.AreEqual(1, boxes.Count);
            Assert.AreEqual(new Vector3(2.5f, 10f, 4.5f), boxes[0].Center);
            Assert.AreEqual(new Vector3(1.5f, 20f, 1.5f), boxes[0].Size);
        }

        [Test]
        public void ColumnOutsideTheVisualMapIsSkipped()
        {
            var boxes = new List<DungeonColumnBox>();

            DungeonColumnBoxes.Collect(
                new[] { new Vector2Int(0, 0), new Vector2Int(40, 40) },
                columnSize: 1.5f,
                columnOffset: Vector2.zero,
                wallHeight: 20f,
                baseHeight: 0f,
                clipToMap: true,
                clipMin: new Vector2(-1f, -1f),
                clipMax: new Vector2(2f, 2f),
                GridToWorld,
                boxes);

            Assert.AreEqual(1, boxes.Count);
            Assert.AreEqual(0.5f, boxes[0].Center.x);
            Assert.AreEqual(0.5f, boxes[0].Center.z);
        }

        [Test]
        public void NonPositiveColumnSizeAddsNothing()
        {
            var boxes = new List<DungeonColumnBox>();

            DungeonColumnBoxes.Collect(
                new[] { new Vector2Int(0, 0) },
                columnSize: 0f,
                columnOffset: Vector2.zero,
                wallHeight: 20f,
                baseHeight: 0f,
                clipToMap: false,
                clipMin: default,
                clipMax: default,
                GridToWorld,
                boxes);

            Assert.AreEqual(0, boxes.Count);
        }

        [Test]
        public void GridOffsetShiftsTheCellBeforeCentering()
        {
            var boxes = new List<DungeonColumnBox>();

            DungeonColumnBoxes.Collect(
                new[] { new Vector2Int(2, 4) },
                columnSize: 1.5f,
                columnOffset: new Vector2(-0.5f, -0.5f),
                wallHeight: 20f,
                baseHeight: 0f,
                clipToMap: false,
                clipMin: default,
                clipMax: default,
                GridToWorld,
                boxes);

            Assert.AreEqual(1, boxes.Count);
            Assert.AreEqual(new Vector3(2f, 10f, 4f), boxes[0].Center);
        }

        private static Vector3 GridToWorld(float gridX, float gridY, float height)
        {
            return new Vector3(gridX, height, gridY);
        }
    }
}
