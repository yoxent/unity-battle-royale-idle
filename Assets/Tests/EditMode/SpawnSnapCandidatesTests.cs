using System.Collections.Generic;
using BattleRoyale.GameModes;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class SpawnSnapCandidatesTests
    {
        [Test]
        public void AMissedScatterTriesTheMarkerThenRingsAroundIt()
        {
            var anchor = new Vector3(10f, 0f, 4f);
            var preferred = anchor + new Vector3(3f, 0f, 0f);
            var candidates = new List<Vector3>();

            SpawnSnapCandidates.Fill(anchor, preferred, 1.5f, candidates);

            Assert.AreEqual(1 + SpawnPlacement.Rings * SpawnSnapCandidates.Directions, candidates.Count);
            Assert.AreEqual(anchor, candidates[0]);
            Assert.IsFalse(candidates.Contains(preferred));
            Assert.AreEqual(1.5f, Vector3.Distance(anchor, candidates[1]), 0.001f);
            Assert.AreEqual(4.5f, Vector3.Distance(anchor, candidates[candidates.Count - 1]), 0.001f);
            Assert.AreEqual(0f, candidates[1].y, 0.001f);
        }

        [Test]
        public void AMarkerThatIsAlreadyTheSearchPointStartsOnTheFirstRing()
        {
            var anchor = new Vector3(2f, 0f, 2f);
            var candidates = new List<Vector3>();

            SpawnSnapCandidates.Fill(anchor, anchor, 1.5f, candidates);

            Assert.AreEqual(SpawnPlacement.Rings * SpawnSnapCandidates.Directions, candidates.Count);
            Assert.AreEqual(1.5f, Vector3.Distance(anchor, candidates[0]), 0.001f);
        }
    }

    public sealed class SpawnPlacementTests
    {
        [Test]
        public void ClearanceFollowsUnitScaleAndStopsAtTheBakedRadius()
        {
            Assert.AreEqual(0f, SpawnPlacement.Clearance(1f, 1f), 0.0001f);
            Assert.AreEqual(2.5f, SpawnPlacement.Clearance(6f, 1f), 0.0001f);
            Assert.AreEqual(4.5f, SpawnPlacement.Clearance(10f, 1f), 0.0001f);
            Assert.Less(SpawnPlacement.Clearance(10f, 1f), 2f + 10f);
        }

        [Test]
        public void ALargerScaleTakesALongerStepButTheSearchStaysInsideOneClearance()
        {
            var clearance = SpawnPlacement.Clearance(10f, 1f);
            var step = SpawnPlacement.Step(clearance);

            Assert.AreEqual(1.5f, step, 0.0001f);
            Assert.AreEqual(clearance, step * SpawnPlacement.Rings, 0.0001f);
            Assert.AreEqual(0.5f, SpawnPlacement.Step(0f), 0.0001f);
        }

        [Test]
        public void ANarrowFloorStopsAtTheMiddle()
        {
            var fitted = SpawnPlacement.FitBetweenEdges(
                Vector3.zero,
                new Vector3(6f, 0f, 0f),
                4.5f,
                1f);

            Assert.AreEqual(3f, fitted.x, 0.001f);
            Assert.AreEqual(1f, fitted.y, 0.001f);
        }
    }
}
