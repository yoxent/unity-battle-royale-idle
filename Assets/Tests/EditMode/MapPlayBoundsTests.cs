using System.Collections.Generic;
using BattleRoyale.Maps;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class MapPlayBoundsTests
    {
        [Test]
        public void BoundaryWallsCloseTheOutsideAndLeaveTheInsideOpen()
        {
            var walls = new List<BoundaryWall>();
            MapPlayBounds.AddBoundaryWalls(new Vector2(0f, 0f), new Vector2(10f, 10f), 2f, walls);

            Assert.AreEqual(4, walls.Count);
            Assert.IsTrue(HitsAny(walls, new Vector2(5f, -0.1f)));
            Assert.IsTrue(HitsAny(walls, new Vector2(5f, 10.1f)));
            Assert.IsTrue(HitsAny(walls, new Vector2(-0.1f, 5f)));
            Assert.IsTrue(HitsAny(walls, new Vector2(10.1f, 5f)));
            Assert.IsTrue(HitsAny(walls, new Vector2(-0.1f, -0.1f)));
            Assert.IsFalse(HitsAny(walls, new Vector2(0.1f, 0.1f)));
            Assert.IsFalse(HitsAny(walls, new Vector2(5f, 5f)));
        }

        [Test]
        public void ALongBoundaryIsSplitIntoShortPiecesWithoutOpeningAGap()
        {
            var walls = new List<BoundaryWall>();
            MapPlayBounds.AddBoundaryWalls(new Vector2(0f, 0f), new Vector2(800f, 800f), 2f, walls);
            MapPlayBounds.Subdivide(walls, MapPlayBounds.MaxColliderEdge);

            Assert.Greater(walls.Count, 4);
            foreach (var wall in walls)
            {
                Assert.LessOrEqual(wall.Size.x, MapPlayBounds.MaxColliderEdge + 0.001f);
                Assert.LessOrEqual(wall.Size.y, MapPlayBounds.MaxColliderEdge + 0.001f);
            }

            Assert.IsTrue(HitsAny(walls, new Vector2(400f, -0.1f)));
            Assert.IsTrue(HitsAny(walls, new Vector2(200f, -0.1f)));
            Assert.IsFalse(HitsAny(walls, new Vector2(400f, 0.1f)));
        }

        [Test]
        public void ASegmentThatCrossesTheEdgeKeepsOnlyTheInside()
        {
            var clipped = MapPlayBounds.TryClipSegment(
                new Vector2(-5f, 5f),
                new Vector2(15f, 5f),
                new Vector2(0f, 0f),
                new Vector2(10f, 10f),
                out var start,
                out var end);

            Assert.IsTrue(clipped);
            Assert.AreEqual(0f, start.x, 0.0001f);
            Assert.AreEqual(5f, start.y, 0.0001f);
            Assert.AreEqual(10f, end.x, 0.0001f);
            Assert.AreEqual(5f, end.y, 0.0001f);
        }

        [Test]
        public void ASegmentOutsideTheMapIsDropped()
        {
            var clipped = MapPlayBounds.TryClipSegment(
                new Vector2(-5f, -2f),
                new Vector2(-1f, -2f),
                new Vector2(0f, 0f),
                new Vector2(10f, 10f),
                out _,
                out _);

            Assert.IsFalse(clipped);
        }

        [Test]
        public void APolygonIsCutToTheVisualMap()
        {
            var ring = new List<Vector2>
            {
                new(-5f, -5f),
                new(15f, -5f),
                new(15f, 15f),
                new(-5f, 15f)
            };

            var clipped = MapPlayBounds.ClipPolygon(
                ring,
                new Vector2(0f, 0f),
                new Vector2(10f, 10f));

            Assert.GreaterOrEqual(clipped.Count, 4);
            Assert.AreEqual(100f, Area(clipped), 0.001f);
            Assert.IsTrue(ContainsPoint(clipped, new Vector2(5f, 5f)));
            Assert.IsFalse(ContainsPoint(clipped, new Vector2(-1f, -1f)));
        }

        static bool HitsAny(List<BoundaryWall> walls, Vector2 point)
        {
            foreach (var wall in walls)
            {
                if (wall.Contains(point))
                {
                    return true;
                }
            }

            return false;
        }

        static float Area(List<Vector2> ring)
        {
            var area = 0f;
            for (var i = 0; i < ring.Count; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % ring.Count];
                area += a.x * b.y - b.x * a.y;
            }

            return Mathf.Abs(area) * 0.5f;
        }

        static bool ContainsPoint(List<Vector2> ring, Vector2 point)
        {
            var inside = false;
            for (var i = 0; i < ring.Count; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % ring.Count];
                var crosses = (a.y > point.y) != (b.y > point.y);
                if (!crosses)
                {
                    continue;
                }

                var x = a.x + (b.x - a.x) * (point.y - a.y) / (b.y - a.y);
                if (point.x < x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
