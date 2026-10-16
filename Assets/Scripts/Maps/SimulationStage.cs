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
        public StatSheetView tracker;
    }

    // Places one baked map per slot side by side and keeps the slots apart with barriers.
    public sealed class SimulationStage : MonoBehaviour
    {
        [SerializeField] private SimulationSlot[] slots = new SimulationSlot[3];
        [SerializeField] private BoxCollider[] barriers = new BoxCollider[2];
        [SerializeField] private PlayableMapCatalog playableMaps;
        [Min(0f)]
        [SerializeField] private float gap = 40f;
        [Min(0.5f)]
        [SerializeField] private float barrierThickness = 4f;
        [Min(1.5f)]
        [SerializeField] private float barrierClearance = 4f;
        [SerializeField] private SimulationCameraController watchCamera;
        [Tooltip("Authored map that shares slot 0's center. Disabled when a slot map is placed; never destroyed.")]
        [SerializeField] private MapInstance sceneMapToDisable;
        [SerializeField] private StatSheetView[] trackerPrefabs = System.Array.Empty<StatSheetView>();
        [Tooltip("Unity time scale for the fight. The company abort window and session day stay on unscaled time.")]
        [Min(0.1f)]
        [SerializeField] private float matchTimeScale = 4f;

        private MapInstance[] placedMaps;
        private Vector3[] slotCenters;

        public int WatchedSlot { get; private set; }

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
            EnsureTrackers();
            if (!TryMeasureCatalog(out var largestWidth, out var largestDepth))
            {
                return;
            }

            slotCenters = SimulationStageLayout.Centers(largestWidth, gap);
            PlaceMaps(slotCenters);
            DisableUnplacedSceneMaps();
            PlaceBarriers(slotCenters, largestDepth);
            SetBarrierRenderersEnabled(false);
            ShowWatchedMap();
            ApplyWatchedTrackers();
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
                ? watchCamera.Watch(placedMaps[slot], ApplyWatchedTrackers)
                : watchCamera.Show(placedMaps[slot]);
            if (!presented)
            {
                return;
            }

            if (!fade)
            {
                ApplyWatchedTrackers();
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
            var moved = Application.isPlaying &&
                placedMaps != null &&
                slot == WatchedSlot &&
                ShowNextOccupied(slot);
            if (!moved)
            {
                ApplyWatchedTrackers();
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

        private void ApplyWatchedTrackers()
        {
            if (slots == null)
            {
                return;
            }

            var count = Mathf.Min(slots.Length, SimulationStageLayout.SlotCount);
            for (var slot = 0; slot < count; slot++)
            {
                var tracker = slots[slot] == null ? null : slots[slot].tracker;
                if (tracker == null)
                {
                    continue;
                }

                tracker.SetWatched(slot == WatchedSlot);
            }
        }

        public void EnsureTrackers()
        {
            if (slots == null)
            {
                return;
            }

            var count = Mathf.Min(slots.Length, SimulationStageLayout.SlotCount);
            for (var slot = 0; slot < count; slot++)
            {
                EnsureTracker(slot);
            }

            ApplyWatchedTrackers();
        }

        public StatSheetView TrackerPrefabFor(SimulationMatch match)
        {
            if (match == null || trackerPrefabs == null)
            {
                return null;
            }

            for (var index = 0; index < trackerPrefabs.Length; index++)
            {
                var prefab = trackerPrefabs[index];
                if (prefab != null && prefab.Matches(match))
                {
                    return prefab;
                }
            }

            return null;
        }

        public void SetTracker(int slot, StatSheetView tracker)
        {
            if (slots == null || slot < 0 || slot >= slots.Length)
            {
                return;
            }

            if (slots[slot] == null)
            {
                slots[slot] = new SimulationSlot();
            }

            slots[slot].tracker = tracker;
            if (tracker != null)
            {
                tracker.SetWatched(slot == WatchedSlot);
            }
        }

        private void EnsureTracker(int slot)
        {
            var entry = slots[slot];
            if (entry == null)
            {
                return;
            }

            var match = entry.match;
            var current = entry.tracker;
            if (match == null)
            {
                DestroyTracker(current);
                entry.tracker = null;
                return;
            }

            if (current != null && current.Matches(match) && current.BoundMatch == match)
            {
                return;
            }

            DestroyTracker(current);
            entry.tracker = null;
            var prefab = TrackerPrefabFor(match);
            if (prefab == null)
            {
                if (HasTrackerPrefab())
                {
                    Debug.LogError(
                        $"Simulation slot {slot} has no stat tracker prefab for {match.GetType().Name}.",
                        this);
                }

                return;
            }

            var instance = Instantiate(prefab, transform);
            instance.name = prefab.name;
            instance.Bind(match);
            entry.tracker = instance;
        }

        private bool HasTrackerPrefab()
        {
            if (trackerPrefabs == null)
            {
                return false;
            }

            for (var index = 0; index < trackerPrefabs.Length; index++)
            {
                if (trackerPrefabs[index] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void DestroyTracker(StatSheetView tracker)
        {
            if (tracker == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(tracker.gameObject);
            }
            else
            {
                DestroyImmediate(tracker.gameObject);
            }
        }

        private bool TryMeasureCatalog(out float largestWidth, out float largestDepth)
        {
            largestWidth = 0f;
            largestDepth = 0f;
            if (playableMaps == null || playableMaps.maps == null)
            {
                Debug.LogError("The simulation stage needs a playable map catalog.", this);
                return false;
            }

            foreach (var definition in playableMaps.maps)
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
            if (previous == null)
            {
                return;
            }

            Destroy(previous.gameObject);
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
            var previous = placedMaps[slot];
            if (previous != null)
            {
                Destroy(previous.gameObject);
            }

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

            var map = Instantiate(
                prefabMap,
                centers[slot] - definition.WorldCenter,
                Quaternion.identity);
            map.name = $"Map Slot {slot}";
            return map;
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

        private void PlaceBarriers(Vector3[] centers, float depth)
        {
            if (barriers == null)
            {
                return;
            }

            var hasFloor = false;
            var lowestFloor = 0f;
            var highestFloor = 0f;
            if (playableMaps != null && playableMaps.maps != null)
            {
                foreach (var definition in playableMaps.maps)
                {
                    if (definition == null)
                    {
                        continue;
                    }

                    var floor = definition.baseHeight;
                    lowestFloor = hasFloor ? Mathf.Min(lowestFloor, floor) : floor;
                    highestFloor = hasFloor ? Mathf.Max(highestFloor, floor) : floor;
                    hasFloor = true;
                }
            }

            if (!hasFloor)
            {
                return;
            }

            var topY = highestFloor + barrierClearance;
            var count = Mathf.Min(barriers.Length, centers.Length - 1);
            for (var index = 0; index < count; index++)
            {
                if (barriers[index] == null)
                {
                    continue;
                }

                SimulationStageLayout.PlaceBarrier(
                    barriers[index],
                    centers[index],
                    centers[index + 1],
                    barrierThickness,
                    depth,
                    lowestFloor,
                    topY);
            }
        }

        private void OnValidate()
        {
            gap = Mathf.Max(0f, gap);
            barrierThickness = Mathf.Max(0.5f, barrierThickness);
            barrierClearance = Mathf.Max(1.5f, barrierClearance);
            if (Application.isPlaying || !TryMeasureCatalogQuiet(out var largestWidth, out var largestDepth))
            {
                return;
            }

            PlaceBarriers(SimulationStageLayout.Centers(largestWidth, gap), largestDepth);
            SetBarrierRenderersEnabled(true);
        }

        private void SetBarrierRenderersEnabled(bool enabled)
        {
            if (barriers == null)
            {
                return;
            }

            foreach (var barrier in barriers)
            {
                if (barrier == null)
                {
                    continue;
                }

                var renderer = barrier.GetComponent<MeshRenderer>();
                if (renderer == null)
                {
                    Debug.LogError("A simulation barrier needs a mesh renderer.", barrier);
                    continue;
                }

                renderer.enabled = enabled;
            }
        }

        private bool TryMeasureCatalogQuiet(out float largestWidth, out float largestDepth)
        {
            largestWidth = 0f;
            largestDepth = 0f;
            if (playableMaps == null || playableMaps.maps == null)
            {
                return false;
            }

            foreach (var definition in playableMaps.maps)
            {
                if (definition == null)
                {
                    continue;
                }

                largestWidth = Mathf.Max(largestWidth, definition.WorldSize.x);
                largestDepth = Mathf.Max(largestDepth, definition.WorldSize.z);
            }

            return largestWidth > 0f && largestDepth > 0f;
        }
    }
}
