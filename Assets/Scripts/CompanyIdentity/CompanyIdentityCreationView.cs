using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.CompanyIdentity
{
    public sealed class CompanyIdentityCreationView
    {
        private GameObject _rootObject;
        private TMP_InputField _companyNameInput;
        private TMP_Text _validationText;
        private Button _createButton;
        private Action<string> _onCreateRequested;

        public void Initialize(
            CompanyIdentityCreationViewBindings bindings,
            Action<string> onCreateRequested)
        {
            _onCreateRequested = onCreateRequested;

            if (bindings == null)
            {
                Debug.LogError(
                    "CompanyIdentityCreationView: A creation view binding reference must be assigned.");
                return;
            }

            _companyNameInput = bindings.CompanyNameInput;
            _validationText = bindings.ValidationText;
            _createButton = bindings.CreateButton;
            if (_companyNameInput == null || _validationText == null || _createButton == null)
            {
                Debug.LogError(
                    "CompanyIdentityCreationView: The scene binding has missing references.");
                return;
            }

            _rootObject = bindings.gameObject;
            _companyNameInput.characterLimit = 0; // Allow unlimited input for validation feedback
            _companyNameInput.onValueChanged.AddListener(OnInputChanged);
            _createButton.onClick.AddListener(OnCreateButtonClicked);

            // Initialize validation
            UpdateValidation();
        }

        public void SetValidationMessage(string message)
        {
            if (_validationText != null)
            {
                _validationText.text = message ?? "";
            }
        }

        public void SetVisible(bool visible)
        {
            if (_rootObject != null)
            {
                _rootObject.SetActive(visible);
            }
        }

        public void Clear()
        {
            if (_companyNameInput != null)
            {
                _companyNameInput.text = "";
            }
            UpdateValidation();
        }

        private void OnInputChanged(string value)
        {
            UpdateValidation();
        }

        private void UpdateValidation()
        {
            var input = _companyNameInput.text;
            var isValid = CompanyIdentityRules.TryNormalizeCompanyName(input, out _);

            _createButton.interactable = isValid;

            var validationMessage = CompanyIdentityRules.GetNameValidationMessage(input);
            SetValidationMessage(validationMessage);
        }

        private void OnCreateButtonClicked()
        {
            if (CompanyIdentityRules.TryNormalizeCompanyName(_companyNameInput.text, out var normalizedName))
            {
                _onCreateRequested?.Invoke(normalizedName);
            }
        }
    }
}
