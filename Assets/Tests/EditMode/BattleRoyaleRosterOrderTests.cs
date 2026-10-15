using BattleRoyale.GameModes.BattleRoyale;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class BattleRoyaleRosterOrderTests
    {
        [Test]
        public void LivingAgentsStayAlphabetical()
        {
            var order = new BattleRoyaleRosterOrder();
            order.Present(0, 2, "Cara");
            order.Present(1, 0, "Amy");
            order.Present(2, 1, "Bea");

            Assert.AreEqual(3, order.Count);
            AssertEntry(order, 0, 1, 0, "Amy", true);
            AssertEntry(order, 1, 2, 1, "Bea", true);
            AssertEntry(order, 2, 0, 2, "Cara", true);
        }

        [Test]
        public void NewAgentInsertsAmongTheLiving()
        {
            var order = AmyCaraNed();
            order.RecordDeath(0);
            order.Present(3, 3, "Bea");

            AssertEntry(order, 0, 1, 0, "Amy", true);
            AssertEntry(order, 1, 3, 3, "Bea", true);
            AssertEntry(order, 2, 2, 1, "Ned", true);
            AssertEntry(order, 3, 0, 2, "Cara", false);
        }

        [Test]
        public void NewestDeathSitsUnderTheLivingAndTheFirstDeathEndsAtTheBottom()
        {
            var order = AmyNedZoe();
            order.RecordDeath(2);
            order.RecordDeath(0);

            AssertEntry(order, 0, 1, 0, "Amy", true);
            AssertEntry(order, 1, 0, 2, "Zoe", false);
            AssertEntry(order, 2, 2, 1, "Ned", false);
        }

        [Test]
        public void SameTickDeathsKeepTheOrderTheyAreReported()
        {
            var order = AmyNedZoe();
            order.RecordDeath(2);
            order.RecordDeath(0);

            Assert.AreEqual("Amy", order.EntryAt(0).DisplayName);
            Assert.AreEqual("Zoe", order.EntryAt(1).DisplayName);
            Assert.AreEqual("Ned", order.EntryAt(2).DisplayName);
            Assert.IsFalse(order.EntryAt(1).Alive);
            Assert.IsFalse(order.EntryAt(2).Alive);
        }

        [Test]
        public void DeadAgentsStayPutWhenAnotherAgentIsPresented()
        {
            var order = AmyNedZoe();
            order.RecordDeath(0);
            order.Present(0, 9, "Zoe");
            order.Present(3, 3, "Bea");

            AssertEntry(order, 0, 1, 0, "Amy", true);
            AssertEntry(order, 1, 3, 3, "Bea", true);
            AssertEntry(order, 2, 2, 1, "Ned", true);
            AssertEntry(order, 3, 0, 2, "Zoe", false);
        }

        [Test]
        public void BlankDisplayNameSortsAsUnknown()
        {
            var order = new BattleRoyaleRosterOrder();
            order.Present(0, 0, "Zoe");
            order.Present(1, 4, "  ");

            AssertEntry(order, 0, 1, 4, "Unknown", true);
            AssertEntry(order, 1, 0, 0, "Zoe", true);
        }

        static BattleRoyaleRosterOrder AmyCaraNed()
        {
            var order = new BattleRoyaleRosterOrder();
            order.Present(0, 2, "Cara");
            order.Present(1, 0, "Amy");
            order.Present(2, 1, "Ned");
            return order;
        }

        static BattleRoyaleRosterOrder AmyNedZoe()
        {
            var order = new BattleRoyaleRosterOrder();
            order.Present(1, 0, "Amy");
            order.Present(2, 1, "Ned");
            order.Present(0, 2, "Zoe");
            return order;
        }

        static void AssertEntry(
            BattleRoyaleRosterOrder order,
            int orderIndex,
            int rosterIndex,
            int teamIndex,
            string displayName,
            bool alive)
        {
            var entry = order.EntryAt(orderIndex);
            Assert.AreEqual(rosterIndex, entry.RosterIndex);
            Assert.AreEqual(teamIndex, entry.TeamIndex);
            Assert.AreEqual(displayName, entry.DisplayName);
            Assert.AreEqual(alive, entry.Alive);
        }
    }
}
