using UnityEngine;
using UnityEngine.EventSystems;

namespace BattleRoyale.CompanyIdentity
{
    public enum CompanyIdentityPage
    {
        Creation,
        Editor,
        Summary
    }

    public sealed class CompanyIdentityTestHarness : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private EventSystem _eventSystem;

        private CompanyIdentityData _currentIdentity;
        private CompanyIdentityPage _currentPage;

        private CompanyIdentityCreationView _creationView;
        private CompanyIdentityEditorView _editorView;
        private CompanyIdentitySummaryView _summaryView;

        private CompanyIdentityPreviewView _editorPreview;
        private bool _initialized;

        public CompanyIdentityData CurrentIdentity => _currentIdentity;
        public CompanyIdentityPage Page => _currentPage;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            Initialize(_canvas, _eventSystem);
        }

        public void Initialize(Canvas requiredCanvas, EventSystem requiredEventSystem)
        {
            if (_initialized)
                return;

            if (requiredCanvas == null)
            {
                Debug.LogError(
                    "CompanyIdentityTestHarness: A Canvas reference must be assigned.");
                return;
            }

            if (requiredEventSystem == null)
            {
                Debug.LogError(
                    "CompanyIdentityTestHarness: An EventSystem reference must be assigned.");
                return;
            }

            _canvas = requiredCanvas;
            _eventSystem = requiredEventSystem;

            // Build all three views under the Canvas
            _creationView = new CompanyIdentityCreationView();
            _creationView.Build(_canvas.transform, OnCreationRequested);

            _editorView = new CompanyIdentityEditorView();
            _editorView.Build(
                _canvas.transform,
                OnOptionSelected,
                OnColorSelected,
                OnDoneRequested);

            _summaryView = new CompanyIdentitySummaryView();
            _summaryView.Build(_canvas.transform, OnEditRequested, OnResetRequested);

            // Create editor preview under CompanyIdentityEditorView.PreviewParent
            _editorPreview = new CompanyIdentityPreviewView();
            _editorPreview.Build(_editorView.PreviewParent);

            // Start in Creation page
            _currentPage = CompanyIdentityPage.Creation;
            _creationView.SetVisible(true);
            _editorView.SetVisible(false);
            _summaryView.SetVisible(false);

            _initialized = true;
        }

        public void ResetCompany()
        {
            _currentIdentity = null;
            _currentPage = CompanyIdentityPage.Creation;

            _creationView.Clear();
            _creationView.SetVisible(true);
            _editorView.SetVisible(false);
            _summaryView.SetVisible(false);
        }

        private void OnCreationRequested(string normalizedName)
        {
            try
            {
                _currentIdentity = CompanyIdentityRandomizer.Create(normalizedName);

                // Render editor preview and refresh summary view
                _editorPreview.Render(_currentIdentity);
                _summaryView.Refresh(_currentIdentity);

                // Transition to Editor
                _creationView.SetVisible(false);
                _editorView.SetVisible(true);
                _editorView.Refresh(_currentIdentity);

                _currentPage = CompanyIdentityPage.Editor;
            }
            catch (System.ArgumentException)
            {
                // Creation failed - show validation message
                var validationMessage = CompanyIdentityRules.GetNameValidationMessage(normalizedName);
                _creationView.SetValidationMessage(validationMessage);
            }
        }

        private void OnOptionSelected(CompanyIdentityTab tab, int optionIndex)
        {
            if (_currentIdentity == null)
                return;

            switch (tab)
            {
                case CompanyIdentityTab.Insignia:
                    if (optionIndex >= 0 && optionIndex < CompanyIdentityRules.InsigniaShapes.Length)
                    {
                        _currentIdentity.insigniaShape = CompanyIdentityRules.InsigniaShapes[optionIndex];
                    }
                    break;

                case CompanyIdentityTab.Icon:
                    if (optionIndex >= 0 && optionIndex < CompanyIdentityRules.Icons.Length)
                    {
                        _currentIdentity.icon = CompanyIdentityRules.Icons[optionIndex];
                    }
                    break;

                case CompanyIdentityTab.Flag:
                    if (optionIndex >= 0 && optionIndex < CompanyIdentityRules.FlagPatterns.Length)
                    {
                        _currentIdentity.flagPattern = CompanyIdentityRules.FlagPatterns[optionIndex];
                    }
                    break;
            }

            // Refresh all views after mutation
            _editorView.Refresh(_currentIdentity);
            _editorPreview.Render(_currentIdentity);
            _summaryView.Refresh(_currentIdentity);
        }

        private void OnColorSelected(CompanyIdentityColorSlot slot, int paletteIndex)
        {
            if (_currentIdentity == null)
                return;

            if (paletteIndex >= 0 && paletteIndex < CompanyIdentityRules.Palette.Length)
            {
                _currentIdentity.SetColor(slot, CompanyIdentityRules.Palette[paletteIndex]);

                // Refresh all views after mutation
                _editorView.Refresh(_currentIdentity);
                _editorPreview.Render(_currentIdentity);
                _summaryView.Refresh(_currentIdentity);
            }
        }

        private void OnDoneRequested()
        {
            if (_currentIdentity == null)
                return;

            // Transition to Summary: hide editor first, then refresh and show summary
            _editorView.SetVisible(false);
            _summaryView.Refresh(_currentIdentity);
            _summaryView.SetVisible(true);

            _currentPage = CompanyIdentityPage.Summary;
        }

        private void OnEditRequested()
        {
            if (_currentIdentity == null)
                return;

            // Transition to Editor
            _summaryView.SetVisible(false);
            _editorView.SetVisible(true);

            _currentPage = CompanyIdentityPage.Editor;
        }

        private void OnResetRequested()
        {
            ResetCompany();
        }
    }
}
