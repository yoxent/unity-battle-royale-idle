namespace BattleRoyale.Company
{
    public readonly struct AgentPrice
    {
        public AgentPrice(int contract, int upkeep, int waivedAppearances)
        {
            Contract = contract;
            Upkeep = upkeep;
            WaivedAppearances = waivedAppearances;
        }

        public int Contract { get; }
        public int Upkeep { get; }
        public int WaivedAppearances { get; }
    }

    public static class ContractRules
    {
        public static bool TryPrice(
            AgentRarity rarity,
            EconomyTuning tuning,
            out AgentPrice price)
        {
            price = default;

            if (!TrySlot(rarity, tuning, out var index))
            {
                return false;
            }

            var contract = tuning.ContractByRarity[index];
            var upkeep = tuning.UpkeepByRarity[index];
            var waived = tuning.WaivedAppearancesByRarity[index];
            if (contract < 0 || upkeep < 0 || waived < 0)
            {
                return false;
            }

            price = new AgentPrice(contract, upkeep, waived);
            return true;
        }

        public static bool TryChargeUpkeep(
            AgentRarity rarity,
            int waivedRemaining,
            EconomyTuning tuning,
            out int charge,
            out int waivedAfter)
        {
            charge = 0;
            waivedAfter = 0;

            if (waivedRemaining < 0 || !TryPrice(rarity, tuning, out var price))
            {
                return false;
            }

            if (waivedRemaining > 0)
            {
                charge = 0;
                waivedAfter = waivedRemaining - 1;
                return true;
            }

            charge = price.Upkeep;
            waivedAfter = 0;
            return true;
        }

        static bool TrySlot(AgentRarity rarity, EconomyTuning tuning, out int index)
        {
            index = (int)rarity;
            if (tuning == null ||
                tuning.ContractByRarity == null ||
                tuning.UpkeepByRarity == null ||
                tuning.WaivedAppearancesByRarity == null)
            {
                return false;
            }

            var count = tuning.ContractByRarity.Length;
            if (count != AgentRollRules.RarityCount ||
                tuning.UpkeepByRarity.Length != count ||
                tuning.WaivedAppearancesByRarity.Length != count ||
                index < 0 ||
                index >= count)
            {
                return false;
            }

            return true;
        }
    }
}
