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

            SpawnSnapCandidates.Fill(anchor, preferred, 20f, candidates);

            Assert.AreEqual(1 + SpawnSnapCandidates.Rings * SpawnSnapCandidates.Directions, candidates.Count);
            Assert.AreEqual(anchor, candidates[0]);
            Assert.IsFalse(candidates.Contains(preferred));
            Assert.AreEqual(10f, Vector3.Distance(anchor, candidates[1]), 0.001f);
            Assert.AreEqual(30f, Vector3.Distance(anchor, candidates[candidates.Count - 1]), 0.001f);
            Assert.AreEqual(0f, candidates[1].y, 0.001f);
        }

        [Test]
        public void AMarkerThatIsAlreadyTheSearchPointStartsOnTheFirstRing()
        {
            var anchor = new Vector3(2f, 0f, 2f);
            var candidates = new List<Vector3>();

            SpawnSnapCandidates.Fill(anchor, anchor, 20f, candidates);

            Assert.AreEqual(SpawnSnapCandidates.Rings * SpawnSnapCandidates.Directions, candidates.Count);
            Assert.Greater(Vector3.Distance(anchor, candidates[0]), 1f);
        }
    }

    public sealed class SpawnFitTests
    {
        [Test]
        public void ClearanceIsTheBodyPastTheBakedRadius()
        {
            Assert.AreEqual(0f, SpawnFit.EdgeClearance(0.5f, 0.5f), 0.0001f);
            Assert.AreEqual(4.5f, SpawnFit.EdgeClearance(5f, 0.5f), 0.0001f);
            Assert.Less(SpawnFit.EdgeClearance(5f, 0.5f), 2f + 10f);
        }

        [Test]
        public void AWideFloorSitsOneClearanceOffTheNearEdge()
        {
            var fitted = SpawnFit.FitBetweenEdges(
                Vector3.zero,
                new Vector3(20f, 0f, 0f),
                4.5f,
                0f);

            Assert.AreEqual(4.5f, fitted.x, 0.001f);
            Assert.AreEqual(0f, fitted.z, 0.001f);
        }

        [Test]
        public void ANarrowFloorStopsAtTheMiddleInsteadOfTheFarWall()
        {
            var fitted = SpawnFit.FitBetweenEdges(
                Vector3.zero,
                new Vector3(6f, 0f, 0f),
                4.5f,
                1f);

            Assert.AreEqual(3f, fitted.x, 0.001f);
            Assert.AreEqual(1f, fitted.y, 0.001f);
        }

        [Test]
        public void AnOpenEdgeMovesByTheMissingClearanceOnly()
        {
            var moved = SpawnFit.MoveInside(
                new Vector3(1f, 0f, 0f),
                Vector3.zero,
                1f,
                4.5f);

            Assert.AreEqual(4.5f, moved.x, 0.001f);
            Assert.IsFalse(SpawnFit.LiesBetween(
                new Vector3(1f, 0f, 0f),
                Vector3.zero,
                new Vector3(0f, 0f, 8f)));
            Assert.IsTrue(SpawnFit.LiesBetween(
                new Vector3(1f, 0f, 0f),
                Vector3.zero,
                new Vector3(8f, 0f, 0f)));
        }
    }
}
