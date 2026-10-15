using System;
using BattleRoyale.Events;
using BattleRoyale.Maps;
using UnityEngine;

namespace BattleRoyale.CameraSystems
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public sealed class SimulationCameraController : MonoBehaviour
    {
        [Header("Active Map")]
        [Tooltip("Assigned by the Active Map manager.")]
        [SerializeField] private MapInstance activeMap;

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

        [Header("Watch Fade")]
        [SerializeField] private WatchFade watchFade;

        private Camera simulationCamera;
        private Action onWatchFramed;

        public MapInstance ActiveMap => activeMap;

        private void Awake()
        {
            simulationCamera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ActiveMapChanged>(HandleActiveMapChanged);
            SyncFromManager();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ActiveMapChanged>(HandleActiveMapChanged);
            CancelWatchFade();
        }

        // Frames the map at once, with no fade. Used when the stage first places its maps.
        public void Show(MapInstance map)
        {
            if (!CanFrame(map))
            {
                return;
            }

            CancelWatchFade();
            ApplyMapFraming(map);
            activeMap = map;
        }

        // Covered cut: fade to black, frame the destination map, fade back in.
        // Returns false when the fade does not start. onFramed runs after RevealMap
        // applies the destination frame. CancelWatchFade drops a pending onFramed.
        public bool Watch(MapInstance map, Action onFramed)
        {
            if (!CanFrame(map))
            {
                return false;
            }

            if (watchFade == null)
            {
                Debug.LogError(
                    $"{nameof(SimulationCameraController)} needs a watch fade.",
                    this);
                return false;
            }

            onWatchFramed = onFramed;
            if (!watchFade.Cover(() => RevealMap(map)))
            {
                onWatchFramed = null;
                return false;
            }

            return true;
        }

        private void RevealMap(MapInstance map)
        {
            ApplyMapFraming(map);
            activeMap = map;
            var framed = onWatchFramed;
            onWatchFramed = null;
            framed?.Invoke();
            watchFade?.Reveal();
        }

        private void CancelWatchFade()
        {
            onWatchFramed = null;
            if (watchFade != null)
            {
                watchFade.Clear();
            }
        }

        private bool CanFrame(MapInstance map)
        {
            if (map == null || map.definition == null || !map.definition.HasBounds)
            {
                Debug.LogError(
                    $"{nameof(SimulationCameraController)} cannot frame a map without a definition and bounds.",
                    this);
                return false;
            }

            return true;
        }

        private void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            SyncFromManager(logIfMissing: true);
        }

        public void SetActiveMap(MapInstance map)
        {
            var manager = ActiveMapManager.Find();
            if (manager == null)
            {
                ApplyActiveMap(map);
                return;
            }

            manager.SetActiveMap(map);
        }

        public void SyncFromManager()
        {
            SyncFromManager(logIfMissing: false);
        }

        public void RefreshActiveMap()
        {
            if (activeMap == null)
            {
                var managerMap = ActiveMapManager.Find()?.ActiveMap;
                if (MapObjectIsActive(managerMap))
                {
                    activeMap = managerMap;
                }
            }

            if (!MapObjectIsActive(activeMap))
            {
                if (activeMap == null)
                {
                    Debug.LogWarning(
                        $"{nameof(SimulationCameraController)} could not find an active map definition.",
                        this);
                }

                return;
            }

            if (activeMap.definition == null)
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

            ApplyMapFraming(activeMap);
        }

        private void HandleActiveMapChanged(ActiveMapChanged change)
        {
            if (change.CurrentIsTemporary &&
                (gameObject.hideFlags & HideFlags.HideAndDontSave) == 0)
            {
                return;
            }

            ApplyActiveMap(change.Current);
        }

        private void SyncFromManager(bool logIfMissing)
        {
            var manager = ActiveMapManager.Find();
            if (manager == null || manager.ActiveMap == null)
            {
                if (logIfMissing)
                {
                    Debug.LogWarning(
                        $"{nameof(SimulationCameraController)} could not find an active map.",
                        this);
                }

                return;
            }

            if (!MapObjectIsActive(manager.ActiveMap))
            {
                return;
            }

            if (activeMap == manager.ActiveMap)
            {
                return;
            }

            ApplyActiveMap(manager.ActiveMap);
        }

        private static bool MapObjectIsActive(MapInstance map)
        {
            return map != null && map.gameObject.activeInHierarchy;
        }

        private void ApplyActiveMap(MapInstance map)
        {
            activeMap = map;
            RefreshActiveMap();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.EditorUtility.SetDirty(transform);
            }
#endif
        }

        private void ApplyMapFraming(MapInstance map)
        {
            if (simulationCamera == null)
            {
                simulationCamera = GetComponent<Camera>();
            }

            simulationCamera.orthographic = true;

            var definition = map.definition;
            simulationCamera.orthographicSize = SimulationCameraFraming.OrthographicSize(
                definition.WorldSize.x,
                definition.WorldSize.z,
                simulationCamera.aspect,
                mapPaddingPercent,
                minimumOrthographicSize,
                maximumOrthographicSize);

            var worldCenter = map.WorldPoint(definition.WorldCenter);
            var cameraHeight = transform.position.y;
            if (adjustHeightForTerrain)
            {
                cameraHeight = SimulationCameraFraming.CameraHeight(
                    definition.baseHeight,
                    definition.heightScale,
                    terrainHeightOffset);
            }

            transform.position = SimulationCameraFraming.FramePosition(
                worldCenter,
                cameraHeight);

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
