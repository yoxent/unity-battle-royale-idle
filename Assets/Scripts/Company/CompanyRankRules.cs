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
    }
}
