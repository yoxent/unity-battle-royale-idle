using BattleRoyale.CompanyIdentity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class CompanyIdentityTestHarnessTests
    {
        private GameObject _harnessObject;
        private GameObject _harnessCanvasObject;
        private GameObject _harnessEventSystemObject;
        private CompanyIdentityTestHarness _harness;
        private Canvas _harnessCanvas;
        private EventSystem _harnessEventSystem;
        private CompanyIdentityCreationViewBindings _creationBindings;
        private CompanyIdentityEditorViewBindings _editorBindings;
        private CompanyIdentitySummaryViewBindings _summaryBindings;

        [SetUp]
        public void SetUp()
        {
            _harnessCanvasObject = CompanyIdentityUiFactory.InstantiatePrefab(
                "CompanyIdentityCanvas",
                null);
            _harnessCanvas = _harnessCanvasObject.GetComponent<Canvas>();

            _harnessEventSystemObject = CompanyIdentityUiFactory.InstantiatePrefab(
                "CompanyIdentityEventSystem",
                null);
            _harnessEventSystem = _harnessEventSystemObject.GetComponent<EventSystem>();

            _harnessObject = new GameObject("TestHarness");
            _harnessObject.SetActive(false);
            _harness = _harnessObject.AddComponent<CompanyIdentityTestHarness>();
            _harness.Initialize(_harnessCanvas, _harnessEventSystem);
            _creationBindings = FindCreatedBinding<CompanyIdentityCreationViewBindings>();
            _editorBindings = FindCreatedBinding<CompanyIdentityEditorViewBindings>();
            _summaryBindings = FindCreatedBinding<CompanyIdentitySummaryViewBindings>();
        }

        [TearDown]
        public void TearDown()
        {
            // Destroy only the view roots created by this harness.
            if (_creationBindings != null)
                Object.DestroyImmediate(_creationBindings.gameObject);
            if (_editorBindings != null)
                Object.DestroyImmediate(_editorBindings.gameObject);
            if (_summaryBindings != null)
                Object.DestroyImmediate(_summaryBindings.gameObject);

            if (_harnessCanvasObject != null)
                Object.DestroyImmediate(_harnessCanvasObject);

            if (_harnessEventSystemObject != null)
                Object.DestroyImmediate(_harnessEventSystemObject);

            if (_harnessObject != null)
            {
                Object.DestroyImmediate(_harnessObject);
            }
        }

        private T FindCreatedBinding<T>() where T : Component
        {
            if (_harnessCanvas == null)
                return null;

            foreach (Transform child in _harnessCanvas.transform)
            {
                var binding = child.GetComponent<T>();
                if (binding != null)
                {
                    return binding;
                }
            }

            return null;
        }

        [Test]
        public void FullFlowFromCreationToSummaryAndBack()
        {
            // Verify initial state: Creation page
            Assert.AreEqual(CompanyIdentityPage.Creation, _harness.Page);
            Assert.IsNull(_harness.CurrentIdentity);

            Assert.IsNotNull(_creationBindings, "Creation view bindings should exist");
            Assert.IsNotNull(_editorBindings, "Editor view bindings should exist");
            Assert.IsNotNull(_summaryBindings, "Summary view bindings should exist");

            var inputField = _creationBindings.CompanyNameInput;
            var createButton = _creationBindings.CreateButton;
            Assert.IsNotNull(inputField, "Company name input should be assigned");
            Assert.IsNotNull(createButton, "Create button should be assigned");

            // Enter company name and create
            inputField.text = "  Test Company  ";
            createButton.onClick.Invoke();

            // Verify transition to Editor page
            Assert.AreEqual(CompanyIdentityPage.Editor, _harness.Page);
            Assert.IsNotNull(_harness.CurrentIdentity);
            Assert.AreEqual("Test Company", _harness.CurrentIdentity.companyName); // normalized (trimmed)

            var insigniaTab = _editorBindings.Tabs[(int)CompanyIdentityTab.Insignia];
            var iconTab = _editorBindings.Tabs[(int)CompanyIdentityTab.Icon];
            var flagTab = _editorBindings.Tabs[(int)CompanyIdentityTab.Flag];
            Assert.IsNotNull(insigniaTab, "Insignia tab bindings should exist");
            Assert.IsNotNull(iconTab, "Icon tab bindings should exist");
            Assert.IsNotNull(flagTab, "Flag tab bindings should exist");

            // Click the second Insignia option (index 1)
            Assert.IsNotNull(insigniaTab.Options, "Insignia options should be assigned");
            Assert.That(insigniaTab.Options.Length, Is.EqualTo(4));
            insigniaTab.Options[1].Button.onClick.Invoke();
            Assert.AreEqual(CompanyIdentityRules.InsigniaShapes[1], _harness.CurrentIdentity.insigniaShape);

            // Click Icon tab
            iconTab.TabButton.onClick.Invoke();

            // Click the third Icon option (index 2)
            Assert.IsNotNull(iconTab.Options, "Icon options should be assigned");
            Assert.That(iconTab.Options.Length, Is.EqualTo(4));
            iconTab.Options[2].Button.onClick.Invoke();
            Assert.AreEqual(CompanyIdentityRules.Icons[2], _harness.CurrentIdentity.icon);

            var initialPrimaryColor = _harness.CurrentIdentity.primaryColor;
            var selectedPaletteIndex = -1;
            for (int i = 0; i < CompanyIdentityRules.Palette.Length; i++)
            {
                if (CompanyIdentityRules.Palette[i] != initialPrimaryColor)
                {
                    selectedPaletteIndex = i;
                    break;
                }
            }

            Assert.That(selectedPaletteIndex, Is.GreaterThanOrEqualTo(0),
                "A palette color different from the initial primary color should exist");
            var selectedPaletteColor = CompanyIdentityRules.Palette[selectedPaletteIndex];
            Assert.IsNotNull(iconTab.Palette, "Icon palette should be assigned");
            Assert.That(iconTab.Palette.Length, Is.EqualTo(CompanyIdentityRules.Palette.Length));
            iconTab.Palette[selectedPaletteIndex].Button.onClick.Invoke();

            // Verify identity color changed (Primary is the default active slot)
            Assert.AreEqual(selectedPaletteColor, _harness.CurrentIdentity.primaryColor);
            Assert.AreNotEqual(initialPrimaryColor, _harness.CurrentIdentity.primaryColor);

            // The Flag tab owns its own color controls as well.
            flagTab.TabButton.onClick.Invoke();
            Assert.IsNotNull(flagTab.ColorSlots, "Flag tab should contain color slots");
            Assert.That(flagTab.ColorSlots.Length, Is.EqualTo(3));
            Assert.IsNotNull(flagTab.Palette, "Flag tab should contain a color palette");
            Assert.That(flagTab.Palette.Length, Is.EqualTo(CompanyIdentityRules.Palette.Length));
            flagTab.ColorSlots[(int)CompanyIdentityColorSlot.Secondary].Button.onClick.Invoke();

            var initialSecondaryColor = _harness.CurrentIdentity.secondaryColor;
            var secondaryPaletteIndex = -1;
            for (int i = 0; i < CompanyIdentityRules.Palette.Length; i++)
            {
                if (CompanyIdentityRules.Palette[i] != initialSecondaryColor)
                {
                    secondaryPaletteIndex = i;
                    break;
                }
            }

            Assert.That(secondaryPaletteIndex, Is.GreaterThanOrEqualTo(0));
            flagTab.Palette[secondaryPaletteIndex].Button.onClick.Invoke();
            Assert.AreEqual(
                CompanyIdentityRules.Palette[secondaryPaletteIndex],
                _harness.CurrentIdentity.secondaryColor);

            // Verify editor preview updated
            var previewBindings =
                _editorBindings.PreviewParent.GetComponentInChildren<CompanyIdentityPreviewViewBindings>();
            Assert.IsNotNull(previewBindings, "Preview bindings should exist");
            Assert.AreEqual(selectedPaletteColor, previewBindings.EmblemPanel.color);
            Assert.AreEqual("Test Company", previewBindings.CompanyNameText.text);

            // Click Done button
            var doneButton = _editorBindings.DoneButton;
            Assert.IsNotNull(doneButton, "Done button should be assigned");
            doneButton.onClick.Invoke();

            // Verify transition to Summary page
            Assert.AreEqual(CompanyIdentityPage.Summary, _harness.Page);

            // Click Edit Profile button
            var editButton = _summaryBindings.EditButton;
            Assert.IsNotNull(editButton, "Edit button should be assigned");
            editButton.onClick.Invoke();

            // Verify transition back to Editor
            Assert.AreEqual(CompanyIdentityPage.Editor, _harness.Page);
            Assert.IsNotNull(_harness.CurrentIdentity);

            // Return to Summary
            doneButton.onClick.Invoke();
            Assert.AreEqual(CompanyIdentityPage.Summary, _harness.Page);

            // Click Create Another Company button
            var resetButton = _summaryBindings.ResetButton;
            Assert.IsNotNull(resetButton, "Reset button should be assigned");
            resetButton.onClick.Invoke();

            // Verify reset to Creation page
            Assert.AreEqual(CompanyIdentityPage.Creation, _harness.Page);
            Assert.IsNull(_harness.CurrentIdentity);

            // Verify input field is empty
            Assert.AreEqual("", inputField.text);
        }
    }
}
