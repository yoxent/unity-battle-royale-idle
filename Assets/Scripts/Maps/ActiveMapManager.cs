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

        public static ActiveMapManager Instance { get; private set; }
        public PlayableMapCatalog PlayableMaps =>
            playableMaps != null ? playableMaps : PlayableMapCatalog.Load();
        public GameObject ActiveMapPrefab => activeMapPrefab;
        public MapInstance ActiveMap => activeMap;
        public int SlotIndex
        {
            get => slotIndex;
            set => slotIndex = value;
        }

        public void SetPlayableMaps(PlayableMapCatalog catalog)
        {
            playableMaps = catalog;
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

        public MapInstance ActivateBakedMap(GameObject prefab)
        {
            if (prefab == null)
            {
                return activeMap;
            }

            if (!prefab.TryGetComponent<MapInstance>(out _))
            {
                Debug.LogError("A map prefab needs a MapInstance on its root.", prefab);
                return activeMap;
            }

            if (activeMap != null && activeMapPrefab == prefab)
            {
                return activeMap;
            }

            var temporary = (prefab.hideFlags & HideFlags.HideAndDontSave) != 0;
            var instance = Instantiate(prefab);
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
