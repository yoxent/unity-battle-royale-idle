using System.Collections.Generic;

namespace BattleRoyale.Maps
{
    public readonly struct DungeonWallEdge
    {
        public DungeonWallEdge(bool horizontal, int boundary, int segment)
        {
            Horizontal = horizontal;
            Boundary = boundary;
            Segment = segment;
        }

        public bool Horizontal { get; }
        public int Boundary { get; }
        public int Segment { get; }
    }

    public readonly struct DungeonWallRun
    {
        public DungeonWallRun(bool horizontal, int boundary, int segmentStart, int length)
        {
            Horizontal = horizontal;
            Boundary = boundary;
            SegmentStart = segmentStart;
            Length = length;
        }

        public bool Horizontal { get; }
        public int Boundary { get; }
        public int SegmentStart { get; }
        public int Length { get; }
    }

    public static class DungeonWallRuns
    {
        public static void Merge(IReadOnlyList<DungeonWallEdge> edges, List<DungeonWallRun> runs)
        {
            runs.Clear();
            if (edges == null || edges.Count == 0)
            {
                return;
            }

            var sorted = new List<DungeonWallEdge>(edges.Count);
            for (var i = 0; i < edges.Count; i++)
            {
                sorted.Add(edges[i]);
            }

            sorted.Sort(CompareEdges);
            var runStart = 0;
            while (runStart < sorted.Count)
            {
                var origin = sorted[runStart];
                var endSegment = origin.Segment;
                var next = runStart + 1;
                while (next < sorted.Count)
                {
                    var edge = sorted[next];
                    if (edge.Horizontal != origin.Horizontal ||
                        edge.Boundary != origin.Boundary)
                    {
                        break;
                    }

                    if (edge.Segment == endSegment)
                    {
                        next++;
                        continue;
                    }

                    if (edge.Segment != endSegment + 1)
                    {
                        break;
                    }

                    endSegment = edge.Segment;
                    next++;
                }

                runs.Add(new DungeonWallRun(
                    origin.Horizontal,
                    origin.Boundary,
                    origin.Segment,
                    endSegment - origin.Segment + 1));
                runStart = next;
            }
        }

        private static int CompareEdges(DungeonWallEdge left, DungeonWallEdge right)
        {
            if (left.Horizontal != right.Horizontal)
            {
                return left.Horizontal ? 1 : -1;
            }

            var boundary = left.Boundary.CompareTo(right.Boundary);
            if (boundary != 0)
            {
                return boundary;
            }

            return left.Segment.CompareTo(right.Segment);
        }
    }
}
