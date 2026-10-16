using BattleRoyale.Company;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class AgentRollRulesTests
    {
        static readonly string[] StatNames =
        {
            "Accuracy",
            "Awareness",
            "Resilience",
            "Athleticism",
            "Endurance",
            "Discipline",
        };

        [Test]
        public void SameSeedReturnsTheSameRoll()
        {
            var weights = new[] { 55, 25, 12, 6, 2 };

            Assert.IsTrue(AgentRollRules.TryRoll(
                weights,
                StatNames,
                new System.Random(17),
                out var first));
            Assert.IsTrue(AgentRollRules.TryRoll(
                weights,
                StatNames,
                new System.Random(17),
                out var second));

            Assert.AreEqual(first.Rarity, second.Rarity);
            Assert.AreEqual(first.PeakIndex, second.PeakIndex);
            CollectionAssert.AreEqual(first.StatNames, second.StatNames);
            CollectionAssert.AreEqual(first.Values, second.Values);
        }

        [Test]
        public void WeightsSelectTheOnlyPositiveRarity()
        {
            var weights = new[] { 0, 0, 1, 0, 0 };

            Assert.IsTrue(AgentRollRules.TryRoll(
                weights,
                StatNames,
                new System.Random(3),
                out var roll));

            Assert.AreEqual(AgentRarity.Rare, roll.Rarity);
        }

        [Test]
        public void PeakStatIsInsideThatRarityBand()
        {
            var random = new System.Random(11);

            for (var rarity = 0; rarity < 5; rarity++)
            {
                var weights = new int[5];
                weights[rarity] = 1;

                for (var n = 0; n < 25; n++)
                {
                    Assert.IsTrue(AgentRollRules.TryRoll(
                        weights,
                        StatNames,
                        random,
                        out var roll));
                    Assert.AreEqual((AgentRarity)rarity, roll.Rarity);

                    var cap = (rarity + 1) * 20;
                    var peakMinimum = rarity * 20 + 1;
                    Assert.GreaterOrEqual(roll.PeakIndex, 0);
                    Assert.Less(roll.PeakIndex, StatNames.Length);
                    Assert.GreaterOrEqual(roll.Values[roll.PeakIndex], peakMinimum);
                    Assert.LessOrEqual(roll.Values[roll.PeakIndex], cap);
                }
            }
        }

        [Test]
        public void OtherStatsStayBetweenOneAndTheCap()
        {
            var random = new System.Random(11);

            for (var rarity = 0; rarity < 5; rarity++)
            {
                var weights = new int[5];
                weights[rarity] = 1;
                var cap = (rarity + 1) * 20;

                for (var n = 0; n < 25; n++)
                {
                    Assert.IsTrue(AgentRollRules.TryRoll(
                        weights,
                        StatNames,
                        random,
                        out var roll));

                    Assert.AreEqual(StatNames.Length, roll.Values.Length);
                    for (var stat = 0; stat < roll.Values.Length; stat++)
                    {
                        Assert.GreaterOrEqual(roll.Values[stat], 1);
                        Assert.LessOrEqual(roll.Values[stat], cap);
                    }
                }
            }
        }

        [Test]
        public void ALegendaryStatOutsideThePeakCanRollBelowThePeakBand()
        {
            var weights = new[] { 0, 0, 0, 0, 1 };
            var random = new System.Random(4);
            var sawLowStat = false;

            for (var n = 0; n < 30 && !sawLowStat; n++)
            {
                Assert.IsTrue(AgentRollRules.TryRoll(
                    weights,
                    StatNames,
                    random,
                    out var roll));

                for (var stat = 0; stat < roll.Values.Length; stat++)
                {
                    if (stat == roll.PeakIndex)
                    {
                        continue;
                    }

                    if (roll.Values[stat] < 81)
                    {
                        sawLowStat = true;
                    }
                }
            }

            Assert.IsTrue(sawLowStat);
        }

        [Test]
        public void MissingRandomIsRejected()
        {
            Assert.IsFalse(AgentRollRules.TryRoll(
                new[] { 1, 0, 0, 0, 0 },
                StatNames,
                null,
                out _));
        }

        [Test]
        public void WeightCountMustMatchTheRarities()
        {
            var weights = new int[System.Enum.GetValues(typeof(AgentRarity)).Length + 1];

            Assert.IsFalse(AgentRollRules.TryRoll(
                weights,
                StatNames,
                new System.Random(1),
                out _));
        }

        [Test]
        public void AllZeroWeightsAreRejected()
        {
            Assert.IsFalse(AgentRollRules.TryRoll(
                new[] { 0, 0, 0, 0, 0 },
                StatNames,
                new System.Random(1),
                out _));
        }

        [Test]
        public void ANegativeWeightIsRejected()
        {
            Assert.IsFalse(AgentRollRules.TryRoll(
                new[] { 1, -1, 0, 0, 0 },
                StatNames,
                new System.Random(1),
                out _));
        }

        [Test]
        public void StatNameCountMustBeSix()
        {
            Assert.IsFalse(AgentRollRules.TryRoll(
                new[] { 1, 0, 0, 0, 0 },
                new[] { "Accuracy" },
                new System.Random(1),
                out _));
        }

        [Test]
        public void ABlankStatNameIsRejected()
        {
            var names = (string[])StatNames.Clone();
            names[2] = " ";

            Assert.IsFalse(AgentRollRules.TryRoll(
                new[] { 1, 0, 0, 0, 0 },
                names,
                new System.Random(1),
                out _));
        }
    }
}
