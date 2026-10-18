using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.Maps
{
    public readonly struct BoundaryWall
    {
        public BoundaryWall(Vector2 center, Vector2 size)
        {
            Center = center;
            Size = size;
        }

        public Vector2 Center { get; }

        public Vector2 Size { get; }

        public bool Contains(Vector2 point)
        {
            var half = Size * 0.5f;
            return point.x >= Center.x - half.x &&
                   point.x <= Center.x + half.x &&
                   point.y >= Center.y - half.y &&
                   point.y <= Center.y + half.y;
        }
    }

    public static class MapPlayBounds
    {
        public const float MaxColliderEdge = 200f;

        public static bool TryGetRect(
            IReadOnlyList<Vector3> corners,
            out Vector2 min,
            out Vector2 max)
        {
            min = default;
            max = default;
            if (corners == null || corners.Count == 0)
            {
                return false;
            }

            min = new Vector2(corners[0].x, corners[0].z);
            max = min;
            for (var i = 1; i < corners.Count; i++)
            {
                var point = new Vector2(corners[i].x, corners[i].z);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            return max.x > min.x && max.y > min.y;
        }

        public static bool TryInset(
            Vector2 min,
            Vector2 max,
            float amount,
            out Vector2 insetMin,
            out Vector2 insetMax)
        {
            insetMin = new Vector2(min.x + amount, min.y + amount);
            insetMax = new Vector2(max.x - amount, max.y - amount);
            if (amount <= 0f || insetMax.x <= insetMin.x || insetMax.y <= insetMin.y)
            {
                insetMin = min;
                insetMax = max;
                return false;
            }

            return true;
        }

        public static bool Contains(Vector2 point, Vector2 min, Vector2 max)
        {
            return point.x >= min.x &&
                   point.x <= max.x &&
                   point.y >= min.y &&
                   point.y <= max.y;
        }

        public static void AddBoundaryWalls(
            Vector2 min,
            Vector2 max,
            float thickness,
            List<BoundaryWall> walls)
        {
            if (walls == null || thickness <= 0f || max.x <= min.x || max.y <= min.y)
            {
                return;
            }

            var half = thickness * 0.5f;
            var spanX = max.x - min.x + thickness;
            var spanY = max.y - min.y + thickness;
            var midX = (min.x + max.x) * 0.5f;
            var midY = (min.y + max.y) * 0.5f;
            walls.Add(new BoundaryWall(new Vector2(midX, min.y - half), new Vector2(spanX, thickness)));
            walls.Add(new BoundaryWall(new Vector2(midX, max.y + half), new Vector2(spanX, thickness)));
            walls.Add(new BoundaryWall(new Vector2(min.x - half, midY), new Vector2(thickness, spanY)));
            walls.Add(new BoundaryWall(new Vector2(max.x + half, midY), new Vector2(thickness, spanY)));
        }

        public static void Subdivide(List<BoundaryWall> walls, float maxSegment)
        {
            if (walls == null || maxSegment <= 0f)
            {
                return;
            }

            var split = new List<BoundaryWall>(walls.Count);
            foreach (var wall in walls)
            {
                AddSegments(wall, maxSegment, split);
            }

            walls.Clear();
            walls.AddRange(split);
        }

        static void AddSegments(BoundaryWall wall, float maxSegment, List<BoundaryWall> output)
        {
            var alongX = wall.Size.x >= wall.Size.y;
            var length = alongX ? wall.Size.x : wall.Size.y;
            if (length <= maxSegment)
            {
                output.Add(wall);
                return;
            }

            var count = Mathf.CeilToInt(length / maxSegment);
            var segment = length / count;
            var origin = (alongX ? wall.Center.x : wall.Center.y) - length * 0.5f;
            for (var i = 0; i < count; i++)
            {
                var center = origin + segment * (i + 0.5f);
                output.Add(alongX
                    ? new BoundaryWall(new Vector2(center, wall.Center.y), new Vector2(segment, wall.Size.y))
                    : new BoundaryWall(new Vector2(wall.Center.x, center), new Vector2(wall.Size.x, segment)));
            }
        }

        public static bool TryClipSegment(
            Vector2 start,
            Vector2 end,
            Vector2 min,
            Vector2 max,
            out Vector2 clippedStart,
            out Vector2 clippedEnd)
        {
            clippedStart = start;
            clippedEnd = end;
            var delta = end - start;
            var t0 = 0f;
            var t1 = 1f;
            if (!ClipAxis(-delta.x, start.x - min.x, ref t0, ref t1) ||
                !ClipAxis(delta.x, max.x - start.x, ref t0, ref t1) ||
                !ClipAxis(-delta.y, start.y - min.y, ref t0, ref t1) ||
                !ClipAxis(delta.y, max.y - start.y, ref t0, ref t1))
            {
                return false;
            }

            clippedStart = start + delta * t0;
            clippedEnd = start + delta * t1;
            return (clippedEnd - clippedStart).sqrMagnitude > 0.000001f;
        }

        public static List<Vector2> ClipPolygon(
            IReadOnlyList<Vector2> ring,
            Vector2 min,
            Vector2 max)
        {
            var output = new List<Vector2>();
            if (ring == null)
            {
                return output;
            }

            foreach (var point in ring)
            {
                if (output.Count == 0 ||
                    (output[output.Count - 1] - point).sqrMagnitude > 0.000001f)
                {
                    output.Add(point);
                }
            }

            if (output.Count > 1 &&
                (output[0] - output[output.Count - 1]).sqrMagnitude <= 0.000001f)
            {
                output.RemoveAt(output.Count - 1);
            }

            if (output.Count < 3 || max.x <= min.x || max.y <= min.y)
            {
                return new List<Vector2>();
            }

            output = ClipEdge(output, min.x, true, true);
            output = ClipEdge(output, max.x, true, false);
            output = ClipEdge(output, min.y, false, true);
            output = ClipEdge(output, max.y, false, false);
            return output.Count >= 3 ? output : new List<Vector2>();
        }

        static List<Vector2> ClipEdge(
            List<Vector2> input,
            float edge,
            bool vertical,
            bool keepGreater)
        {
            var output = new List<Vector2>();
            if (input.Count == 0)
            {
                return output;
            }

            var previous = input[input.Count - 1];
            var previousInside = IsInside(previous, edge, vertical, keepGreater);
            foreach (var current in input)
            {
                var currentInside = IsInside(current, edge, vertical, keepGreater);
                if (currentInside)
                {
                    if (!previousInside)
                    {
                        output.Add(Intersect(previous, current, edge, vertical));
                    }

                    output.Add(current);
                }
                else if (previousInside)
                {
                    output.Add(Intersect(previous, current, edge, vertical));
                }

                previous = current;
                previousInside = currentInside;
            }

            return output;
        }

        static bool IsInside(Vector2 point, float edge, bool vertical, bool keepGreater)
        {
            var value = vertical ? point.x : point.y;
            return keepGreater ? value >= edge - 0.00001f : value <= edge + 0.00001f;
        }

        static Vector2 Intersect(Vector2 start, Vector2 end, float edge, bool vertical)
        {
            var delta = end - start;
            var span = vertical ? delta.x : delta.y;
            if (Mathf.Abs(span) <= 0.000001f)
            {
                return start;
            }

            var t = ((vertical ? edge - start.x : edge - start.y) / span);
            return Vector2.LerpUnclamped(start, end, t);
        }

        static bool ClipAxis(float direction, float distance, ref float t0, ref float t1)
        {
            if (Mathf.Abs(direction) <= 0.0000001f)
            {
                return distance >= 0f;
            }

            var t = distance / direction;
            if (direction < 0f)
            {
                if (t > t1)
                {
                    return false;
                }

                if (t > t0)
                {
                    t0 = t;
                }
            }
            else
            {
                if (t < t0)
                {
                    return false;
                }

                if (t < t1)
                {
                    t1 = t;
                }
            }

            return true;
        }
    }
}
