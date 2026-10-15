namespace BattleRoyale.Company
{
    public sealed class AgentRoll
    {
        public AgentRoll(
            AgentRarity rarity,
            string[] statNames,
            int[] values,
            int peakIndex)
        {
            Rarity = rarity;
            StatNames = statNames;
            Values = values;
            PeakIndex = peakIndex;
        }

        public AgentRarity Rarity { get; }
        public string[] StatNames { get; }
        public int[] Values { get; }
        public int PeakIndex { get; }
    }
}
