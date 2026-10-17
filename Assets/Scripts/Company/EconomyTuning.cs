namespace BattleRoyale.Company
{
    public sealed class EconomyTuning
    {
        public int[] UpkeepByRarity;
        public int[] ContractByRarity;
        public int[] WaivedAppearancesByRarity;
        public int CheapEntranceFee;
        public int CheapFirstPrize;
        public int CheapLastPrize;
        public int Salary;
        public int FanPayCap;
        public int StartingGold;
        public int[] RarityWeights;
        public int BaseEarlyRefreshFee;
        public int EarlyRefreshRaise;
        public int FreeRefreshesPerDay = 3;
        public float RecruitRefreshSeconds = 14400f;
        public float AbortWindowSeconds = 15f;
        public float SessionDaySeconds = 600f;
        public float OfflineEarningRate = 0.5f;

        public static EconomyTuning Illustration()
        {
            return new EconomyTuning
            {
                UpkeepByRarity = new[] { 1, 2, 4, 6, 8 },
                ContractByRarity = new[] { 6, 12, 24, 36, 48 },
                WaivedAppearancesByRarity = new[] { 1, 1, 0, 0, 0 },
                CheapEntranceFee = 5,
                CheapFirstPrize = 24,
                CheapLastPrize = 0,
                Salary = 5,
                FanPayCap = 5,
                StartingGold = 100,
                RarityWeights = new[] { 700, 200, 70, 25, 5 },
                BaseEarlyRefreshFee = 3,
                EarlyRefreshRaise = 3,
                FreeRefreshesPerDay = 3,
                RecruitRefreshSeconds = 14400f,
                AbortWindowSeconds = 15f,
                SessionDaySeconds = 600f,
                OfflineEarningRate = 0.5f,
            };
        }
    }
}
