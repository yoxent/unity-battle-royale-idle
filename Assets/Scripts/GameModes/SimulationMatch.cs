using System;
using System.Collections.Generic;
using BattleRoyale.Events;
using BattleRoyale.GameModes.SoloBattleRoyale;
using BattleRoyale.Maps;
using UnityEngine;
using UnityEngine.AI;

namespace BattleRoyale.GameModes
{
    public abstract class SimulationMatch : MonoBehaviour, IMatch, IMatchNavigation
    {
        private static readonly BattleRoyaleAIStrategy[] StrategyRotation =
        {
            BattleRoyaleAIStrategy.AggressiveHunter,
            BattleRoyaleAIStrategy.AggressiveSkirmisher,
            BattleRoyaleAIStrategy.NeutralOpportunist,
            BattleRoyaleAIStrategy.NeutralScout,
            BattleRoyaleAIStrategy.DefensiveAnchor,
            BattleRoyaleAIStrategy.DefensiveSurvivor
        };

        [Header("Map")]
        [Tooltip("Assigned by the Active Map manager. While that manager is empty, this map seeds it.")]
        [SerializeField] private MapInstance activeMap;
        [SerializeField] private MapModeConfiguration modeConfiguration;

        [Header("Team Management")]
        [Tooltip(
            "Player-selected behavior applied to every managed AI agent. " +
            "Individual bot strategies still provide variation.")]
        [SerializeField] private TeamPlaystyle teamPlaystyle = TeamPlaystyle.Neutral;

        [Header("Characters")]
        [SerializeField] private BattleRoyaleCharacter characterPrefab;
        [SerializeField] private BattleRoyaleCharacterProfile defaultProfile;
        [SerializeField] private bool spawnTestPlayer;
        [SerializeField] private bool distributeStrategies = true;
        [SerializeField] private BattleRoyaleAIStrategy fallbackStrategy =
            BattleRoyaleAIStrategy.NeutralOpportunist;
        [SerializeField] private Transform characterParent;
        [Min(0.1f)]
        [SerializeField] private float navMeshSnapDistance = 20f;

        [Header("Runtime")]
        [SerializeField] private bool autoStart = true;

        private readonly List<BattleRoyaleCharacter> roster = new();
        private BattleRoyaleProjectilePool projectilePool;
        private BattleRoyaleCharacterProfile runtimeDefaultProfile;

        public TeamPlaystyle TeamPlaystyle => teamPlaystyle;
        public BattleRoyaleProjectilePool Projectiles => projectilePool;
        public MapInstance ActiveMap => activeMap;
        public MapModeConfiguration ModeConfiguration => modeConfiguration;
        public bool IsMatchStarted => MatchStarted;
        public virtual IReadOnlyList<BattleRoyaleCharacter> Characters
        {
            get
            {
                RefreshRoster();
                return roster;
            }
        }

        protected bool MatchStarted { get; set; }
        protected bool SpawnTestPlayer => spawnTestPlayer;
        protected BattleRoyaleAIStrategy FallbackStrategy => fallbackStrategy;
        protected List<BattleRoyaleCharacter> Roster => roster;
        protected abstract string CharacterRootName { get; }
        protected abstract string ProjectileRootName { get; }
        protected abstract string RuntimeProfileName { get; }

        public abstract bool BeginMatch();
        public abstract void NotifyCharacterEliminated(
            BattleRoyaleCharacter eliminated,
            BattleRoyaleCharacter damageSource);
        public abstract bool TryGetHazardDestination(
            Vector3 fromPosition,
            out Vector3 destination);
        public abstract Vector3 GetRetreatPoint(
            Vector3 fromPosition,
            Vector3 threatPosition);
        public abstract Vector3 GetWanderPoint();

        public void SetTeamPlaystyle(TeamPlaystyle playstyle)
        {
            teamPlaystyle = playstyle;
        }

        public void AdoptActiveMap(MapInstance map)
        {
            if (MatchStarted || activeMap == map)
            {
                return;
            }

            activeMap = map;
        }

        protected void SetModeConfiguration(MapModeConfiguration configuration)
        {
            modeConfiguration = configuration;
        }

        protected virtual void OnEnable()
        {
            EventBus.Subscribe<ActiveMapChanged>(HandleActiveMapChanged);
        }

        protected virtual void OnDisable()
        {
            EventBus.Unsubscribe<ActiveMapChanged>(HandleActiveMapChanged);
        }

        protected virtual void OnDestroy()
        {
            if (runtimeDefaultProfile != null)
            {
                Destroy(runtimeDefaultProfile);
            }
        }

        protected virtual void OnValidate()
        {
            navMeshSnapDistance = Mathf.Max(0.1f, navMeshSnapDistance);
        }

        protected virtual void RefreshRoster()
        {
        }

        private void HandleActiveMapChanged(ActiveMapChanged change)
        {
            if (change.CurrentIsTemporary &&
                (gameObject.hideFlags & HideFlags.HideAndDontSave) == 0)
            {
                return;
            }

            AdoptActiveMap(change.Current);
        }

        private void Start()
        {
            if (!Application.isPlaying || !autoStart)
            {
                return;
            }

            BeginMatch();
        }

        protected bool TryBindActiveMap(string missingMapMessage)
        {
            var manager = ActiveMapManager.Find();
            if (manager != null && manager.ActiveMap != null)
            {
                activeMap = manager.ActiveMap;
            }
            else if (activeMap != null)
            {
                if (manager == null)
                {
                    var managerObject = new GameObject("Active Map");
                    manager = managerObject.AddComponent<ActiveMapManager>();
                }

                manager.SetActiveMap(activeMap);
            }

            if (activeMap == null || activeMap.definition == null)
            {
                Debug.LogError(missingMapMessage, this);
                return false;
            }

            return true;
        }

        protected MapModeConfiguration FindConfiguration(
            Predicate<MapModeConfiguration> matches)
        {
            var definition = activeMap == null ? null : activeMap.definition;
            if (definition == null || definition.modeConfigurations == null)
            {
                return null;
            }

            foreach (var configuration in definition.modeConfigurations)
            {
                if (configuration != null &&
                    configuration.map == definition &&
                    matches(configuration))
                {
                    return configuration;
                }
            }

            return null;
        }

        protected void EnsureSimulation()
        {
            EnsureCharacterParent();
            EnsureDefaultProfile();
            EnsureProjectilePool();
        }

        protected BattleRoyaleCharacter SpawnCharacter(
            string objectName,
            Vector3 position,
            BattleRoyaleAIStrategy strategy,
            bool isBot,
            Action<BattleRoyaleCharacter> configure = null)
        {
            var characterObject = CreateCharacterObject(objectName);
            characterObject.transform.SetParent(characterParent, true);
            characterObject.transform.position = position;

            var agent = characterObject.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = characterObject.AddComponent<NavMeshAgent>();
            }

            var character = characterObject.GetComponent<BattleRoyaleCharacter>();
            if (character == null)
            {
                character = characterObject.AddComponent<BattleRoyaleCharacter>();
            }

            var profile = defaultProfile != null
                ? defaultProfile
                : runtimeDefaultProfile;
            character.Initialize(this, profile, strategy, isBot);
            configure?.Invoke(character);

            var snappedPosition = SnapToNavigation(position);
            if (agent.enabled && agent.Warp(snappedPosition))
            {
                characterObject.transform.position = snappedPosition;
            }
            else
            {
                characterObject.transform.position = snappedPosition;
            }

            if (isBot)
            {
                var ai = characterObject.AddComponent<SoloBattleRoyaleAgent>();
                ai.Initialize(this, character);
            }

            roster.Add(character);
            return character;
        }

        protected Vector3 SnapToNavigation(Vector3 position)
        {
            if (NavMesh.SamplePosition(
                    position,
                    out var hit,
                    navMeshSnapDistance,
                    NavMesh.AllAreas))
            {
                return hit.position;
            }

            return position;
        }

        protected BattleRoyaleAIStrategy GetStrategy(int index)
        {
            if (!distributeStrategies)
            {
                return fallbackStrategy;
            }

            return StrategyRotation[index % StrategyRotation.Length];
        }

        private GameObject CreateCharacterObject(string objectName)
        {
            if (characterPrefab == null)
            {
                return new GameObject(objectName);
            }

            var characterObject = Instantiate(characterPrefab).gameObject;
            characterObject.name = objectName;
            return characterObject;
        }

        private void EnsureProjectilePool()
        {
            if (projectilePool != null)
            {
                projectilePool.Prewarm();
                MuzzleFlashPool.Ensure(projectilePool.transform);
                return;
            }

            var poolObject = new GameObject(ProjectileRootName);
            poolObject.transform.SetParent(transform, false);
            projectilePool = poolObject.AddComponent<BattleRoyaleProjectilePool>();
            projectilePool.Prewarm();
            MuzzleFlashPool.Ensure(poolObject.transform);
        }

        private void EnsureCharacterParent()
        {
            if (characterParent != null)
            {
                return;
            }

            var parentObject = new GameObject(CharacterRootName);
            parentObject.transform.SetParent(transform, false);
            characterParent = parentObject.transform;
        }

        private void EnsureDefaultProfile()
        {
            if (defaultProfile != null)
            {
                return;
            }

            runtimeDefaultProfile =
                ScriptableObject.CreateInstance<BattleRoyaleCharacterProfile>();
            runtimeDefaultProfile.name = RuntimeProfileName;
            runtimeDefaultProfile.displayName = "Prototype Bot";
            runtimeDefaultProfile.defaultStrategy = fallbackStrategy;
            runtimeDefaultProfile.hideFlags = HideFlags.DontSave;
        }
    }
}
