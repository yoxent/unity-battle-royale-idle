using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.Maps
{
    public static class VillageBridgeCut
    {
        public static bool TryBuildOpenedVolume(
            IReadOnlyList<Vector2> water,
            IReadOnlyList<List<Vector2>> openings,
            float height,
            List<Vector3> vertices,
            List<int> triangles,
            List<List<Vector2>> decks,
            out int skipped)
        {
            skipped = 0;
            var pieces = new List<List<Vector2>>();
            if (!Cut(water, openings, pieces, decks, out skipped) || pieces.Count == 0)
            {
                return false;
            }

            var built = false;
            foreach (var piece in pieces)
            {
                if (VillageWaterGeometry.TryBuildVolume(piece, null, height, vertices, triangles))
                {
                    built = true;
                }
            }

            return built;
        }

        public static bool Cut(
            IReadOnlyList<Vector2> water,
            IReadOnlyList<List<Vector2>> openings,
            List<List<Vector2>> pieces,
            List<List<Vector2>> decks,
            out int skipped)
        {
            skipped = 0;
            pieces?.Clear();
            decks?.Clear();
            var ring = Clean(water);
            if (pieces == null || ring.Count < 3)
            {
                return false;
            }

            EnsureWinding(ring, true);
            pieces.Add(ring);
            if (openings == null)
            {
                return true;
            }

            foreach (var opening in openings)
            {
                var quad = Clean(opening);
                if (!IsSimpleQuad(quad) || !TryOpen(quad, ring, out var opened))
                {
                    skipped++;
                    continue;
                }

                var next = new List<List<Vector2>>();
                foreach (var piece in pieces)
                {
                    next.AddRange(SubtractConvex(piece, opened));
                }

                if (next.Count == 0)
                {
                    skipped++;
                    continue;
                }

                pieces.Clear();
                pieces.AddRange(next);
                decks?.Add(opened);
            }

            return pieces.Count > 0;
        }

        public static bool IsSimpleQuad(IReadOnlyList<Vector2> corners)
        {
            var quad = Clean(corners);
            if (quad.Count != 4 || Mathf.Abs(SignedArea(quad)) <= 0.0001f)
            {
                return false;
            }

            return !ProperCross(quad[0], quad[1], quad[2], quad[3]) &&
                   !ProperCross(quad[1], quad[2], quad[3], quad[0]);
        }

        static bool TryOpen(List<Vector2> quad, List<Vector2> water, out List<Vector2> opened)
        {
            opened = new List<Vector2>(quad);
            EnsureWinding(opened, true);
            if (!TryEndEdges(opened, out var firstA, out var firstB, out var secondA, out var secondB))
            {
                return false;
            }

            var limit = Diagonal(water) + Diagonal(opened) + 1f;
            var step = Mathf.Max(0.5f, limit * 0.01f);
            var traveled = 0f;
            while (traveled <= limit)
            {
                var firstInside = Contains(water, Midpoint(opened, firstA, firstB));
                var secondInside = Contains(water, Midpoint(opened, secondA, secondB));
                if (!firstInside && !secondInside)
                {
                    var across = Midpoint(opened, secondA, secondB) - Midpoint(opened, firstA, firstB);
                    if (across.sqrMagnitude <= 0.0001f ||
                        !Crosses(water, opened))
                    {
                        return false;
                    }

                    Push(opened, firstA, firstB, secondA, secondB, 0.5f);
                    return true;
                }

                if (firstInside)
                {
                    PushEnd(opened, firstA, firstB, secondA, secondB, step);
                }

                if (secondInside)
                {
                    PushEnd(opened, secondA, secondB, firstA, firstB, step);
                }

                traveled += step;
            }

            return false;
        }

        static bool Crosses(List<Vector2> water, List<Vector2> quad)
        {
            for (var i = 0; i < quad.Count; i++)
            {
                if (Contains(water, quad[i]))
                {
                    return true;
                }
            }

            for (var i = 0; i < quad.Count; i++)
            {
                var start = quad[i];
                var end = quad[(i + 1) % quad.Count];
                for (var j = 0; j < water.Count; j++)
                {
                    if (ProperCross(start, end, water[j], water[(j + 1) % water.Count]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static List<List<Vector2>> SubtractConvex(List<Vector2> subject, List<Vector2> cutter)
        {
            var pending = new List<List<Vector2>> { Clean(subject) };
            var kept = new List<List<Vector2>>();
            for (var edge = 0; edge < cutter.Count; edge++)
            {
                var start = cutter[edge];
                var end = cutter[(edge + 1) % cutter.Count];
                var nextPending = new List<List<Vector2>>();
                foreach (var piece in pending)
                {
                    nextPending.AddRange(ClipLeft(piece, start, end));
                    kept.AddRange(ClipLeft(piece, end, start));
                }

                pending = nextPending;
            }

            kept.RemoveAll(piece => piece.Count < 3 || Mathf.Abs(SignedArea(piece)) <= 0.001f);
            return kept;
        }

        static List<List<Vector2>> ClipLeft(List<Vector2> polygon, Vector2 lineA, Vector2 lineB)
        {
            var result = new List<List<Vector2>>();
            if (polygon == null || polygon.Count < 3)
            {
                return result;
            }

            var current = new List<Vector2>();
            var startedKept = Side(lineA, lineB, polygon[0]) >= 0;
            for (var i = 0; i < polygon.Count; i++)
            {
                var point = polygon[i];
                var next = polygon[(i + 1) % polygon.Count];
                var pointSide = Side(lineA, lineB, point);
                var nextSide = Side(lineA, lineB, next);
                if (pointSide >= 0)
                {
                    AddPoint(current, point);
                }

                if (pointSide == 0 || nextSide == 0 || pointSide == nextSide)
                {
                    continue;
                }

                AddPoint(current, Intersect(point, next, lineA, lineB));
                if (nextSide < 0)
                {
                    if (current.Count >= 3)
                    {
                        result.Add(current);
                    }

                    current = new List<Vector2>();
                }
            }

            if (current.Count == 0)
            {
                return result;
            }

            if (result.Count > 0 && startedKept)
            {
                result[0].InsertRange(0, current);
                result[0] = Clean(result[0]);
            }
            else if (current.Count >= 3)
            {
                result.Add(current);
            }

            for (var i = 0; i < result.Count; i++)
            {
                result[i] = Clean(result[i]);
            }

            result.RemoveAll(piece => piece.Count < 3);
            return result;
        }

        static bool TryEndEdges(
            List<Vector2> quad,
            out int firstA,
            out int firstB,
            out int secondA,
            out int secondB)
        {
            var acrossEnds = (Midpoint(quad, 0, 1) - Midpoint(quad, 2, 3)).sqrMagnitude;
            var acrossSides = (Midpoint(quad, 1, 2) - Midpoint(quad, 3, 0)).sqrMagnitude;
            if (acrossEnds >= acrossSides)
            {
                firstA = 0;
                firstB = 1;
                secondA = 2;
                secondB = 3;
            }
            else
            {
                firstA = 1;
                firstB = 2;
                secondA = 3;
                secondB = 0;
            }

            return (Midpoint(quad, firstA, firstB) - Midpoint(quad, secondA, secondB)).sqrMagnitude > 0.0001f;
        }

        static void Push(
            List<Vector2> quad,
            int firstA,
            int firstB,
            int secondA,
            int secondB,
            float distance)
        {
            PushEnd(quad, firstA, firstB, secondA, secondB, distance);
            PushEnd(quad, secondA, secondB, firstA, firstB, distance);
        }

        static void PushEnd(
            List<Vector2> quad,
            int endA,
            int endB,
            int otherA,
            int otherB,
            float distance)
        {
            var direction = Midpoint(quad, endA, endB) - Midpoint(quad, otherA, otherB);
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            direction.Normalize();
            quad[endA] += direction * distance;
            quad[endB] += direction * distance;
        }

        static Vector2 Midpoint(List<Vector2> quad, int a, int b)
        {
            return (quad[a] + quad[b]) * 0.5f;
        }

        static float Diagonal(List<Vector2> ring)
        {
            var min = ring[0];
            var max = ring[0];
            foreach (var point in ring)
            {
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            return Vector2.Distance(min, max);
        }

        static int Side(Vector2 lineA, Vector2 lineB, Vector2 point)
        {
            var cross = (lineB.x - lineA.x) * (point.y - lineA.y) -
                        (lineB.y - lineA.y) * (point.x - lineA.x);
            if (cross > 0.00001f)
            {
                return 1;
            }

            if (cross < -0.00001f)
            {
                return -1;
            }

            return 0;
        }

        static Vector2 Intersect(Vector2 start, Vector2 end, Vector2 lineA, Vector2 lineB)
        {
            var edge = end - start;
            var line = lineB - lineA;
            var denominator = edge.x * line.y - edge.y * line.x;
            if (Mathf.Abs(denominator) <= 0.000001f)
            {
                return (start + end) * 0.5f;
            }

            var offset = lineA - start;
            var t = (offset.x * line.y - offset.y * line.x) / denominator;
            return start + edge * Mathf.Clamp01(t);
        }

        static bool ProperCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            var ab = Side(a, b, c);
            var ab2 = Side(a, b, d);
            var cd = Side(c, d, a);
            var cd2 = Side(c, d, b);
            return ab * ab2 < 0 && cd * cd2 < 0;
        }

        static bool Contains(List<Vector2> ring, Vector2 point)
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

        static void AddPoint(List<Vector2> points, Vector2 point)
        {
            if (points.Count == 0 || (points[points.Count - 1] - point).sqrMagnitude > 0.000001f)
            {
                points.Add(point);
            }
        }

        static List<Vector2> Clean(IReadOnlyList<Vector2> source)
        {
            var ring = new List<Vector2>();
            if (source == null)
            {
                return ring;
            }

            foreach (var point in source)
            {
                AddPoint(ring, point);
            }

            if (ring.Count > 1 && (ring[0] - ring[ring.Count - 1]).sqrMagnitude <= 0.000001f)
            {
                ring.RemoveAt(ring.Count - 1);
            }

            return ring;
        }

        static void EnsureWinding(List<Vector2> ring, bool counterClockwise)
        {
            var isCounterClockwise = SignedArea(ring) > 0f;
            if (isCounterClockwise != counterClockwise)
            {
                ring.Reverse();
            }
        }

        static float SignedArea(List<Vector2> ring)
        {
            var area = 0f;
            for (var i = 0; i < ring.Count; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % ring.Count];
                area += a.x * b.y - b.x * a.y;
            }

            return area * 0.5f;
        }
    }
}
