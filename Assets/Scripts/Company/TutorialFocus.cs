namespace BattleRoyale.Company
{
    public enum TutorialTarget
    {
        None,
        RecruitTab,
        Hire,
        NewSimulation,
        Offer,
        Deploy,
    }

    public enum TutorialPlace
    {
        Company,
        Offers,
        Setup,
        Other,
    }

    public static class TutorialFocus
    {
        public static bool TryCopy(TutorialStep step, out string title, out string body)
        {
            switch (step)
            {
                case TutorialStep.Recruit:
                    title = "Recruit";
                    body = "Open the Recruit tab.";
                    return true;
                case TutorialStep.Hire:
                    title = "Hire";
                    body = "Hire an agent you can afford.";
                    return true;
                case TutorialStep.Deploy:
                    title = "Deploy";
                    body = "Open New Simulation, choose the match, then deploy.";
                    return true;
                default:
                    title = "";
                    body = "";
                    return false;
            }
        }

        public static TutorialTarget TargetFor(TutorialStep step, TutorialPlace place)
        {
            if (step == TutorialStep.Recruit)
            {
                return TutorialTarget.RecruitTab;
            }

            if (step == TutorialStep.Hire)
            {
                return TutorialTarget.Hire;
            }

            if (step != TutorialStep.Deploy)
            {
                return TutorialTarget.None;
            }

            if (place == TutorialPlace.Setup)
            {
                return TutorialTarget.Deploy;
            }

            if (place == TutorialPlace.Offers)
            {
                return TutorialTarget.Offer;
            }

            return TutorialTarget.NewSimulation;
        }
    }
}
