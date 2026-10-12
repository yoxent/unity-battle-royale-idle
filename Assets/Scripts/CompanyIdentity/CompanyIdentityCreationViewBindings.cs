using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.CompanyIdentity
{
    public sealed class CompanyIdentityCreationViewBindings : MonoBehaviour
    {
        [SerializeField] private TMP_InputField companyNameInput;
        [SerializeField] private TMP_Text validationText;
        [SerializeField] private Button createButton;

        public TMP_InputField CompanyNameInput => companyNameInput;
        public TMP_Text ValidationText => validationText;
        public Button CreateButton => createButton;
    }
}
