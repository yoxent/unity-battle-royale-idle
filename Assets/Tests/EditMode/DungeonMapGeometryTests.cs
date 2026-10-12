using BattleRoyale.Maps;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class DungeonMapGeometryTests
    {
        [Test]
        public void NewMapDefinitionUsesDungeonGeometryDefaults()
        {
            var definition = ScriptableObject.CreateInstance<MapDefinition>();

            try
            {
                Assert.AreEqual(20f, definition.worldScale);
                Assert.AreEqual(20f, definition.wallHeight);
                Assert.AreEqual(1f, definition.wallThickness);
                Assert.AreEqual(1.5f, definition.columnSize);
                Assert.AreEqual(10f, definition.dungeonUnitScale);

                var geometry = DungeonMapGeometry.Read(definition);

                Assert.AreEqual(20f, geometry.CellWorldSize);
                Assert.AreEqual(0f, geometry.BaseHeight);
                Assert.AreEqual(20f, geometry.WallHeight);
                Assert.AreEqual(1f, geometry.WallThickness);
                Assert.AreEqual(1.5f, geometry.ColumnSize);
                Assert.AreEqual(10f, geometry.DungeonUnitScale);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void SavedGeometryLoadsInsteadOfDefaults()
        {
            var definition = ScriptableObject.CreateInstance<MapDefinition>();
            definition.generator = "dungeon";
            definition.worldScale = 12f;
            definition.baseHeight = 4f;
            definition.wallHeight = 8f;
            definition.wallThickness = 2.5f;
            definition.columnSize = 3f;
            definition.dungeonUnitScale = 6f;

            try
            {
                var geometry = DungeonMapGeometry.Read(definition);

                Assert.AreEqual(12f, geometry.CellWorldSize);
                Assert.AreEqual(4f, geometry.BaseHeight);
                Assert.AreEqual(8f, geometry.WallHeight);
                Assert.AreEqual(2.5f, geometry.WallThickness);
                Assert.AreEqual(3f, geometry.ColumnSize);
                Assert.AreEqual(6f, geometry.DungeonUnitScale);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }
    }
}
