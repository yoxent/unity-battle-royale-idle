using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BattleRoyale.Maps
{
    [CreateAssetMenu(
        fileName = "MapDefinition",
        menuName = "Battle Royale/Maps/Map Definition")]
    public sealed class MapDefinition : ScriptableObject
    {
        [Header("Source")]
        public TextAsset layoutJson;
        public TextAsset narrativeMarkdown;
        public Texture2D visualMap;
        public Sprite visualSprite;
        public Texture2D heightMap;
        public Material visualMaterial;

        [Header("World Mapping")]
        [Min(0.0001f)]
        public float worldScale = DungeonMapGeometry.DefaultCellWorldSize;
        public bool flipHeightY;
        public Vector2 sourceMin;
        public Vector2 sourceMax;

        [Header("Terrain")]
        public float baseHeight;

        [Min(0f)]
        public float heightScale = 20f;

        [Min(2)]
        public int heightResolution = 129;

        [Header("Collision")]
        [Min(0f)]
        public float wallHeight = DungeonMapGeometry.DefaultWallHeight;

        [Min(0.001f)]
        public float wallThickness = DungeonMapGeometry.DefaultWallThickness;

        [FormerlySerializedAs("dungeonUnitScale")]
        [Min(0.01f)]
        public float unitScale = DungeonMapGeometry.DefaultUnitScale;

        public bool generateBuildingWalls = true;
        public bool generateTreeColliders;
        public Vector2 treeUpperLeft;
        public Vector2 treeLowerRight;
        public bool editTreeCornersInScene;

        [Min(0.01f)]
        public float treeRadiusMultiplier = 1f;

        [Min(0.01f)]
        public float treeHeight = 2f;

        [Header("Image Fit")]
        public bool useImageCornerFit;

        public Vector2 imageUpperLeftUv = new(0f, 1f);

        public Vector2 imageLowerRightUv = new(1f, 0f);

        public bool editImageCornersInScene;

        [Header("Dungeon Authoring")]
        [Min(0.01f)]
        public float columnSize = DungeonMapGeometry.DefaultColumnSize;

        public Vector2 columnOffset = DungeonMapGeometry.DefaultColumnOffset;

        public bool overrideNavMeshVoxelSize;

        [Min(0.01f)]
        public float navMeshVoxelSize = DungeonMapAuthoring.DefaultNavMeshVoxelSize;

        public bool replaceGeneratedRoot = true;

        public bool disableOtherGeneratedMaps = true;

        [Tooltip(
            "When dungeon generation is enabled for this option, the baked prefab is placed as the shared active map.")]
        public bool setAsActiveCameraMap = true;

        public bool editPointsInSceneView = true;

        [Header("Bake")]
        [Tooltip(
            "Baked floor, walls, colliders, and NavMesh. A simulation slot places one instance of this prefab.")]
        public GameObject bakedPrefab;

        [Header("Imported Metadata")]
        public string generator;
        public string generatorVersion;
        public MapFeatureSummary[] features = Array.Empty<MapFeatureSummary>();

        [Header("Tactical Anchors")]
        public List<MapTacticalAnchor> tacticalAnchors = new();

        [Header("Game Modes")]
        public List<MapGameMode> supportedGameModes = new();
        public List<MapModeConfiguration> modeConfigurations = new();

        public bool HasBounds =>
            sourceMax.x > sourceMin.x &&
            sourceMax.y > sourceMin.y;

        public bool SupportsMode(MapGameMode mode) =>
            supportedGameModes != null &&
            supportedGameModes.Contains(mode);

        public bool TryGetSingleConfiguration(MapGameMode mode, out MapModeConfiguration configuration)
        {
            configuration = null;
            if (modeConfigurations == null)
            {
                return false;
            }

            foreach (var candidate in modeConfigurations)
            {
                if (candidate == null || candidate.mode != mode)
                {
                    continue;
                }

                if (configuration != null)
                {
                    configuration = null;
                    return false;
                }

                configuration = candidate;
            }

            return configuration != null;
        }

        public Vector2 SourceSize => sourceMax - sourceMin;

        public Vector2 SourceCenter => (sourceMin + sourceMax) * 0.5f;

        public Vector3 WorldCenter => new Vector3(
            SourceCenter.x * worldScale,
            baseHeight,
            SourceCenter.y * worldScale);

        public Vector3 WorldSize => new Vector3(
            SourceSize.x * worldScale,
            heightScale,
            SourceSize.y * worldScale);
    }

    [Serializable]
    public sealed class MapFeatureSummary
    {
        public string id;
        public string type;
        public int geometryCount;
        public int coordinateCount;
    }

    [Serializable]
    public sealed class MapTacticalAnchor
    {
        public string id = "Anchor";
        public Vector3 position;

        [Min(0f)]
        public float weight = 1f;
    }
}
