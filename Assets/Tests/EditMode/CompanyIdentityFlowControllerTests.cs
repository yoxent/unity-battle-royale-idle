using BattleRoyale.CompanyIdentity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class CompanyIdentityFlowControllerTests
    {
        private GameObject _companyIdentityObject;
        private CompanyIdentityFlowController _controller;
        private CompanyIdentityCreationViewBindings _creationBindings;
        private CompanyIdentityEditorViewBindings _editorBindings;
        private CompanyIdentitySummaryViewBindings _summaryBindings;

        [SetUp]
        public void SetUp()
        {
            var prefab = LoadPrefab(
                "Assets/UI/Identity/CompanyIdentity.prefab");
            _companyIdentityObject = Object.Instantiate(prefab);
            _companyIdentityObject.name = prefab.name;

            _controller = _companyIdentityObject.GetComponent<CompanyIdentityFlowController>();
            _creationBindings =
                _companyIdentityObject.GetComponentInChildren<CompanyIdentityCreationViewBindings>(true);
            _editorBindings =
                _companyIdentityObject.GetComponentInChildren<CompanyIdentityEditorViewBindings>(true);
            _summaryBindings =
                _companyIdentityObject.GetComponentInChildren<CompanyIdentitySummaryViewBindings>(true);
            _controller.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            if (_companyIdentityObject != null)
                Object.DestroyImmediate(_companyIdentityObject);
        }

        private static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, $"Expected prefab at {path}");
            return prefab;
        }

        private static GameObject InstantiatePrefab(string path, Transform parent = null)
        {
            var prefab = LoadPrefab(path);
            var instance = Object.Instantiate(prefab, parent, false);
            instance.name = prefab.name;
            return instance;
        }

        [Test]
        public void FullFlowFromCreationToSummaryAndBack()
        {
            // Verify initial state: Creation page
            Assert.AreEqual(CompanyIdentityPage.Creation, _controller.Page);
            Assert.IsNull(_controller.CurrentIdentity);

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
            Assert.AreEqual(CompanyIdentityPage.Editor, _controller.Page);
            Assert.IsNotNull(_controller.CurrentIdentity);
            Assert.AreEqual(
                "Test Company",
                _controller.CurrentIdentity.companyName);

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
            Assert.AreEqual(
                CompanyIdentityRules.InsigniaShapes[1],
                _controller.CurrentIdentity.insigniaShape);

            // Click Icon tab
            iconTab.TabButton.onClick.Invoke();

            // Click the third Icon option (index 2)
            Assert.IsNotNull(iconTab.Options, "Icon options should be assigned");
            Assert.That(iconTab.Options.Length, Is.EqualTo(4));
            iconTab.Options[2].Button.onClick.Invoke();
            Assert.AreEqual(
                CompanyIdentityRules.Icons[2],
                _controller.CurrentIdentity.icon);

            var initialPrimaryColor = _controller.CurrentIdentity.primaryColor;
            var selectedPaletteIndex = -1;
            for (var i = 0; i < CompanyIdentityRules.Palette.Length; i++)
            {
                if (CompanyIdentityRules.Palette[i] != initialPrimaryColor)
                {
                    selectedPaletteIndex = i;
                    break;
                }
            }

            Assert.That(
                selectedPaletteIndex,
                Is.GreaterThanOrEqualTo(0),
                "A palette color different from the initial primary color should exist");
            var selectedPaletteColor = CompanyIdentityRules.Palette[selectedPaletteIndex];
            Assert.IsNotNull(iconTab.Palette, "Icon palette should be assigned");
            Assert.That(
                iconTab.Palette.Length,
                Is.EqualTo(CompanyIdentityRules.Palette.Length));
            iconTab.Palette[selectedPaletteIndex].Button.onClick.Invoke();

            // Verify identity color changed (Primary is the default active slot)
            Assert.AreEqual(
                selectedPaletteColor,
                _controller.CurrentIdentity.primaryColor);
            Assert.AreNotEqual(
                initialPrimaryColor,
                _controller.CurrentIdentity.primaryColor);

            // The Flag tab owns its own color controls as well.
            flagTab.TabButton.onClick.Invoke();
            Assert.IsNotNull(flagTab.ColorSlots, "Flag tab should contain color slots");
            Assert.That(flagTab.ColorSlots.Length, Is.EqualTo(3));
            Assert.IsNotNull(flagTab.Palette, "Flag tab should contain a color palette");
            Assert.That(
                flagTab.Palette.Length,
                Is.EqualTo(CompanyIdentityRules.Palette.Length));
            flagTab.ColorSlots[(int)CompanyIdentityColorSlot.Secondary].Button.onClick.Invoke();

            var initialSecondaryColor = _controller.CurrentIdentity.secondaryColor;
            var secondaryPaletteIndex = -1;
            for (var i = 0; i < CompanyIdentityRules.Palette.Length; i++)
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
                _controller.CurrentIdentity.secondaryColor);

            // Verify editor preview updated
            var previewBindings =
                _editorBindings.PreviewParent.GetComponentInChildren<CompanyIdentityPreviewViewBindings>();
            Assert.IsNotNull(previewBindings, "Preview bindings should exist");
            Assert.AreEqual(selectedPaletteColor, previewBindings.PrimaryColorBar.color);
            Assert.AreEqual(CompanyIdentityRules.BodyFillColor, previewBindings.EmblemPanel.color);
            var border = previewBindings.transform.Find("Emblem/BodyBorder").GetComponent<Image>();
            Assert.AreEqual(CompanyIdentityRules.BodyBorderColor, border.color);
            Assert.AreEqual("Test Company", previewBindings.CompanyNameText.text);

            _editorBindings.NextBodyButton.onClick.Invoke();
            Assert.AreEqual(1, _controller.CurrentIdentity.bodyIndex);
            Assert.AreEqual("1", _editorBindings.BodyIndexLabel.text);
            Assert.AreEqual("emblem_body_fill_1", previewBindings.EmblemPanel.sprite.name);

            _editorBindings.PreviousBodyButton.onClick.Invoke();
            Assert.AreEqual(0, _controller.CurrentIdentity.bodyIndex);
            _editorBindings.PreviousBodyButton.onClick.Invoke();
            Assert.AreEqual(
                previewBindings.BodyPairCount - 1,
                _controller.CurrentIdentity.bodyIndex);

            // Click Done button
            var doneButton = _editorBindings.DoneButton;
            Assert.IsNotNull(doneButton, "Done button should be assigned");
            doneButton.onClick.Invoke();

            // Verify transition to Summary page
            Assert.AreEqual(CompanyIdentityPage.Summary, _controller.Page);

            // Click Edit Profile button
            var editButton = _summaryBindings.EditButton;
            Assert.IsNotNull(editButton, "Edit button should be assigned");
            editButton.onClick.Invoke();

            // Verify transition back to Editor
            Assert.AreEqual(CompanyIdentityPage.Editor, _controller.Page);
            Assert.IsNotNull(_controller.CurrentIdentity);

            // Return to Summary
            doneButton.onClick.Invoke();
            Assert.AreEqual(CompanyIdentityPage.Summary, _controller.Page);

            // Click Create Another Company button
            var resetButton = _summaryBindings.ResetButton;
            Assert.IsNotNull(resetButton, "Reset button should be assigned");
            resetButton.onClick.Invoke();

            // Verify reset to Creation page
            Assert.AreEqual(CompanyIdentityPage.Creation, _controller.Page);
            Assert.IsNull(_controller.CurrentIdentity);

            // Verify input field is empty
            Assert.AreEqual(string.Empty, inputField.text);
        }

        [Test]
        public void PreviewHousesEmblemLayers()
        {
            var preview = InstantiatePrefab(
                "Assets/UI/Identity/CompanyIdentityPreview.prefab");
            try
            {
                AssertEmblemLayers(preview.transform);
            }
            finally
            {
                Object.DestroyImmediate(preview);
            }

            AssertEmblemLayers(
                _editorBindings.PreviewParent
                    .GetComponentInChildren<CompanyIdentityPreviewViewBindings>(true)
                    .transform);
            AssertEmblemLayers(
                _summaryBindings.PreviewParent
                    .GetComponentInChildren<CompanyIdentityPreviewViewBindings>(true)
                    .transform);
        }

        private static void AssertEmblemLayers(Transform root)
        {
            var emblem = root.Find("Emblem");
            Assert.IsNotNull(emblem, "Emblem layer root should exist");

            var fill = emblem.Find("BodyFill");
            var stripes = emblem.Find("BodyFill/Stripes");
            var border = emblem.Find("BodyBorder");
            Assert.IsNotNull(fill, "BodyFill should exist");
            Assert.IsNotNull(stripes, "Stripes should exist");
            Assert.IsNotNull(emblem.Find("BodyFill/Stripes/Stripe01"));
            Assert.IsNotNull(emblem.Find("BodyFill/Stripes/Stripe02"));
            Assert.IsNotNull(emblem.Find("BodyFill/Stripes/Stripe03"));
            Assert.IsNotNull(border, "BodyBorder should exist");
            Assert.IsNotNull(fill.GetComponent<Mask>());
            Assert.IsNotNull(stripes.GetComponent<VerticalLayoutGroup>());
            Assert.AreEqual(0f, emblem.Find("BodyFill/Stripes/Stripe02").GetComponent<CanvasGroup>().alpha);

            var fillImage = fill.GetComponent<Image>();
            var borderImage = border.GetComponent<Image>();
            Assert.AreEqual("emblem_body_fill_5", fillImage.sprite.name);
            Assert.AreEqual("emblem_body_border_5", borderImage.sprite.name);

            var bindings = root.GetComponent<CompanyIdentityPreviewViewBindings>();
            if (bindings == null)
            {
                bindings = root.GetComponentInChildren<CompanyIdentityPreviewViewBindings>(true);
            }

            Assert.AreEqual(fillImage, bindings.EmblemPanel);
        }
    }
}
