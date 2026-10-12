using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.CompanyIdentity
{
    public sealed class CompanyIdentitySummaryView
    {
        private GameObject _rootObject;
        private Transform _previewParent;
        private TMP_Text _companyNameText;
        private CompanyIdentityPreviewView _preview;
        private Action _onEditRequested;
        private Action _onResetRequested;

        public Transform PreviewParent => _previewParent;

        public void Build(Transform parent, Action onEditRequested, Action onResetRequested)
        {
            _onEditRequested = onEditRequested;
            _onResetRequested = onResetRequested;

            var rootObject = CompanyIdentityUiFactory.InstantiatePrefab(
                "CompanyIdentitySummaryView",
                parent);
            var bindings = rootObject.GetComponent<CompanyIdentitySummaryViewBindings>();
            if (bindings == null)
            {
                Debug.LogError(
                    "CompanyIdentitySummaryView: The prefab is missing " +
                    "CompanyIdentitySummaryViewBindings.");
                UnityEngine.Object.Destroy(rootObject);
                return;
            }

            _previewParent = bindings.PreviewParent;
            _companyNameText = bindings.CompanyNameText;
            var editButton = bindings.EditButton;
            var resetButton = bindings.ResetButton;
            if (_previewParent == null ||
                _companyNameText == null ||
                editButton == null ||
                resetButton == null)
            {
                Debug.LogError(
                    "CompanyIdentitySummaryView: The prefab bindings have missing references.");
                UnityEngine.Object.Destroy(rootObject);
                return;
            }

            _rootObject = rootObject;
            editButton.onClick.AddListener(() => _onEditRequested?.Invoke());
            resetButton.onClick.AddListener(() => _onResetRequested?.Invoke());

            _preview = new CompanyIdentityPreviewView();
            _preview.Build(_previewParent);
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
