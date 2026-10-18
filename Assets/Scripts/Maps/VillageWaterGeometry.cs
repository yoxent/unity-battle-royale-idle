using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.Maps
{
    public sealed class VillageRoadLine
    {
        public VillageRoadLine(List<Vector2> points, float width)
        {
            Points = points;
            Width = width;
        }

        public List<Vector2> Points { get; }

        public float Width { get; }
    }

    public static class VillageWaterGeometry
    {
        public const float BlockHeight = 2f;
        public const float DeckClearance = 0.05f;

        public static List<List<Vector2>> FindBridgeDecks(
            IReadOnlyList<Vector2> water,
            IReadOnlyList<VillageRoadLine> roads,
            float deckWidth,
            float minimumInsideLength)
        {
            var decks = new List<List<Vector2>>();
            if (water == null || water.Count < 3 || deckWidth <= 0f)
            {
                return decks;
            }

            var ring = CleanRing(water);
            foreach (var road in roads)
            {
                if (road?.Points == null || road.Points.Count < 2)
                {
                    continue;
                }

                foreach (var crossing in FindCrossings(ring, road.Points, minimumInsideLength))
                {
                    var inset = InsetEnds(crossing, deckWidth * 0.5f);
                    if (inset.Count < 2)
                    {
                        continue;
                    }

                    var deck = Ribbon(inset, deckWidth);
                    if (deck.Count >= 3 && RingInside(ring, deck))
                    {
                        decks.Add(deck);
                    }
                }
            }

            return decks;
        }

        public static List<List<Vector2>> FindBridgeOpenings(
            IReadOnlyList<Vector2> water,
            IReadOnlyList<VillageRoadLine> roads,
            float deckWidth,
            float minimumInsideLength)
        {
            var openings = new List<List<Vector2>>();
            if (water == null || water.Count < 3 || roads == null || deckWidth <= 0f)
            {
                return openings;
            }

            var ring = CleanRing(water);
            foreach (var road in roads)
            {
                if (road?.Points == null || road.Points.Count < 2)
                {
                    continue;
                }

                foreach (var crossing in FindCrossings(ring, road.Points, minimumInsideLength))
                {
                    if (crossing.Count < 2)
                    {
                        continue;
                    }

                    var span = new List<Vector2> { crossing[0], crossing[crossing.Count - 1] };
                    var quad = Ribbon(span, deckWidth);
                    if (quad.Count >= 4)
                    {
                        openings.Add(quad);
                    }
                }
            }

            return openings;
        }

        public static bool TryBuildVolume(
            IReadOnlyList<Vector2> water,
            IReadOnlyList<List<Vector2>> holes,
            float height,
            List<Vector3> vertices,
            List<int> triangles)
        {
            var outer = CleanRing(water);
            if (outer.Count < 3 || height <= 0f)
            {
                return false;
            }

            EnsureWinding(outer, true);
            var holeRings = new List<List<Vector2>>();
            if (holes != null)
            {
                foreach (var hole in holes)
                {
                    var cleaned = CleanRing(hole);
                    if (cleaned.Count < 3)
                    {
                        continue;
                    }

                    EnsureWinding(cleaned, false);
                    holeRings.Add(cleaned);
                }
            }

            var cap = new List<Vector2>(outer);
            foreach (var hole in holeRings)
            {
                if (!TryMergeHole(cap, hole, out var merged))
                {
                    return false;
                }

                cap = merged;
            }

            if (!TryTriangulate(cap, out var capTriangles))
            {
                return false;
            }

            var capStart = vertices.Count;
            foreach (var point in cap)
            {
                vertices.Add(new Vector3(point.x, height, point.y));
            }

            foreach (var index in capTriangles)
            {
                triangles.Add(capStart + index);
            }

            AppendSides(vertices, triangles, outer, 0f, height, true);
            foreach (var hole in holeRings)
            {
                AppendSides(vertices, triangles, hole, 0f, height, false);
            }

            return true;
        }

        public static bool TryBuildDeck(
            IReadOnlyList<Vector2> deck,
            float height,
            List<Vector3> vertices,
            List<int> triangles)
        {
            var ring = CleanRing(deck);
            if (ring.Count < 3)
            {
                return false;
            }

            EnsureWinding(ring, true);
            if (!TryTriangulate(ring, out var indices))
            {
                return false;
            }

            var start = vertices.Count;
            foreach (var point in ring)
            {
                vertices.Add(new Vector3(point.x, height, point.y));
            }

            foreach (var index in indices)
            {
                triangles.Add(start + index);
            }

            return true;
        }

        public static bool TopCovers(IReadOnlyList<Vector3> vertices, IReadOnlyList<int> triangles, Vector2 point)
        {
            for (var i = 0; i + 2 < triangles.Count; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i + 1]];
                var c = vertices[triangles[i + 2]];
                if (!Mathf.Approximately(a.y, b.y) || !Mathf.Approximately(b.y, c.y))
                {
                    continue;
                }

                if (PointInTriangle(point, new Vector2(a.x, a.z), new Vector2(b.x, b.z), new Vector2(c.x, c.z)))
                {
                    return true;
                }
            }

            return false;
        }

        static List<List<Vector2>> FindCrossings(
            List<Vector2> water,
            List<Vector2> road,
            float minimumInsideLength)
        {
            var crossings = new List<List<Vector2>>();
            var inside = false;
            var run = new List<Vector2>();
            var length = 0f;
            for (var i = 0; i < road.Count - 1; i++)
            {
                var marks = new List<Vector2> { road[i] };
                foreach (var hit in BoundaryHits(water, road[i], road[i + 1]))
                {
                    if ((marks[marks.Count - 1] - hit).sqrMagnitude > 0.000001f)
                    {
                        marks.Add(hit);
                    }
                }

                if ((marks[marks.Count - 1] - road[i + 1]).sqrMagnitude > 0.000001f)
                {
                    marks.Add(road[i + 1]);
                }

                for (var mark = 0; mark < marks.Count - 1; mark++)
                {
                    var start = marks[mark];
                    var end = marks[mark + 1];
                    var span = Vector2.Distance(start, end);
                    if (span <= 0.0001f)
                    {
                        continue;
                    }

                    var midpointInside = Contains(water, (start + end) * 0.5f);
                    if (midpointInside)
                    {
                        if (!inside)
                        {
                            run = new List<Vector2> { start };
                            length = 0f;
                        }

                        length += span;
                        run.Add(end);
                        inside = true;
                        continue;
                    }

                    if (inside && length >= minimumInsideLength && run.Count >= 2)
                    {
                        crossings.Add(run);
                    }

                    inside = false;
                    run = new List<Vector2>();
                    length = 0f;
                }
            }

            if (inside &&
                length >= minimumInsideLength &&
                run.Count >= 2 &&
                DistanceToRing(water, run[run.Count - 1]) <= 0.05f)
            {
                crossings.Add(run);
            }

            return crossings;
        }

        static float DistanceToRing(List<Vector2> ring, Vector2 point)
        {
            var best = float.MaxValue;
            for (var i = 0; i < ring.Count; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % ring.Count];
                var edge = b - a;
                var lengthSquared = edge.sqrMagnitude;
                if (lengthSquared <= 0.000001f)
                {
                    continue;
                }

                var along = Mathf.Clamp01(Vector2.Dot(point - a, edge) / lengthSquared);
                var closest = a + edge * along;
                var gap = Vector2.Distance(point, closest);
                if (gap < best)
                {
                    best = gap;
                }
            }

            return best;
        }

        static List<Vector2> BoundaryHits(List<Vector2> ring, Vector2 start, Vector2 end)
        {
            var hits = new List<(float along, Vector2 point)>();
            for (var i = 0; i < ring.Count; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % ring.Count];
                if (!TrySegmentHit(start, end, a, b, out var point, out var along))
                {
                    continue;
                }

                var duplicate = false;
                foreach (var existing in hits)
                {
                    if (Mathf.Abs(existing.along - along) <= 0.0001f)
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (!duplicate)
                {
                    hits.Add((along, point));
                }
            }

            hits.Sort((left, right) => left.along.CompareTo(right.along));
            var points = new List<Vector2>(hits.Count);
            foreach (var hit in hits)
            {
                points.Add(hit.point);
            }

            return points;
        }

        static bool RingInside(List<Vector2> water, List<Vector2> ring)
        {
            foreach (var point in ring)
            {
                if (!Contains(water, point))
                {
                    return false;
                }
            }

            return true;
        }

        static List<Vector2> InsetEnds(List<Vector2> line, float distance)
        {
            var trimmed = Trim(line, distance, fromStart: true);
            trimmed = Trim(trimmed, distance, fromStart: false);
            return trimmed;
        }

        static List<Vector2> Trim(List<Vector2> line, float distance, bool fromStart)
        {
            if (line.Count < 2 || distance <= 0f)
            {
                return new List<Vector2>(line);
            }

            var points = new List<Vector2>(line);
            if (!fromStart)
            {
                points.Reverse();
            }

            var remaining = distance;
            while (points.Count >= 2 && remaining > 0f)
            {
                var span = Vector2.Distance(points[0], points[1]);
                if (span <= 0.0001f)
                {
                    points.RemoveAt(0);
                    continue;
                }

                if (span > remaining)
                {
                    points[0] = Vector2.Lerp(points[0], points[1], remaining / span);
                    remaining = 0f;
                }
                else
                {
                    points.RemoveAt(0);
                    remaining -= span;
                }
            }

            if (!fromStart)
            {
                points.Reverse();
            }

            return points.Count >= 2 ? points : new List<Vector2>();
        }

        static List<Vector2> Ribbon(List<Vector2> line, float width)
        {
            var half = width * 0.5f;
            var left = new List<Vector2>();
            var right = new List<Vector2>();
            for (var i = 0; i < line.Count; i++)
            {
                var direction = Direction(line, i);
                var normal = new Vector2(-direction.y, direction.x);
                left.Add(line[i] + normal * half);
                right.Add(line[i] - normal * half);
            }

            var ring = new List<Vector2>(left);
            for (var i = right.Count - 1; i >= 0; i--)
            {
                ring.Add(right[i]);
            }

            return ring;
        }

        static Vector2 Direction(List<Vector2> line, int index)
        {
            Vector2 direction;
            if (index <= 0)
            {
                direction = line[1] - line[0];
            }
            else if (index >= line.Count - 1)
            {
                direction = line[index] - line[index - 1];
            }
            else
            {
                var incoming = (line[index] - line[index - 1]).normalized;
                var outgoing = (line[index + 1] - line[index]).normalized;
                direction = incoming + outgoing;
            }

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return Vector2.right;
            }

            return direction.normalized;
        }

        static bool TryMergeHole(List<Vector2> outer, List<Vector2> hole, out List<Vector2> merged)
        {
            merged = null;
            var holeIndex = 0;
            for (var i = 1; i < hole.Count; i++)
            {
                if (hole[i].x > hole[holeIndex].x)
                {
                    holeIndex = i;
                }
            }

            var origin = hole[holeIndex];
            var best = -1;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < outer.Count; i++)
            {
                if (outer[i].x + 0.0001f < origin.x)
                {
                    continue;
                }

                var distance = (outer[i] - origin).sqrMagnitude;
                if (distance >= bestDistance || !SegmentIsClear(origin, outer[i], outer, hole))
                {
                    continue;
                }

                best = i;
                bestDistance = distance;
            }

            if (best < 0)
            {
                for (var i = 0; i < outer.Count; i++)
                {
                    var distance = (outer[i] - origin).sqrMagnitude;
                    if (distance >= bestDistance || !SegmentIsClear(origin, outer[i], outer, hole))
                    {
                        continue;
                    }

                    best = i;
                    bestDistance = distance;
                }
            }

            if (best < 0)
            {
                return false;
            }

            merged = new List<Vector2>(outer.Count + hole.Count + 2);
            for (var i = 0; i <= best; i++)
            {
                merged.Add(outer[i]);
            }

            for (var i = 0; i < hole.Count; i++)
            {
                merged.Add(hole[(holeIndex + i) % hole.Count]);
            }

            merged.Add(hole[holeIndex]);
            for (var i = best; i < outer.Count; i++)
            {
                merged.Add(outer[i]);
            }

            return true;
        }

        static bool TryTriangulate(List<Vector2> ring, out List<int> triangles)
        {
            triangles = new List<int>();
            var indices = new List<int>(ring.Count);
            for (var i = 0; i < ring.Count; i++)
            {
                indices.Add(i);
            }

            var guard = ring.Count * ring.Count;
            while (indices.Count > 3 && guard-- > 0)
            {
                var ear = FindEar(ring, indices);
                if (ear < 0)
                {
                    return false;
                }

                var previous = indices[(ear + indices.Count - 1) % indices.Count];
                var current = indices[ear];
                var next = indices[(ear + 1) % indices.Count];
                triangles.Add(previous);
                triangles.Add(current);
                triangles.Add(next);
                indices.RemoveAt(ear);
            }

            if (indices.Count != 3)
            {
                return false;
            }

            triangles.Add(indices[0]);
            triangles.Add(indices[1]);
            triangles.Add(indices[2]);
            return true;
        }

        static int FindEar(List<Vector2> ring, List<int> indices)
        {
            for (var i = 0; i < indices.Count; i++)
            {
                var previous = indices[(i + indices.Count - 1) % indices.Count];
                var current = indices[i];
                var next = indices[(i + 1) % indices.Count];
                if (Cross(ring[previous], ring[current], ring[next]) <= 0.00001f)
                {
                    continue;
                }

                var containsPoint = false;
                for (var j = 0; j < indices.Count; j++)
                {
                    var candidate = indices[j];
                    if (candidate == previous || candidate == current || candidate == next)
                    {
                        continue;
                    }

                    if (StrictlyInsideTriangle(
                            ring[candidate],
                            ring[previous],
                            ring[current],
                            ring[next]))
                    {
                        containsPoint = true;
                        break;
                    }
                }

                if (!containsPoint)
                {
                    return i;
                }
            }

            return -1;
        }

        static void AppendSides(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> ring,
            float bottom,
            float top,
            bool outward)
        {
            for (var i = 0; i < ring.Count; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % ring.Count];
                var start = vertices.Count;
                vertices.Add(new Vector3(a.x, bottom, a.y));
                vertices.Add(new Vector3(b.x, bottom, b.y));
                vertices.Add(new Vector3(b.x, top, b.y));
                vertices.Add(new Vector3(a.x, top, a.y));
                if (outward)
                {
                    triangles.Add(start);
                    triangles.Add(start + 1);
                    triangles.Add(start + 2);
                    triangles.Add(start);
                    triangles.Add(start + 2);
                    triangles.Add(start + 3);
                }
                else
                {
                    triangles.Add(start);
                    triangles.Add(start + 2);
                    triangles.Add(start + 1);
                    triangles.Add(start);
                    triangles.Add(start + 3);
                    triangles.Add(start + 2);
                }
            }
        }

        static List<Vector2> CleanRing(IReadOnlyList<Vector2> source)
        {
            var ring = new List<Vector2>();
            if (source == null)
            {
                return ring;
            }

            foreach (var point in source)
            {
                if (ring.Count > 0 && (ring[ring.Count - 1] - point).sqrMagnitude <= 0.000001f)
                {
                    continue;
                }

                ring.Add(point);
            }

            if (ring.Count > 1 && (ring[0] - ring[ring.Count - 1]).sqrMagnitude <= 0.000001f)
            {
                ring.RemoveAt(ring.Count - 1);
            }

            return ring;
        }

        static void EnsureWinding(List<Vector2> ring, bool counterClockwise)
        {
            var area = 0f;
            for (var i = 0; i < ring.Count; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % ring.Count];
                area += a.x * b.y - b.x * a.y;
            }

            var isCounterClockwise = area > 0f;
            if (isCounterClockwise != counterClockwise)
            {
                ring.Reverse();
            }
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

        static bool TrySegmentHit(
            Vector2 a,
            Vector2 b,
            Vector2 c,
            Vector2 d,
            out Vector2 point,
            out float along)
        {
            point = default;
            along = 0f;
            var ab = b - a;
            var cd = d - c;
            var denominator = ab.x * cd.y - ab.y * cd.x;
            if (Mathf.Abs(denominator) <= 0.000001f)
            {
                return false;
            }

            var ac = c - a;
            var t = (ac.x * cd.y - ac.y * cd.x) / denominator;
            var u = (ac.x * ab.y - ac.y * ab.x) / denominator;
            if (t < -0.0001f || t > 1.0001f || u < -0.0001f || u > 1.0001f)
            {
                return false;
            }

            along = Mathf.Clamp01(t);
            point = a + ab * along;
            return true;
        }

        static bool SegmentIsClear(Vector2 start, Vector2 end, List<Vector2> outer, List<Vector2> hole)
        {
            if ((end - start).sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            return !HitsRing(start, end, outer) && !HitsRing(start, end, hole);
        }

        static bool HitsRing(Vector2 start, Vector2 end, List<Vector2> ring)
        {
            for (var i = 0; i < ring.Count; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % ring.Count];
                if (SharesEnd(start, end, a, b))
                {
                    continue;
                }

                if (TrySegmentHit(start, end, a, b, out _, out var along) &&
                    along > 0.001f &&
                    along < 0.999f)
                {
                    return true;
                }
            }

            return false;
        }

        static bool SharesEnd(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            return Near(a, c) || Near(a, d) || Near(b, c) || Near(b, d);
        }

        static bool Near(Vector2 a, Vector2 b)
        {
            return (a - b).sqrMagnitude <= 0.000001f;
        }

        static float Cross(Vector2 a, Vector2 b, Vector2 c)
        {
            return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        }

        static bool StrictlyInsideTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            var ab = Cross(a, b, point);
            var bc = Cross(b, c, point);
            var ca = Cross(c, a, point);
            return ab > 0.00001f && bc > 0.00001f && ca > 0.00001f;
        }

        static bool PointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            var ab = Cross(a, b, point);
            var bc = Cross(b, c, point);
            var ca = Cross(c, a, point);
            var hasNegative = ab < -0.00001f || bc < -0.00001f || ca < -0.00001f;
            var hasPositive = ab > 0.00001f || bc > 0.00001f || ca > 0.00001f;
            return !(hasNegative && hasPositive);
        }
    }
}
