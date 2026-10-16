using BattleRoyale.GameModes;
using BattleRoyale.GameModes.BattleRoyale;
using BattleRoyale.GameModes.Deathmatch;
using BattleRoyale.GameModes.Elimination;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class MatchStatBoardTests
    {
        [Test]
        public void DeathmatchBoardShowsScoresClockAndRespawn()
        {
            var stats = Agents();
            stats.SetTeamName(0, "Home");
            stats.SetTeamName(1, "Away");
            var board = new MatchStatBoard();

            DeathmatchStatTracker.Write(
                board,
                stats,
                true,
                new[] { 3, 1 },
                252f,
                new[] { true, false },
                new[] { 0, 4 });

            Assert.IsTrue(board.Visible);
            Assert.IsTrue(board.Teams);
            Assert.AreEqual("Home", board.LeftName);
            Assert.AreEqual(3, board.LeftScore);
            Assert.AreEqual("Away", board.RightName);
            Assert.AreEqual(1, board.RightScore);
            Assert.AreEqual("TIME", board.CenterCaption);
            Assert.AreEqual("04:12", board.CenterValue);
            Assert.AreEqual(2, board.Rows.Count);
            Assert.AreEqual("Ash", board.Rows[0].Name);
            Assert.IsTrue(board.Rows[0].Alive);
            Assert.AreEqual("Briar", board.Rows[1].Name);
            Assert.IsFalse(board.Rows[1].Alive);
            Assert.AreEqual(4, board.Rows[1].RespawnSeconds);
            Assert.IsTrue(stats.AgentAt(1).Alive == false);
        }

        [Test]
        public void DeathmatchBoardStaysHiddenBeforeTheMatchStarts()
        {
            var board = new MatchStatBoard();

            DeathmatchStatTracker.Write(
                board,
                Agents(),
                false,
                new[] { 0, 0 },
                0f,
                new[] { true, true },
                new[] { 0, 0 });

            Assert.IsFalse(board.Visible);
            Assert.IsFalse(board.RejectedTeamCount);
        }

        [Test]
        public void DeathmatchBoardRejectsATeamCountOtherThanTwo()
        {
            var board = new MatchStatBoard();

            DeathmatchStatTracker.Write(
                board,
                Agents(),
                true,
                new[] { 1 },
                0f,
                new[] { true, true },
                new[] { 0, 0 });

            Assert.IsFalse(board.Visible);
            Assert.IsTrue(board.RejectedTeamCount);
        }

        [Test]
        public void DeathmatchBoardKeepsTheLatestElimination()
        {
            var stats = Agents();
            stats.RecordElimination(1, 0, 3f);
            var board = new MatchStatBoard();

            DeathmatchStatTracker.Write(
                board,
                stats,
                true,
                new[] { 1, 0 },
                3f,
                new[] { true, false },
                new[] { 0, 0 });

            Assert.IsTrue(board.ShowEvent);
            Assert.IsTrue(board.EventHasActor);
            Assert.AreEqual("Ash", board.ActorName);
            Assert.AreEqual(0, board.ActorTeam);
            Assert.AreEqual("Briar", board.SubjectName);
            Assert.AreEqual(1, board.SubjectTeam);
        }

        [Test]
        public void EliminationBoardPrefixesIntermission()
        {
            var stats = Agents();
            var board = new MatchStatBoard();

            EliminationStatTracker.Write(
                board,
                stats,
                true,
                new[] { 1, 0 },
                12f,
                true,
                new[] { true, true });

            Assert.IsTrue(board.Visible);
            Assert.AreEqual(1, board.LeftScore);
            Assert.AreEqual(0, board.RightScore);
            Assert.AreEqual("INT 00:12", board.CenterValue);
            Assert.AreEqual(0, board.Rows[0].RespawnSeconds);
        }

        [Test]
        public void BattleRoyaleBoardCopiesTheRosterOrder()
        {
            var order = new BattleRoyaleRosterOrder();
            order.Present(0, 0, "Zed");
            order.Present(1, 1, "Amy");
            var board = new MatchStatBoard();

            BattleRoyaleStatTracker.Write(board, true, 2, order);

            Assert.IsTrue(board.Visible);
            Assert.IsFalse(board.Teams);
            Assert.AreEqual(string.Empty, board.CenterCaption);
            Assert.AreEqual("2 alive", board.CenterValue);
            Assert.AreEqual(2, board.Rows.Count);
            Assert.AreEqual("Amy", board.Rows[0].Name);
            Assert.IsFalse(board.Rows[0].Combat);
            Assert.AreEqual("Zed", board.Rows[1].Name);
        }

        [Test]
        public void BattleRoyaleBoardStaysHiddenBeforeTheMatchStarts()
        {
            var board = new MatchStatBoard();

            BattleRoyaleStatTracker.Write(board, false, 4, new BattleRoyaleRosterOrder());

            Assert.IsFalse(board.Visible);
            Assert.AreEqual(0, board.Rows.Count);
        }

        static DeathmatchCombatStats Agents()
        {
            var stats = new DeathmatchCombatStats();
            stats.RegisterAgent(0, 0, "Ash");
            stats.RegisterAgent(1, 1, "Briar");
            return stats;
        }
    }
}
