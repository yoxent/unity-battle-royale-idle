using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.CompanyIdentity
{
    public sealed class CompanyIdentityPreviewView
    {
        private GameObject _rootObject;
        private TMP_Text _companyNameText;
        private Image _emblemPanel;
        private TMP_Text _shapeLabel;
        private TMP_Text _iconLabel;
        private TMP_Text _flagLabel;
        private Image _primaryColorBar;
        private Image _secondaryColorBar;
        private Image _accentColorBar;

        public void Build(Transform parent)
        {
            if (_rootObject != null)
                return;

            var rootObject = CompanyIdentityUiFactory.InstantiatePrefab("CompanyIdentityPreview", parent);
            var bindings = rootObject.GetComponent<CompanyIdentityPreviewViewBindings>();
            if (bindings == null)
            {
                Debug.LogError(
                    "CompanyIdentityPreviewView: The prefab is missing " +
                    "CompanyIdentityPreviewViewBindings.");
                UnityEngine.Object.Destroy(rootObject);
                return;
            }

            _companyNameText = bindings.CompanyNameText;
            _emblemPanel = bindings.EmblemPanel;
            _shapeLabel = bindings.ShapeLabel;
            _iconLabel = bindings.IconLabel;
            _flagLabel = bindings.FlagLabel;
            _primaryColorBar = bindings.PrimaryColorBar;
            _secondaryColorBar = bindings.SecondaryColorBar;
            _accentColorBar = bindings.AccentColorBar;
            if (_companyNameText == null ||
                _emblemPanel == null ||
                _shapeLabel == null ||
                _iconLabel == null ||
                _flagLabel == null ||
                _primaryColorBar == null ||
                _secondaryColorBar == null ||
                _accentColorBar == null)
            {
                Debug.LogError(
                    "CompanyIdentityPreviewView: The prefab bindings have missing references.");
                UnityEngine.Object.Destroy(rootObject);
                return;
            }

            _rootObject = rootObject;
        }

        public void Render(CompanyIdentityData identity)
        {
            if (_companyNameText == null)
            {
                Debug.LogError("CompanyIdentityPreviewView: Build must be called before Render");
                return;
            }

            _companyNameText.text = identity.companyName;

            _emblemPanel.color = identity.primaryColor;

            _shapeLabel.text = CompanyIdentityRules.GetDisplayName(identity.insigniaShape);
            _iconLabel.text = CompanyIdentityRules.GetDisplayName(identity.icon);
            _flagLabel.text = CompanyIdentityRules.GetDisplayName(identity.flagPattern);

            _primaryColorBar.color = identity.primaryColor;
            _secondaryColorBar.color = identity.secondaryColor;
            _accentColorBar.color = identity.accentColor;
        }
    }
}
