using BattleRoyale.Maps.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class TreePointPlacementTests
    {
        [Test]
        public void MatchingCornersKeepTheExportedTreePoint()
        {
            var sourceMin = new Vector2(-10f, -4f);
            var sourceMax = new Vector2(10f, 6f);
            var point = new Vector2(2f, 1f);

            var placed = TreePointPlacement.ToSource(
                point,
                sourceMin,
                sourceMax,
                new Vector2(sourceMin.x, sourceMax.y),
                new Vector2(sourceMax.x, sourceMin.y));

            Assert.AreEqual(point.x, placed.x, 0.0001f);
            Assert.AreEqual(point.y, placed.y, 0.0001f);
        }

        [Test]
        public void NegativeCornersMoveTheTreeOutsideTheExportedBounds()
        {
            var sourceMin = new Vector2(0f, 0f);
            var sourceMax = new Vector2(10f, 10f);

            var placed = TreePointPlacement.ToSource(
                new Vector2(0f, 10f),
                sourceMin,
                sourceMax,
                new Vector2(-5f, 15f),
                new Vector2(5f, 5f));

            Assert.AreEqual(new Vector2(-5f, 15f), placed);
        }
    }
}
