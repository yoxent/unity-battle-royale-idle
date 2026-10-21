using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.Maps
{
    public readonly struct DungeonColumnBox
    {
        public DungeonColumnBox(Vector3 center, Vector3 size)
        {
            Center = center;
            Size = size;
        }

        public Vector3 Center { get; }

        public Vector3 Size { get; }
    }

    public static class DungeonColumnBoxes
    {
        public static void Collect(
            IReadOnlyList<Vector2Int> columns,
            float columnSize,
            Vector2 columnOffset,
            float wallHeight,
            float baseHeight,
            bool clipToMap,
            Vector2 clipMin,
            Vector2 clipMax,
            Func<float, float, float, Vector3> gridToWorld,
            List<DungeonColumnBox> boxes)
        {
            if (columns == null ||
                boxes == null ||
                gridToWorld == null ||
                columnSize <= 0.001f ||
                wallHeight <= 0.001f)
            {
                return;
            }

            var size = new Vector3(columnSize, wallHeight, columnSize);
            for (var i = 0; i < columns.Count; i++)
            {
                var cell = columns[i];
                var center = gridToWorld(
                    cell.x + columnOffset.x + 0.5f,
                    cell.y + columnOffset.y + 0.5f,
                    baseHeight + wallHeight * 0.5f);
                if (clipToMap &&
                    !MapPlayBounds.Contains(new Vector2(center.x, center.z), clipMin, clipMax))
                {
                    continue;
                }

                boxes.Add(new DungeonColumnBox(center, size));
            }
        }
    }
}
