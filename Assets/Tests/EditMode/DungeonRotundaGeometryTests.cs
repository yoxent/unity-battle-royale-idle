using System.Collections.Generic;
using BattleRoyale.Maps;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class DungeonRotundaGeometryTests
    {
        [Test]
        public void DoorOnTheRightEdgeBecomesAnOpening()
        {
            var found = DungeonRotundaGeometry.TryOpening(
                3,
                1,
                directionX: -1,
                directionY: 0,
                roomX: 0,
                roomY: 0,
                roomWidth: 3,
                roomHeight: 3,
                out var opening);

            Assert.IsTrue(found);
            Assert.IsTrue(opening.Vertical);
            Assert.AreEqual(3f, opening.Edge);
            Assert.AreEqual(1f, opening.SpanStart);
            Assert.AreEqual(2f, opening.SpanEnd);
        }

        [Test]
        public void DoorCellOutsideTheEllipseStillOpensTheTouchedSide()
        {
            var openings = new List<DungeonRotundaGeometry.Opening>();
            DungeonRotundaGeometry.CollectOpenings(
                doorX: 5,
                doorY: 3,
                roomX: 4,
                roomY: 0,
                roomWidth: 3,
                roomHeight: 3,
                openings);

            Assert.AreEqual(1, openings.Count);
            Assert.IsFalse(openings[0].Vertical);
            Assert.AreEqual(3f, openings[0].Edge);
            Assert.AreEqual(5f, openings[0].SpanStart);
            Assert.AreEqual(6f, openings[0].SpanEnd);

            var loop = new List<DungeonRotundaGeometry.OutlinePoint>();
            DungeonRotundaGeometry.BuildOutline(5.5f, 1.5f, 1.5f, 1.5f, openings, 48, loop);
            var starts = new List<Vector2>();
            var ends = new List<Vector2>();
            DungeonRotundaGeometry.AppendWallSegments(loop, starts, ends);
            for (var i = 0; i < starts.Count; i++)
            {
                var crossesMouth = CrossesHorizontal(starts[i], ends[i], 3f, 5f, 6f);
                Assert.IsFalse(crossesMouth);
            }
        }

        [Test]
        public void DoorOnAnotherRoomIsIgnored()
        {
            var found = DungeonRotundaGeometry.TryOpening(
                0,
                0,
                directionX: 0,
                directionY: 1,
                roomX: -9,
                roomY: 1,
                roomWidth: 3,
                roomHeight: 3,
                out _);

            Assert.IsFalse(found);
        }

        [Test]
        public void OutlineIsACircleWithADoorGap()
        {
            var openings = new List<DungeonRotundaGeometry.Opening>
            {
                new(true, 3f, 1f, 2f)
            };
            var loop = new List<DungeonRotundaGeometry.OutlinePoint>();
            DungeonRotundaGeometry.BuildOutline(
                1.5f,
                1.5f,
                1.5f,
                1.5f,
                openings,
                DungeonRotundaGeometry.DefaultSegments,
                loop);

            Assert.IsTrue(ContainsPoint(loop, new Vector2(3f, 1f)));
            Assert.IsTrue(ContainsPoint(loop, new Vector2(3f, 2f)));
            Assert.Greater(DistanceToNearest(loop, new Vector2(0f, 0f)), 0.4f);
            Assert.Greater(DistanceToNearest(loop, new Vector2(0f, 3f)), 0.4f);

            var starts = new List<Vector2>();
            var ends = new List<Vector2>();
            DungeonRotundaGeometry.AppendWallSegments(loop, starts, ends);
            for (var i = 0; i < starts.Count; i++)
            {
                var crossesDoor = IsDoorEdge(starts[i], ends[i]) ||
                                  IsDoorEdge(ends[i], starts[i]);
                Assert.IsFalse(crossesDoor);
            }
        }

        [Test]
        public void ClosedRotundaKeepsTheFullEllipse()
        {
            var loop = new List<DungeonRotundaGeometry.OutlinePoint>();
            DungeonRotundaGeometry.BuildOutline(
                1.5f,
                1.5f,
                1.5f,
                1.5f,
                new List<DungeonRotundaGeometry.Opening>(),
                DungeonRotundaGeometry.DefaultSegments,
                loop);

            Assert.AreEqual(DungeonRotundaGeometry.DefaultSegments, loop.Count);
            Assert.IsTrue(ContainsPoint(loop, new Vector2(3f, 1.5f)));
            Assert.Greater(DistanceToNearest(loop, new Vector2(0f, 0f)), 0.4f);
        }

        private static bool ContainsPoint(
            List<DungeonRotundaGeometry.OutlinePoint> loop,
            Vector2 point)
        {
            for (var i = 0; i < loop.Count; i++)
            {
                if ((loop[i].Position - point).sqrMagnitude <= 0.0001f)
                {
                    return true;
                }
            }

            return false;
        }

        private static float DistanceToNearest(
            List<DungeonRotundaGeometry.OutlinePoint> loop,
            Vector2 point)
        {
            var best = float.MaxValue;
            for (var i = 0; i < loop.Count; i++)
            {
                var distance = (loop[i].Position - point).magnitude;
                if (distance < best)
                {
                    best = distance;
                }
            }

            return best;
        }

        private static bool IsDoorEdge(Vector2 start, Vector2 end)
        {
            return Mathf.Abs(start.x - 3f) <= 0.001f &&
                   Mathf.Abs(end.x - 3f) <= 0.001f &&
                   Mathf.Abs(start.y - 1f) <= 0.001f &&
                   Mathf.Abs(end.y - 2f) <= 0.001f;
        }

        private static bool CrossesHorizontal(
            Vector2 start,
            Vector2 end,
            float y,
            float x0,
            float x1)
        {
            if (Mathf.Abs(start.y - y) > 0.02f || Mathf.Abs(end.y - y) > 0.02f)
            {
                return false;
            }

            var min = Mathf.Min(start.x, end.x);
            var max = Mathf.Max(start.x, end.x);
            return Mathf.Min(max, x1) - Mathf.Max(min, x0) > 0.05f;
        }
    }
}
