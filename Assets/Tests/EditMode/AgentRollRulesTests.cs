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
        public void IllustrationWeightsMakeLegendaryTheChase()
        {
            var weights = EconomyTuning.Illustration().RarityWeights;
            CollectionAssert.AreEqual(new[] { 700, 200, 70, 25, 5 }, weights);

            var counts = new int[5];
            var random = new System.Random(5);
            for (var n = 0; n < 1000; n++)
            {
                Assert.IsTrue(AgentRollRules.TryRoll(weights, StatNames, random, out var roll));
                counts[(int)roll.Rarity]++;
            }

            Assert.Less(counts[(int)AgentRarity.Legendary], counts[(int)AgentRarity.Epic]);
            Assert.Less(counts[(int)AgentRarity.Epic], counts[(int)AgentRarity.Rare]);
            Assert.Less(counts[(int)AgentRarity.Rare], counts[(int)AgentRarity.Uncommon]);
            Assert.Less(counts[(int)AgentRarity.Uncommon], counts[(int)AgentRarity.Common]);
        }

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
        public void RarityBandsUseTheFloorAndTheCapTwoStepsDown()
        {
            Assert.AreEqual(10, AgentRollRules.StatFloor);
            Assert.AreEqual(25, AgentRollRules.Cap(AgentRarity.Common));
            Assert.AreEqual(10, AgentRollRules.Minimum(AgentRarity.Common));
            Assert.AreEqual(45, AgentRollRules.Cap(AgentRarity.Uncommon));
            Assert.AreEqual(10, AgentRollRules.Minimum(AgentRarity.Uncommon));
            Assert.AreEqual(65, AgentRollRules.Cap(AgentRarity.Rare));
            Assert.AreEqual(26, AgentRollRules.Minimum(AgentRarity.Rare));
            Assert.AreEqual(85, AgentRollRules.Cap(AgentRarity.Epic));
            Assert.AreEqual(46, AgentRollRules.Minimum(AgentRarity.Epic));
            Assert.AreEqual(100, AgentRollRules.Cap(AgentRarity.Legendary));
            Assert.AreEqual(66, AgentRollRules.Minimum(AgentRarity.Legendary));
        }

        [Test]
        public void EveryStatStaysInsideThatRarityBand()
        {
            var random = new System.Random(11);

            for (var rarity = 0; rarity < 5; rarity++)
            {
                var weights = new int[5];
                weights[rarity] = 1;
                var band = (AgentRarity)rarity;
                var cap = AgentRollRules.Cap(band);
                var minimum = AgentRollRules.Minimum(band);

                for (var n = 0; n < 25; n++)
                {
                    Assert.IsTrue(AgentRollRules.TryRoll(
                        weights,
                        StatNames,
                        random,
                        out var roll));
                    Assert.AreEqual(band, roll.Rarity);
                    Assert.GreaterOrEqual(roll.PeakIndex, 0);
                    Assert.Less(roll.PeakIndex, StatNames.Length);
                    Assert.AreEqual(StatNames.Length, roll.Values.Length);

                    for (var stat = 0; stat < roll.Values.Length; stat++)
                    {
                        Assert.GreaterOrEqual(roll.Values[stat], minimum);
                        Assert.LessOrEqual(roll.Values[stat], cap);
                    }
                }
            }
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
