namespace BattleRoyale.UI
{
    public enum PopupId
    {
        EntryCommit = 0,
        Preview = 1,
    }

    public readonly struct PopupRequested
    {
        public PopupRequested(PopupId id, PopupContent content)
        {
            Id = id;
            Content = content;
        }

        public PopupId Id { get; }
        public PopupContent Content { get; }
    }

    public readonly struct PopupClosed
    {
        public PopupClosed(PopupId id, PopupResult result, bool doNotShowAgain)
        {
            Id = id;
            Result = result;
            DoNotShowAgain = doNotShowAgain;
        }

        public PopupId Id { get; }
        public PopupResult Result { get; }
        public bool DoNotShowAgain { get; }
    }

    public readonly struct EntryCommitAccepted
    {
    }

    public readonly struct EntryCommitDismissed
    {
    }
}
