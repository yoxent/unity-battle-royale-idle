using BattleRoyale.Maps;
using UnityEngine;

namespace BattleRoyale.CameraSystems
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class SimulationCameraController : MonoBehaviour
    {
        [Header("Active Map")]
        [SerializeField] private MapInstance activeMap;
        [SerializeField] private bool findMapOnStart = true;

        [Header("Map Framing")]
        [Min(1f)]
        [SerializeField] private float mapPaddingPercent = 5f;

        [Min(0.01f)]
        [SerializeField] private float minimumOrthographicSize = 6f;

        [Min(0.01f)]
        [SerializeField] private float maximumOrthographicSize = 2000f;

        [Header("Camera Height")]
        [SerializeField] private bool adjustHeightForTerrain = true;

        [Min(0f)]
        [SerializeField] private float terrainHeightOffset = 16f;

        [SerializeField] private bool keepTopDownRotation = true;

        private Camera simulationCamera;

        public MapInstance ActiveMap => activeMap;

        private void Awake()
        {
            simulationCamera = GetComponent<Camera>();
        }

        private void Start()
        {
            RefreshActiveMap();
        }

        public void SetActiveMap(MapInstance map)
        {
            activeMap = map;
            RefreshActiveMap();
        }

        public void RefreshActiveMap()
        {
            if (activeMap == null && findMapOnStart)
            {
                activeMap = Object.FindFirstObjectByType<MapInstance>();
            }

            if (activeMap == null || activeMap.definition == null)
            {
                Debug.LogWarning(
                    $"{nameof(SimulationCameraController)} could not find an active map definition.",
                    this);
                return;
            }

            if (!activeMap.definition.HasBounds)
            {
                Debug.LogWarning(
                    $"Map '{activeMap.definition.name}' does not have valid bounds.",
                    activeMap);
                return;
            }

            ApplyMapFraming(activeMap.definition);
        }

        private void ApplyMapFraming(MapDefinition map)
        {
            if (simulationCamera == null)
            {
                simulationCamera = GetComponent<Camera>();
            }

            simulationCamera.orthographic = true;

            var aspect = Mathf.Max(0.01f, simulationCamera.aspect);
            var mapWidth = map.WorldSize.x;
            var mapDepth = map.WorldSize.z;
            var requiredVerticalSize = Mathf.Max(mapDepth, mapWidth / aspect);
            var paddingMultiplier = 1f + mapPaddingPercent / 100f;
            var targetSize = requiredVerticalSize * 0.5f * paddingMultiplier;

            simulationCamera.orthographicSize = Mathf.Clamp(
                targetSize,
                minimumOrthographicSize,
                maximumOrthographicSize);

            var center = map.WorldCenter;
            var cameraHeight = transform.position.y;
            if (adjustHeightForTerrain)
            {
                cameraHeight = map.baseHeight +
                               Mathf.Max(0f, map.heightScale) +
                               terrainHeightOffset;
            }

            transform.position = new Vector3(
                center.x,
                cameraHeight,
                center.z);

            if (keepTopDownRotation)
            {
                transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }

        }

        private void OnValidate()
        {
            mapPaddingPercent = Mathf.Max(0f, mapPaddingPercent);
            minimumOrthographicSize = Mathf.Max(0.01f, minimumOrthographicSize);
            maximumOrthographicSize = Mathf.Max(
                minimumOrthographicSize,
                maximumOrthographicSize);
            terrainHeightOffset = Mathf.Max(0f, terrainHeightOffset);
        }

        private void OnDrawGizmosSelected()
        {
            if (activeMap == null ||
                activeMap.definition == null ||
                !activeMap.definition.HasBounds)
            {
                return;
            }

            var definition = activeMap.definition;
            var center = definition.WorldCenter;
            var size = definition.WorldSize;
            size.y = 0.1f;

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
