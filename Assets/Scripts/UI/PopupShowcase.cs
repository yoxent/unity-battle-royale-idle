using BattleRoyale.Events;
using TMPro;
using UnityEngine;

namespace BattleRoyale.UI
{
    /// <summary>
    /// Opens the popup combinations from the test scene.
    /// </summary>
    public sealed class PopupShowcase : MonoBehaviour
    {
        const string Heading = "Lorem ipsum dolor sit amet";
        const string Body =
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.";
        const string LongBody =
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.\n\nDuis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
        const string Checkbox = "Lorem ipsum dolor sit amet";

        [SerializeField] TMP_Text resultLabel;

        bool subscribed;

        void OnEnable()
        {
            if (resultLabel == null)
            {
                Debug.LogError("PopupShowcase: assign the result label.", this);
                return;
            }

            EventBus.Subscribe<PopupClosed>(ShowResult);
            subscribed = true;
            resultLabel.text = "Last result: none";
        }

        void OnDisable()
        {
            if (!subscribed)
            {
                return;
            }

            EventBus.Unsubscribe<PopupClosed>(ShowResult);
            subscribed = false;
        }

        public void ShowConfirmOnly()
        {
            Request(PopupContent.Create(Heading, Body, "Lorem"));
        }

        public void ShowConfirmAndDismiss()
        {
            Request(PopupContent.Create(Heading, Body, "Lorem", "Ipsum"));
        }

        public void ShowCloseOnly()
        {
            Request(PopupContent.Create(Heading, Body, null, null, true, null, 0f));
        }

        public void ShowActionsAndClose()
        {
            Request(PopupContent.Create(Heading, Body, "Lorem", "Ipsum").WithClose());
        }

        public void ShowCheckboxOff()
        {
            Request(PopupContent.Create(Heading, Body, "Lorem", "Ipsum").WithDoNotShowAgain(Checkbox, false));
        }

        public void ShowCheckboxOn()
        {
            Request(PopupContent.Create(Heading, Body, "Lorem", "Ipsum").WithDoNotShowAgain(Checkbox, true));
        }

        public void ShowAutoClose()
        {
            Request(PopupContent.Create(Heading, Body).WithAutoClose(4f));
        }

        public void ShowHeaderOnly()
        {
            Request(PopupContent.Create(Heading, null, "Lorem"));
        }

        public void ShowBodyOnly()
        {
            Request(PopupContent.Create(null, Body, "Lorem"));
        }

        public void ShowLongCopy()
        {
            Request(PopupContent.Create(Heading, LongBody, "Consectetur", "Adipiscing"));
        }

        void Request(PopupContent content)
        {
            EventBus.Publish(new PopupRequested(PopupId.Preview, content));
        }

        void ShowResult(PopupClosed closed)
        {
            if (closed.Id != PopupId.Preview || resultLabel == null)
            {
                return;
            }

            var checkbox = closed.DoNotShowAgain ? "on" : "off";
            resultLabel.text = "Last result: " + ResultName(closed.Result) + ". Checkbox: " + checkbox;
        }

        static string ResultName(PopupResult result)
        {
            switch (result)
            {
                case PopupResult.Confirmed:
                    return "Confirmed";
                case PopupResult.Dismissed:
                    return "Dismissed";
                case PopupResult.Closed:
                    return "Closed";
                default:
                    return "Timed out";
            }
        }
    }
}
