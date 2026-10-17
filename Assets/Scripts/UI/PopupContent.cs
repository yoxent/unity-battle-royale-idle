namespace BattleRoyale.UI
{
    public enum PopupResult
    {
        Confirmed = 0,
        Dismissed = 1,
        Closed = 2,
        TimedOut = 3,
    }

    public sealed class PopupContent
    {
        public string Header = "";
        public string Body = "";
        public bool ShowHeader = true;
        public bool ShowBody = true;
        public bool ShowButtons = true;
        public bool ShowConfirm = true;
        public bool ShowDismiss;
        public string ConfirmLabel = "Confirm";
        public string DismissLabel = "Dismiss";
        public bool ShowClose;
        public bool ShowDoNotShowAgain;
        public string DoNotShowAgainLabel = "Do not show again";
        public bool DoNotShowAgain;
        public float AutoCloseSeconds;

        public bool ShowsButtonRow => ShowButtons && (ShowConfirm || ShowDismiss);

        public bool CanClose => ShowsButtonRow || ShowClose || AutoCloseSeconds > 0f;

        public static PopupContent Create(string header, string body)
        {
            return Create(header, body, null, null, false, null, 0f);
        }

        public static PopupContent Create(string header, string body, string confirmLabel)
        {
            return Create(header, body, confirmLabel, null, false, null, 0f);
        }

        public static PopupContent Create(string header, string body, string confirmLabel, string dismissLabel)
        {
            return Create(header, body, confirmLabel, dismissLabel, false, null, 0f);
        }

        public static PopupContent Create(
            string header,
            string body,
            string confirmLabel,
            string dismissLabel,
            bool showClose,
            string doNotShowAgainLabel,
            float autoCloseSeconds)
        {
            bool showConfirm = !string.IsNullOrEmpty(confirmLabel);
            bool showDismiss = !string.IsNullOrEmpty(dismissLabel);
            return new PopupContent
            {
                Header = header ?? "",
                Body = body ?? "",
                ShowHeader = !string.IsNullOrEmpty(header),
                ShowBody = !string.IsNullOrEmpty(body),
                ShowButtons = showConfirm || showDismiss,
                ShowConfirm = showConfirm,
                ShowDismiss = showDismiss,
                ConfirmLabel = confirmLabel ?? "",
                DismissLabel = dismissLabel ?? "",
                ShowClose = showClose,
                ShowDoNotShowAgain = !string.IsNullOrEmpty(doNotShowAgainLabel),
                DoNotShowAgainLabel = string.IsNullOrEmpty(doNotShowAgainLabel)
                    ? "Do not show again"
                    : doNotShowAgainLabel,
                AutoCloseSeconds = autoCloseSeconds,
            };
        }

        public PopupContent WithClose()
        {
            ShowClose = true;
            return this;
        }

        public PopupContent WithDoNotShowAgain(string label = "Do not show again", bool isOn = false)
        {
            ShowDoNotShowAgain = true;
            DoNotShowAgainLabel = string.IsNullOrEmpty(label) ? "Do not show again" : label;
            DoNotShowAgain = isOn;
            return this;
        }

        public PopupContent WithAutoClose(float seconds)
        {
            AutoCloseSeconds = seconds;
            return this;
        }

        public static PopupContent Hire(int price, int balanceAfter)
        {
            return Create(
                "Hire this agent",
                $"The contract costs {price}. After you pay, the company has {balanceAfter} gold.",
                "Hire",
                "Not now");
        }

        public static PopupContent Refresh(int fee, int nextFee)
        {
            return Create(
                "Refresh the board",
                $"This refresh costs {fee}g. The next one today costs {nextFee}g.",
                "Refresh",
                "Not now");
        }

        public static PopupContent EntryCommit()
        {
            return Create(
                "Entering this tournament",
                "The entrance fee and your agents' upkeep are held when you enter. They are not spent yet.\n\nYou can leave before the match starts and get that gold back. Your agents stay free, and no result is recorded.\n\nOnce the match starts, that gold is spent. Forfeiting is a loss: no prize, and the fee and upkeep are not returned. Closing the game before a result keeps the company as it was when this entry began.",
                "Enter",
                "Not now");
        }
    }
}
