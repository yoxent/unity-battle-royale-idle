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
            if (!Application.isPlaying)
            {
                return;
            }

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
            SetActiveIfChanged(gameObject, true);
            SetTextIfChanged(modeLabel, mode);
            SetTextIfChanged(difficultyLabel, difficulty);
            SetTextIfChanged(totalTeamsValue, teams.ToString());
            SetTextIfChanged(totalTeamLabel, Plural(teams, "team", "teams"));
            SetTextIfChanged(teamSizeValue, agents.ToString());
            SetTextIfChanged(teamSizeLabel, Plural(agents, "agent", "agents"));
            SetTextIfChanged(suggestedTeamPowerValue, suggestedRating.ToString());
            SetTextIfChanged(entryValue, entry.ToString());
            SetTextIfChanged(prizeValue, prize + "g");
            if (chooseButton.interactable != enterable)
            {
                chooseButton.interactable = enterable;
            }

            SetTextIfChanged(
                mapName,
                string.IsNullOrEmpty(mapTitle) ? "Map unavailable" : mapTitle);
            if (mapPicture.sprite != mapSprite)
            {
                mapPicture.sprite = mapSprite;
            }

            if (mapPicture.enabled != (mapSprite != null))
            {
                mapPicture.enabled = mapSprite != null;
            }
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

        static void SetTextIfChanged(TMP_Text label, string value)
        {
            if (label == null)
            {
                return;
            }

            var text = value ?? string.Empty;
            if (label.text != text)
            {
                label.text = text;
            }
        }

        static void SetActiveIfChanged(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
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
