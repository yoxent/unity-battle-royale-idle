using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BattleRoyale.Company
{
    public sealed class CompanyRosterView : MonoBehaviour
    {
        [SerializeField] CompanySessionController company;
        [SerializeField] CompanyRosterCardView[] cards;

        UnityAction[] releaseActions;
        bool subscribed;

        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (company == null || cards == null || cards.Length != RecruitBoardRules.RosterCap)
            {
                Debug.LogError("The roster needs the company and eight cards.", this);
                return;
            }

            if (releaseActions == null || releaseActions.Length != cards.Length)
            {
                releaseActions = new UnityAction[cards.Length];
            }

            for (var i = 0; i < cards.Length; i++)
            {
                var slot = i;
                if (cards[i] == null || cards[i].ReleaseButton == null)
                {
                    Debug.LogError("The roster is missing a card.", this);
                    return;
                }

                releaseActions[i] = () => company.ReleaseAgent(slot);
                cards[i].ReleaseButton.onClick.AddListener(releaseActions[i]);
            }

            subscribed = true;
        }

        void OnDisable()
        {
            if (!subscribed)
            {
                return;
            }

            for (var i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null &&
                    cards[i].ReleaseButton != null &&
                    releaseActions != null &&
                    i < releaseActions.Length &&
                    releaseActions[i] != null)
                {
                    cards[i].ReleaseButton.onClick.RemoveListener(releaseActions[i]);
                    releaseActions[i] = null;
                }
            }

            subscribed = false;
        }

        public void Show(RosterSlotPresentation[] slots)
        {
            if (cards == null)
            {
                return;
            }

            for (var i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null)
                {
                    continue;
                }

                if (slots == null || i >= slots.Length || !slots[i].Occupied)
                {
                    cards[i].ShowOpen();
                    continue;
                }

                cards[i].Show(slots[i]);
            }
        }
    }
}
