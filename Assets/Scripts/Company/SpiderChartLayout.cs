using System;

namespace BattleRoyale.Company
{
    public static class SpiderChartLayout
    {
        public const int AxisCount = 6;
        public const int RingCount = 5;

        public static bool HasValues(int[] values)
        {
            if (values == null || values.Length != AxisCount)
            {
                return false;
            }

            for (var i = 0; i < values.Length; i++)
            {
                if (values[i] < 0 || values[i] > 100)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool TryRingRadius(int ring, out float radius)
        {
            radius = 0f;
            if (ring < 0 || ring >= RingCount)
            {
                return false;
            }

            radius = (ring + 1) / (float)RingCount;
            return true;
        }

        public static bool TryUnitPoint(int axis, int value, out float x, out float y)
        {
            x = 0f;
            y = 0f;
            if (axis < 0 || axis >= AxisCount || value < 0 || value > 100)
            {
                return false;
            }

            var radius = value / 100d;
            var angle = Math.PI / 2d - axis * (Math.PI / 3d);
            x = (float)(Math.Cos(angle) * radius);
            y = (float)(Math.Sin(angle) * radius);
            return true;
        }

        public static bool TryFillTriangle(int sector, out int center, out int from, out int to)
        {
            center = 0;
            from = 0;
            to = 0;
            if (sector < 0 || sector >= AxisCount)
            {
                return false;
            }

            from = sector + 1;
            to = (sector + 1) % AxisCount + 1;
            return true;
        }

        public static bool TryRingPoint(int ring, int axis, out float x, out float y)
        {
            x = 0f;
            y = 0f;
            if (!TryRingRadius(ring, out var radius))
            {
                return false;
            }

            return TryUnitPoint(axis, (int)Math.Round(radius * 100d), out x, out y);
        }
    }
}
