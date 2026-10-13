using System;
using System.Collections.Generic;
using BattleRoyale.Events;
using BattleRoyale.GameModes.BattleRoyale;
using BattleRoyale.Maps;
using UnityEngine;
using UnityEngine.AI;

namespace BattleRoyale.GameModes
{
    [ExecuteAlways]
    public abstract class SimulationMatch : MonoBehaviour,
        IMatch,
        IMatchNavigation,
        ITacticalAnchorProvider,
        IAgentDirectiveProvider,
        ITeamTacticalPicture
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

        [Header("Coach Directive")]
        [SerializeField] private BattleRoyaleAgentDirective coachDirective = new();

        [Header("Agents")]
        [SerializeField] private BattleRoyaleAgent agentPrefab;
        [SerializeField] private BattleRoyaleAgentProfile defaultProfile;
        [Tooltip(
            "Optional bot profile catalog. Profiles rotate by bot spawn index " +
            "to make prototype behavior differences observable.")]
        [SerializeField] private List<BattleRoyaleAgentProfile> profileCatalog =
            new();
        [SerializeField] private bool spawnTestPlayer;
        [SerializeField] private bool distributeStrategies = true;
        [SerializeField] private BattleRoyaleAIStrategy fallbackStrategy =
            BattleRoyaleAIStrategy.NeutralOpportunist;
        [SerializeField] private Transform agentParent;
        [Min(0.1f)]
        [SerializeField] private float navMeshSnapDistance = 20f;

        [Header("Runtime")]
        [SerializeField] private bool autoStart = true;

        private readonly List<BattleRoyaleAgent> roster = new();
        private BattleRoyaleProjectilePool projectilePool;
        private BattleRoyaleAgentProfile runtimeDefaultProfile;
        private BattleRoyaleSquadCoordinator squadCoordinator;

        public TeamPlaystyle TeamPlaystyle => teamPlaystyle;
        public BattleRoyaleAgentDirective CurrentDirective => coachDirective;
        public BattleRoyaleProjectilePool Projectiles => projectilePool;
        public MapInstance ActiveMap => activeMap;
        public MapModeConfiguration ModeConfiguration => modeConfiguration;
        public bool IsMatchStarted => MatchStarted;
        public virtual IReadOnlyList<BattleRoyaleAgent> Agents
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
        protected List<BattleRoyaleAgent> Roster => roster;
        protected abstract string AgentRootName { get; }
        protected abstract string ProjectileRootName { get; }
        protected abstract string RuntimeProfileName { get; }

        public abstract bool BeginMatch();
        public abstract void NotifyAgentEliminated(
            BattleRoyaleAgent eliminated,
            BattleRoyaleAgent damageSource);

        public virtual void RecordFiredShot(BattleRoyaleAgent shooter)
        {
        }

        public virtual void RecordDealtDamage(
            BattleRoyaleAgent attacker,
            BattleRoyaleAgent victim,
            float healthRemoved)
        {
        }
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

        public void SetDirective(
            BattleRoyaleDirectiveType type,
            Vector3 destination,
            bool hasDestination,
            BattleRoyaleEngagementPolicy engagementPolicy)
        {
            if (coachDirective == null)
            {
                coachDirective = new BattleRoyaleAgentDirective();
            }

            coachDirective.type = type;
            coachDirective.destination = destination;
            coachDirective.hasDestination = hasDestination;
            coachDirective.engagementPolicy = engagementPolicy;
            coachDirective.revision = Mathf.Max(0, coachDirective.revision) + 1;
        }

        public void ClearDirective()
        {
            SetDirective(
                BattleRoyaleDirectiveType.None,
                Vector3.zero,
                false,
                BattleRoyaleEngagementPolicy.Balanced);
        }

        public void PublishObservation(
            BattleRoyaleAgent observer,
            BattleRoyaleAgent target,
            Vector3 position,
            float confidence)
        {
            EnsureSquadCoordinator();
            squadCoordinator.PublishObservation(
                observer,
                target,
                position,
                confidence);
        }

        public bool TryGetFreshObservation(
            int teamIndex,
            BattleRoyaleAgent requester,
            float maxAge,
            out BattleRoyaleTeamObservation observation)
        {
            EnsureSquadCoordinator();
            return squadCoordinator.TryGetFreshObservation(
                teamIndex,
                requester,
                maxAge,
                out observation);
        }

        public bool IsTargetClaimed(
            BattleRoyaleAgent target,
            BattleRoyaleAgent requester)
        {
            EnsureSquadCoordinator();
            return squadCoordinator.IsTargetClaimed(target, requester);
        }

        public bool TryClaimTarget(
            BattleRoyaleAgent claimer,
            BattleRoyaleAgent target,
            float duration)
        {
            EnsureSquadCoordinator();
            return squadCoordinator.TryClaimTarget(
                claimer,
                target,
                duration);
        }

        public void ReleaseTargetClaim(
            BattleRoyaleAgent claimer,
            BattleRoyaleAgent target)
        {
            EnsureSquadCoordinator();
            squadCoordinator.ReleaseTargetClaim(claimer, target);
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
            if (coachDirective == null)
            {
                coachDirective = new BattleRoyaleAgentDirective();
            }

            coachDirective.revision = Mathf.Max(0, coachDirective.revision);
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
            EnsureAgentParent();
            EnsureDefaultProfile();
            EnsureProjectilePool();
            EnsureSquadCoordinator();
        }

        protected BattleRoyaleAgent SpawnAgent(
            string objectName,
            Vector3 position,
            BattleRoyaleAIStrategy strategy,
            bool isBot,
            Action<BattleRoyaleAgent> configure = null)
        {
            var agentObject = CreateAgentObject(objectName);
            agentObject.transform.SetParent(agentParent, true);
            agentObject.transform.position = position;

            var navigationAgent = agentObject.GetComponent<NavMeshAgent>();
            if (navigationAgent == null)
            {
                navigationAgent = agentObject.AddComponent<NavMeshAgent>();
            }

            var agent = agentObject.GetComponent<BattleRoyaleAgent>();
            if (agent == null)
            {
                agent = agentObject.AddComponent<BattleRoyaleAgent>();
            }

            var profile = isBot
                ? GetBotProfile(roster.Count)
                : defaultProfile != null
                    ? defaultProfile
                    : runtimeDefaultProfile;
            agent.Initialize(this, profile, strategy, isBot);
            agent.AssignSquadRole(
                BattleRoyaleSquadCoordinator.GetDefaultRole(
                    agent.Strategy,
                    agent.Specialty,
                    roster.Count));
            configure?.Invoke(agent);

            var snappedPosition = SnapToNavigation(position);
            if (navigationAgent.enabled && navigationAgent.Warp(snappedPosition))
            {
                agentObject.transform.position = snappedPosition;
            }
            else
            {
                agentObject.transform.position = snappedPosition;
            }

            if (isBot)
            {
                var ai = agentObject.AddComponent<BattleRoyaleAgentController>();
                ai.Initialize(
                    this,
                    agent,
                    BattleRoyaleAgentDecisionRules.GetDecisionPhaseDelay(
                        roster.Count));
            }

            roster.Add(agent);
            return agent;
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

        public bool TryGetTacticalAnchor(
            Vector3 fromPosition,
            Vector3 threatPosition,
            float preferredRange,
            out Vector3 destination)
        {
            destination = fromPosition;
            var anchors = activeMap == null || activeMap.definition == null
                ? null
                : activeMap.definition.tacticalAnchors;
            if (anchors == null || anchors.Count == 0)
            {
                return false;
            }

            var desiredRange = Mathf.Max(2f, preferredRange);
            var bestScore = float.MaxValue;
            var foundAnchor = false;
            foreach (var anchor in anchors)
            {
                if (anchor == null || anchor.weight <= 0f ||
                    !NavMesh.SamplePosition(
                        anchor.position,
                        out var hit,
                        navMeshSnapDistance,
                        NavMesh.AllAreas))
                {
                    continue;
                }

                var candidate = hit.position;
                var rangeError = HorizontalDistance(
                    candidate,
                    threatPosition) - desiredRange;
                var travelDistance = HorizontalDistance(
                    fromPosition,
                    candidate);
                var score = Mathf.Abs(rangeError) +
                            travelDistance * 0.1f -
                            anchor.weight * 2f;
                if (score < bestScore)
                {
                    bestScore = score;
                    destination = candidate;
                    foundAnchor = true;
                }
            }

            return foundAnchor;
        }

        protected BattleRoyaleAIStrategy GetStrategy(int index)
        {
            if (!distributeStrategies)
            {
                return fallbackStrategy;
            }

            return StrategyRotation[index % StrategyRotation.Length];
        }

        private BattleRoyaleAgentProfile GetBotProfile(int index)
        {
            if (profileCatalog != null && profileCatalog.Count > 0)
            {
                var startIndex = Mathf.Max(0, index);
                for (var offset = 0; offset < profileCatalog.Count; offset++)
                {
                    var profile = profileCatalog[
                        (startIndex + offset) % profileCatalog.Count];
                    if (profile != null)
                    {
                        return profile;
                    }
                }
            }

            return defaultProfile != null
                ? defaultProfile
                : runtimeDefaultProfile;
        }

        private GameObject CreateAgentObject(string objectName)
        {
            if (agentPrefab == null)
            {
                return new GameObject(objectName);
            }

            var agentObject = Instantiate(agentPrefab).gameObject;
            agentObject.name = objectName;
            return agentObject;
        }

        private void EnsureSquadCoordinator()
        {
            if (squadCoordinator == null)
            {
                squadCoordinator = new BattleRoyaleSquadCoordinator(
                    () => Time.time);
            }
        }

        private static float HorizontalDistance(
            Vector3 first,
            Vector3 second)
        {
            first.y = 0f;
            second.y = 0f;
            return Vector3.Distance(first, second);
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

        private void EnsureAgentParent()
        {
            if (agentParent != null)
            {
                return;
            }

            var parentObject = new GameObject(AgentRootName);
            parentObject.transform.SetParent(transform, false);
            agentParent = parentObject.transform;
        }

        private void EnsureDefaultProfile()
        {
            if (defaultProfile != null)
            {
                return;
            }

            runtimeDefaultProfile =
                ScriptableObject.CreateInstance<BattleRoyaleAgentProfile>();
            runtimeDefaultProfile.name = RuntimeProfileName;
            runtimeDefaultProfile.displayName = "Prototype Bot";
            runtimeDefaultProfile.defaultStrategy = fallbackStrategy;
            runtimeDefaultProfile.hideFlags = HideFlags.DontSave;
        }
    }
}
