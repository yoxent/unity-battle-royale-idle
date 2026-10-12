using BattleRoyale.GameModes.SoloBattleRoyale;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class AgentIntelligenceGapTests
    {
        [Test]
        public void PerceptionRangeIncludesStrategyMultiplier()
        {
            var range =
                BattleRoyaleAgentDecisionRules.GetPerceptionRange(80f, 1.5f);

            Assert.AreEqual(120f, range, 0.001f);
        }

        [Test]
        public void PerceptionRangeRejectsCandidatesBeyondBoundary()
        {
            Assert.IsTrue(
                BattleRoyaleAgentDecisionRules.IsWithinPerceptionRange(
                    120f,
                    120f));
            Assert.IsFalse(
                BattleRoyaleAgentDecisionRules.IsWithinPerceptionRange(
                    120.01f,
                    120f));
        }

        [Test]
        public void AggressionControlsPursuitBeyondEffectiveRange()
        {
            Assert.AreEqual(
                20f,
                BattleRoyaleAgentDecisionRules.GetPursuitRange(
                    20f,
                    80f,
                    0f),
                0.001f);
            Assert.AreEqual(
                80f,
                BattleRoyaleAgentDecisionRules.GetPursuitRange(
                    20f,
                    80f,
                    1f),
                0.001f);
        }

        [Test]
        public void DirectivePursuitRangeUsesTheSameAggressionForSelectionAndValidation()
        {
            var push = new BattleRoyaleAgentDirective
            {
                type = BattleRoyaleDirectiveType.Push
            };

            Assert.AreEqual(
                80f,
                BattleRoyaleDirectiveRules.GetPursuitRange(
                    push,
                    weaponRange: 20f,
                    perceptionRange: 80f,
                    baseAggression: 0f),
                0.001f);
            Assert.AreEqual(
                20f,
                BattleRoyaleDirectiveRules.GetPursuitRange(
                    directive: null,
                    weaponRange: 20f,
                    perceptionRange: 80f,
                    baseAggression: 0f),
                0.001f);
        }

        [Test]
        public void WanderDestinationIsHeldUntilIntervalExpires()
        {
            Assert.IsFalse(
                BattleRoyaleAgentDecisionRules.ShouldRefreshWanderDestination(
                    BattleRoyaleAgentState.Exploring,
                    1f));
            Assert.IsTrue(
                BattleRoyaleAgentDecisionRules.ShouldRefreshWanderDestination(
                    BattleRoyaleAgentState.Exploring,
                    0f));
            Assert.IsTrue(
                BattleRoyaleAgentDecisionRules.ShouldRefreshWanderDestination(
                    BattleRoyaleAgentState.SeekingTarget,
                    1f));
        }

        [Test]
        public void TargetMemoryExpiresAtZeroRemainingTime()
        {
            Assert.IsTrue(
                BattleRoyaleAgentDecisionRules.IsTargetMemoryFresh(0.01f));
            Assert.IsFalse(
                BattleRoyaleAgentDecisionRules.IsTargetMemoryFresh(0f));
        }

        [Test]
        public void TargetCommitmentBlocksMarginalSwitches()
        {
            Assert.IsFalse(
                BattleRoyaleAgentDecisionRules.ShouldSwitchTarget(
                    currentTargetIsValid: true,
                    commitmentTimeRemaining: 0.5f,
                    currentTargetScore: 10f,
                    candidateTargetScore: 1f,
                    switchScoreMargin: 0.15f));
        }

        [Test]
        public void TargetSwitchRequiresMeaningfulScoreImprovement()
        {
            Assert.IsFalse(
                BattleRoyaleAgentDecisionRules.ShouldSwitchTarget(
                    currentTargetIsValid: true,
                    commitmentTimeRemaining: 0f,
                    currentTargetScore: 10f,
                    candidateTargetScore: 9.5f,
                    switchScoreMargin: 0.15f));
            Assert.IsTrue(
                BattleRoyaleAgentDecisionRules.ShouldSwitchTarget(
                    currentTargetIsValid: true,
                    commitmentTimeRemaining: 0f,
                    currentTargetScore: 10f,
                    candidateTargetScore: 8f,
                    switchScoreMargin: 0.15f));
        }

        [Test]
        public void InvalidTargetCanBeReplacedImmediately()
        {
            Assert.IsTrue(
                BattleRoyaleAgentDecisionRules.ShouldSwitchTarget(
                    currentTargetIsValid: false,
                    commitmentTimeRemaining: 0.5f,
                    currentTargetScore: 10f,
                    candidateTargetScore: 100f,
                    switchScoreMargin: 0.15f));
        }

        [Test]
        public void DecisionPhaseDelaysSpreadAndRepeat()
        {
            Assert.AreEqual(
                0f,
                BattleRoyaleAgentDecisionRules.GetDecisionPhaseDelay(0),
                0.001f);
            Assert.AreEqual(
                0.1f,
                BattleRoyaleAgentDecisionRules.GetDecisionPhaseDelay(1),
                0.001f);
            Assert.AreEqual(
                0f,
                BattleRoyaleAgentDecisionRules.GetDecisionPhaseDelay(5),
                0.001f);
        }

        [Test]
        public void LineOfSightResultIsReusedOnlyWhenTheAgentDidNotMove()
        {
            Assert.IsTrue(
                BattleRoyaleAgentDecisionRules.CanReuseLineOfSightResult(
                    lineOfSightConfirmed: true,
                    agentMoved: false));
            Assert.IsFalse(
                BattleRoyaleAgentDecisionRules.CanReuseLineOfSightResult(
                    lineOfSightConfirmed: true,
                    agentMoved: true));
        }

        [Test]
        public void PerceptionSnapshotIsReusableWhenPositionsRemainStable()
        {
            var agentPosition = new Vector3(2f, 0f, 3f);
            var targetPosition = new Vector3(8f, 0f, 3f);

            Assert.IsTrue(
                BattleRoyaleAgentDecisionRules.CanReusePerceptionSnapshot(
                    sameTarget: true,
                    remainingTime: 0.05f,
                    cachedAgentPosition: agentPosition,
                    currentAgentPosition: agentPosition,
                    cachedTargetPosition: targetPosition,
                    currentTargetPosition: targetPosition,
                    movementTolerance: 0.1f));
        }

        [Test]
        public void PerceptionSnapshotInvalidatesOnExpiryOrMovement()
        {
            var agentPosition = new Vector3(2f, 0f, 3f);
            var targetPosition = new Vector3(8f, 0f, 3f);

            Assert.IsFalse(
                BattleRoyaleAgentDecisionRules.CanReusePerceptionSnapshot(
                    sameTarget: true,
                    remainingTime: 0f,
                    cachedAgentPosition: agentPosition,
                    currentAgentPosition: agentPosition,
                    cachedTargetPosition: targetPosition,
                    currentTargetPosition: targetPosition,
                    movementTolerance: 0.1f));
            Assert.IsFalse(
                BattleRoyaleAgentDecisionRules.CanReusePerceptionSnapshot(
                    sameTarget: true,
                    remainingTime: 0.05f,
                    cachedAgentPosition: agentPosition,
                    currentAgentPosition: agentPosition +
                        Vector3.right * 0.11f,
                    cachedTargetPosition: targetPosition,
                    currentTargetPosition: targetPosition,
                    movementTolerance: 0.1f));
            Assert.IsFalse(
                BattleRoyaleAgentDecisionRules.CanReusePerceptionSnapshot(
                    sameTarget: true,
                    remainingTime: 0.05f,
                    cachedAgentPosition: agentPosition,
                    currentAgentPosition: agentPosition,
                    cachedTargetPosition: targetPosition,
                    currentTargetPosition: targetPosition +
                        Vector3.forward * 0.11f,
                    movementTolerance: 0.1f));
        }

        [Test]
        public void LocalActionSetChoosesEngagementAndPursuit()
        {
            Assert.AreEqual(
                BattleRoyaleAgentAction.Engage,
                BattleRoyaleAgentDecisionRules.SelectAction(
                    hasVisibleTarget: true,
                    targetWithinHoldDistance: true,
                    hasFreshTargetMemory: false,
                    targetDistance: 18f,
                    preferredCombatRange: 20f,
                    aggression: 0.5f));
            Assert.AreEqual(
                BattleRoyaleAgentAction.Pursue,
                BattleRoyaleAgentDecisionRules.SelectAction(
                    hasVisibleTarget: true,
                    targetWithinHoldDistance: false,
                    hasFreshTargetMemory: false,
                    targetDistance: 40f,
                    preferredCombatRange: 20f,
                    aggression: 0.5f));
        }

        [Test]
        public void LocalActionSetSearchesMemoryBeforeExploring()
        {
            Assert.AreEqual(
                BattleRoyaleAgentAction.Search,
                BattleRoyaleAgentDecisionRules.SelectAction(
                    hasVisibleTarget: false,
                    targetWithinHoldDistance: false,
                    hasFreshTargetMemory: true,
                    targetDistance: 0f,
                    preferredCombatRange: 20f,
                    aggression: 0.5f));
            Assert.AreEqual(
                BattleRoyaleAgentAction.Explore,
                BattleRoyaleAgentDecisionRules.SelectAction(
                    hasVisibleTarget: false,
                    targetWithinHoldDistance: false,
                    hasFreshTargetMemory: false,
                    targetDistance: 0f,
                    preferredCombatRange: 20f,
                    aggression: 0.5f));
        }

        [Test]
        public void CoachDirectivesChangePursuitWithoutTakingOverLocalCombat()
        {
            var push = new BattleRoyaleAgentDirective
            {
                type = BattleRoyaleDirectiveType.Push
            };
            var hold = new BattleRoyaleAgentDirective
            {
                type = BattleRoyaleDirectiveType.Hold
            };

            Assert.AreEqual(
                1f,
                BattleRoyaleDirectiveRules.GetPursuitAggression(
                    push,
                    0.2f),
                0.001f);
            Assert.AreEqual(
                0f,
                BattleRoyaleDirectiveRules.GetPursuitAggression(
                    hold,
                    0.8f),
                0.001f);
            Assert.IsTrue(BattleRoyaleDirectiveRules.IsHold(hold));
            Assert.IsFalse(BattleRoyaleDirectiveRules.IsRetreat(push));
        }

        [Test]
        public void RetreatDirectiveIsAnExplicitEmergencyDirective()
        {
            var retreat = new BattleRoyaleAgentDirective
            {
                type = BattleRoyaleDirectiveType.Retreat,
                hasDestination = true
            };

            Assert.IsTrue(BattleRoyaleDirectiveRules.IsRetreat(retreat));
            Assert.IsTrue(BattleRoyaleDirectiveRules.HasDestination(retreat));
        }
    }
}
