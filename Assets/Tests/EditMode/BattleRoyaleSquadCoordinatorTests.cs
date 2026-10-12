using System.Collections.Generic;
using BattleRoyale.GameModes.SoloBattleRoyale;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class BattleRoyaleSquadCoordinatorTests
    {
        private readonly List<GameObject> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var createdObject in createdObjects)
            {
                if (createdObject != null)
                {
                    Object.DestroyImmediate(createdObject);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void DefaultSquadRolesReflectStrategyAndSpecialty()
        {
            Assert.AreEqual(
                BattleRoyaleSquadRole.Assault,
                BattleRoyaleSquadCoordinator.GetDefaultRole(
                    BattleRoyaleAIStrategy.AggressiveHunter,
                    BattleRoyaleAgentSpecialty.Balanced,
                    0));
            Assert.AreEqual(
                BattleRoyaleSquadRole.Scout,
                BattleRoyaleSquadCoordinator.GetDefaultRole(
                    BattleRoyaleAIStrategy.NeutralScout,
                    BattleRoyaleAgentSpecialty.ReconSpecialist,
                    1));
            Assert.AreEqual(
                BattleRoyaleSquadRole.Defender,
                BattleRoyaleSquadCoordinator.GetDefaultRole(
                    BattleRoyaleAIStrategy.DefensiveAnchor,
                    BattleRoyaleAgentSpecialty.LockdownDefender,
                    2));
            Assert.AreEqual(
                BattleRoyaleSquadRole.Marksman,
                BattleRoyaleSquadCoordinator.GetDefaultRole(
                    BattleRoyaleAIStrategy.AggressiveHunter,
                    BattleRoyaleAgentSpecialty.Sharpshooter,
                    3));
        }

        [Test]
        public void FreshObservationIsAvailableToAnotherTeammate()
        {
            var now = 10f;
            var coordinator = new BattleRoyaleSquadCoordinator(() => now);
            var observer = CreateAgent(1);
            var teammate = CreateAgent(1);
            var target = CreateAgent(2);

            coordinator.PublishObservation(
                observer,
                target,
                new Vector3(12f, 0f, 4f),
                0.8f);

            Assert.IsTrue(
                coordinator.TryGetFreshObservation(
                    teammate.TeamIndex,
                    teammate,
                    1f,
                    out var observation));
            Assert.AreSame(observer, observation.Observer);
            Assert.AreSame(target, observation.Target);
            Assert.AreEqual(0.8f, observation.Confidence, 0.001f);
        }

        [Test]
        public void ObservationExpiresAfterItsConfiguredAge()
        {
            var now = 10f;
            var coordinator = new BattleRoyaleSquadCoordinator(() => now);
            var observer = CreateAgent(1);
            var teammate = CreateAgent(1);
            var target = CreateAgent(2);

            coordinator.PublishObservation(
                observer,
                target,
                target.transform.position,
                1f);

            now = 11.01f;

            Assert.IsFalse(
                coordinator.TryGetFreshObservation(
                    teammate.TeamIndex,
                    teammate,
                    1f,
                    out _));
        }

        [Test]
        public void TargetClaimPreventsDuplicateSameTeamAssignment()
        {
            var coordinator = new BattleRoyaleSquadCoordinator(() => 0f);
            var firstAgent = CreateAgent(1);
            var secondAgent = CreateAgent(1);
            var target = CreateAgent(2);

            Assert.IsTrue(
                coordinator.TryClaimTarget(firstAgent, target, 1.5f));
            Assert.IsTrue(
                coordinator.IsTargetClaimed(target, secondAgent));
            Assert.IsFalse(
                coordinator.TryClaimTarget(secondAgent, target, 1.5f));
            Assert.IsFalse(
                coordinator.IsTargetClaimed(target, firstAgent));
        }

        [Test]
        public void ExpiredTargetClaimCanBeTakenByAnotherTeammate()
        {
            var now = 0f;
            var coordinator = new BattleRoyaleSquadCoordinator(() => now);
            var firstAgent = CreateAgent(1);
            var secondAgent = CreateAgent(1);
            var target = CreateAgent(2);

            Assert.IsTrue(
                coordinator.TryClaimTarget(firstAgent, target, 1.5f));

            now = 1.5f;

            Assert.IsFalse(
                coordinator.IsTargetClaimed(target, secondAgent));
            Assert.IsTrue(
                coordinator.TryClaimTarget(secondAgent, target, 1.5f));
        }

        private BattleRoyaleAgent CreateAgent(int teamIndex)
        {
            var gameObject = new GameObject($"Squad Test Agent {createdObjects.Count}");
            createdObjects.Add(gameObject);
            var agent = gameObject.AddComponent<BattleRoyaleAgent>();
            agent.AssignTeam(teamIndex);
            return agent;
        }
    }
}
