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
                Assert.AreEqual(3f, definition.wallHeight);
                Assert.AreEqual(1f, definition.wallThickness);
                Assert.AreEqual(6f, definition.columnSize);
                Assert.AreEqual(new Vector2(-0.5f, -0.5f), definition.columnOffset);
                Assert.AreEqual(1f, definition.unitScale);
                Assert.IsTrue(definition.useImageCornerFit);
                Assert.IsTrue(definition.editImageCornersInScene);

                var geometry = DungeonMapGeometry.Read(definition);

                Assert.AreEqual(20f, geometry.CellWorldSize);
                Assert.AreEqual(0f, geometry.BaseHeight);
                Assert.AreEqual(3f, geometry.WallHeight);
                Assert.AreEqual(1f, geometry.WallThickness);
                Assert.AreEqual(6f, geometry.ColumnSize);
                Assert.AreEqual(new Vector2(-0.5f, -0.5f), geometry.ColumnOffset);
                Assert.AreEqual(1f, geometry.UnitScale);
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
            definition.columnOffset = new Vector2(0.25f, -0.5f);
            definition.unitScale = 6f;

            try
            {
                var geometry = DungeonMapGeometry.Read(definition);

                Assert.AreEqual(12f, geometry.CellWorldSize);
                Assert.AreEqual(4f, geometry.BaseHeight);
                Assert.AreEqual(8f, geometry.WallHeight);
                Assert.AreEqual(2.5f, geometry.WallThickness);
                Assert.AreEqual(3f, geometry.ColumnSize);
                Assert.AreEqual(new Vector2(0.25f, -0.5f), geometry.ColumnOffset);
                Assert.AreEqual(6f, geometry.UnitScale);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }
    }
}
