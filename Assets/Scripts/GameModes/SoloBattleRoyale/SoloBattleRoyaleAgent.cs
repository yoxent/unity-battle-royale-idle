using BattleRoyale.GameModes;
using UnityEngine;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    [DisallowMultipleComponent]
    public sealed class SoloBattleRoyaleAgent : MonoBehaviour
    {
        [SerializeField] private BattleRoyaleCharacter character;
        [SerializeField] private BattleRoyaleCharacter target;
        [SerializeField] private Vector3 currentDestination;
        [SerializeField] private float decisionTimeRemaining;

        private SoloBattleRoyaleGameMode match;
        private BattleRoyaleStrategyTuning tuning;
        private Vector3 lastPathDestination;
        private bool hasPathDestination;

        public BattleRoyaleCharacter Character => character;
        public BattleRoyaleCharacter Target => target;
        public Vector3 CurrentDestination => currentDestination;
        public string DebugLabel =>
            character == null
                ? "Uninitialized"
                : $"{character.Profile?.displayName ?? "Unknown"} / " +
                  $"{character.Strategy} / {character.Specialty} / " +
                  $"{character.State}";

        public void Initialize(
            SoloBattleRoyaleGameMode owningMatch,
            BattleRoyaleCharacter owningCharacter)
        {
            match = owningMatch;
            character = owningCharacter;
            decisionTimeRemaining = 0f;
            RefreshTuning();
        }

        private void Awake()
        {
            if (character == null)
            {
                character = GetComponent<BattleRoyaleCharacter>();
            }
        }

        private void Update()
        {
            if (character == null || !character.IsAlive)
            {
                return;
            }

            if (match == null)
            {
                match = character.Match;
            }

            if (match == null)
            {
                return;
            }

            decisionTimeRemaining -= Time.deltaTime;
            if (decisionTimeRemaining <= 0f)
            {
                Think();
                decisionTimeRemaining = Mathf.Max(
                    0.1f,
                    character.Profile.ai.decisionInterval);
            }

            Act();
        }

        private void Think()
        {
            RefreshTuning();

            if (!match.IsInsideSafeZone(transform.position))
            {
                target = null;
                character.SetState(BattleRoyaleCharacterState.MovingToSafeZone);
                currentDestination = match.GetSafeZonePoint(transform.position);
                return;
            }

            if (character.HealthPercent <= tuning.RetreatHealthPercent)
            {
                character.SetState(BattleRoyaleCharacterState.Retreating);
                currentDestination = match.GetRetreatPoint(
                    transform.position,
                    target == null ? transform.position : target.transform.position);
                return;
            }

            target = FindTarget();
            if (target != null)
            {
                var distance = HorizontalDistance(
                    transform.position,
                    target.transform.position);
                if (distance <= character.WeaponRange)
                {
                    character.SetState(BattleRoyaleCharacterState.Engaging);
                    currentDestination = GetCombatPosition();
                }
                else
                {
                    character.SetState(BattleRoyaleCharacterState.SeekingTarget);
                    currentDestination = target.transform.position;
                }

                return;
            }

            character.SetState(BattleRoyaleCharacterState.Exploring);
            currentDestination = match.GetRandomPointWithinSafeZone();
        }

        private void RefreshTuning()
        {
            tuning = BattleRoyaleStrategyRules.GetTuning(
                character.Strategy,
                character.Profile.ai,
                match == null
                    ? TeamPlaystyle.Neutral
                    : match.TeamPlaystyle);
        }

        private void Act()
        {
            if (target != null && !target.IsAlive)
            {
                target = null;
            }

            if (target != null &&
                !BattleRoyaleLineOfSight.IsClear(character, target))
            {
                target = null;
                Think();
                return;
            }

            switch (character.State)
            {
                case BattleRoyaleCharacterState.MovingToSafeZone:
                case BattleRoyaleCharacterState.Retreating:
                case BattleRoyaleCharacterState.Exploring:
                case BattleRoyaleCharacterState.SeekingTarget:
                    MoveTo(currentDestination);
                    break;
                case BattleRoyaleCharacterState.Engaging:
                    ActWhileEngaging();
                    break;
            }
        }

        private void ActWhileEngaging()
        {
            if (target == null || !target.IsAlive)
            {
                Think();
                return;
            }

            var distance = HorizontalDistance(
                transform.position,
                target.transform.position);
            if (distance > character.WeaponRange)
            {
                character.SetState(BattleRoyaleCharacterState.SeekingTarget);
                currentDestination = target.transform.position;
                MoveTo(currentDestination);
                return;
            }

            StopMoving();
            FacePosition(target.transform.position);
            var toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.001f ||
                Vector3.Angle(transform.forward, toTarget) > 12f)
            {
                return;
            }

            character.Weapon?.TryFire(target);
        }

        private BattleRoyaleCharacter FindTarget()
        {
            var perceptionRange = character.PerceptionRange *
                                  tuning.PerceptionMultiplier;
            var bestScore = float.MaxValue;
            BattleRoyaleCharacter bestTarget = null;

            foreach (var candidate in match.Characters)
            {
                if (candidate == null ||
                    candidate == character ||
                    !candidate.IsAlive)
                {
                    continue;
                }

                var distance = HorizontalDistance(
                    transform.position,
                    candidate.transform.position);
                if (distance > perceptionRange ||
                    !BattleRoyaleLineOfSight.IsClear(character, candidate))
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

            return bestTarget;
        }

        private float GetTargetScore(
            BattleRoyaleCharacter candidate,
            float distance)
        {
            switch (character.Strategy)
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

            var desiredDistance = Mathf.Clamp(
                tuning.PreferredCombatRange,
                2f,
                character.WeaponRange);
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

        private bool MoveTo(Vector3 destination)
        {
            destination.y = transform.position.y;
            var agent = character.Agent;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                if (HorizontalDistance(transform.position, destination) <=
                    Mathf.Max(0.5f, agent.stoppingDistance))
                {
                    StopMoving();
                    return false;
                }

                var destinationChanged = !hasPathDestination ||
                    HorizontalDistance(lastPathDestination, destination) > 0.5f;
                if (agent.isStopped || !agent.hasPath || destinationChanged)
                {
                    if (!agent.SetDestination(destination))
                    {
                        hasPathDestination = false;
                        FaceDirection(destination - transform.position);
                        return agent.hasPath;
                    }
                    else
                    {
                        lastPathDestination = destination;
                        hasPathDestination = true;
                    }
                }

                if (agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    var movementDirection = agent.desiredVelocity;
                    if (movementDirection.sqrMagnitude <= 0.001f)
                    {
                        movementDirection = destination - transform.position;
                    }

                    FaceDirection(movementDirection);
                    return true;
                }
            }

            if (agent != null && agent.enabled)
            {
                agent.enabled = false;
            }

            var before = transform.position;
            var nextPosition = Vector3.MoveTowards(
                before,
                destination,
                character.MoveSpeed * Time.deltaTime);
            var movement = nextPosition - before;
            if (movement.sqrMagnitude <= 0.001f)
            {
                return false;
            }

            transform.position = nextPosition;
            FaceDirection(movement);
            return true;
        }

        private void StopMoving()
        {
            hasPathDestination = false;
            var agent = character.Agent;
            if (agent == null || !agent.enabled)
            {
                return;
            }

            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.isStopped = true;
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
            var turnSpeed = character.Profile == null ||
                            character.Profile.ai == null
                ? 540f
                : Mathf.Max(0f, character.Profile.ai.turnSpeed);
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
            if (character == null)
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
