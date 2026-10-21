using BattleRoyale.Events;
using UnityEngine;

namespace BattleRoyale.UI
{
    public sealed class PopupController : MonoBehaviour
    {
        [SerializeField] PopupView popup;

        PopupId currentId;
        bool hasCurrentPopup;
        bool subscribed;

        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

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
            hasCurrentPopup = false;
            currentId = default;
            subscribed = false;
        }

        void Show(PopupRequested request)
        {
            if (request.Content == null)
            {
                Debug.LogError("PopupController: the popup request has no content.", this);
                return;
            }

            if (hasCurrentPopup || popup.IsBusy)
            {
                Debug.LogWarning(
                    $"PopupController: ignored popup {request.Id} while another popup is active.",
                    this);
                return;
            }

            if (!popup.Apply(request.Content))
            {
                return;
            }

            currentId = request.Id;
            hasCurrentPopup = true;
            popup.Open();
        }

        void HandleFinished(PopupResult result)
        {
            if (!hasCurrentPopup)
            {
                Debug.LogWarning("PopupController: received a result without an active popup.", this);
                return;
            }

            bool doNotShowAgain = popup.DoNotShowAgain;
            var closedId = currentId;
            currentId = default;
            hasCurrentPopup = false;
            EventBus.Publish(new PopupClosed(closedId, result, doNotShowAgain));

            if (closedId != PopupId.EntryCommit)
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
