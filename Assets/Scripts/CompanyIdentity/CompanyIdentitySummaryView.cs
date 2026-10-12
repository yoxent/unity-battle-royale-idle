using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.CompanyIdentity
{
    public sealed class CompanyIdentitySummaryView
    {
        private GameObject _rootObject;
        private TMP_Text _companyNameText;
        private CompanyIdentityPreviewView _preview;
        private Action _onEditRequested;
        private Action _onResetRequested;

        public void Initialize(
            CompanyIdentitySummaryViewBindings bindings,
            CompanyIdentityPreviewView preview,
            Action onEditRequested,
            Action onResetRequested)
        {
            _onEditRequested = onEditRequested;
            _onResetRequested = onResetRequested;

            if (bindings == null)
            {
                Debug.LogError(
                    "CompanyIdentitySummaryView: A summary view binding reference must be assigned.");
                return;
            }

            if (preview == null)
            {
                Debug.LogError(
                    "CompanyIdentitySummaryView: A summary preview reference must be assigned.");
                return;
            }

            _companyNameText = bindings.CompanyNameText;
            var editButton = bindings.EditButton;
            var resetButton = bindings.ResetButton;
            if (_companyNameText == null ||
                editButton == null ||
                resetButton == null)
            {
                Debug.LogError(
                    "CompanyIdentitySummaryView: The scene binding has missing references.");
                return;
            }

            _rootObject = bindings.gameObject;
            _preview = preview;
            editButton.onClick.AddListener(() => _onEditRequested?.Invoke());
            resetButton.onClick.AddListener(() => _onResetRequested?.Invoke());
        }

        public void Refresh(CompanyIdentityData identity)
        {
            if (_companyNameText != null && identity != null)
            {
                _companyNameText.text = identity.companyName;
            }

            if (_preview != null && identity != null)
            {
                _preview.Render(identity);
            }
        }

        public void SetVisible(bool visible)
        {
            if (_rootObject != null)
            {
                _rootObject.SetActive(visible);
            }
        }
    }
}
