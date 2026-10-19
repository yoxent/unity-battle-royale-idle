namespace BattleRoyale.Company
{
    public enum TutorialStep
    {
        Recruit = 0,
        Hire = 1,
        Deploy = 2,
        Done = 3,
    }

    public enum TutorialAction
    {
        OpenRecruit,
        HireSucceeded,
        Deploy,
    }

    public static class TutorialRules
    {
        public static bool Runs(bool enabled, TutorialStep step)
        {
            return enabled && step != TutorialStep.Done;
        }

        public static TutorialStep Advance(TutorialStep step, TutorialAction action)
        {
            if (action == TutorialAction.Deploy)
            {
                return TutorialStep.Done;
            }

            if (step == TutorialStep.Recruit && action == TutorialAction.OpenRecruit)
            {
                return TutorialStep.Hire;
            }

            if (step == TutorialStep.Hire && action == TutorialAction.HireSucceeded)
            {
                return TutorialStep.Deploy;
            }

            return step;
        }
    }
}
