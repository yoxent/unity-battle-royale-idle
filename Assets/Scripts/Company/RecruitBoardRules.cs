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
        public int RetryDate;
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

        public static int QuoteFee(
            EconomyTuning tuning,
            int retries,
            int retryDate,
            int companyDate)
        {
            if (tuning == null)
            {
                return 0;
            }

            var effective = retryDate == companyDate ? retries : 0;
            if (effective < 0)
            {
                effective = 0;
            }

            return tuning.BaseEarlyRefreshFee + (effective * tuning.EarlyRefreshRaise);
        }

        public static bool TryPayRefresh(
            EconomyTuning tuning,
            int balance,
            float remaining,
            int retries,
            int retryDate,
            int companyDate,
            out int nextBalance,
            out int nextRetries,
            out int nextRetryDate,
            out bool paid)
        {
            nextBalance = balance;
            nextRetries = retries;
            nextRetryDate = retryDate;
            paid = false;
            if (tuning == null || balance < 0 || remaining < 0f)
            {
                return false;
            }

            if (remaining <= 0f)
            {
                return true;
            }

            var fee = QuoteFee(tuning, retries, retryDate, companyDate);
            if (balance < fee)
            {
                return false;
            }

            var effective = retryDate == companyDate ? retries : 0;
            if (effective < 0)
            {
                effective = 0;
            }

            nextBalance = balance - fee;
            nextRetries = effective + 1;
            nextRetryDate = companyDate;
            paid = true;
            return true;
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
