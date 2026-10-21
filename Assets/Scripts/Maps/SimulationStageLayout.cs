using UnityEngine;

namespace BattleRoyale.Maps
{
    public static class SimulationStageLayout
    {
        public const int SlotCount = 3;

        public static Vector3[] Centers(float largestWidth, float gap)
        {
            var stride = Mathf.Max(0f, largestWidth) + Mathf.Max(0f, gap);
            var centers = new Vector3[SlotCount];
            // The origin stays clear for the dashboard preview map.
            for (var slot = 0; slot < SlotCount; slot++)
            {
                centers[slot] = new Vector3((slot + 1) * stride, 0f, 0f);
            }

            return centers;
        }

        public static Vector3 PreviewCenter(float largestSpan, float gap)
        {
            var stride = Mathf.Max(0f, largestSpan) + Mathf.Max(0f, gap);
            return new Vector3(0f, 0f, -stride);
        }
    }
}
