using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.Maps
{
    public static class DungeonRotundaGeometry
    {
        public const int DefaultSegments = 48;

        public readonly struct Opening
        {
            public Opening(bool vertical, float edge, float spanStart, float spanEnd)
            {
                Vertical = vertical;
                Edge = edge;
                SpanStart = spanStart;
                SpanEnd = spanEnd;
            }

            public bool Vertical { get; }

            public float Edge { get; }

            public float SpanStart { get; }

            public float SpanEnd { get; }
        }

        public readonly struct OutlinePoint
        {
            public OutlinePoint(Vector2 position, int openingIndex)
            {
                Position = position;
                OpeningIndex = openingIndex;
            }

            public Vector2 Position { get; }

            public int OpeningIndex { get; }

            public bool IsMouth => OpeningIndex >= 0;
        }

        public static bool TryOpening(
            int doorX,
            int doorY,
            int directionX,
            int directionY,
            int roomX,
            int roomY,
            int roomWidth,
            int roomHeight,
            out Opening opening)
        {
            opening = default;
            if (roomWidth <= 0 || roomHeight <= 0)
            {
                return false;
            }

            float edge;
            float span;
            bool vertical;
            if (directionX > 0)
            {
                vertical = true;
                edge = doorX + 1;
                span = doorY;
            }
            else if (directionX < 0)
            {
                vertical = true;
                edge = doorX;
                span = doorY;
            }
            else if (directionY > 0)
            {
                vertical = false;
                edge = doorY + 1;
                span = doorX;
            }
            else if (directionY < 0)
            {
                vertical = false;
                edge = doorY;
                span = doorX;
            }
            else
            {
                return false;
            }

            return TryEdgeOpening(
                vertical,
                edge,
                span,
                roomX,
                roomY,
                roomWidth,
                roomHeight,
                out opening);
        }

        public static void CollectOpenings(
            int doorX,
            int doorY,
            int roomX,
            int roomY,
            int roomWidth,
            int roomHeight,
            List<Opening> openings)
        {
            if (openings == null)
            {
                return;
            }

            TryAddCellEdge(openings, true, doorX, doorY, roomX, roomY, roomWidth, roomHeight);
            TryAddCellEdge(openings, true, doorX + 1, doorY, roomX, roomY, roomWidth, roomHeight);
            TryAddCellEdge(openings, false, doorY, doorX, roomX, roomY, roomWidth, roomHeight);
            TryAddCellEdge(openings, false, doorY + 1, doorX, roomX, roomY, roomWidth, roomHeight);
        }

        private static void TryAddCellEdge(
            List<Opening> openings,
            bool vertical,
            int edge,
            int span,
            int roomX,
            int roomY,
            int roomWidth,
            int roomHeight)
        {
            if (!TryEdgeOpening(
                    vertical,
                    edge,
                    span,
                    roomX,
                    roomY,
                    roomWidth,
                    roomHeight,
                    out var opening))
            {
                return;
            }

            for (var i = 0; i < openings.Count; i++)
            {
                var existing = openings[i];
                if (existing.Vertical == opening.Vertical &&
                    Mathf.Abs(existing.Edge - opening.Edge) <= 0.001f &&
                    Mathf.Abs(existing.SpanStart - opening.SpanStart) <= 0.001f &&
                    Mathf.Abs(existing.SpanEnd - opening.SpanEnd) <= 0.001f)
                {
                    return;
                }
            }

            openings.Add(opening);
        }

        private static bool TryEdgeOpening(
            bool vertical,
            float edge,
            float span,
            int roomX,
            int roomY,
            int roomWidth,
            int roomHeight,
            out Opening opening)
        {
            opening = default;
            if (roomWidth <= 0 || roomHeight <= 0)
            {
                return false;
            }

            var roomStart = vertical ? roomX : roomY;
            var roomEnd = roomStart + (vertical ? roomWidth : roomHeight);
            if (Mathf.Abs(edge - roomStart) > 0.001f &&
                Mathf.Abs(edge - roomEnd) > 0.001f)
            {
                return false;
            }

            var spanRoomStart = vertical ? roomY : roomX;
            var spanRoomEnd = spanRoomStart + (vertical ? roomHeight : roomWidth);
            var spanStart = Mathf.Max(span, spanRoomStart);
            var spanEnd = Mathf.Min(span + 1f, spanRoomEnd);
            if (spanEnd - spanStart <= 0.001f)
            {
                return false;
            }

            opening = new Opening(vertical, edge, spanStart, spanEnd);
            return true;
        }

        public static void BuildOutline(
            float centerX,
            float centerY,
            float radiusX,
            float radiusY,
            IReadOnlyList<Opening> openings,
            int segments,
            List<OutlinePoint> loop)
        {
            if (loop == null ||
                radiusX <= 0.001f ||
                radiusY <= 0.001f ||
                segments < 8)
            {
                return;
            }

            var count = segments;
            var samples = new Vector2[count];
            var open = new bool[count];
            var step = Mathf.PI * 2f / count;
            for (var i = 0; i < count; i++)
            {
                samples[i] = PointOnEllipse(centerX, centerY, radiusX, radiusY, i * step);
                open[i] = FacesAnyOpening(
                    samples[i],
                    centerX,
                    centerY,
                    radiusX,
                    radiusY,
                    openings);
            }

            for (var i = 0; i < count; i++)
            {
                var previous = (i + count - 1) % count;
                if (open[i])
                {
                    if (!open[previous])
                    {
                        AddCrossedCorner(
                            loop,
                            samples[previous],
                            samples[i],
                            centerX,
                            centerY,
                            radiusX,
                            radiusY,
                            openings);
                    }

                    continue;
                }

                if (open[previous])
                {
                    AddCrossedCorner(
                        loop,
                        samples[i],
                        samples[previous],
                        centerX,
                        centerY,
                        radiusX,
                        radiusY,
                        openings);
                }

                loop.Add(new OutlinePoint(samples[i], -1));
            }
        }

        public static void AppendWallSegments(
            IReadOnlyList<OutlinePoint> loop,
            List<Vector2> starts,
            List<Vector2> ends)
        {
            if (loop == null || starts == null || ends == null || loop.Count < 2)
            {
                return;
            }

            for (var i = 0; i < loop.Count; i++)
            {
                var start = loop[i];
                var end = loop[(i + 1) % loop.Count];
                if (start.IsMouth &&
                    end.IsMouth &&
                    start.OpeningIndex == end.OpeningIndex)
                {
                    continue;
                }

                if ((end.Position - start.Position).sqrMagnitude <= 0.000001f)
                {
                    continue;
                }

                starts.Add(start.Position);
                ends.Add(end.Position);
            }
        }

        public static Vector2 PointOnEllipse(
            float centerX,
            float centerY,
            float radiusX,
            float radiusY,
            float angle)
        {
            return new Vector2(
                centerX + radiusX * Mathf.Cos(angle),
                centerY + radiusY * Mathf.Sin(angle));
        }

        private static void AddCrossedCorner(
            List<OutlinePoint> loop,
            Vector2 closedPoint,
            Vector2 openPoint,
            float centerX,
            float centerY,
            float radiusX,
            float radiusY,
            IReadOnlyList<Opening> openings)
        {
            var openingIndex = FindOpening(
                openPoint,
                centerX,
                centerY,
                radiusX,
                radiusY,
                openings);
            if (openingIndex < 0)
            {
                return;
            }

            var opening = openings[openingIndex];
            Vector2 first;
            Vector2 second;
            if (opening.Vertical)
            {
                first = new Vector2(opening.Edge, opening.SpanStart);
                second = new Vector2(opening.Edge, opening.SpanEnd);
            }
            else
            {
                first = new Vector2(opening.SpanStart, opening.Edge);
                second = new Vector2(opening.SpanEnd, opening.Edge);
            }

            var mid = (closedPoint + openPoint) * 0.5f;
            var corner = (first - mid).sqrMagnitude <= (second - mid).sqrMagnitude
                ? first
                : second;
            loop.Add(new OutlinePoint(corner, openingIndex));
        }

        private static int FindOpening(
            Vector2 point,
            float centerX,
            float centerY,
            float radiusX,
            float radiusY,
            IReadOnlyList<Opening> openings)
        {
            if (openings == null)
            {
                return -1;
            }

            for (var i = 0; i < openings.Count; i++)
            {
                if (FacesOpening(point, centerX, centerY, radiusX, radiusY, openings[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool FacesAnyOpening(
            Vector2 point,
            float centerX,
            float centerY,
            float radiusX,
            float radiusY,
            IReadOnlyList<Opening> openings)
        {
            return FindOpening(point, centerX, centerY, radiusX, radiusY, openings) >= 0;
        }

        private static bool FacesOpening(
            Vector2 point,
            float centerX,
            float centerY,
            float radiusX,
            float radiusY,
            Opening opening)
        {
            var nx = (point.x - centerX) / radiusX;
            var ny = (point.y - centerY) / radiusY;
            if (opening.Vertical)
            {
                if (Mathf.Abs(nx) < Mathf.Abs(ny))
                {
                    return false;
                }

                var edge = centerX + Mathf.Sign(nx) * radiusX;
                if (Mathf.Abs(edge - opening.Edge) > 0.02f)
                {
                    return false;
                }

                return point.y >= opening.SpanStart - 0.02f &&
                       point.y <= opening.SpanEnd + 0.02f;
            }

            if (Mathf.Abs(ny) < Mathf.Abs(nx))
            {
                return false;
            }

            var horizontalEdge = centerY + Mathf.Sign(ny) * radiusY;
            if (Mathf.Abs(horizontalEdge - opening.Edge) > 0.02f)
            {
                return false;
            }

            return point.x >= opening.SpanStart - 0.02f &&
                   point.x <= opening.SpanEnd + 0.02f;
        }
    }
}
