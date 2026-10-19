using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Company
{
    public sealed class MatchOfferView : MonoBehaviour
    {
        [SerializeField] Image mapPicture;
        [SerializeField] TMP_Text mapName;
        [SerializeField] TMP_Text modeLabel;
        [SerializeField] TMP_Text difficultyLabel;
        [SerializeField] TMP_Text totalTeamLabel;
        [SerializeField] TMP_Text totalTeamsValue;
        [SerializeField] TMP_Text teamSizeLabel;
        [SerializeField] TMP_Text teamSizeValue;
        [SerializeField] TMP_Text suggestedTeamPowerLabel;
        [SerializeField] TMP_Text suggestedTeamPowerValue;
        [SerializeField] TMP_Text entryValue;
        [SerializeField] TMP_Text prizeValue;
        [SerializeField] Button chooseButton;
        [SerializeField] CompanySessionController company;

        int offerIndex = -1;

        public RectTransform ChooseTarget =>
            chooseButton == null ? null : chooseButton.transform as RectTransform;

        void OnEnable()
        {
            if (!HasReferences())
            {
                Debug.LogError("Match offer needs its map, labels, and button.", this);
                return;
            }

            chooseButton.onClick.AddListener(Choose);
        }

        void OnDisable()
        {
            if (chooseButton != null)
            {
                chooseButton.onClick.RemoveListener(Choose);
            }
        }

        public void Show(
            int index,
            string mode,
            string difficulty,
            int teams,
            int agents,
            int suggestedRating,
            int entry,
            int prize,
            bool enterable,
            string mapTitle,
            Sprite mapSprite)
        {
            if (!HasReferences())
            {
                Debug.LogError("Match offer needs its map, labels, and button.", this);
                return;
            }

            offerIndex = index;
            gameObject.SetActive(true);
            modeLabel.text = mode;
            difficultyLabel.text = difficulty;
            totalTeamsValue.text = teams.ToString();
            totalTeamLabel.text = Plural(teams, "team", "teams");
            teamSizeValue.text = agents.ToString();
            teamSizeLabel.text = Plural(agents, "agent", "agents");
            suggestedTeamPowerValue.text = suggestedRating.ToString();
            entryValue.text = entry.ToString();
            prizeValue.text = prize + "g";
            chooseButton.interactable = enterable;
            mapName.text = string.IsNullOrEmpty(mapTitle) ? "" : mapTitle;
            mapPicture.sprite = mapSprite;
            mapPicture.enabled = mapSprite != null;
        }

        public void Hide()
        {
            offerIndex = -1;
            gameObject.SetActive(false);
        }

        void Choose()
        {
            if (company == null)
            {
                Debug.LogError("Match offer needs the company session.", this);
                return;
            }

            company.SelectTournament(offerIndex);
        }

        static string Plural(int count, string singular, string plural)
        {
            return count == 1 ? singular : plural;
        }

        bool HasReferences()
        {
            return mapPicture != null &&
                mapName != null &&
                modeLabel != null &&
                difficultyLabel != null &&
                totalTeamLabel != null &&
                totalTeamsValue != null &&
                teamSizeLabel != null &&
                teamSizeValue != null &&
                suggestedTeamPowerLabel != null &&
                suggestedTeamPowerValue != null &&
                entryValue != null &&
                prizeValue != null &&
                chooseButton != null;
        }
    }
}
