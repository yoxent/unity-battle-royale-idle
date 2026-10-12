namespace BattleRoyale.CompanyIdentity
{
    public static class CompanyIdentityRandomizer
    {
        public static bool TryCreate(
            string rawName,
            System.Random random,
            out CompanyIdentityData identity)
        {
            identity = null;

            if (random == null)
                return false;

            if (!CompanyIdentityRules.TryNormalizeCompanyName(rawName, out var normalizedName))
                return false;

            // Select one from each supported set
            var insigniaShape = CompanyIdentityRules.InsigniaShapes[
                random.Next(CompanyIdentityRules.InsigniaShapes.Length)];
            var icon = CompanyIdentityRules.Icons[
                random.Next(CompanyIdentityRules.Icons.Length)];
            var flagPattern = CompanyIdentityRules.FlagPatterns[
                random.Next(CompanyIdentityRules.FlagPatterns.Length)];

            // Select three distinct colors from palette
            var palette = CompanyIdentityRules.Palette;
            var indices = SelectDistinctIndices(random, palette.Length, 3);

            identity = new CompanyIdentityData
            {
                companyName = normalizedName,
                insigniaShape = insigniaShape,
                icon = icon,
                flagPattern = flagPattern,
                primaryColor = palette[indices[0]],
                secondaryColor = palette[indices[1]],
                accentColor = palette[indices[2]]
            };

            return true;
        }

        public static CompanyIdentityData Create(string rawName)
        {
            if (!CompanyIdentityRules.TryNormalizeCompanyName(rawName, out _))
            {
                var message = CompanyIdentityRules.GetNameValidationMessage(rawName);
                throw new System.ArgumentException(message ?? "Invalid company name.", nameof(rawName));
            }

            if (!TryCreate(rawName, new System.Random(), out var identity))
            {
                var message = CompanyIdentityRules.GetNameValidationMessage(rawName);
                throw new System.ArgumentException(message ?? "Failed to create identity.", nameof(rawName));
            }

            return identity;
        }

        private static int[] SelectDistinctIndices(System.Random random, int maxValue, int count)
        {
            if (count > maxValue)
                throw new System.ArgumentException(
                    $"Cannot select {count} distinct indices from range 0-{maxValue - 1}");

            var result = new int[count];
            var available = new System.Collections.Generic.List<int>();

            for (int i = 0; i < maxValue; i++)
                available.Add(i);

            for (int i = 0; i < count; i++)
            {
                var selectedIndex = random.Next(available.Count);
                result[i] = available[selectedIndex];
                available.RemoveAt(selectedIndex);
            }

            return result;
        }
    }
}
