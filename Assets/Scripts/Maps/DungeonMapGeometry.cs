using UnityEngine;

namespace BattleRoyale.Maps
{
    public readonly struct DungeonMapGeometrySettings
    {
        public DungeonMapGeometrySettings(
            float cellWorldSize,
            float baseHeight,
            float wallHeight,
            float wallThickness,
            float columnSize,
            float unitScale,
            Vector2 columnOffset)
        {
            CellWorldSize = cellWorldSize;
            BaseHeight = baseHeight;
            WallHeight = wallHeight;
            WallThickness = wallThickness;
            ColumnSize = columnSize;
            UnitScale = unitScale;
            ColumnOffset = columnOffset;
        }

        public float CellWorldSize { get; }
        public float BaseHeight { get; }
        public float WallHeight { get; }
        public float WallThickness { get; }
        public float ColumnSize { get; }
        public float UnitScale { get; }
        public Vector2 ColumnOffset { get; }
    }

    public static class DungeonMapGeometry
    {
        public const float DefaultCellWorldSize = 20f;
        public const float DefaultBaseHeight = 0f;
        public const float DefaultWallHeight = 3f;
        public const float DefaultWallThickness = 1f;
        public const float DefaultColumnSize = 6f;
        public const float DefaultUnitScale = 1f;
        public static readonly Vector2 DefaultColumnOffset = new(-0.5f, -0.5f);

        public static DungeonMapGeometrySettings Defaults { get; } = new(
            DefaultCellWorldSize,
            DefaultBaseHeight,
            DefaultWallHeight,
            DefaultWallThickness,
            DefaultColumnSize,
            DefaultUnitScale,
            DefaultColumnOffset);

        public static Vector3 UnitLocalScale(float unitScale)
        {
            var scale = unitScale < 0.01f ? 0.01f : unitScale;
            return new Vector3(scale, scale, scale);
        }

        public static DungeonMapGeometrySettings Read(MapDefinition definition)
        {
            if (definition == null)
            {
                return Defaults;
            }

            return new DungeonMapGeometrySettings(
                definition.worldScale,
                definition.baseHeight,
                definition.wallHeight,
                definition.wallThickness,
                definition.columnSize,
                definition.unitScale,
                definition.columnOffset);
        }

        public static void Write(MapDefinition definition, DungeonMapGeometrySettings geometry)
        {
            definition.worldScale = geometry.CellWorldSize;
            definition.baseHeight = geometry.BaseHeight;
            definition.wallHeight = geometry.WallHeight;
            definition.wallThickness = geometry.WallThickness;
            definition.columnSize = geometry.ColumnSize;
            definition.unitScale = geometry.UnitScale;
            definition.columnOffset = geometry.ColumnOffset;
        }
    }
}
