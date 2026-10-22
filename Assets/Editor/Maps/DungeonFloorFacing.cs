#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.Maps.EditorTools
{
    internal static class DungeonFloorFacing
    {
        public static int CountDownward(
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<int> triangles)
        {
            if (vertices == null || triangles == null)
            {
                return 0;
            }

            var downward = 0;
            for (var i = 0; i + 2 < triangles.Count; i += 3)
            {
                var cross = Vector3.Cross(
                    vertices[triangles[i + 1]] - vertices[triangles[i]],
                    vertices[triangles[i + 2]] - vertices[triangles[i]]);
                if (cross.sqrMagnitude <= 0.00000001f)
                {
                    continue;
                }

                if (cross.y < 0f)
                {
                    downward++;
                }
            }

            return downward;
        }

        public static string DescribeDownward(int downward)
        {
            if (downward == 1)
            {
                return "1 floor triangle faces down. The navigation bake drops it and leaves a hole.";
            }

            return downward +
                   " floor triangles face down. The navigation bake drops them and leaves a hole.";
        }

        // Rim points walk forward around the room in grid space. World Z flips
        // that axis, and this index order is what still faces up afterward.
        public static void AddGridFan(List<int> triangles, int center, int rimStart, int rimCount)
        {
            for (var i = 0; i < rimCount; i++)
            {
                var next = (i + 1) % rimCount;
                triangles.Add(center);
                triangles.Add(rimStart + i);
                triangles.Add(rimStart + next);
            }
        }
    }
}
#endif
