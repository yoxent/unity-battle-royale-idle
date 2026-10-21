using BattleRoyale.CompanyIdentity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Company
{
    public sealed class CompanyDashboardView : MonoBehaviour
    {
        [SerializeField] CompanySessionController company;
        [SerializeField] Button emblemButton;
        [SerializeField] Button returnToTitleButton;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text tierLabel;
        [SerializeField] TMP_Text pointsLabel;
        [SerializeField] TMP_Text nextLabel;
        [SerializeField] Image rankFill;
        [SerializeField] TMP_Text goldLabel;
        [SerializeField] TMP_Text fansLabel;
        [SerializeField] TMP_Text dayLabel;
        [SerializeField] TMP_Text stanceLabel;
        [SerializeField] TMP_Text rosterCountLabel;
        [SerializeField] CompanyIdentityPreviewViewBindings emblem;

        CompanyIdentityPreviewView preview;
        bool subscribed;

        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (company == null || nameLabel == null || tierLabel == null ||
                pointsLabel == null || nextLabel == null || rankFill == null ||
                goldLabel == null || fansLabel == null || dayLabel == null ||
                stanceLabel == null || rosterCountLabel == null || emblem == null)
            {
                Debug.LogError("Company dashboard is missing a binding.", this);
                return;
            }

            if (emblemButton == null || returnToTitleButton == null)
            {
                Debug.LogError("Company dashboard needs its emblem and return buttons.", this);
                return;
            }

            emblemButton.onClick.AddListener(company.ShowEditor);
            returnToTitleButton.onClick.AddListener(company.ReturnToTitle);

            preview = new CompanyIdentityPreviewView();
            preview.Initialize(emblem);
            subscribed = true;
        }

        void OnDisable()
        {
            if (!subscribed)
            {
                return;
            }

            emblemButton.onClick.RemoveListener(company.ShowEditor);
            returnToTitleButton.onClick.RemoveListener(company.ReturnToTitle);

            subscribed = false;
        }

        public void Present(
            string companyName,
            CompanyIdentityData emblemSource,
            int rankPoints,
            string gold,
            int fans,
            int day,
            string stance,
            int rosterCount,
            int rosterCap)
        {
            if (!subscribed)
            {
                return;
            }

            var band = CompanyRankRules.Band(rankPoints);
            SetTextIfChanged(nameLabel, companyName);
            SetTextIfChanged(tierLabel, band.Tier);
            SetTextIfChanged(pointsLabel, band.Points.ToString());
            SetTextIfChanged(nextLabel, band.HasNext
                ? band.Remaining + " to " + band.NextTier
                : "Highest tier");
            if (!Mathf.Approximately(rankFill.fillAmount, band.Fill))
            {
                rankFill.fillAmount = band.Fill;
            }

            SetTextIfChanged(goldLabel, gold);
            SetTextIfChanged(fansLabel, "Fans " + fans);
            SetTextIfChanged(dayLabel, "Day " + day);
            SetTextIfChanged(stanceLabel, stance);
            SetTextIfChanged(rosterCountLabel, rosterCount + " / " + rosterCap);
            if (emblemSource != null)
            {
                preview.Render(emblemSource);
            }
        }

        public void PresentLiveValues(string gold, int fans, int day, string stance)
        {
            if (!subscribed)
            {
                return;
            }

            SetTextIfChanged(goldLabel, gold);
            SetTextIfChanged(fansLabel, "Fans " + fans);
            SetTextIfChanged(dayLabel, "Day " + day);
            SetTextIfChanged(stanceLabel, stance);
        }

        static void SetTextIfChanged(TMP_Text label, string value)
        {
            if (label != null && label.text != value)
            {
                label.text = value ?? "";
            }
        }
    }
}
