using System.Collections.Generic;
using BattleRoyale.GameModes;

namespace BattleRoyale.GameModes.BattleRoyale
{
    public readonly struct BattleRoyaleRosterEntry
    {
        public BattleRoyaleRosterEntry(
            int rosterIndex,
            int teamIndex,
            string displayName,
            bool alive)
        {
            RosterIndex = rosterIndex;
            TeamIndex = teamIndex;
            DisplayName = displayName ?? string.Empty;
            Alive = alive;
        }

        public int RosterIndex { get; }
        public int TeamIndex { get; }
        public string DisplayName { get; }
        public bool Alive { get; }
    }

    public sealed class BattleRoyaleRosterOrder
    {
        readonly List<Slot> order = new();

        public int Count => order.Count;

        public void Clear()
        {
            order.Clear();
        }

        public void Present(int rosterIndex, int teamIndex, string displayName)
        {
            if (rosterIndex < 0 || Find(rosterIndex) >= 0)
            {
                return;
            }

            var name = StatSheetRow.CleanName(displayName);
            var slot = new Slot(rosterIndex, teamIndex, name, true);
            var insertAt = order.Count;
            for (var index = 0; index < order.Count; index++)
            {
                if (!order[index].Alive ||
                    string.CompareOrdinal(name, order[index].DisplayName) < 0)
                {
                    insertAt = index;
                    break;
                }
            }

            order.Insert(insertAt, slot);
        }

        public void RecordDeath(int rosterIndex)
        {
            var index = Find(rosterIndex);
            if (index < 0 || !order[index].Alive)
            {
                return;
            }

            var slot = order[index];
            order.RemoveAt(index);
            slot.Alive = false;
            var aliveCount = 0;
            for (var scan = 0; scan < order.Count; scan++)
            {
                if (!order[scan].Alive)
                {
                    break;
                }

                aliveCount++;
            }

            order.Insert(aliveCount, slot);
        }

        public BattleRoyaleRosterEntry EntryAt(int orderIndex)
        {
            if (orderIndex < 0 || orderIndex >= order.Count)
            {
                return new BattleRoyaleRosterEntry(-1, -1, string.Empty, false);
            }

            var slot = order[orderIndex];
            return new BattleRoyaleRosterEntry(
                slot.RosterIndex,
                slot.TeamIndex,
                slot.DisplayName,
                slot.Alive);
        }

        int Find(int rosterIndex)
        {
            for (var index = 0; index < order.Count; index++)
            {
                if (order[index].RosterIndex == rosterIndex)
                {
                    return index;
                }
            }

            return -1;
        }

        sealed class Slot
        {
            public Slot(int rosterIndex, int teamIndex, string displayName, bool alive)
            {
                RosterIndex = rosterIndex;
                TeamIndex = teamIndex;
                DisplayName = displayName;
                Alive = alive;
            }

            public int RosterIndex { get; }
            public int TeamIndex { get; }
            public string DisplayName { get; }
            public bool Alive { get; set; }
        }
    }
}
