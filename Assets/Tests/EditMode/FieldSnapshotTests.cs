using System.Reflection;
using BattleRoyale.Company;
using BattleRoyale.GameModes;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class FieldSnapshotTests
    {
        [Test]
        public void ARoundTripKeepsTheFightRecord()
        {
            var source = Source();

            Assert.IsTrue(FieldSnapshotRules.TryCapture(source, out var snapshot));
            Assert.IsTrue(FieldSnapshotRules.TryCopy(snapshot, out var copy));

            AssertSameFight(source, copy);
            copy.Agents[0].Rolls[0] = 100;
            copy.CompanyName = "Other";
            Assert.AreEqual(41, snapshot.Agents[0].Rolls[0]);
            Assert.AreEqual("Plaguecrown", snapshot.CompanyName);
            Assert.AreEqual(41, source.Agents[0].Rolls[0]);
        }

        [Test]
        public void TreasuryAndUpkeepAreNotOnTheSnapshot()
        {
            var rich = Source();
            rich.Balance = 99;
            rich.Upkeep = 8;
            var poor = Source();
            poor.Balance = 0;
            poor.Upkeep = 0;

            Assert.IsTrue(FieldSnapshotRules.TryCapture(rich, out var fromRich));
            Assert.IsTrue(FieldSnapshotRules.TryCapture(poor, out var fromPoor));
            Assert.IsTrue(FieldSnapshotRules.TryCopy(fromRich, out var copy));

            AssertSameFight(poor, copy);
            AssertSameFight(rich, fromPoor);
            Assert.IsNull(typeof(FieldSnapshot).GetProperty("Balance"));
            Assert.IsNull(typeof(FieldSnapshot).GetField("Balance"));
            Assert.IsNull(typeof(FieldSnapshot).GetProperty("Upkeep"));
            Assert.IsNull(typeof(FieldSnapshot).GetField("Upkeep"));
            Assert.IsNull(typeof(FieldAgent).GetField("Upkeep"));
        }

        [Test]
        public void ABlankNameIsRejected()
        {
            var source = Source();
            source.CompanyName = " ";

            Assert.IsFalse(FieldSnapshotRules.TryCapture(source, out _));
        }

        [Test]
        public void ARollListThatIsNotSixStatsIsRejected()
        {
            var source = Source();
            source.Agents[0].Rolls = new[] { 41, 12, 60 };

            Assert.IsFalse(FieldSnapshotRules.TryCapture(source, out _));
        }

        static void AssertSameFight(FieldSnapshotSource source, FieldSnapshot snapshot)
        {
            Assert.AreEqual(source.CompanyName, snapshot.CompanyName);
            Assert.AreEqual(source.Stance, snapshot.Stance);
            Assert.AreEqual(source.Emblem, snapshot.Emblem);
            Assert.AreEqual(source.Agents.Length, snapshot.Agents.Length);
            for (var i = 0; i < source.Agents.Length; i++)
            {
                Assert.AreEqual(source.Agents[i].ArchetypeId, snapshot.Agents[i].ArchetypeId);
                Assert.AreEqual(source.Agents[i].Rarity, snapshot.Agents[i].Rarity);
                CollectionAssert.AreEqual(source.Agents[i].Rolls, snapshot.Agents[i].Rolls);
            }
        }

        static FieldSnapshotSource Source()
        {
            return new FieldSnapshotSource
            {
                CompanyName = "Plaguecrown",
                Stance = TeamPlaystyle.Defensive,
                Emblem = new FieldEmblem
                {
                    InsigniaShape = 1,
                    Icon = 2,
                    FlagPattern = 3,
                    Primary = new FieldColor { Red = 0.72f, Green = 0.20f, Blue = 0.14f },
                    Secondary = new FieldColor { Red = 0.10f, Green = 0.12f, Blue = 0.16f },
                    Accent = new FieldColor { Red = 0.82f, Green = 0.60f, Blue = 0.16f },
                },
                Agents = new[]
                {
                    new FieldAgent
                    {
                        ArchetypeId = "hunter",
                        Rarity = AgentRarity.Rare,
                        Rolls = new[] { 41, 12, 60, 3, 18, 7 },
                    },
                    new FieldAgent
                    {
                        ArchetypeId = "anchor",
                        Rarity = AgentRarity.Common,
                        Rolls = new[] { 20, 1, 4, 9, 11, 2 },
                    },
                },
                Balance = 20,
                Upkeep = 1,
            };
        }
    }
}
