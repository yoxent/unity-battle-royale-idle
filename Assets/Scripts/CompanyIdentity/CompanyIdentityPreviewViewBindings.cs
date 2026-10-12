using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.CompanyIdentity
{
    public sealed class CompanyIdentityPreviewViewBindings : MonoBehaviour
    {
        [SerializeField] private TMP_Text companyNameText;
        [SerializeField] private Image emblemPanel;
        [SerializeField] private TMP_Text shapeLabel;
        [SerializeField] private TMP_Text iconLabel;
        [SerializeField] private TMP_Text flagLabel;
        [SerializeField] private Image primaryColorBar;
        [SerializeField] private Image secondaryColorBar;
        [SerializeField] private Image accentColorBar;

        public TMP_Text CompanyNameText => companyNameText;
        public Image EmblemPanel => emblemPanel;
        public TMP_Text ShapeLabel => shapeLabel;
        public TMP_Text IconLabel => iconLabel;
        public TMP_Text FlagLabel => flagLabel;
        public Image PrimaryColorBar => primaryColorBar;
        public Image SecondaryColorBar => secondaryColorBar;
        public Image AccentColorBar => accentColorBar;
    }
}
