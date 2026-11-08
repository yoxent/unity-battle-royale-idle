namespace BattleRoyale.Company
{
    /// <summary>
    /// What one Mode Select card represents right now. The deployment panel
    /// shows for <see cref="Deploying"/>. The match column shows the fight for
    /// <see cref="Live"/> and <see cref="Finished"/>.
    /// </summary>
    public enum MatchCardState
    {
        None = 0,
        Deploying = 1,
        Live = 2,
        Finished = 3,
    }

    /// <summary>
    /// Which Mode Select cards are past selection, what each is doing, and the
    /// order the list draws them in. The list is the same card prefab either
    /// way; only the order and the divider between the two groups change. Pure
    /// rules: the caller passes the session phase and the card it entered, so
    /// this class does not read a scene, a clock, or a random source.
    /// </summary>
    public static class ActiveMatchListRules
    {
        public static bool IsActive(MatchCardState state)
        {
            return state != MatchCardState.None;
        }

        /// <summary>
        /// The state of one card. Only the entered card can be past selection,
        /// and a second deploy cannot take the slot it already holds.
        /// </summary>
        public static MatchCardState StateOf(
            TournamentEntryPhase phase,
            int entryCard,
            int card)
        {
            if (entryCard < 0 || card != entryCard)
            {
                return MatchCardState.None;
            }

            switch (phase)
            {
                case TournamentEntryPhase.Window:
                    return MatchCardState.Deploying;
                case TournamentEntryPhase.Running:
                    return MatchCardState.Live;
                case TournamentEntryPhase.Resolved:
                    return MatchCardState.Finished;
                default:
                    return MatchCardState.None;
            }
        }

        /// <summary>
        /// Writes every card index into <paramref name="order"/> with the active
        /// cards first, then the rest, each group in card order. Returns the
        /// number of active cards written, which is also where the divider goes.
        /// Returns -1 when the caller's array cannot hold every card.
        /// </summary>
        public static int Arrange(
            TournamentEntryPhase phase,
            int entryCard,
            int cardCount,
            int[] order)
        {
            if (cardCount < 0 || order == null || order.Length < cardCount)
            {
                return -1;
            }

            var written = 0;
            for (var card = 0; card < cardCount; card++)
            {
                if (!IsActive(StateOf(phase, entryCard, card)))
                {
                    continue;
                }

                order[written] = card;
                written++;
            }

            var activeCount = written;
            for (var card = 0; card < cardCount; card++)
            {
                if (IsActive(StateOf(phase, entryCard, card)))
                {
                    continue;
                }

                order[written] = card;
                written++;
            }

            return activeCount;
        }

        /// <summary>
        /// True while the card is the one the right-hand column can present as
        /// a live or finished fight rather than the deployment panel.
        /// </summary>
        public static bool ShowsLiveMatch(MatchCardState state)
        {
            return state == MatchCardState.Live || state == MatchCardState.Finished;
        }

        public static string Label(MatchCardState state, int windowSecond)
        {
            switch (state)
            {
                case MatchCardState.Deploying:
                    return windowSecond > 0
                        ? "Deploying \u00b7 " + windowSecond + "s"
                        : "Deploying";
                case MatchCardState.Live:
                    return "Live";
                case MatchCardState.Finished:
                    return "Result";
                default:
                    return "";
            }
        }
    }
}
