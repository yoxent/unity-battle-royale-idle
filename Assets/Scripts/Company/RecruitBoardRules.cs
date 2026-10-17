using System;

namespace BattleRoyale.Company
{
    public sealed class RecruitCard
    {
        public bool Filled;
        public string ArchetypeId;
        public AgentRarity Rarity;
        public int[] Rolls;
        public int Contract;
        public int Upkeep;
        public int Waived;
        public BattleRoyale.Agents.AgentAvatarAppearance Avatar;
        public string DisplayName;
        public string Specialty;
        public string Weapon;
    }

    public sealed class RecruitBoard
    {
        public RecruitCard[] Slots;
        public float Remaining;
        public int PaidRetries;
        public int FreeUsed;
        public int DayIndex;
        public long AcceptedUnix;
        public bool Rolled;
    }

    public static class RecruitBoardRules
    {
        public const int BoardSize = 5;
        public const int RosterCap = 8;

        public static readonly string[] StatNames =
        {
            "accuracy",
            "awareness",
            "resilience",
            "athleticism",
            "endurance",
            "discipline",
        };

        public static bool TryRoll(
            string[] archetypeIds,
            EconomyTuning tuning,
            Random random,
            bool forceAffordable,
            int affordableGold,
            out RecruitCard[] cards)
        {
            cards = null;
            if (!CanRoll(archetypeIds, tuning, random))
            {
                return false;
            }

            cards = new RecruitCard[BoardSize];
            for (var i = 0; i < BoardSize; i++)
            {
                if (!TryCard(archetypeIds, tuning.RarityWeights, tuning, random, out cards[i]))
                {
                    cards = null;
                    return false;
                }
            }

            if (!forceAffordable || HasAffordable(cards, affordableGold))
            {
                return true;
            }

            var common = new[] { 1, 0, 0, 0, 0 };
            return TryCard(archetypeIds, common, tuning, random, out cards[0]);
        }

        public static bool TryHire(
            RecruitBoard board,
            int slot,
            int balance,
            int rosterCount,
            out int nextBalance,
            out RecruitCard hired)
        {
            nextBalance = balance;
            hired = null;
            if (board == null || board.Slots == null || rosterCount >= RosterCap || rosterCount < 0)
            {
                return false;
            }

            if (slot < 0 || slot >= board.Slots.Length)
            {
                return false;
            }

            var card = board.Slots[slot];
            if (card == null || !card.Filled || balance < card.Contract)
            {
                return false;
            }

            card.Filled = false;
            nextBalance = balance - card.Contract;
            hired = card;
            return true;
        }

        public static bool TryRelease(CompanySaveAgent[] roster, int index, bool busy, out CompanySaveAgent[] next)
        {
            next = null;
            if (busy || roster == null || index < 0 || index >= roster.Length)
            {
                return false;
            }

            next = new CompanySaveAgent[roster.Length - 1];
            var write = 0;
            for (var i = 0; i < roster.Length; i++)
            {
                if (i == index)
                {
                    continue;
                }

                next[write] = roster[i];
                write++;
            }

            return true;
        }

        public const int UnixDaySeconds = 86400;

        public static int QuoteFee(EconomyTuning tuning, int freeUsed, int paidRetries)
        {
            if (tuning == null || freeUsed < 0 || paidRetries < 0)
            {
                return 0;
            }

            if (freeUsed < tuning.FreeRefreshesPerDay)
            {
                return 0;
            }

            return tuning.BaseEarlyRefreshFee + (paidRetries * tuning.EarlyRefreshRaise);
        }

        public static string FormatRefreshCost(int fee)
        {
            if (fee <= 0)
            {
                return "Refresh · Free";
            }

            return $"Refresh · {fee}g";
        }

        public static bool TryPayRefresh(
            EconomyTuning tuning,
            int balance,
            int freeUsed,
            int paidRetries,
            out int nextBalance,
            out int nextFreeUsed,
            out int nextPaidRetries,
            out bool paid)
        {
            nextBalance = balance;
            nextFreeUsed = freeUsed;
            nextPaidRetries = paidRetries;
            paid = false;
            if (tuning == null || balance < 0 || freeUsed < 0 || paidRetries < 0)
            {
                return false;
            }

            if (freeUsed < tuning.FreeRefreshesPerDay)
            {
                nextFreeUsed = freeUsed + 1;
                return true;
            }

            var fee = QuoteFee(tuning, freeUsed, paidRetries);
            if (balance < fee)
            {
                return false;
            }

            nextBalance = balance - fee;
            nextPaidRetries = paidRetries + 1;
            paid = true;
            return true;
        }

        public static bool TryAcceptWorldTime(
            int freeUsed,
            int paidRetries,
            int dayIndex,
            long acceptedUnix,
            long worldUnix,
            out int nextFreeUsed,
            out int nextPaidRetries,
            out int nextDayIndex,
            out long nextAcceptedUnix,
            out bool dayReset)
        {
            nextFreeUsed = freeUsed;
            nextPaidRetries = paidRetries;
            nextDayIndex = dayIndex;
            nextAcceptedUnix = acceptedUnix;
            dayReset = false;
            if (worldUnix <= 0 || (acceptedUnix > 0 && worldUnix < acceptedUnix))
            {
                return false;
            }

            var day = (int)(worldUnix / UnixDaySeconds);
            nextAcceptedUnix = worldUnix;
            if (acceptedUnix <= 0)
            {
                nextDayIndex = day;
                return true;
            }

            if (day <= dayIndex)
            {
                return true;
            }

            nextFreeUsed = 0;
            nextPaidRetries = 0;
            nextDayIndex = day;
            dayReset = true;
            return true;
        }

        public static float OfflineSeconds(long acceptedUnix, long worldUnix, float tickedSeconds)
        {
            if (acceptedUnix <= 0 || worldUnix < acceptedUnix || tickedSeconds < 0f)
            {
                return 0f;
            }

            var gap = (float)(worldUnix - acceptedUnix);
            var offline = gap - tickedSeconds;
            return offline < 0f ? 0f : offline;
        }

        public static bool TryCatchUp(
            float remaining,
            float elapsed,
            float period,
            out float nextRemaining,
            out bool refreshDue)
        {
            nextRemaining = remaining;
            refreshDue = false;
            if (elapsed < 0f || period <= 0f)
            {
                return false;
            }

            if (remaining <= 0f)
            {
                if (elapsed <= 0f)
                {
                    return true;
                }

                refreshDue = true;
                nextRemaining = WindowRemainder(elapsed, period);
                return true;
            }

            var next = remaining - elapsed;
            if (next > 0f)
            {
                nextRemaining = next;
                return true;
            }

            refreshDue = true;
            nextRemaining = WindowRemainder(elapsed - remaining, period);
            return true;
        }

        static float WindowRemainder(float overshoot, float period)
        {
            var into = overshoot % period;
            if (into <= 0f)
            {
                return period;
            }

            return period - into;
        }

        public static float AdvanceClock(float remaining, float elapsed)
        {
            if (elapsed < 0f)
            {
                return remaining;
            }

            var next = remaining - elapsed;
            return next < 0f ? 0f : next;
        }

        static bool CanRoll(string[] archetypeIds, EconomyTuning tuning, Random random)
        {
            if (random == null || tuning == null || archetypeIds == null || archetypeIds.Length == 0)
            {
                return false;
            }

            if (tuning.RarityWeights == null || tuning.RarityWeights.Length != AgentRollRules.RarityCount)
            {
                return false;
            }

            for (var i = 0; i < archetypeIds.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(archetypeIds[i]))
                {
                    return false;
                }
            }

            return true;
        }

        static bool TryCard(
            string[] archetypeIds,
            int[] weights,
            EconomyTuning tuning,
            Random random,
            out RecruitCard card)
        {
            card = null;
            if (!AgentRollRules.TryRoll(weights, StatNames, random, out var roll) ||
                !ContractRules.TryPrice(roll.Rarity, tuning, out var price))
            {
                return false;
            }

            var values = new int[roll.Values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = roll.Values[i];
            }

            card = new RecruitCard
            {
                Filled = true,
                ArchetypeId = archetypeIds[random.Next(archetypeIds.Length)],
                Rarity = roll.Rarity,
                Rolls = values,
                Contract = price.Contract,
                Upkeep = price.Upkeep,
                Waived = price.WaivedAppearances,
            };
            return true;
        }

        static bool HasAffordable(RecruitCard[] cards, int gold)
        {
            for (var i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null && cards[i].Filled && cards[i].Contract <= gold)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
