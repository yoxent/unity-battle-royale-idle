using BattleRoyale.Events;
using UnityEngine;

namespace BattleRoyale.UI
{
    public sealed class PopupController : MonoBehaviour
    {
        [SerializeField] PopupView popup;

        PopupId currentId;
        bool subscribed;

        void OnEnable()
        {
            if (popup == null)
            {
                Debug.LogError("PopupController: assign the popup.", this);
                return;
            }

            EventBus.Subscribe<PopupRequested>(Show);
            popup.Finished += HandleFinished;
            subscribed = true;
        }

        void OnDisable()
        {
            if (!subscribed)
            {
                return;
            }

            EventBus.Unsubscribe<PopupRequested>(Show);
            popup.Finished -= HandleFinished;
            subscribed = false;
        }

        void Show(PopupRequested request)
        {
            if (request.Content == null)
            {
                Debug.LogError("PopupController: the popup request has no content.", this);
                return;
            }

            if (!popup.Apply(request.Content))
            {
                return;
            }

            currentId = request.Id;
            popup.Open();
        }

        void HandleFinished(PopupResult result)
        {
            bool doNotShowAgain = popup.DoNotShowAgain;
            EventBus.Publish(new PopupClosed(currentId, result, doNotShowAgain));

            if (currentId != PopupId.EntryCommit)
            {
                return;
            }

            if (result == PopupResult.Confirmed)
            {
                EventBus.Publish(new EntryCommitAccepted());
                return;
            }

            EventBus.Publish(new EntryCommitDismissed());
        }
    }
}
