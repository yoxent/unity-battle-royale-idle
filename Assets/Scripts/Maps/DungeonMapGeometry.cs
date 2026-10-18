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
            float unitScale)
        {
            CellWorldSize = cellWorldSize;
            BaseHeight = baseHeight;
            WallHeight = wallHeight;
            WallThickness = wallThickness;
            ColumnSize = columnSize;
            UnitScale = unitScale;
        }

        public float CellWorldSize { get; }
        public float BaseHeight { get; }
        public float WallHeight { get; }
        public float WallThickness { get; }
        public float ColumnSize { get; }
        public float UnitScale { get; }
    }

    public static class DungeonMapGeometry
    {
        public const float DefaultCellWorldSize = 20f;
        public const float DefaultBaseHeight = 0f;
        public const float DefaultWallHeight = 20f;
        public const float DefaultWallThickness = 1f;
        public const float DefaultColumnSize = 1.5f;
        public const float DefaultUnitScale = 10f;

        public static DungeonMapGeometrySettings Defaults { get; } = new(
            DefaultCellWorldSize,
            DefaultBaseHeight,
            DefaultWallHeight,
            DefaultWallThickness,
            DefaultColumnSize,
            DefaultUnitScale);

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
                definition.unitScale);
        }

        public static void Write(MapDefinition definition, DungeonMapGeometrySettings geometry)
        {
            definition.worldScale = geometry.CellWorldSize;
            definition.baseHeight = geometry.BaseHeight;
            definition.wallHeight = geometry.WallHeight;
            definition.wallThickness = geometry.WallThickness;
            definition.columnSize = geometry.ColumnSize;
            definition.unitScale = geometry.UnitScale;
        }
    }
}
