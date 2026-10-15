using System.Collections.Generic;
using BattleRoyale.GameModes;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class StatSheetTests
    {
        [Test]
        public void RowKeepsNameTeamAndAlive()
        {
            var sheet = new BareSheet();
            sheet.Add("Scout", 1, true);

            var row = sheet.RowAt(0);
            Assert.AreEqual(1, sheet.RowCount);
            Assert.AreEqual("Scout", row.DisplayName);
            Assert.AreEqual(1, row.TeamIndex);
            Assert.IsTrue(row.Alive);
        }

        [Test]
        public void BlankDisplayNameIsUnknown()
        {
            var row = new StatSheetRow("   ", 0, false);

            Assert.AreEqual("Unknown", row.DisplayName);
            Assert.AreEqual(0, row.TeamIndex);
            Assert.IsFalse(row.Alive);
        }

        [Test]
        public void LatestEliminationNamesBothSides()
        {
            var sheet = new BareSheet();
            sheet.SetElimination(new StatSheetElimination(
                true,
                true,
                "Gunner",
                0,
                "Scout",
                1));

            var elimination = sheet.LatestElimination;
            Assert.IsTrue(elimination.HasEvent);
            Assert.IsTrue(elimination.HasActor);
            Assert.AreEqual("Gunner", elimination.ActorName);
            Assert.AreEqual(0, elimination.ActorTeam);
            Assert.AreEqual("Scout", elimination.SubjectName);
            Assert.AreEqual(1, elimination.SubjectTeam);
        }

        [Test]
        public void FreshSheetHasNoElimination()
        {
            var sheet = new BareSheet();

            Assert.IsFalse(sheet.LatestElimination.HasEvent);
            Assert.IsFalse(sheet.LatestElimination.HasActor);
            Assert.AreEqual(0, sheet.RowCount);
            Assert.IsNull(sheet.RowAt(0));
        }

        sealed class BareSheet : StatSheet
        {
            readonly List<StatSheetRow> rows = new();

            public void Add(string name, int team, bool alive)
            {
                rows.Add(new StatSheetRow(name, team, alive));
            }

            public void SetElimination(StatSheetElimination elimination)
            {
                SetLatestElimination(elimination);
            }

            public override int RowCount => rows.Count;

            public override StatSheetRow RowAt(int index)
            {
                if (index < 0 || index >= rows.Count)
                {
                    return null;
                }

                return rows[index];
            }
        }
    }
}
