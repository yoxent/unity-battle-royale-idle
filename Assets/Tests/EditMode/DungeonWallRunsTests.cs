using System.Collections.Generic;
using BattleRoyale.Maps;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class DungeonWallRunsTests
    {
        [Test]
        public void ColinearEdgesBecomeOneRun()
        {
            var edges = new List<DungeonWallEdge>
            {
                new(true, 1, -1),
                new(true, 1, -3),
                new(true, 1, -2)
            };
            var runs = new List<DungeonWallRun>();

            DungeonWallRuns.Merge(edges, runs);

            Assert.AreEqual(1, runs.Count);
            Assert.IsTrue(runs[0].Horizontal);
            Assert.AreEqual(1, runs[0].Boundary);
            Assert.AreEqual(-3, runs[0].SegmentStart);
            Assert.AreEqual(3, runs[0].Length);
        }

        [Test]
        public void DoorGapSplitsARun()
        {
            var edges = new List<DungeonWallEdge>
            {
                new(false, 4, 0),
                new(false, 4, 1),
                new(false, 4, 3),
                new(false, 4, 4)
            };
            var runs = new List<DungeonWallRun>();

            DungeonWallRuns.Merge(edges, runs);

            Assert.AreEqual(2, runs.Count);
            Assert.AreEqual(0, runs[0].SegmentStart);
            Assert.AreEqual(2, runs[0].Length);
            Assert.AreEqual(3, runs[1].SegmentStart);
            Assert.AreEqual(2, runs[1].Length);
        }

        [Test]
        public void DifferentLinesStaySeparate()
        {
            var edges = new List<DungeonWallEdge>
            {
                new(true, 1, 0),
                new(true, 2, 0),
                new(false, 1, 0)
            };
            var runs = new List<DungeonWallRun>();

            DungeonWallRuns.Merge(edges, runs);

            Assert.AreEqual(3, runs.Count);
            Assert.AreEqual(1, runs[0].Length);
            Assert.AreEqual(1, runs[1].Length);
            Assert.AreEqual(1, runs[2].Length);
        }

        [Test]
        public void DuplicateEdgeDoesNotLengthenTheRun()
        {
            var edges = new List<DungeonWallEdge>
            {
                new(true, 8, 2),
                new(true, 8, 2),
                new(true, 8, 3)
            };
            var runs = new List<DungeonWallRun>();

            DungeonWallRuns.Merge(edges, runs);

            Assert.AreEqual(1, runs.Count);
            Assert.AreEqual(2, runs[0].SegmentStart);
            Assert.AreEqual(2, runs[0].Length);
        }
    }
}
