using UnityEngine;

namespace BattleRoyale.CompanyIdentity
{
    public enum CompanyIdentityPage
    {
        Creation,
        Editor,
        Summary
    }

    public sealed class CompanyIdentityFlowController : MonoBehaviour
    {
        [SerializeField] private CompanyIdentityCreationViewBindings _creationViewBindings;
        [SerializeField] private CompanyIdentityEditorViewBindings _editorViewBindings;
        [SerializeField] private CompanyIdentitySummaryViewBindings _summaryViewBindings;
        [SerializeField] private CompanyIdentityPreviewViewBindings _editorPreviewBindings;
        [SerializeField] private CompanyIdentityPreviewViewBindings _summaryPreviewBindings;

        private CompanyIdentityData _currentIdentity;
        private CompanyIdentityPage _currentPage;

        private CompanyIdentityCreationView _creationView;
        private CompanyIdentityEditorView _editorView;
        private CompanyIdentitySummaryView _summaryView;
        private CompanyIdentityPreviewView _editorPreview;
        private CompanyIdentityPreviewView _summaryPreview;
        private bool _initialized;

        public CompanyIdentityData CurrentIdentity => _currentIdentity;
        public CompanyIdentityPage Page => _currentPage;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            Initialize(
                _creationViewBindings,
                _editorViewBindings,
                _summaryViewBindings,
                _editorPreviewBindings,
                _summaryPreviewBindings);
        }

        public void Initialize(
            CompanyIdentityCreationViewBindings creationViewBindings,
            CompanyIdentityEditorViewBindings editorViewBindings,
            CompanyIdentitySummaryViewBindings summaryViewBindings,
            CompanyIdentityPreviewViewBindings editorPreviewBindings,
            CompanyIdentityPreviewViewBindings summaryPreviewBindings)
        {
            if (_initialized)
                return;

            if (creationViewBindings == null ||
                editorViewBindings == null ||
                summaryViewBindings == null ||
                editorPreviewBindings == null ||
                summaryPreviewBindings == null)
            {
                Debug.LogError(
                    "CompanyIdentityFlowController: Creation, editor, summary, and preview " +
                    "scene references must be assigned.");
                return;
            }

            _creationViewBindings = creationViewBindings;
            _editorViewBindings = editorViewBindings;
            _summaryViewBindings = summaryViewBindings;
            _editorPreviewBindings = editorPreviewBindings;
            _summaryPreviewBindings = summaryPreviewBindings;

            _creationView = new CompanyIdentityCreationView();
            _creationView.Initialize(_creationViewBindings, OnCreationRequested);

            _editorView = new CompanyIdentityEditorView();
            _editorView.Initialize(
                _editorViewBindings,
                OnOptionSelected,
                OnColorSelected,
                OnBodyStep,
                OnEmblemEdited,
                OnDoneRequested);

            _editorPreview = new CompanyIdentityPreviewView();
            _editorPreview.Initialize(_editorPreviewBindings);

            _summaryPreview = new CompanyIdentityPreviewView();
            _summaryPreview.Initialize(_summaryPreviewBindings);

            _summaryView = new CompanyIdentitySummaryView();
            _summaryView.Initialize(
                _summaryViewBindings,
                _summaryPreview,
                OnEditRequested,
                OnResetRequested);

            _currentPage = CompanyIdentityPage.Creation;
            _creationView.SetVisible(true);
            _editorView.SetVisible(false);
            _summaryView.SetVisible(false);

            _initialized = true;
        }

        public void ResetCompany()
        {
            if (!_initialized)
                return;

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
                _editorPreview.Render(_currentIdentity);
                _summaryView.Refresh(_currentIdentity);

                _creationView.SetVisible(false);
                _editorView.SetVisible(true);
                _editorView.Refresh(_currentIdentity, _editorPreviewBindings.ShapeSprites);
                _currentPage = CompanyIdentityPage.Editor;
            }
            catch (System.ArgumentException)
            {
                _creationView.SetValidationMessage(
                    CompanyIdentityRules.GetNameValidationMessage(normalizedName));
            }
        }

        private void OnOptionSelected(CompanyIdentityTab tab, int optionIndex)
        {
            if (_currentIdentity == null)
                return;

            switch (tab)
            {
                case CompanyIdentityTab.Insignia:
                    if (optionIndex >= 0 &&
                        optionIndex < CompanyIdentityRules.InsigniaShapes.Length)
                    {
                        _currentIdentity.insigniaShape =
                            CompanyIdentityRules.InsigniaShapes[optionIndex];
                    }
                    break;

                case CompanyIdentityTab.Icon:
                    if (optionIndex >= 0 &&
                        optionIndex < CompanyIdentityRules.Icons.Length)
                    {
                        _currentIdentity.icon = CompanyIdentityRules.Icons[optionIndex];
                    }
                    break;

                case CompanyIdentityTab.Flag:
                    if (optionIndex >= 0 &&
                        optionIndex < CompanyIdentityRules.FlagPatterns.Length)
                    {
                        _currentIdentity.flagPattern =
                            CompanyIdentityRules.FlagPatterns[optionIndex];
                    }
                    break;
            }

            RefreshIdentityViews();
        }

        private void OnBodyStep(int direction)
        {
            if (_currentIdentity == null ||
                _editorPreviewBindings == null ||
                _editorPreviewBindings.BodyPairCount <= 0)
            {
                return;
            }

            _currentIdentity.bodyIndex = CompanyIdentityRules.StepBodyIndex(
                _currentIdentity.bodyIndex,
                direction,
                _editorPreviewBindings.BodyPairCount);
            RefreshIdentityViews();
        }

        private void OnEmblemEdited()
        {
            RefreshIdentityViews();
        }

        private void OnColorSelected(CompanyIdentityColorSlot slot, int paletteIndex)
        {
            if (_currentIdentity == null ||
                paletteIndex < 0 ||
                paletteIndex >= CompanyIdentityRules.Palette.Length)
            {
                return;
            }

            _currentIdentity.SetColor(slot, CompanyIdentityRules.Palette[paletteIndex]);
            RefreshIdentityViews();
        }

        private void RefreshIdentityViews()
        {
            _editorView.Refresh(_currentIdentity, _editorPreviewBindings.ShapeSprites);
            _editorPreview.Render(_currentIdentity);
            _summaryView.Refresh(_currentIdentity);
        }

        private void OnDoneRequested()
        {
            if (_currentIdentity == null)
                return;

            _editorView.SetVisible(false);
            _summaryView.Refresh(_currentIdentity);
            _summaryView.SetVisible(true);
            _currentPage = CompanyIdentityPage.Summary;
        }

        private void OnEditRequested()
        {
            if (_currentIdentity == null)
                return;

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
