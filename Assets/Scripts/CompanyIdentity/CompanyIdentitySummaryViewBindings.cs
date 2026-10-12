using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.CompanyIdentity
{
    public sealed class CompanyIdentitySummaryViewBindings : MonoBehaviour
    {
        [SerializeField] private Transform previewParent;
        [SerializeField] private TMP_Text companyNameText;
        [SerializeField] private Button editButton;
        [SerializeField] private Button resetButton;

        public Transform PreviewParent => previewParent;
        public TMP_Text CompanyNameText => companyNameText;
        public Button EditButton => editButton;
        public Button ResetButton => resetButton;
    }
}
