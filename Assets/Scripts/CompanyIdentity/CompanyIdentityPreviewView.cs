using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.CompanyIdentity
{
    public sealed class CompanyIdentityPreviewView
    {
        private TMP_Text _companyNameText;
        private Image _emblemPanel;
        private CompanyEmblemGraphic _emblem;
        private IReadOnlyList<Sprite> _shapeSprites;
        private IReadOnlyList<Sprite> _shapeBorderSprites;
        private TMP_Text _shapeLabel;
        private TMP_Text _iconLabel;
        private TMP_Text _flagLabel;
        private Image _primaryColorBar;
        private Image _secondaryColorBar;
        private Image _accentColorBar;

        public void Initialize(CompanyIdentityPreviewViewBindings bindings)
        {
            if (bindings == null)
            {
                Debug.LogError(
                    "CompanyIdentityPreviewView: A preview binding reference must be assigned.");
                return;
            }

            _companyNameText = bindings.CompanyNameText;
            _emblemPanel = bindings.EmblemPanel;
            _emblem = bindings.Emblem;
            _shapeSprites = bindings.ShapeSprites;
            _shapeBorderSprites = bindings.ShapeBorderSprites;
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
                    "CompanyIdentityPreviewView: The scene binding has missing references.");
                return;
            }
        }

        public void Render(CompanyIdentityData identity)
        {
            if (_companyNameText == null)
            {
                Debug.LogError("CompanyIdentityPreviewView: Build must be called before Render");
                return;
            }

            _companyNameText.text = identity.companyName;

            if (_emblem != null)
            {
                _emblem.Apply(
                    identity.bodyIndex,
                    _shapeSprites,
                    _shapeBorderSprites,
                    identity.bodyFillColor,
                    identity.bodyBorderColor);
            }
            else if (_emblemPanel != null)
            {
                _emblemPanel.color = identity.primaryColor;
            }

            if (_shapeLabel != null)
            {
                _shapeLabel.text = CompanyIdentityRules.GetDisplayName(identity.insigniaShape);
            }

            if (_iconLabel != null)
            {
                _iconLabel.text = CompanyIdentityRules.GetDisplayName(identity.icon);
            }

            if (_flagLabel != null)
            {
                _flagLabel.text = CompanyIdentityRules.GetDisplayName(identity.flagPattern);
            }

            if (_primaryColorBar != null)
            {
                _primaryColorBar.color = identity.primaryColor;
            }

            if (_secondaryColorBar != null)
            {
                _secondaryColorBar.color = identity.secondaryColor;
            }

            if (_accentColorBar != null)
            {
                _accentColorBar.color = identity.accentColor;
            }
        }
    }
}
