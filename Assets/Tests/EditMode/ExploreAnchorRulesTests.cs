using System.Collections.Generic;
using BattleRoyale.GameModes;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class ExploreAnchorRulesTests
    {
        private static readonly Vector3 ZoneCenter = new Vector3(10f, 0f, 10f);

        [Test]
        public void TrySelect_EmptyOrMissingList_SelectsNothing()
        {
            Assert.IsFalse(ExploreAnchorRules.TrySelect(
                null,
                false,
                ZoneCenter,
                20f,
                out _));
            Assert.IsFalse(ExploreAnchorRules.TrySelect(
                new List<ExploreAnchorRules.Anchor>(),
                true,
                ZoneCenter,
                20f,
                out _));
        }

        [Test]
        public void TrySelect_HigherWeightWins_AndIgnoresDistance()
        {
            var anchors = new[]
            {
                new ExploreAnchorRules.Anchor("near", ZoneCenter, 1f),
                new ExploreAnchorRules.Anchor("far", ZoneCenter + new Vector3(18f, 0f, 0f), 4f)
            };

            var selected = Select(anchors, restrictToZone: true, zoneRadius: 20f);

            Assert.AreEqual("far", selected.Id);
        }

        [Test]
        public void TrySelect_EqualWeight_KeepsTheEarlierAnchor()
        {
            var anchors = new[]
            {
                new ExploreAnchorRules.Anchor("first", ZoneCenter, 2f),
                new ExploreAnchorRules.Anchor("second", ZoneCenter + Vector3.forward, 2f)
            };

            var selected = Select(anchors, restrictToZone: false, zoneRadius: 0f);

            Assert.AreEqual("first", selected.Id);
        }

        [Test]
        public void TrySelect_NonPositiveWeight_DoesNotQualify()
        {
            var anchors = new[]
            {
                new ExploreAnchorRules.Anchor("zero", ZoneCenter, 0f),
                new ExploreAnchorRules.Anchor("negative", ZoneCenter, -3f)
            };

            Assert.IsFalse(ExploreAnchorRules.TrySelect(
                anchors,
                false,
                ZoneCenter,
                20f,
                out _));
        }

        [Test]
        public void TrySelect_BattleRoyale_RejectsAnchorsOutsideTheZone()
        {
            var anchors = new[]
            {
                new ExploreAnchorRules.Anchor("outside", ZoneCenter + new Vector3(21f, 0f, 0f), 9f),
                new ExploreAnchorRules.Anchor("inside", ZoneCenter + new Vector3(5f, 0f, 0f), 1f)
            };

            var selected = Select(anchors, restrictToZone: true, zoneRadius: 20f);

            Assert.AreEqual("inside", selected.Id);
        }

        [Test]
        public void TrySelect_ZoneEdgeAndHeight_StayInside()
        {
            var anchors = new[]
            {
                new ExploreAnchorRules.Anchor(
                    "edge",
                    ZoneCenter + new Vector3(20f, 40f, 0f),
                    1f)
            };

            var selected = Select(anchors, restrictToZone: true, zoneRadius: 20f);

            Assert.AreEqual("edge", selected.Id);
        }

        [Test]
        public void TrySelect_DeathmatchAndElimination_ConsiderEveryAnchor()
        {
            var anchors = new[]
            {
                new ExploreAnchorRules.Anchor("outside", ZoneCenter + new Vector3(80f, 0f, 0f), 3f),
                new ExploreAnchorRules.Anchor("inside", ZoneCenter, 1f)
            };

            var selected = Select(anchors, restrictToZone: false, zoneRadius: 20f);

            Assert.AreEqual("outside", selected.Id);
        }

        [Test]
        public void TrySelectFlank_PicksThePointOffTheApproachLine()
        {
            var from = Vector3.zero;
            var hot = new ExploreAnchorRules.Anchor("Hot", new Vector3(0f, 0f, 10f), 5f);
            var anchors = new[]
            {
                hot,
                new ExploreAnchorRules.Anchor("Line", new Vector3(0f, 4f, 5f), 4f),
                new ExploreAnchorRules.Anchor("Behind", new Vector3(0f, 0f, -6f), 9f),
                new ExploreAnchorRules.Anchor("Side", new Vector3(8f, 0f, 6f), 1f)
            };

            Assert.IsTrue(
                ExploreAnchorRules.TrySelectFlank(
                    anchors,
                    from,
                    hot.Id,
                    hot.Position,
                    false,
                    Vector3.zero,
                    0f,
                    out var selected));
            Assert.AreEqual("Side", selected.Id);
        }

        [Test]
        public void TryChoose_ScoutTakesTheSideAndOthersTakeTheHotAnchor()
        {
            var anchors = ApproachAnchors();

            Assert.IsTrue(
                ExploreAnchorRules.TryChoose(
                    anchors,
                    Vector3.zero,
                    true,
                    0,
                    false,
                    Vector3.zero,
                    0f,
                    out var scout,
                    out var scoutSlots));
            Assert.AreEqual("Side", scout.Id);
            Assert.AreEqual(1, scoutSlots);

            Assert.IsTrue(
                ExploreAnchorRules.TryChoose(
                    anchors,
                    Vector3.zero,
                    false,
                    0,
                    false,
                    Vector3.zero,
                    0f,
                    out var assault,
                    out var assaultSlots));
            Assert.AreEqual("Hot", assault.Id);
            Assert.AreEqual(2, assaultSlots);

            Assert.IsFalse(
                ExploreAnchorRules.TryChoose(
                    anchors,
                    Vector3.zero,
                    false,
                    2,
                    false,
                    Vector3.zero,
                    0f,
                    out _,
                    out _));
        }

        [Test]
        public void TryChoose_FlankOutsideTheZone_LeavesTheScoutOnTheHotAnchor()
        {
            var anchors = new[]
            {
                new ExploreAnchorRules.Anchor("Hot", new Vector3(0f, 0f, 3f), 5f),
                new ExploreAnchorRules.Anchor("Side", new Vector3(20f, 0f, 3f), 1f)
            };

            Assert.IsTrue(
                ExploreAnchorRules.TryChoose(
                    anchors,
                    Vector3.zero,
                    true,
                    0,
                    true,
                    Vector3.zero,
                    5f,
                    out var selected,
                    out var slots));
            Assert.AreEqual("Hot", selected.Id);
            Assert.AreEqual(2, slots);
        }

        private static ExploreAnchorRules.Anchor[] ApproachAnchors()
        {
            return new[]
            {
                new ExploreAnchorRules.Anchor("Hot", new Vector3(0f, 0f, 10f), 5f),
                new ExploreAnchorRules.Anchor("Side", new Vector3(8f, 0f, 6f), 1f)
            };
        }

        private static ExploreAnchorRules.Anchor Select(
            IReadOnlyList<ExploreAnchorRules.Anchor> anchors,
            bool restrictToZone,
            float zoneRadius)
        {
            var found = ExploreAnchorRules.TrySelect(
                anchors,
                restrictToZone,
                ZoneCenter,
                zoneRadius,
                out var selected);
            Assert.IsTrue(found);
            return selected;
        }
    }
}
