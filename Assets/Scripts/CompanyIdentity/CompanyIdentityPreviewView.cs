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
            if (_emblemPanel == null && _emblem == null)
            {
                Debug.LogError(
                    "CompanyIdentityPreviewView: An emblem panel or graphic is required.");
                return;
            }

            if (_emblem != null &&
                (_shapeSprites == null ||
                 _shapeBorderSprites == null ||
                 _shapeSprites.Count == 0 ||
                 _shapeSprites.Count != _shapeBorderSprites.Count))
            {
                Debug.LogError(
                    "CompanyIdentityPreviewView: Full previews need paired emblem body lists.");
                return;
            }
        }

        public void Render(CompanyIdentityData identity)
        {
            if (_emblemPanel == null && _emblem == null)
            {
                Debug.LogError(
                    "CompanyIdentityPreviewView: Initialize must succeed before Render.");
                return;
            }

            if (_companyNameText != null)
            {
                _companyNameText.text = identity.companyName;
            }

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
