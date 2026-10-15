namespace BattleRoyale.Company
{
    public static class AgentRollRules
    {
        public const int RarityCount = 5;
        public const int StatCount = 6;

        public static bool TryRoll(
            int[] weights,
            string[] statNames,
            System.Random random,
            out AgentRoll roll)
        {
            roll = null;

            if (random == null || !TryTotalWeight(weights, out var total))
            {
                return false;
            }

            if (statNames == null || statNames.Length != StatCount)
            {
                return false;
            }

            for (var i = 0; i < statNames.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(statNames[i]))
                {
                    return false;
                }
            }

            var rarity = ChooseRarity(weights, total, random);
            var peakIndex = random.Next(statNames.Length);
            var cap = Cap(rarity);
            var peakMinimum = PeakMinimum(rarity);
            var values = new int[statNames.Length];

            for (var i = 0; i < values.Length; i++)
            {
                var minimum = i == peakIndex ? peakMinimum : 1;
                values[i] = random.Next(minimum, cap + 1);
            }

            var names = new string[statNames.Length];
            for (var i = 0; i < names.Length; i++)
            {
                names[i] = statNames[i];
            }

            roll = new AgentRoll(rarity, names, values, peakIndex);
            return true;
        }

        public static int Cap(AgentRarity rarity)
        {
            return ((int)rarity + 1) * 20;
        }

        public static int PeakMinimum(AgentRarity rarity)
        {
            return (int)rarity * 20 + 1;
        }

        static bool TryTotalWeight(int[] weights, out int total)
        {
            total = 0;

            if (weights == null || weights.Length != RarityCount)
            {
                return false;
            }

            for (var i = 0; i < weights.Length; i++)
            {
                if (weights[i] < 0)
                {
                    return false;
                }

                total += weights[i];
            }

            return total > 0;
        }

        static AgentRarity ChooseRarity(int[] weights, int total, System.Random random)
        {
            var pick = random.Next(total);
            var cursor = 0;

            for (var i = 0; i < weights.Length; i++)
            {
                cursor += weights[i];
                if (pick < cursor)
                {
                    return (AgentRarity)i;
                }
            }

            return (AgentRarity)(weights.Length - 1);
        }
    }
}
