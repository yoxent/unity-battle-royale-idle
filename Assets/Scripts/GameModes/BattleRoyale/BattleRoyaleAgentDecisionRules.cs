using UnityEngine;

namespace BattleRoyale.GameModes.BattleRoyale
{
    public enum BattleRoyaleAgentAction
    {
        Explore,
        Search,
        Pursue,
        Engage
    }

    public static class BattleRoyaleAgentDecisionRules
    {
        public static float GetPerceptionRange(
            float baseRange,
            float strategyMultiplier)
        {
            return Mathf.Max(0f, baseRange) *
                   Mathf.Max(0f, strategyMultiplier);
        }

        public static bool CanPerceive(float distance)
        {
            return float.IsFinite(distance) && distance >= 0f;
        }

        public static bool WillFireAtDistance(
            float distance,
            float weaponRange,
            float aggression)
        {
            var reach = Mathf.Max(0f, weaponRange);
            var shotDistance = Mathf.Max(0f, distance);
            if (shotDistance <= reach)
            {
                return true;
            }

            var willingness = Mathf.Clamp01(aggression);
            if (willingness <= 0f)
            {
                return false;
            }

            if (willingness >= 1f)
            {
                return true;
            }

            return shotDistance <= reach / (1f - willingness);
        }

        public static float GetPursuitRange(
            float weaponRange,
            float perceptionRange,
            float aggression)
        {
            var perceivedRange = Mathf.Max(0f, perceptionRange);
            var effectiveRange = Mathf.Min(
                Mathf.Max(0f, weaponRange),
                perceivedRange);
            return Mathf.Lerp(
                effectiveRange,
                perceivedRange,
                Mathf.Clamp01(aggression));
        }

        public static bool ShouldRefreshWanderDestination(
            BattleRoyaleAgentState state,
            float remainingTime)
        {
            return state != BattleRoyaleAgentState.Exploring ||
                   remainingTime <= 0f;
        }

        public static bool IsTargetMemoryFresh(float remainingTime)
        {
            return remainingTime > 0f;
        }

        public static bool ShouldSwitchTarget(
            bool currentTargetIsValid,
            float commitmentTimeRemaining,
            float currentTargetScore,
            float candidateTargetScore,
            float switchScoreMargin)
        {
            if (!currentTargetIsValid)
            {
                return true;
            }

            if (commitmentTimeRemaining > 0f)
            {
                return false;
            }

            var requiredImprovement =
                Mathf.Max(0f, switchScoreMargin) *
                Mathf.Max(1f, Mathf.Abs(currentTargetScore));
            return candidateTargetScore <
                   currentTargetScore - requiredImprovement;
        }

        public static float GetDecisionPhaseDelay(
            int agentIndex,
            float phaseDuration = 0.1f,
            int phaseCount = 5)
        {
            if (phaseDuration <= 0f || phaseCount <= 0)
            {
                return 0f;
            }

            var phase = Mathf.Max(0, agentIndex) % phaseCount;
            return phase * phaseDuration;
        }

        public static bool CanReuseLineOfSightResult(
            bool lineOfSightConfirmed,
            bool agentMoved)
        {
            return lineOfSightConfirmed && !agentMoved;
        }

        public static bool CanReusePerceptionSnapshot(
            bool sameTarget,
            float remainingTime,
            Vector3 cachedAgentPosition,
            Vector3 currentAgentPosition,
            Vector3 cachedTargetPosition,
            Vector3 currentTargetPosition,
            float movementTolerance)
        {
            if (!sameTarget || remainingTime <= 0f)
            {
                return false;
            }

            var tolerance = Mathf.Max(0f, movementTolerance);
            var toleranceSquared = tolerance * tolerance;
            return (currentAgentPosition - cachedAgentPosition).sqrMagnitude <=
                       toleranceSquared &&
                   (currentTargetPosition - cachedTargetPosition).sqrMagnitude <=
                       toleranceSquared;
        }

        public static BattleRoyaleAgentAction SelectAction(
            bool hasVisibleTarget,
            bool targetWithinHoldDistance,
            bool hasFreshTargetMemory,
            float targetDistance,
            float preferredCombatRange,
            float aggression)
        {
            var selectedAction = BattleRoyaleAgentAction.Explore;
            var bestScore = GetActionUtility(
                selectedAction,
                hasVisibleTarget,
                targetWithinHoldDistance,
                hasFreshTargetMemory,
                targetDistance,
                preferredCombatRange,
                aggression);

            var searchScore = GetActionUtility(
                BattleRoyaleAgentAction.Search,
                hasVisibleTarget,
                targetWithinHoldDistance,
                hasFreshTargetMemory,
                targetDistance,
                preferredCombatRange,
                aggression);
            if (searchScore > bestScore)
            {
                selectedAction = BattleRoyaleAgentAction.Search;
                bestScore = searchScore;
            }

            var pursueScore = GetActionUtility(
                BattleRoyaleAgentAction.Pursue,
                hasVisibleTarget,
                targetWithinHoldDistance,
                hasFreshTargetMemory,
                targetDistance,
                preferredCombatRange,
                aggression);
            if (pursueScore > bestScore)
            {
                selectedAction = BattleRoyaleAgentAction.Pursue;
                bestScore = pursueScore;
            }

            var engageScore = GetActionUtility(
                BattleRoyaleAgentAction.Engage,
                hasVisibleTarget,
                targetWithinHoldDistance,
                hasFreshTargetMemory,
                targetDistance,
                preferredCombatRange,
                aggression);
            if (engageScore > bestScore)
            {
                selectedAction = BattleRoyaleAgentAction.Engage;
            }

            return selectedAction;
        }

        public static float GetActionUtility(
            BattleRoyaleAgentAction action,
            bool hasVisibleTarget,
            bool targetWithinHoldDistance,
            bool hasFreshTargetMemory,
            float targetDistance,
            float preferredCombatRange,
            float aggression)
        {
            var clampedAggression = Mathf.Clamp01(aggression);
            var preferredRange = Mathf.Max(1f, preferredCombatRange);
            var rangeFit = Mathf.Clamp01(
                1f -
                Mathf.Abs(targetDistance - preferredRange) /
                preferredRange);

            switch (action)
            {
                case BattleRoyaleAgentAction.Search:
                    return hasFreshTargetMemory
                        ? 0.45f + (1f - clampedAggression) * 0.1f
                        : -1f;
                case BattleRoyaleAgentAction.Pursue:
                    return !hasVisibleTarget ||
                           targetWithinHoldDistance
                        ? -1f
                        : 0.6f +
                          clampedAggression * 0.2f +
                          (1f - rangeFit) * 0.1f;
                case BattleRoyaleAgentAction.Engage:
                    return !hasVisibleTarget ||
                           !targetWithinHoldDistance
                        ? -1f
                        : 0.7f +
                          rangeFit * 0.2f +
                          clampedAggression * 0.1f;
                case BattleRoyaleAgentAction.Explore:
                default:
                    return hasVisibleTarget || hasFreshTargetMemory
                        ? 0f
                        : 0.2f;
            }
        }
    }
}
