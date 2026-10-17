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
        [SerializeField] TMP_Text fieldLabel;
        [SerializeField] TMP_Text ratingLabel;
        [SerializeField] TMP_Text entryValue;
        [SerializeField] TMP_Text prizeValue;
        [SerializeField] Button chooseButton;
        [SerializeField] CompanySessionController company;

        int offerIndex = -1;

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
            fieldLabel.text = Count(teams, "team", "teams") +
                "  ·  " +
                Count(agents, "agent", "agents");
            ratingLabel.text = "Suggested " + suggestedRating;
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

        static string Count(int count, string singular, string plural)
        {
            return count + " " + (count == 1 ? singular : plural);
        }

        bool HasReferences()
        {
            return mapPicture != null &&
                mapName != null &&
                modeLabel != null &&
                difficultyLabel != null &&
                fieldLabel != null &&
                ratingLabel != null &&
                entryValue != null &&
                prizeValue != null &&
                chooseButton != null;
        }
    }
}
