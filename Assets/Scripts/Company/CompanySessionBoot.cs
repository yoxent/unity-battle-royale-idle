namespace BattleRoyale.Company
{
    public enum CompanyBoot
    {
        Saved = 0,
        Authored = 1,
        Reject = 2,
    }

    public static class CompanySessionBoot
    {
        public static CompanyBoot Choose(bool saveAssigned, bool fileExists, bool fileParses)
        {
            if (!saveAssigned || !fileExists)
            {
                return CompanyBoot.Authored;
            }

            return fileParses ? CompanyBoot.Saved : CompanyBoot.Reject;
        }
    }
}
