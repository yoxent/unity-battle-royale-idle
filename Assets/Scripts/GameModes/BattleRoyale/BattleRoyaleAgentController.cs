using BattleRoyale.Company;
using BattleRoyale.GameModes;
using BattleRoyale.GameModes.Deathmatch;
using UnityEngine;

namespace BattleRoyale.GameModes.BattleRoyale
{
    [DisallowMultipleComponent]
    public sealed class BattleRoyaleAgentController : MonoBehaviour
    {
        private const float PerceptionMovementTolerance = 0.1f;

        [SerializeField] private BattleRoyaleAgent agent;
        [SerializeField] private BattleRoyaleAgent target;
        [SerializeField] private Vector3 currentDestination;
        [SerializeField] private float decisionTimeRemaining;
        [SerializeField] private float wanderTimeRemaining;
        [SerializeField] private Vector3 lastKnownTargetPosition;
        [SerializeField] private float targetMemoryTimeRemaining;
        [SerializeField] private float targetCommitmentTimeRemaining;
        [SerializeField] private BattleRoyaleAgent cachedPerceptionTarget;
        [SerializeField] private Vector3 cachedPerceptionAgentPosition;
        [SerializeField] private Vector3 cachedPerceptionTargetPosition;
        [SerializeField] private bool cachedPerceptionLineOfSight;
        [SerializeField] private float perceptionCacheTimeRemaining;

        private IMatch match;
        private IMatchNavigation navigation;
        private ITacticalAnchorProvider tacticalAnchorProvider;
        private IAgentDirectiveProvider directiveProvider;
        private ITeamTacticalPicture tacticalPicture;
        private BattleRoyaleStrategyTuning tuning;
        private Vector3 lastPathDestination;
        private bool hasPathDestination;
        private int lastDirectiveRevision = -1;
        private float lastSharedObservationTime = -1f;

        public BattleRoyaleAgent Agent => agent;
        public BattleRoyaleAgent Target => target;
        public Vector3 CurrentDestination => currentDestination;
        public string DebugLabel =>
            agent == null
                ? "Uninitialized"
                : $"{agent.Profile?.displayName ?? "Unknown"} / " +
                  $"{agent.Strategy} / {agent.Specialty} / " +
                  $"{agent.State}";

        public void Initialize(
            IMatch owningMatch,
            BattleRoyaleAgent owningAgent,
            float initialDecisionDelay = 0f)
        {
            match = owningMatch;
            navigation = owningMatch as IMatchNavigation;
            tacticalAnchorProvider = owningMatch as ITacticalAnchorProvider;
            directiveProvider = owningMatch as IAgentDirectiveProvider;
            tacticalPicture = owningMatch as ITeamTacticalPicture;
            agent = owningAgent;
            decisionTimeRemaining = Mathf.Max(0f, initialDecisionDelay);
            wanderTimeRemaining = 0f;
            lastKnownTargetPosition = Vector3.zero;
            targetMemoryTimeRemaining = 0f;
            targetCommitmentTimeRemaining = 0f;
            InvalidatePerceptionSnapshot();
            lastDirectiveRevision = -1;
            lastSharedObservationTime = -1f;
            RefreshTuning();
        }

        private void Awake()
        {
            if (agent == null)
            {
                agent = GetComponent<BattleRoyaleAgent>();
            }
        }

        private void Update()
        {
            if (agent == null || !agent.IsAlive)
            {
                return;
            }

            if (match == null)
            {
                match = agent.Match;
                navigation = match as IMatchNavigation;
                tacticalAnchorProvider = match as ITacticalAnchorProvider;
                directiveProvider = match as IAgentDirectiveProvider;
                tacticalPicture = match as ITeamTacticalPicture;
            }

            if (match is IMatchCombatGate combatGate && !combatGate.CombatEnabled)
            {
                StopMoving();
                return;
            }

            if (match == null)
            {
                return;
            }

            decisionTimeRemaining -= Time.deltaTime;
            wanderTimeRemaining = Mathf.Max(
                0f,
                wanderTimeRemaining - Time.deltaTime);
            targetMemoryTimeRemaining = Mathf.Max(
                0f,
                targetMemoryTimeRemaining - Time.deltaTime);
            targetCommitmentTimeRemaining = Mathf.Max(
                0f,
                targetCommitmentTimeRemaining - Time.deltaTime);
            perceptionCacheTimeRemaining = Mathf.Max(
                0f,
                perceptionCacheTimeRemaining - Time.deltaTime);
            if (decisionTimeRemaining <= 0f)
            {
                Think();
                decisionTimeRemaining = Mathf.Max(
                    0.1f,
                    agent.Profile.ai.decisionInterval);
            }

            Act();
        }

        private void Think()
        {
            RefreshTuning();
            RefreshDirectiveState();

            if (navigation != null &&
                navigation.TryGetHazardDestination(transform.position, out var hazard))
            {
                target = null;
                targetMemoryTimeRemaining = 0f;
                targetCommitmentTimeRemaining = 0f;
                InvalidatePerceptionSnapshot();
                agent.SetState(BattleRoyaleAgentState.MovingToSafeZone);
                currentDestination = hazard;
                return;
            }

            if (agent.HealthPercent <= tuning.RetreatHealthPercent)
            {
                agent.SetState(BattleRoyaleAgentState.Retreating);
                currentDestination = navigation == null
                    ? transform.position
                    : navigation.GetRetreatPoint(
                    transform.position,
                    target == null ? transform.position : target.transform.position);
                return;
            }

            var directive = GetCurrentDirective();
            if (BattleRoyaleDirectiveRules.IsRetreat(directive))
            {
                target = null;
                targetMemoryTimeRemaining = 0f;
                targetCommitmentTimeRemaining = 0f;
                InvalidatePerceptionSnapshot();
                agent.SetState(BattleRoyaleAgentState.Retreating);
                currentDestination =
                    BattleRoyaleDirectiveRules.HasDestination(directive)
                        ? directive.destination
                        : navigation == null
                            ? transform.position
                            : navigation.GetRetreatPoint(
                                transform.position,
                                transform.position);
                return;
            }

            var previousTarget = target;
            var candidateTarget = FindTarget();
            var currentTargetIsValid = IsLivingTarget(previousTarget);
            var currentTargetScore = currentTargetIsValid
                ? GetTargetScore(
                    previousTarget,
                    HorizontalDistance(
                        transform.position,
                        previousTarget.transform.position))
                : float.MaxValue;
            var candidateTargetScore = candidateTarget == null
                ? float.MaxValue
                : GetTargetScore(
                    candidateTarget,
                    HorizontalDistance(
                        transform.position,
                        candidateTarget.transform.position));
            var shouldClearCurrentTarget =
                candidateTarget == null &&
                currentTargetIsValid &&
                targetCommitmentTimeRemaining <= 0f;
            var shouldSwitchTarget =
                shouldClearCurrentTarget ||
                BattleRoyaleAgentDecisionRules.ShouldSwitchTarget(
                    currentTargetIsValid,
                    targetCommitmentTimeRemaining,
                    currentTargetScore,
                    candidateTargetScore,
                    GetTargetSwitchScoreMargin());
            target = shouldSwitchTarget ? candidateTarget : previousTarget;
            if (target != previousTarget)
            {
                targetCommitmentTimeRemaining =
                    target == null
                        ? 0f
                        : GetTargetCommitmentDuration();
            }

            if (target != null &&
                target == candidateTarget &&
                agent.TeamIndex >= 0 &&
                tacticalPicture != null)
            {
                tacticalPicture.PublishObservation(
                    agent,
                    target,
                    target.transform.position,
                    1f);
                if (!tacticalPicture.TryClaimTarget(
                        agent,
                        target,
                        GetTargetClaimDuration()))
                {
                    target = null;
                    targetCommitmentTimeRemaining = 0f;
                    InvalidatePerceptionSnapshot();
                }
            }

            if (target != null &&
                agent.TeamIndex >= 0 &&
                tacticalPicture != null &&
                tacticalPicture.IsTargetClaimed(target, agent))
            {
                target = null;
                targetCommitmentTimeRemaining = 0f;
                InvalidatePerceptionSnapshot();
            }

            if (target != null)
            {
                if (target == candidateTarget)
                {
                    RememberTarget(target);
                }

                var distance = HorizontalDistance(
                    transform.position,
                    target.transform.position);
                var action = BattleRoyaleAgentDecisionRules.SelectAction(
                    hasVisibleTarget: true,
                    targetWithinHoldDistance: distance <= HoldDistance(),
                    hasFreshTargetMemory: false,
                    targetDistance: distance,
                    preferredCombatRange: tuning.PreferredCombatRange,
                    aggression: tuning.Aggression);
                agent.SetState(
                    action == BattleRoyaleAgentAction.Engage
                        ? BattleRoyaleAgentState.Engaging
                        : BattleRoyaleAgentState.SeekingTarget);
                currentDestination = GetCombatPosition();
                return;
            }

            if (BattleRoyaleDirectiveRules.IsHold(directive))
            {
                agent.SetState(BattleRoyaleAgentState.Exploring);
                currentDestination =
                    BattleRoyaleDirectiveRules.HasDestination(directive)
                        ? directive.destination
                        : transform.position;
                return;
            }

            if (tacticalPicture != null &&
                agent.TeamIndex >= 0 &&
                tacticalPicture.TryGetFreshObservation(
                    agent.TeamIndex,
                    agent,
                    GetTargetMemoryDuration(),
                    out var sharedObservation))
            {
                RememberSharedObservation(sharedObservation);
            }

            var hasFreshTargetMemory =
                BattleRoyaleAgentDecisionRules.IsTargetMemoryFresh(
                    targetMemoryTimeRemaining);
            var actionWithoutTarget =
                BattleRoyaleAgentDecisionRules.SelectAction(
                    hasVisibleTarget: false,
                    targetWithinHoldDistance: false,
                    hasFreshTargetMemory: hasFreshTargetMemory,
                    targetDistance: 0f,
                    preferredCombatRange: tuning.PreferredCombatRange,
                    aggression: tuning.Aggression);
            if (actionWithoutTarget == BattleRoyaleAgentAction.Search)
            {
                agent.SetState(
                    BattleRoyaleAgentState.SeekingTarget);
                currentDestination = lastKnownTargetPosition;
                return;
            }

            if (directive != null &&
                directive.type == BattleRoyaleDirectiveType.Push &&
                BattleRoyaleDirectiveRules.HasDestination(directive))
            {
                agent.SetState(BattleRoyaleAgentState.Exploring);
                currentDestination = directive.destination;
                return;
            }

            if (!BattleRoyaleAgentDecisionRules.ShouldRefreshWanderDestination(
                    agent.State,
                    wanderTimeRemaining))
            {
                return;
            }

            agent.SetState(BattleRoyaleAgentState.Exploring);
            currentDestination = ResolveExploreDestination();
            wanderTimeRemaining = agent.Profile == null ||
                                  agent.Profile.ai == null
                ? 3f
                : Mathf.Max(
                    0.1f,
                    agent.Profile.ai.wanderInterval);
        }

        private void RefreshTuning()
        {
            var matchPlaystyle = match == null
                ? TeamPlaystyle.Neutral
                : match.TeamPlaystyle;
            tuning = BattleRoyaleStrategyRules.GetTuning(
                agent.Strategy,
                agent.Profile.ai,
                SideStanceRules.Resolve(
                    agent.HasSidePlaystyle,
                    agent.SidePlaystyle,
                    matchPlaystyle));
        }

        private void RefreshDirectiveState()
        {
            var directive = GetCurrentDirective();
            var revision = directive == null ? 0 : directive.revision;
            if (revision == lastDirectiveRevision)
            {
                return;
            }

            lastDirectiveRevision = revision;
            target = null;
            targetMemoryTimeRemaining = 0f;
            targetCommitmentTimeRemaining = 0f;
            InvalidatePerceptionSnapshot();
        }

        private BattleRoyaleAgentDirective GetCurrentDirective()
        {
            if (directiveProvider == null || agent == null)
            {
                return null;
            }

            if (!SideStanceRules.ReceivesCoachDirective(
                    agent.HasSidePlaystyle,
                    agent.TeamIndex,
                    playerTeamIndex: 0))
            {
                return null;
            }

            return directiveProvider.CurrentDirective;
        }

        private void Act()
        {
            if (target != null && !target.IsAlive)
            {
                target = null;
                targetMemoryTimeRemaining = 0f;
                targetCommitmentTimeRemaining = 0f;
                InvalidatePerceptionSnapshot();
            }

            var lineOfSightConfirmed = false;
            if (target != null)
            {
                if (!TryGetCachedLineOfSightResult(
                        target,
                        out lineOfSightConfirmed))
                {
                    lineOfSightConfirmed =
                        BattleRoyaleLineOfSight.IsClear(agent, target);
                    CachePerceptionSnapshot(
                        target,
                        lineOfSightConfirmed);
                }

                if (!lineOfSightConfirmed)
                {
                    RememberTarget(target);
                    target = null;
                    Think();
                    return;
                }
            }

            switch (agent.State)
            {
                case BattleRoyaleAgentState.MovingToSafeZone:
                case BattleRoyaleAgentState.Retreating:
                case BattleRoyaleAgentState.Exploring:
                    MoveTo(currentDestination);
                    break;
                case BattleRoyaleAgentState.SeekingTarget:
                    if (target == null)
                    {
                        ActWhileSearching();
                        break;
                    }

                    ActWhileEngaging(lineOfSightConfirmed);
                    break;
                case BattleRoyaleAgentState.Engaging:
                    ActWhileEngaging(lineOfSightConfirmed);
                    break;
            }
        }

        private void ActWhileSearching()
        {
            if (!BattleRoyaleAgentDecisionRules.IsTargetMemoryFresh(
                    targetMemoryTimeRemaining))
            {
                Think();
                return;
            }

            if (!MoveTo(currentDestination))
            {
                targetMemoryTimeRemaining = 0f;
                Think();
            }
        }

        private void ActWhileEngaging(bool lineOfSightConfirmed)
        {
            if (target == null || !target.IsAlive)
            {
                targetMemoryTimeRemaining = 0f;
                Think();
                return;
            }

            var distance = HorizontalDistance(
                transform.position,
                target.transform.position);
            var agentMoved = false;
            if (distance > HoldDistance())
            {
                agent.SetState(BattleRoyaleAgentState.SeekingTarget);
                currentDestination = GetCombatPosition();
                agentMoved = MoveTo(currentDestination);
            }
            else
            {
                agent.SetState(BattleRoyaleAgentState.Engaging);
                StopMoving();
            }

            FacePosition(target.transform.position);
            var toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.001f ||
                Vector3.Angle(transform.forward, toTarget) > 12f)
            {
                return;
            }

            var fireAggression = BattleRoyaleDirectiveRules.GetPursuitAggression(
                GetCurrentDirective(),
                tuning.Aggression);
            if (!BattleRoyaleAgentDecisionRules.WillFireAtDistance(
                    distance,
                    agent.WeaponRange,
                    fireAggression))
            {
                return;
            }

            agent.Weapon?.TryFire(
                target,
                BattleRoyaleAgentDecisionRules.CanReuseLineOfSightResult(
                    lineOfSightConfirmed,
                    agentMoved));
        }

        private BattleRoyaleAgent FindTarget()
        {
            if (TryGetCachedPerceptionTarget(out var cachedTarget))
            {
                return cachedTarget;
            }

            var bestScore = float.MaxValue;
            BattleRoyaleAgent bestTarget = null;

            foreach (var candidate in match.Agents)
            {
                if (candidate == null ||
                    candidate == agent ||
                    !candidate.IsAlive ||
                    DeathmatchRules.SharesTeam(
                        agent.TeamIndex,
                        candidate.TeamIndex))
                {
                    continue;
                }

                if (agent.TeamIndex >= 0 &&
                    tacticalPicture != null &&
                    tacticalPicture.IsTargetClaimed(candidate, agent))
                {
                    continue;
                }

                var distance = HorizontalDistance(
                    transform.position,
                    candidate.transform.position);
                if (!BattleRoyaleLineOfSight.IsClear(agent, candidate))
                {
                    continue;
                }

                var score = GetTargetScore(candidate, distance);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }

            if (bestTarget != null)
            {
                CachePerceptionSnapshot(bestTarget, true);
            }
            else
            {
                InvalidatePerceptionSnapshot();
            }

            return bestTarget;
        }

        private bool TryGetCachedPerceptionTarget(
            out BattleRoyaleAgent cachedTarget)
        {
            cachedTarget = null;
            if (cachedPerceptionTarget == null ||
                !cachedPerceptionTarget.IsAlive ||
                !TryGetCachedLineOfSightResult(
                    cachedPerceptionTarget,
                    out var lineOfSightConfirmed) ||
                !lineOfSightConfirmed)
            {
                return false;
            }

            if (agent.TeamIndex >= 0 &&
                tacticalPicture != null &&
                tacticalPicture.IsTargetClaimed(
                    cachedPerceptionTarget,
                    agent))
            {
                return false;
            }

            cachedTarget = cachedPerceptionTarget;
            return true;
        }

        private bool TryGetCachedLineOfSightResult(
            BattleRoyaleAgent expectedTarget,
            out bool lineOfSightConfirmed)
        {
            lineOfSightConfirmed = false;
            if (!BattleRoyaleAgentDecisionRules.CanReusePerceptionSnapshot(
                    expectedTarget == cachedPerceptionTarget,
                    perceptionCacheTimeRemaining,
                    cachedPerceptionAgentPosition,
                    transform.position,
                    cachedPerceptionTargetPosition,
                    expectedTarget == null
                        ? Vector3.zero
                        : expectedTarget.transform.position,
                    PerceptionMovementTolerance))
            {
                return false;
            }

            lineOfSightConfirmed = cachedPerceptionLineOfSight;
            return true;
        }

        private void CachePerceptionSnapshot(
            BattleRoyaleAgent observedTarget,
            bool lineOfSightConfirmed)
        {
            cachedPerceptionTarget = observedTarget;
            cachedPerceptionAgentPosition = transform.position;
            cachedPerceptionTargetPosition = observedTarget == null
                ? Vector3.zero
                : observedTarget.transform.position;
            cachedPerceptionLineOfSight = lineOfSightConfirmed;
            perceptionCacheTimeRemaining =
                GetPerceptionCacheDuration();
        }

        private void InvalidatePerceptionSnapshot()
        {
            cachedPerceptionTarget = null;
            cachedPerceptionAgentPosition = Vector3.zero;
            cachedPerceptionTargetPosition = Vector3.zero;
            cachedPerceptionLineOfSight = false;
            perceptionCacheTimeRemaining = 0f;
        }

        private void RememberTarget(BattleRoyaleAgent observedTarget)
        {
            if (observedTarget == null)
            {
                return;
            }

            lastKnownTargetPosition = observedTarget.transform.position;
            targetMemoryTimeRemaining = GetTargetMemoryDuration();
        }

        private void RememberSharedObservation(
            BattleRoyaleTeamObservation observation)
        {
            if (observation == null ||
                observation.ObservedAt <= lastSharedObservationTime)
            {
                return;
            }

            lastSharedObservationTime = observation.ObservedAt;
            lastKnownTargetPosition = observation.Position;
            targetMemoryTimeRemaining = GetTargetMemoryDuration();
        }

        private static bool IsLivingTarget(BattleRoyaleAgent candidate)
        {
            return candidate != null && candidate.IsAlive;
        }

        private float GetTargetCommitmentDuration()
        {
            return agent.Profile == null ||
                   agent.Profile.ai == null
                ? 1f
                : Mathf.Max(
                    0f,
                    agent.Profile.ai.targetCommitmentDuration);
        }

        private float GetTargetMemoryDuration()
        {
            return agent.Profile == null ||
                   agent.Profile.ai == null
                ? 1.5f
                : Mathf.Max(
                    0f,
                    agent.Profile.ai.targetMemoryDuration);
        }

        private Vector3 ResolveExploreDestination()
        {
            if (match is SimulationMatch simulation &&
                simulation.TryGetExploreDestination(
                    agent,
                    GetExploreClaimDuration(),
                    out var destination))
            {
                return destination;
            }

            return navigation == null
                ? transform.position
                : navigation.GetWanderPoint();
        }

        private float GetExploreClaimDuration()
        {
            return agent.Profile == null ||
                   agent.Profile.ai == null
                ? 3f
                : Mathf.Max(0.1f, agent.Profile.ai.wanderInterval);
        }

        private float GetTargetClaimDuration()
        {
            return agent.Profile == null ||
                   agent.Profile.ai == null
                ? 1.5f
                : Mathf.Max(
                    0f,
                    agent.Profile.ai.targetClaimDuration);
        }

        private float GetPerceptionCacheDuration()
        {
            return agent.Profile == null ||
                   agent.Profile.ai == null
                ? 0.1f
                : Mathf.Max(
                    0f,
                    agent.Profile.ai.perceptionCacheDuration);
        }

        private float GetTargetSwitchScoreMargin()
        {
            return agent.Profile == null ||
                   agent.Profile.ai == null
                ? 0.15f
                : Mathf.Clamp01(
                    agent.Profile.ai.targetSwitchScoreMargin);
        }

        private float GetTargetScore(
            BattleRoyaleAgent candidate,
            float distance)
        {
            switch (agent.Strategy)
            {
                case BattleRoyaleAIStrategy.AggressiveHunter:
                    return distance;
                case BattleRoyaleAIStrategy.AggressiveSkirmisher:
                    return distance * 0.85f;
                case BattleRoyaleAIStrategy.NeutralOpportunist:
                    return candidate.HealthPercent * 100f +
                           distance * 0.05f;
                case BattleRoyaleAIStrategy.DefensiveAnchor:
                    return distance * 1.5f -
                           candidate.HealthPercent * 10f;
                case BattleRoyaleAIStrategy.NeutralScout:
                    return distance * 0.8f;
                case BattleRoyaleAIStrategy.DefensiveSurvivor:
                    return distance * 1.8f -
                           candidate.HealthPercent * 5f;
                default:
                    return distance;
            }
        }

        private Vector3 GetCombatPosition()
        {
            if (target == null)
            {
                return transform.position;
            }

            var directive = GetCurrentDirective();
            if (BattleRoyaleDirectiveRules.IsHold(directive))
            {
                return BattleRoyaleDirectiveRules.HasDestination(directive)
                    ? directive.destination
                    : transform.position;
            }

            if (tacticalAnchorProvider != null &&
                tacticalAnchorProvider.TryGetTacticalAnchor(
                    transform.position,
                    target.transform.position,
                    tuning.PreferredCombatRange,
                    out var tacticalAnchor))
            {
                return tacticalAnchor;
            }

            var desiredDistance = HoldDistance();
            var fromTarget = transform.position - target.transform.position;
            fromTarget.y = 0f;
            if (fromTarget.sqrMagnitude <= 0.001f)
            {
                fromTarget = Vector3.forward;
            }

            fromTarget.Normalize();
            var combatPosition = target.transform.position +
                                 fromTarget * desiredDistance;
            combatPosition.y = transform.position.y;
            return combatPosition;
        }

        private float HoldDistance()
        {
            var clearance = agent.BodyRadius;
            if (target != null)
            {
                clearance += target.BodyRadius;
            }

            return Mathf.Max(2f, clearance, tuning.PreferredCombatRange);
        }

        private bool MoveTo(Vector3 destination)
        {
            destination.y = transform.position.y;
            var navigationAgent = agent.Agent;
            if (navigationAgent != null &&
                navigationAgent.enabled &&
                navigationAgent.isOnNavMesh)
            {
                if (HorizontalDistance(transform.position, destination) <=
                    Mathf.Max(0.5f, navigationAgent.stoppingDistance))
                {
                    StopMoving();
                    return false;
                }

                var destinationChanged = !hasPathDestination ||
                    HorizontalDistance(lastPathDestination, destination) > 0.5f;
                if (navigationAgent.isStopped ||
                    !navigationAgent.hasPath ||
                    destinationChanged)
                {
                    if (!navigationAgent.SetDestination(destination))
                    {
                        hasPathDestination = false;
                        FaceDirection(destination - transform.position);
                        return navigationAgent.hasPath;
                    }
                    else
                    {
                        lastPathDestination = destination;
                        hasPathDestination = true;
                    }
                }

                if (navigationAgent.enabled && navigationAgent.isOnNavMesh)
                {
                    navigationAgent.isStopped = false;
                    var movementDirection = navigationAgent.desiredVelocity;
                    if (movementDirection.sqrMagnitude <= 0.001f)
                    {
                        movementDirection = destination - transform.position;
                    }

                    FaceDirection(movementDirection);
                    return true;
                }
            }

            return false;
        }

        private void StopMoving()
        {
            hasPathDestination = false;
            var navigationAgent = agent.Agent;
            if (navigationAgent == null || !navigationAgent.enabled)
            {
                return;
            }

            if (navigationAgent.isOnNavMesh)
            {
                navigationAgent.ResetPath();
                navigationAgent.isStopped = true;
            }
        }

        private void FacePosition(Vector3 position)
        {
            FaceDirection(position - transform.position);
        }

        private void FaceDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(direction.normalized);
            var turnSpeed = agent.Profile == null ||
                            agent.Profile.ai == null
                ? 540f
                : Mathf.Max(0f, agent.Profile.ai.turnSpeed);
            transform.rotation = turnSpeed <= 0f
                ? targetRotation
                : Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime);
        }

        private static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            first.y = 0f;
            second.y = 0f;
            return Vector3.Distance(first, second);
        }

        private void OnDrawGizmosSelected()
        {
            if (agent == null)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentDestination);
            if (target != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, target.transform.position);
            }
        }
    }
}
