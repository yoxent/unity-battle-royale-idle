using System;
using BattleRoyale.CameraSystems;
using BattleRoyale.Events;
using BattleRoyale.GameModes;
using UnityEngine;

namespace BattleRoyale.Maps
{
    [Serializable]
    public sealed class SimulationSlot
    {
        public SimulationMatch match;
    }

    // Places one baked map per slot on +X. The dashboard preview sits on -Z, clear of that row. Boundary walls stop shots from leaving a map.
    public sealed class SimulationStage : MonoBehaviour
    {
        [SerializeField] private SimulationSlot[] slots = new SimulationSlot[3];
        [Tooltip("Empty space between the widest maps, and between that row and the dashboard preview. The match preview camera is wider than those maps, so a short gap lets the next slot show in the frame. Slot 0 starts one stride from the origin.")]
        [Min(0f)]
        [SerializeField] private float gap = 400f;
        [SerializeField] private SimulationCameraController watchCamera;
        [SerializeField] private ActiveMapManager mapManager;
        [Tooltip("Authored map at the preview origin. Disabled when a slot map is placed; never destroyed.")]
        [SerializeField] private MapInstance sceneMapToDisable;
        [Tooltip("Dashboard preview. Placed off the slot row when the stage wakes.")]
        [SerializeField] private MapInstance previewMap;
        [Tooltip("Unity time scale for the fight. The company abort window and session day stay on unscaled time.")]
        [Min(0.1f)]
        [SerializeField] private float matchTimeScale = 4f;

        private MapInstance[] placedMaps;
        private Vector3[] slotCenters;

        public int WatchedSlot { get; private set; }

        public SimulationMatch WatchedMatch
        {
            get
            {
                if (slots == null || WatchedSlot < 0 || WatchedSlot >= slots.Length)
                {
                    return null;
                }

                var entry = slots[WatchedSlot];
                return entry == null ? null : entry.match;
            }
        }

        public int OccupiedMapCount
        {
            get
            {
                if (placedMaps == null)
                {
                    return 0;
                }

                var count = 0;
                var limit = Mathf.Min(placedMaps.Length, SimulationStageLayout.SlotCount);
                for (var slot = 0; slot < limit; slot++)
                {
                    if (placedMaps[slot] != null)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        private void Awake()
        {
            Time.timeScale = Mathf.Max(0.1f, matchTimeScale);
            placedMaps = new MapInstance[SimulationStageLayout.SlotCount];
            if (!TryMeasureCatalog(out var largestWidth, out var largestDepth))
            {
                return;
            }

            slotCenters = SimulationStageLayout.Centers(largestWidth, gap);
            PlacePreviewMap(largestWidth, largestDepth);
            PlaceMaps(slotCenters);
            DisableUnplacedSceneMaps();
            ShowWatchedMap();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<MapViewStepRequested>(HandleMapViewStep);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<MapViewStepRequested>(HandleMapViewStep);
        }

        public void Watch(int slot)
        {
            Watch(slot, true);
        }

        private void Watch(int slot, bool fade)
        {
            if (slot < 0 || slot >= SimulationStageLayout.SlotCount)
            {
                Debug.LogWarning($"Simulation slot {slot} does not exist.", this);
                return;
            }

            if (slot == WatchedSlot)
            {
                return;
            }

            if (placedMaps == null || placedMaps[slot] == null)
            {
                Debug.LogWarning($"Simulation slot {slot} is empty.", this);
                return;
            }

            if (watchCamera == null)
            {
                Debug.LogError("The simulation stage needs a watch camera.", this);
                return;
            }

            var presented = fade
                ? watchCamera.Watch(placedMaps[slot], null)
                : watchCamera.Show(placedMaps[slot]);
            if (!presented)
            {
                return;
            }

            var previousSlot = WatchedSlot;
            WatchedSlot = slot;
            EventBus.Publish(new MapViewChanged(previousSlot, slot, placedMaps[slot]));
        }

        private void HandleMapViewStep(MapViewStepRequested request)
        {
            WatchOffset(request.Direction, request.UseFade);
        }

        public void WatchOffset(int direction, bool useFade)
        {
            if (direction == 0)
            {
                return;
            }

            if (placedMaps == null)
            {
                Debug.LogWarning("Map watch runs while the simulation is playing.", this);
                return;
            }

            var count = Mathf.Min(placedMaps.Length, SimulationStageLayout.SlotCount);
            var slot = WatchedSlot;
            for (var step = 0; step < count; step++)
            {
                slot += direction > 0 ? 1 : -1;
                if (slot < 0)
                {
                    slot = count - 1;
                }
                else if (slot >= count)
                {
                    slot = 0;
                }

                if (placedMaps[slot] != null)
                {
                    Watch(slot, useFade);
                    return;
                }
            }
        }

        public bool TryAssign(int slot, MapModeConfiguration configuration)
        {
            if (configuration == null)
            {
                Debug.LogError("A simulation slot needs a mode configuration.", this);
                return false;
            }

            if (slots == null ||
                slot < 0 ||
                slot >= slots.Length ||
                slot >= SimulationStageLayout.SlotCount)
            {
                Debug.LogError($"Simulation slot {slot} does not exist.", this);
                return false;
            }

            var match = slots[slot] == null ? null : slots[slot].match;
            if (match == null)
            {
                Debug.LogError($"Simulation slot {slot} has no match.", this);
                return false;
            }

            var placed = placedMaps != null &&
                slotCenters != null &&
                slot < placedMaps.Length &&
                slot < slotCenters.Length;
            var sameMap = placed &&
                match.ModeConfiguration == configuration &&
                placedMaps[slot] != null &&
                placedMaps[slot].definition == configuration.map;
            match.UseConfiguration(configuration);
            if (!placed || sameMap)
            {
                match.Occupy();
                return true;
            }

            ReplaceSlotMap(slot);
            PublishPlacement();
            if (placedMaps[slot] == null)
            {
                return false;
            }

            match.Occupy();
            return true;
        }

        public bool TryBegin(int slot)
        {
            if (slots == null ||
                slot < 0 ||
                slot >= slots.Length ||
                slot >= SimulationStageLayout.SlotCount)
            {
                Debug.LogError($"Simulation slot {slot} does not exist.", this);
                return false;
            }

            var match = slots[slot] == null ? null : slots[slot].match;
            if (match == null || !match.isActiveAndEnabled)
            {
                Debug.LogError($"Simulation slot {slot} has no match.", this);
                return false;
            }

            return match.BeginMatch();
        }

        public void HoldMatchesAfterFirst()
        {
            if (slots == null)
            {
                return;
            }

            var count = Mathf.Min(slots.Length, SimulationStageLayout.SlotCount);
            for (var slot = 1; slot < count; slot++)
            {
                var match = slots[slot] == null ? null : slots[slot].match;
                if (match == null)
                {
                    continue;
                }

                match.HoldUntilPlayed();
            }
        }

        public bool TryClear(int slot)
        {
            if (slots == null ||
                slot < 0 ||
                slot >= slots.Length ||
                slot >= SimulationStageLayout.SlotCount)
            {
                Debug.LogError($"Simulation slot {slot} does not exist.", this);
                return false;
            }

            var match = slots[slot] == null ? null : slots[slot].match;
            if (match == null)
            {
                return true;
            }

            RemovePlacedMap(slot);
            match.Vacate();
            if (Application.isPlaying &&
                placedMaps != null &&
                slot == WatchedSlot)
            {
                ShowNextOccupied(slot);
            }

            PublishPlacement();
            return true;
        }

        private void ShowWatchedMap()
        {
            if (placedMaps == null)
            {
                return;
            }

            if (WatchedSlot < 0 ||
                WatchedSlot >= placedMaps.Length ||
                placedMaps[WatchedSlot] == null)
            {
                for (var slot = 0; slot < placedMaps.Length; slot++)
                {
                    if (placedMaps[slot] != null)
                    {
                        WatchedSlot = slot;
                        break;
                    }
                }
            }

            var map = WatchedSlot >= 0 && WatchedSlot < placedMaps.Length
                ? placedMaps[WatchedSlot]
                : null;
            if (map == null)
            {
                return;
            }

            if (watchCamera == null)
            {
                Debug.LogError("The simulation stage needs a watch camera.", this);
                return;
            }

            watchCamera.Show(map);
        }

        public void AdoptPlacedMap(int slot, MapInstance map)
        {
            if (map == null ||
                placedMaps == null ||
                slot < 0 ||
                slot >= placedMaps.Length)
            {
                return;
            }

            if (placedMaps[slot] == map)
            {
                map.gameObject.SetActive(true);
                mapManager?.Claim(map);
                return;
            }

            DiscardPlaced(placedMaps[slot]);
            if (slotCenters != null &&
                slot < slotCenters.Length &&
                map.definition != null)
            {
                map.transform.SetPositionAndRotation(
                    slotCenters[slot] - map.definition.WorldCenter,
                    Quaternion.identity);
            }

            map.gameObject.SetActive(true);
            placedMaps[slot] = map;
            mapManager?.Claim(map);
        }

        public bool TryPreviewPosition(MapDefinition definition, out Vector3 position)
        {
            position = default;
            if (definition == null ||
                !TryMeasureCatalog(out var largestWidth, out var largestDepth))
            {
                return false;
            }

            var span = Mathf.Max(largestWidth, largestDepth);
            position = SimulationStageLayout.PreviewCenter(span, gap) - definition.WorldCenter;
            return true;
        }

        private void PlacePreviewMap(float largestWidth, float largestDepth)
        {
            if (previewMap == null || previewMap.definition == null)
            {
                return;
            }

            var span = Mathf.Max(largestWidth, largestDepth);
            previewMap.transform.SetPositionAndRotation(
                SimulationStageLayout.PreviewCenter(span, gap) - previewMap.definition.WorldCenter,
                Quaternion.identity);
        }

        private bool TryMeasureCatalog(out float largestWidth, out float largestDepth)
        {
            largestWidth = 0f;
            largestDepth = 0f;
            var catalog = mapManager == null ? null : mapManager.PlayableMaps;
            if (catalog == null || catalog.maps == null)
            {
                Debug.LogError("The simulation stage needs the active map's playable catalog.", this);
                return false;
            }

            foreach (var definition in catalog.maps)
            {
                if (definition == null)
                {
                    continue;
                }

                largestWidth = Mathf.Max(largestWidth, definition.WorldSize.x);
                largestDepth = Mathf.Max(largestDepth, definition.WorldSize.z);
            }

            if (largestWidth <= 0f || largestDepth <= 0f)
            {
                Debug.LogError(
                    "The simulation stage found no playable map with a size.",
                    this);
                return false;
            }

            return true;
        }

        private void PlaceMaps(Vector3[] centers)
        {
            var count = Mathf.Min(slots == null ? 0 : slots.Length, centers.Length);
            for (var slot = 0; slot < count; slot++)
            {
                var match = slots[slot] == null ? null : slots[slot].match;
                if (!SlotRuns(match))
                {
                    continue;
                }

                var map = PlaceSlotMap(slot, match.ModeConfiguration, centers);
                if (map == null)
                {
                    continue;
                }

                placedMaps[slot] = map;
                match.BindMap(map);
            }

            PublishPlacement();
        }

        private void PublishPlacement()
        {
            EventBus.Publish(new MapPlacementChanged(OccupiedMapCount));
        }

        private static bool SlotRuns(SimulationMatch match)
        {
            return match != null &&
                   match.gameObject.activeInHierarchy &&
                   match.ModeConfiguration != null;
        }

        private void RemovePlacedMap(int slot)
        {
            if (placedMaps == null || slot < 0 || slot >= placedMaps.Length)
            {
                return;
            }

            var previous = placedMaps[slot];
            placedMaps[slot] = null;
            DiscardPlaced(previous);
        }

        private bool ShowNextOccupied(int cleared)
        {
            if (placedMaps == null)
            {
                return false;
            }

            for (var slot = 0; slot < placedMaps.Length; slot++)
            {
                if (slot == cleared || placedMaps[slot] == null)
                {
                    continue;
                }

                var previous = WatchedSlot;
                Watch(slot);
                return WatchedSlot != previous;
            }

            return false;
        }

        private void ReplaceSlotMap(int slot)
        {
            DiscardPlaced(placedMaps[slot]);

            var match = slots[slot].match;
            var map = PlaceSlotMap(slot, match.ModeConfiguration, slotCenters);
            placedMaps[slot] = map;
            if (map == null)
            {
                return;
            }

            match.BindMap(map);
            if (slot == WatchedSlot && watchCamera != null)
            {
                watchCamera.Show(map);
            }
        }

        private MapInstance PlaceSlotMap(int slot, MapModeConfiguration configuration, Vector3[] centers)
        {
            var definition = configuration == null ? null : configuration.map;
            var prefab = definition == null ? null : definition.bakedPrefab;
            if (prefab == null)
            {
                Debug.LogError(
                    $"Simulation slot {slot} has no baked map prefab. The slot stays empty.",
                    slots[slot].match);
                return null;
            }

            if (!prefab.TryGetComponent<MapInstance>(out var prefabMap))
            {
                Debug.LogError(
                    $"The baked prefab for slot {slot} needs a MapInstance on its root.",
                    prefab);
                return null;
            }

            if (mapManager != null && mapManager.TryClaim(definition, out var loaded))
            {
                loaded.transform.SetPositionAndRotation(
                    centers[slot] - definition.WorldCenter,
                    Quaternion.identity);
                return loaded;
            }

            var map = Instantiate(
                prefabMap,
                centers[slot] - definition.WorldCenter,
                Quaternion.identity);
            map.name = $"Map Slot {slot}";
            return map;
        }

        private void DiscardPlaced(MapInstance previous)
        {
            if (previous == null)
            {
                return;
            }

            if (mapManager != null && mapManager.Release(previous))
            {
                return;
            }

            Destroy(previous.gameObject);
        }

        // The assigned scene map would overlap the slot maps with a second NavMesh
        // and a second set of colliders. Disable it. Do not destroy it.
        private void DisableUnplacedSceneMaps()
        {
            var placedAny = false;
            foreach (var placed in placedMaps)
            {
                placedAny |= placed != null;
            }

            if (!placedAny)
            {
                return;
            }

            if (sceneMapToDisable == null ||
                Array.IndexOf(placedMaps, sceneMapToDisable) >= 0)
            {
                return;
            }

            sceneMapToDisable.gameObject.SetActive(false);
        }

        private void OnValidate()
        {
            gap = Mathf.Max(0f, gap);
        }

    }
}
