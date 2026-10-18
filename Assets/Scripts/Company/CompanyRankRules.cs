namespace BattleRoyale.Company
{
    public static class CompanyRankRules
    {
        public const int WinPoints = 25;
        public const int LossPoints = 15;

        public static int Apply(int points, bool won)
        {
            var next = points + (won ? WinPoints : -LossPoints);
            return next < 0 ? 0 : next;
        }

        public static string Tier(int points)
        {
            if (points >= 700)
            {
                return "Legend";
            }

            if (points >= 450)
            {
                return "Elite";
            }

            if (points >= 250)
            {
                return "Rival";
            }

            if (points >= 100)
            {
                return "Contender";
            }

            return "Initiate";
        }

        public static string Label(int points)
        {
            var safe = points < 0 ? 0 : points;
            return Tier(safe) + "  ·  " + safe;
        }

        public static RankBand Band(int points)
        {
            var safe = points < 0 ? 0 : points;
            if (safe >= 700)
            {
                return new RankBand("Legend", "", 700, 700, safe);
            }

            if (safe >= 450)
            {
                return new RankBand("Elite", "Legend", 450, 700, safe);
            }

            if (safe >= 250)
            {
                return new RankBand("Rival", "Elite", 250, 450, safe);
            }

            if (safe >= 100)
            {
                return new RankBand("Contender", "Rival", 100, 250, safe);
            }

            return new RankBand("Initiate", "Contender", 0, 100, safe);
        }
    }

    public readonly struct RankBand
    {
        public RankBand(string tier, string nextTier, int floor, int nextFloor, int points)
        {
            Tier = tier ?? "";
            NextTier = nextTier ?? "";
            Floor = floor;
            NextFloor = nextFloor;
            Points = points < 0 ? 0 : points;
        }

        public string Tier { get; }
        public string NextTier { get; }
        public int Floor { get; }
        public int NextFloor { get; }
        public int Points { get; }
        public bool HasNext => NextTier.Length > 0;

        public int Remaining => HasNext ? NextFloor - Points : 0;

        public float Fill
        {
            get
            {
                if (!HasNext)
                {
                    return 1f;
                }

                var span = NextFloor - Floor;
                if (span <= 0)
                {
                    return 1f;
                }

                return (Points - Floor) / (float)span;
            }
        }
    }
}
