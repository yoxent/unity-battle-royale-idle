using BattleRoyale.UI;
using UnityEngine;

namespace BattleRoyale.Company
{
    public sealed class CompanyTabBar : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] UIButton company;
        [SerializeField] UIButton matches;
        [SerializeField] UIButton recruit;

        public void SetVisible(bool visible)
        {
            if (root == null)
            {
                Debug.LogError("Company tab bar needs its root.", this);
                return;
            }

            root.SetActive(visible);
        }

        public void SelectCompany()
        {
            Select(company);
        }

        public void SelectMatches()
        {
            Select(matches);
        }

        public void SelectRecruit()
        {
            Select(recruit);
        }

        void Select(UIButton selected)
        {
            if (company == null || matches == null || recruit == null)
            {
                Debug.LogError("Company tab bar needs its three tabs.", this);
                return;
            }

            company.SetTreatment(company == selected
                ? UIButtonTreatment.Filled
                : UIButtonTreatment.Outlined);
            matches.SetTreatment(matches == selected
                ? UIButtonTreatment.Filled
                : UIButtonTreatment.Outlined);
            recruit.SetTreatment(recruit == selected
                ? UIButtonTreatment.Filled
                : UIButtonTreatment.Outlined);
        }
    }
}
