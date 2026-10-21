using System.Collections.Generic;
using BattleRoyale.Events;
using UnityEngine;

namespace BattleRoyale.Maps
{
    [DisallowMultipleComponent]
    public sealed class ActiveMapManager : MonoBehaviour
    {
        [SerializeField] private PlayableMapCatalog playableMaps;
        [SerializeField] private SimulationStage stage;
        [SerializeField] private int slotIndex;
        [SerializeField] private GameObject activeMapPrefab;
        [SerializeField] private MapInstance activeMap;

        private readonly List<MapInstance> loadedMaps = new();
        private readonly List<MapInstance> claimedMaps = new();

        private PlayableMapCatalog activeCatalog;

        public static ActiveMapManager Instance { get; private set; }
        public PlayableMapCatalog PlayableMaps =>
            activeCatalog != null ? activeCatalog : playableMaps;
        public GameObject ActiveMapPrefab => activeMapPrefab;
        public MapInstance ActiveMap => activeMap;
        public int SlotIndex
        {
            get => slotIndex;
            set => slotIndex = value;
        }

        public void SetPlayableMaps(PlayableMapCatalog catalog)
        {
            activeCatalog = catalog;
        }

        public void UseAuthoredMaps()
        {
            activeCatalog = null;
        }

        // One instance per catalog map. The scene instance assigned as the active map is kept.
        public void EnsureLoaded()
        {
            var catalog = PlayableMaps;
            if (catalog == null || catalog.maps == null)
            {
                return;
            }

            if (activeMap != null && !Contains(loadedMaps, activeMap))
            {
                loadedMaps.Add(activeMap);
            }

            for (var i = 0; i < catalog.maps.Count; i++)
            {
                var definition = catalog.maps[i];
                if (definition == null || definition.bakedPrefab == null || FindLoaded(definition) != null)
                {
                    continue;
                }

                var spawned = Instantiate(definition.bakedPrefab, transform);
                spawned.name = definition.name;
                if (!spawned.TryGetComponent<MapInstance>(out var map))
                {
                    Debug.LogError(
                        "A map prefab needs a MapInstance on its root.",
                        definition.bakedPrefab);
                    DestroyObject(spawned);
                    continue;
                }

                loadedMaps.Add(map);
                if (activeMap == null)
                {
                    activeMapPrefab = definition.bakedPrefab;
                    SetActiveMap(map);
                }
                else
                {
                    map.gameObject.SetActive(false);
                }
            }
        }

        public bool TryClaim(MapDefinition definition, out MapInstance map)
        {
            EnsureLoaded();
            map = FindLoaded(definition);
            if (map == null || Contains(claimedMaps, map))
            {
                map = null;
                return false;
            }

            claimedMaps.Add(map);
            map.gameObject.SetActive(true);
            return true;
        }

        public void Claim(MapInstance map)
        {
            if (map == null || !Contains(loadedMaps, map) || Contains(claimedMaps, map))
            {
                return;
            }

            claimedMaps.Add(map);
        }

        // Loaded maps stay in the scene. Returns false when the caller should destroy the instance.
        public bool Release(MapInstance map)
        {
            if (map == null || !Contains(loadedMaps, map))
            {
                return false;
            }

            Remove(claimedMaps, map);
            if (map == activeMap)
            {
                MoveToPreview(map);
                map.gameObject.SetActive(true);
            }
            else
            {
                map.gameObject.SetActive(false);
            }

            return true;
        }

        public MapInstance ActivatePlayableMap(MapDefinition definition)
        {
            var catalog = PlayableMaps;
            if (catalog == null || !catalog.TryGetBakedPrefab(definition, out var prefab))
            {
                Debug.LogError(
                    "Choose a map from the playable catalog. It needs a baked prefab.",
                    definition);
                return activeMap;
            }

            return ActivateBakedMap(prefab);
        }

        public MapInstance ActivateForMode(MapGameMode mode)
        {
            var catalog = PlayableMaps;
            if (catalog == null)
            {
                Debug.LogError("No playable map catalog is assigned.");
                return activeMap;
            }

            var current = activeMap != null ? activeMap.definition : null;
            if (catalog.IsPlayable(current, mode))
            {
                return activeMap;
            }

            var next = catalog.FirstPlayable(mode);
            if (next == null)
            {
                Debug.LogError($"The playable catalog has no baked map for {mode}.");
                return activeMap;
            }

            return ActivatePlayableMap(next);
        }

        public bool AssignSlot(MapDefinition definition, MapGameMode mode)
        {
            if (stage == null)
            {
                Debug.LogError("The active map manager needs a simulation stage.", this);
                return false;
            }

            var catalog = PlayableMaps;
            if (catalog == null || !catalog.IsPlayable(definition, mode))
            {
                Debug.LogError(
                    "Choose a map from the playable catalog. It needs a baked prefab for this mode.",
                    definition);
                return false;
            }

            if (!definition.TryGetSingleConfiguration(mode, out var configuration))
            {
                Debug.LogError(
                    $"'{definition.name}' needs one {mode} configuration.",
                    definition);
                return false;
            }

            return stage.TryAssign(slotIndex, configuration);
        }

        public bool ClearSlot()
        {
            if (stage == null)
            {
                Debug.LogError("The active map manager needs a simulation stage.", this);
                return false;
            }

            return stage.TryClear(slotIndex);
        }

        // Slot 0 starts from its own caller. Later slots wait for this.
        public bool PlaySlot()
        {
            if (!Application.isPlaying)
            {
                return false;
            }

            if (stage == null)
            {
                Debug.LogError("The active map manager needs a simulation stage.", this);
                return false;
            }

            return stage.TryBegin(slotIndex);
        }

        public MapInstance ActivateBakedMap(GameObject prefab)
        {
            if (prefab == null)
            {
                return activeMap;
            }

            if (!prefab.TryGetComponent<MapInstance>(out var prefabMap))
            {
                Debug.LogError("A map prefab needs a MapInstance on its root.", prefab);
                return activeMap;
            }

            if (Application.isPlaying)
            {
                EnsureLoaded();
            }

            var loaded = FindLoaded(prefabMap.definition);
            if (loaded != null)
            {
                return ShowLoaded(prefab, loaded);
            }

            if (activeMap != null && activeMapPrefab == prefab)
            {
                return activeMap;
            }

            var position = prefab.transform.position;
            var rotation = prefab.transform.rotation;
            if (stage != null &&
                stage.TryPreviewPosition(prefabMap.definition, out var previewPosition))
            {
                position = previewPosition;
                rotation = Quaternion.identity;
            }

            var temporary = (prefab.hideFlags & HideFlags.HideAndDontSave) != 0;
            var instance = Instantiate(prefab, position, rotation);
            instance.name = "Map";
            if (temporary)
            {
                instance.hideFlags = HideFlags.HideAndDontSave;
            }

            var map = instance.GetComponent<MapInstance>();
            var previous = activeMap != null ? activeMap.gameObject : null;
            activeMapPrefab = prefab;
            SetActiveMap(map);

            if (previous != null && previous != instance)
            {
                if (Application.isPlaying)
                {
                    Destroy(previous);
                }
                else
                {
                    DestroyImmediate(previous);
                }
            }

            return map;
        }

        private MapInstance ShowLoaded(GameObject prefab, MapInstance loaded)
        {
            var previous = activeMap;
            activeMapPrefab = prefab;
            loaded.gameObject.SetActive(true);
            if (previous != loaded)
            {
                SetActiveMap(loaded);
            }

            if (Application.isPlaying && stage != null)
            {
                stage.AdoptPlacedMap(slotIndex, loaded);
            }
            else if (!Contains(claimedMaps, loaded))
            {
                MoveToPreview(loaded);
            }

            if (previous == null || previous == loaded || Contains(claimedMaps, previous))
            {
                return loaded;
            }

            if (Contains(loadedMaps, previous))
            {
                previous.gameObject.SetActive(false);
            }
            else
            {
                DestroyObject(previous.gameObject);
            }

            return loaded;
        }

        private void MoveToPreview(MapInstance map)
        {
            if (map == null || map.definition == null || stage == null)
            {
                return;
            }

            if (!stage.TryPreviewPosition(map.definition, out var position))
            {
                return;
            }

            map.transform.SetPositionAndRotation(position, Quaternion.identity);
        }

        private MapInstance FindLoaded(MapDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            for (var i = 0; i < loadedMaps.Count; i++)
            {
                var map = loadedMaps[i];
                if (map != null && map.definition == definition)
                {
                    return map;
                }
            }

            if (activeMap != null &&
                definition.bakedPrefab != null &&
                activeMapPrefab == definition.bakedPrefab &&
                (activeMap.definition == null || activeMap.definition == definition))
            {
                return activeMap;
            }

            return null;
        }

        private static bool Contains(List<MapInstance> maps, MapInstance map)
        {
            for (var i = 0; i < maps.Count; i++)
            {
                if (maps[i] == map)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Remove(List<MapInstance> maps, MapInstance map)
        {
            for (var i = maps.Count - 1; i >= 0; i--)
            {
                if (maps[i] == map)
                {
                    maps.RemoveAt(i);
                }
            }
        }

        private static void DestroyObject(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(instance);
            }
            else
            {
                DestroyImmediate(instance);
            }
        }

        public static ActiveMapManager Find()
        {
            if (IsSceneManager(Instance))
            {
                return Instance;
            }

            var managers = Object.FindObjectsByType<ActiveMapManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var manager in managers)
            {
                if (IsSceneManager(manager))
                {
                    return manager;
                }
            }

            return null;
        }

        public void SetActiveMap(MapInstance map)
        {
            if (activeMap == map)
            {
                return;
            }

            var previous = activeMap;
            activeMap = map;
            EventBus.Publish(new ActiveMapChanged(previous, activeMap));
        }

        private void Awake()
        {
            if (!Application.isPlaying || stage == null)
            {
                return;
            }

            EnsureLoaded();
            stage.HoldMatchesAfterFirst();
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Register()
        {
            if (!IsSceneManager(this))
            {
                return;
            }

            if (Instance != null && Instance != this)
            {
                Debug.LogError(
                    "The scene already has an Active Map manager.",
                    this);
                return;
            }

            Instance = this;
        }

        private static bool IsSceneManager(ActiveMapManager manager)
        {
            return manager != null &&
                   (manager.gameObject.hideFlags & HideFlags.HideAndDontSave) == 0 &&
                   manager.gameObject.scene.IsValid();
        }
    }
}
