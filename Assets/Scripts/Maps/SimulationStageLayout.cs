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
            for (var slot = 0; slot < SlotCount; slot++)
            {
                centers[slot] = new Vector3(slot * stride, 0f, 0f);
            }

            return centers;
        }

        public static void PlaceBarrier(
            BoxCollider barrier,
            Vector3 leftCenter,
            Vector3 rightCenter,
            float thickness,
            float depth,
            float floorY,
            float topY)
        {
            var mid = (leftCenter + rightCenter) * 0.5f;
            var height = Mathf.Max(1f, topY - floorY);
            var scale = new Vector3(
                Mathf.Max(0.5f, thickness),
                height,
                Mathf.Max(1f, depth));
            barrier.isTrigger = false;
            var position = new Vector3(
                mid.x,
                floorY + height * 0.5f,
                mid.z);
            if (barrier.transform.position == position &&
                barrier.transform.localScale == scale &&
                barrier.size == Vector3.one)
            {
                return;
            }

            barrier.transform.position = position;
            // Unit cube mesh and a size of 1 both follow this scale.
            barrier.transform.localScale = scale;
            barrier.size = Vector3.one;
        }
    }
}
