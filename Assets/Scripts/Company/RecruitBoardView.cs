using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Company
{
    public sealed class RecruitBoardView : MonoBehaviour
    {
        [SerializeField] CompanySessionController company;
        [SerializeField] RecruitSlotView[] cards;
        [SerializeField] TMP_Text balanceLabel;
        [SerializeField] TMP_Text spotsLabel;
        [SerializeField] TMP_Text refreshLabel;
        [SerializeField] Button refreshButton;
        [SerializeField] Button backButton;

        bool subscribed;

        void OnEnable()
        {
            if (company == null || cards == null || cards.Length != RecruitBoardRules.BoardSize)
            {
                Debug.LogError("Recruit board needs the company and five cards.", this);
                return;
            }

            for (var i = 0; i < cards.Length; i++)
            {
                var slot = i;
                if (cards[i] == null || cards[i].HireButton == null)
                {
                    Debug.LogError("Recruit board is missing a card.", this);
                    return;
                }

                cards[i].HireButton.onClick.AddListener(() => company.RequestHire(slot));
            }

            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(company.RequestRefresh);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(company.ShowDashboard);
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
                cards[i].HireButton.onClick.RemoveAllListeners();
            }

            if (refreshButton != null)
            {
                refreshButton.onClick.RemoveListener(company.RequestRefresh);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(company.ShowDashboard);
            }

            subscribed = false;
        }

        public RectTransform HireButtonAt(int slot)
        {
            if (cards == null || slot < 0 || slot >= cards.Length || cards[slot] == null ||
                cards[slot].HireButton == null)
            {
                return null;
            }

            return cards[slot].HireButton.transform as RectTransform;
        }

        public void Refresh(RecruitBoard board, int balance, int rosterCount, int fee)
        {
            if (balanceLabel != null)
            {
                balanceLabel.text = $"Gold {balance}";
            }

            if (spotsLabel != null)
            {
                var open = RecruitBoardRules.RosterCap - rosterCount;
                spotsLabel.text = $"Open spots {open}";
            }

            if (refreshLabel != null)
            {
                refreshLabel.text = RecruitBoardRules.FormatRefreshCost(fee);
            }

            if (refreshButton != null)
            {
                refreshButton.interactable = board != null && board.Rolled && (fee <= 0 || balance >= fee);
            }

            if (cards == null || board == null || board.Slots == null)
            {
                return;
            }

            for (var i = 0; i < cards.Length && i < board.Slots.Length; i++)
            {
                var card = board.Slots[i];
                if (card == null || !card.Filled)
                {
                    cards[i].ShowEmpty();
                    continue;
                }

                var block = "";
                var canHire = true;
                if (balance < card.Contract)
                {
                    block = "Short on gold";
                    canHire = false;
                }
                else if (rosterCount >= RecruitBoardRules.RosterCap)
                {
                    block = "Roster is full";
                    canHire = false;
                }

                var name = string.IsNullOrEmpty(card.DisplayName) ? card.ArchetypeId : card.DisplayName;
                cards[i].Show(
                    name,
                    card.Specialty,
                    card.Rarity,
                    card.Weapon,
                    card.Upkeep.ToString(),
                    card.Waived.ToString(),
                    card.Contract,
                    card.Rolls,
                    card.Avatar,
                    block,
                    canHire);
            }
        }
    }
}
