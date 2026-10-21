using BattleRoyale.Maps;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class DungeonMapAuthoringTests
    {
        [Test]
        public void NewMapUsesAuthoringDefaults()
        {
            var definition = ScriptableObject.CreateInstance<MapDefinition>();
            definition.worldScale = 4f;
            definition.wallHeight = 3f;
            definition.wallThickness = 0.7f;
            definition.overrideNavMeshVoxelSize = true;
            definition.navMeshVoxelSize = 0.5f;
            definition.replaceGeneratedRoot = false;
            definition.disableOtherGeneratedMaps = false;
            definition.setAsActiveCameraMap = false;
            definition.useImageCornerFit = true;
            definition.imageUpperLeftUv = new Vector2(0.2f, 0.8f);
            definition.imageLowerRightUv = new Vector2(0.9f, 0.1f);
            definition.editImageCornersInScene = true;
            definition.columnOffset = new Vector2(2f, -1f);
            definition.editPointsInSceneView = false;
            definition.supportedGameModes.Add(MapGameMode.BattleRoyale);

            try
            {
                DungeonMapAuthoring.ApplyNewMapDefaults(definition);

                var geometry = DungeonMapGeometry.Read(definition);
                var authoring = DungeonMapAuthoring.Read(definition);
                Assert.AreEqual(DungeonMapGeometry.DefaultCellWorldSize, geometry.CellWorldSize);
                Assert.AreEqual(DungeonMapGeometry.DefaultBaseHeight, geometry.BaseHeight);
                Assert.AreEqual(DungeonMapGeometry.DefaultWallHeight, geometry.WallHeight);
                Assert.AreEqual(DungeonMapGeometry.DefaultWallThickness, geometry.WallThickness);
                Assert.AreEqual(DungeonMapGeometry.DefaultColumnSize, geometry.ColumnSize);
                Assert.AreEqual(DungeonMapGeometry.DefaultColumnOffset, geometry.ColumnOffset);
                Assert.AreEqual(DungeonMapGeometry.DefaultUnitScale, geometry.UnitScale);
                AssertAuthoringEquals(DungeonMapAuthoring.Defaults, authoring);
                Assert.AreEqual(0, definition.supportedGameModes.Count);
                Assert.AreEqual(0, definition.modeConfigurations.Count);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void SavedAuthoringLoadsInsteadOfDefaults()
        {
            var definition = ScriptableObject.CreateInstance<MapDefinition>();
            definition.overrideNavMeshVoxelSize = true;
            definition.navMeshVoxelSize = 0.42f;
            definition.replaceGeneratedRoot = false;
            definition.disableOtherGeneratedMaps = false;
            definition.setAsActiveCameraMap = false;
            definition.useImageCornerFit = true;
            definition.imageUpperLeftUv = new Vector2(0.15f, 0.75f);
            definition.imageLowerRightUv = new Vector2(0.85f, 0.2f);
            definition.editImageCornersInScene = true;
            definition.editPointsInSceneView = false;

            try
            {
                AssertAuthoringEquals(
                    new DungeonMapAuthoringSettings(
                        true,
                        0.42f,
                        false,
                        false,
                        false,
                        true,
                        new Vector2(0.15f, 0.75f),
                        new Vector2(0.85f, 0.2f),
                        true,
                        false),
                    DungeonMapAuthoring.Read(definition));
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        private static void AssertAuthoringEquals(
            DungeonMapAuthoringSettings expected,
            DungeonMapAuthoringSettings actual)
        {
            Assert.AreEqual(expected.OverrideNavMeshVoxelSize, actual.OverrideNavMeshVoxelSize);
            Assert.AreEqual(expected.NavMeshVoxelSize, actual.NavMeshVoxelSize);
            Assert.AreEqual(expected.ReplaceGeneratedRoot, actual.ReplaceGeneratedRoot);
            Assert.AreEqual(expected.DisableOtherGeneratedMaps, actual.DisableOtherGeneratedMaps);
            Assert.AreEqual(expected.SetAsActiveCameraMap, actual.SetAsActiveCameraMap);
            Assert.AreEqual(expected.UseImageCornerFit, actual.UseImageCornerFit);
            Assert.AreEqual(expected.ImageUpperLeftUv, actual.ImageUpperLeftUv);
            Assert.AreEqual(expected.ImageLowerRightUv, actual.ImageLowerRightUv);
            Assert.AreEqual(expected.EditImageCornersInScene, actual.EditImageCornersInScene);
            Assert.AreEqual(expected.EditPointsInSceneView, actual.EditPointsInSceneView);
        }
    }
}
