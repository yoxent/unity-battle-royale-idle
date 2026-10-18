using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.Maps
{
    public readonly struct DungeonMapAuthoringSettings
    {
        public DungeonMapAuthoringSettings(
            bool overrideNavMeshVoxelSize,
            float navMeshVoxelSize,
            bool replaceGeneratedRoot,
            bool disableOtherGeneratedMaps,
            bool setAsActiveCameraMap,
            bool useImageCornerFit,
            Vector2 imageUpperLeftUv,
            Vector2 imageLowerRightUv,
            bool editImageCornersInScene,
            bool editPointsInSceneView)
        {
            OverrideNavMeshVoxelSize = overrideNavMeshVoxelSize;
            NavMeshVoxelSize = navMeshVoxelSize;
            ReplaceGeneratedRoot = replaceGeneratedRoot;
            DisableOtherGeneratedMaps = disableOtherGeneratedMaps;
            SetAsActiveCameraMap = setAsActiveCameraMap;
            UseImageCornerFit = useImageCornerFit;
            ImageUpperLeftUv = imageUpperLeftUv;
            ImageLowerRightUv = imageLowerRightUv;
            EditImageCornersInScene = editImageCornersInScene;
            EditPointsInSceneView = editPointsInSceneView;
        }

        public bool OverrideNavMeshVoxelSize { get; }
        public float NavMeshVoxelSize { get; }
        public bool ReplaceGeneratedRoot { get; }
        public bool DisableOtherGeneratedMaps { get; }
        public bool SetAsActiveCameraMap { get; }
        public bool UseImageCornerFit { get; }
        public Vector2 ImageUpperLeftUv { get; }
        public Vector2 ImageLowerRightUv { get; }
        public bool EditImageCornersInScene { get; }
        public bool EditPointsInSceneView { get; }
    }

    public static class DungeonMapAuthoring
    {
        public const float DefaultNavMeshVoxelSize = 0.16666667f;

        public static readonly Vector2 DefaultImageUpperLeftUv = new(0f, 1f);

        public static readonly Vector2 DefaultImageLowerRightUv = new(1f, 0f);

        public static DungeonMapAuthoringSettings Defaults { get; } = new(
            false,
            DefaultNavMeshVoxelSize,
            true,
            true,
            true,
            false,
            DefaultImageUpperLeftUv,
            DefaultImageLowerRightUv,
            false,
            true);

        public static DungeonMapAuthoringSettings Read(MapDefinition definition)
        {
            if (definition == null)
            {
                return Defaults;
            }

            return new DungeonMapAuthoringSettings(
                definition.overrideNavMeshVoxelSize,
                definition.navMeshVoxelSize,
                definition.replaceGeneratedRoot,
                definition.disableOtherGeneratedMaps,
                definition.setAsActiveCameraMap,
                definition.useImageCornerFit,
                definition.imageUpperLeftUv,
                definition.imageLowerRightUv,
                definition.editImageCornersInScene,
                definition.editPointsInSceneView);
        }

        public static void Write(
            MapDefinition definition,
            DungeonMapAuthoringSettings settings)
        {
            definition.overrideNavMeshVoxelSize = settings.OverrideNavMeshVoxelSize;
            definition.navMeshVoxelSize = settings.NavMeshVoxelSize;
            definition.replaceGeneratedRoot = settings.ReplaceGeneratedRoot;
            definition.disableOtherGeneratedMaps = settings.DisableOtherGeneratedMaps;
            definition.setAsActiveCameraMap = settings.SetAsActiveCameraMap;
            definition.useImageCornerFit = settings.UseImageCornerFit;
            definition.imageUpperLeftUv = settings.ImageUpperLeftUv;
            definition.imageLowerRightUv = settings.ImageLowerRightUv;
            definition.editImageCornersInScene = settings.EditImageCornersInScene;
            definition.editPointsInSceneView = settings.EditPointsInSceneView;
        }

        public static void ApplyNewMapDefaults(MapDefinition definition)
        {
            DungeonMapGeometry.Write(definition, DungeonMapGeometry.Defaults);
            Write(definition, Defaults);
            definition.supportedGameModes = new List<MapGameMode>();
            definition.modeConfigurations = new List<MapModeConfiguration>();
        }
    }
}
