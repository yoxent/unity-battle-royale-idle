using System;
using System.Collections.Generic;
using UnityEngine;

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
        public Texture2D heightMap;
        public Material visualMaterial;

        [Header("World Mapping")]
        [Min(0.0001f)]
        public float worldScale = 1f;
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
        public float wallHeight = 3f;

        [Min(0.001f)]
        public float wallThickness = 0.7f;

        public bool generateBuildingWalls = true;
        public bool generateTreeColliders;

        [Header("Dungeon Image Fit")]
        public bool useImageCornerFit;

        public Vector2 imageUpperLeftUv = new(0f, 1f);

        public Vector2 imageLowerRightUv = new(1f, 0f);

        public bool editImageCornersInScene;

        [Header("Dungeon Authoring")]
        public bool showDungeonWalls;

        public bool overrideNavMeshVoxelSize;

        [Min(0.01f)]
        public float navMeshVoxelSize = 0.16666667f;

        public bool replaceGeneratedRoot = true;

        public bool disableOtherGeneratedMaps = true;

        [Tooltip(
            "When dungeon generation is enabled for this option, the baked prefab is placed as the shared active map.")]
        public bool setAsActiveCameraMap = true;

        [Header("Bake")]
        [Tooltip(
            "Baked floor, walls, colliders, and NavMesh. The active map manager places one instance of this prefab.")]
        public GameObject bakedPrefab;

        [Header("Imported Metadata")]
        public string generator;
        public string generatorVersion;
        public MapFeatureSummary[] features = Array.Empty<MapFeatureSummary>();

        [Header("Game Modes")]
        public List<MapGameMode> supportedGameModes = new();
        public List<MapModeConfiguration> modeConfigurations = new();

        public bool HasBounds =>
            sourceMax.x > sourceMin.x &&
            sourceMax.y > sourceMin.y;

        public bool SupportsMode(MapGameMode mode) =>
            supportedGameModes != null &&
            supportedGameModes.Contains(mode);

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
}
