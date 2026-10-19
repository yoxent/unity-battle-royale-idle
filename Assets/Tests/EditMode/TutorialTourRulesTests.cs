using BattleRoyale.Company;
using BattleRoyale.Maps;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class TutorialTourRulesTests
    {
        [Test]
        public void TourStaysOffWhenTheSwitchIsOff()
        {
            Assert.IsTrue(TutorialRules.Runs(true, TutorialStep.Deploy));
            Assert.IsFalse(TutorialRules.Runs(false, TutorialStep.Deploy));
            Assert.IsFalse(TutorialRules.Runs(true, TutorialStep.Done));
        }

        [Test]
        public void EachOpenStepNamesTheNextAction()
        {
            Assert.IsTrue(TutorialFocus.TryCopy(TutorialStep.Recruit, out var recruit, out _));
            Assert.IsTrue(TutorialFocus.TryCopy(TutorialStep.Hire, out var hire, out _));
            Assert.IsTrue(TutorialFocus.TryCopy(TutorialStep.Deploy, out var deploy, out _));
            Assert.IsFalse(TutorialFocus.TryCopy(TutorialStep.Done, out _, out _));
            Assert.AreEqual("Recruit", recruit);
            Assert.AreEqual("Hire", hire);
            Assert.AreEqual("Deploy", deploy);
        }

        [Test]
        public void DeployPointsAtNewSimulationThenTheOfferThenDeploy()
        {
            Assert.AreEqual(
                TutorialTarget.NewSimulation,
                TutorialFocus.TargetFor(TutorialStep.Deploy, TutorialPlace.Company));
            Assert.AreEqual(
                TutorialTarget.Offer,
                TutorialFocus.TargetFor(TutorialStep.Deploy, TutorialPlace.Offers));
            Assert.AreEqual(
                TutorialTarget.Deploy,
                TutorialFocus.TargetFor(TutorialStep.Deploy, TutorialPlace.Setup));
        }

        [Test]
        public void TutorialSoloUsesFourSidesWhenTheMapAndSnapshotCanCoverThem()
        {
            var configuration = ScriptableObject.CreateInstance<BattleRoyaleModeConfiguration>();
            configuration.mode = MapGameMode.BattleRoyale;
            configuration.teamCount = 20;
            configuration.teamSize = 1;

            Assert.IsTrue(TutorialMatchRules.TryOffer(configuration, 20, 5, out var teams, out var size));
            Assert.AreEqual(4, teams);
            Assert.AreEqual(1, size);
            Object.DestroyImmediate(configuration);
        }

        [Test]
        public void TutorialSoloRefusesASnapshotThatCannotFillThreeSides()
        {
            var configuration = ScriptableObject.CreateInstance<BattleRoyaleModeConfiguration>();
            configuration.mode = MapGameMode.BattleRoyale;
            configuration.teamCount = 20;
            configuration.teamSize = 1;

            Assert.IsFalse(TutorialMatchRules.TryOffer(configuration, 20, 2, out _, out _));
            Object.DestroyImmediate(configuration);
        }
    }
}
