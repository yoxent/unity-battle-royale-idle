using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Company
{
    public sealed class CompanyRosterView : MonoBehaviour
    {
        [SerializeField] CompanySessionController company;
        [SerializeField] CompanyRosterCardView[] cards;

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

            for (var i = 0; i < cards.Length; i++)
            {
                var slot = i;
                if (cards[i] == null || cards[i].ReleaseButton == null)
                {
                    Debug.LogError("The roster is missing a card.", this);
                    return;
                }

                cards[i].ReleaseButton.onClick.AddListener(() => company.ReleaseAgent(slot));
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
                cards[i].ReleaseButton.onClick.RemoveAllListeners();
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
