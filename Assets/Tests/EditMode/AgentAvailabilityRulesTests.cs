using BattleRoyale.Company;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class AgentAvailabilityRulesTests
    {
        [Test]
        public void DeployMarksTheFieldedAgentBusy()
        {
            var roster = new[] { Agent(1), Agent(2) };

            Assert.IsTrue(AgentAvailabilityRules.TryDeploy(
                roster,
                new[] { 1 },
                out var fielded));

            Assert.IsTrue(IsBusy(fielded, 1));
            Assert.IsFalse(IsBusy(fielded, 2));
            Assert.IsFalse(IsBusy(roster, 1));
        }

        [Test]
        public void ABusyAgentCannotDeployAgain()
        {
            Assert.IsTrue(AgentAvailabilityRules.TryDeploy(
                new[] { Agent(1) },
                new[] { 1 },
                out var fielded));

            Assert.IsFalse(AgentAvailabilityRules.TryDeploy(
                fielded,
                new[] { 1 },
                out _));
            Assert.IsTrue(IsBusy(fielded, 1));
        }

        [Test]
        public void AFreeAgentCanDeployWhileAnotherIsBusy()
        {
            Assert.IsTrue(AgentAvailabilityRules.TryDeploy(
                new[] { Agent(1), Agent(2) },
                new[] { 1 },
                out var first));
            Assert.IsTrue(AgentAvailabilityRules.TryDeploy(
                first,
                new[] { 2 },
                out var second));

            Assert.IsTrue(IsBusy(second, 1));
            Assert.IsTrue(IsBusy(second, 2));
        }

        [Test]
        public void DeployingABusyAgentBesideAFreeOneIsRefused()
        {
            Assert.IsTrue(AgentAvailabilityRules.TryDeploy(
                new[] { Agent(1), Agent(2) },
                new[] { 1 },
                out var fielded));

            Assert.IsFalse(AgentAvailabilityRules.TryDeploy(
                fielded,
                new[] { 1, 2 },
                out _));
            Assert.IsTrue(IsBusy(fielded, 1));
            Assert.IsFalse(IsBusy(fielded, 2));
        }

        [Test]
        public void AbortFreesTheAgent()
        {
            Assert.IsTrue(AgentAvailabilityRules.TryDeploy(
                new[] { Agent(1), Agent(2) },
                new[] { 1 },
                out var fielded));
            Assert.IsTrue(AgentAvailabilityRules.TryAbort(
                fielded,
                new[] { 1 },
                out var aborted));

            Assert.IsFalse(IsBusy(aborted, 1));
            Assert.IsFalse(IsBusy(aborted, 2));
            Assert.IsTrue(AgentAvailabilityRules.TryDeploy(
                aborted,
                new[] { 1 },
                out _));
        }

        [Test]
        public void FinishFreesTheAgentForAnotherDeploy()
        {
            Assert.IsTrue(AgentAvailabilityRules.TryDeploy(
                new[] { Agent(1) },
                new[] { 1 },
                out var fielded));
            Assert.IsTrue(AgentAvailabilityRules.TryFinish(
                fielded,
                new[] { 1 },
                out var finished));

            Assert.IsFalse(IsBusy(finished, 1));
            Assert.IsTrue(AgentAvailabilityRules.TryDeploy(
                finished,
                new[] { 1 },
                out var again));
            Assert.IsTrue(IsBusy(again, 1));
        }

        static AgentAvailability Agent(int id)
        {
            return new AgentAvailability { Id = id };
        }

        static bool IsBusy(AgentAvailability[] roster, int id)
        {
            for (var i = 0; i < roster.Length; i++)
            {
                if (roster[i].Id == id)
                {
                    return roster[i].Busy;
                }
            }

            return false;
        }
    }
}
