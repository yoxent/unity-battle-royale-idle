using UnityEngine;

namespace BattleRoyale.Maps
{
    public static class DungeonFloorBox
    {
        public const float Thickness = 2f;

        public static void GetCenterAndSize(
            Vector3 cornerA,
            Vector3 cornerB,
            Vector3 cornerC,
            Vector3 cornerD,
            float top,
            float thickness,
            out Vector3 center,
            out Vector3 size)
        {
            var minX = Mathf.Min(Mathf.Min(cornerA.x, cornerB.x), Mathf.Min(cornerC.x, cornerD.x));
            var maxX = Mathf.Max(Mathf.Max(cornerA.x, cornerB.x), Mathf.Max(cornerC.x, cornerD.x));
            var minZ = Mathf.Min(Mathf.Min(cornerA.z, cornerB.z), Mathf.Min(cornerC.z, cornerD.z));
            var maxZ = Mathf.Max(Mathf.Max(cornerA.z, cornerB.z), Mathf.Max(cornerC.z, cornerD.z));
            center = new Vector3(
                (minX + maxX) * 0.5f,
                top - thickness * 0.5f,
                (minZ + maxZ) * 0.5f);
            size = new Vector3(maxX - minX, thickness, maxZ - minZ);
        }
    }
}
