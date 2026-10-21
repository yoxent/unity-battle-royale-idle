using System;
using System.Collections.Generic;
using BattleRoyale.Company;
using BattleRoyale.CompanyIdentity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace BattleRoyale.UI.EditorTools
{
    static class PresentationWiringValidator
    {
        const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        const string IdentityPrefabPath = "Assets/UI/Identity/CompanyIdentity.prefab";
        const string PreviewPrefabPath =
            "Assets/UI/Identity/CompanyIdentityPreview.prefab";

        [MenuItem("Tools/Battle Royale/Validate UI Wiring")]
        static void ValidateUiWiring()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "UI wiring validation must run while the editor is not entering Play Mode.");
                return;
            }

            var report = new ValidationReport();
            ValidatePrefab(IdentityPrefabPath, ValidateIdentityPrefab, report);
            ValidatePrefab(PreviewPrefabPath, ValidatePreviewPrefab, report);
            ValidateScene(MainMenuScenePath, ValidateMainMenuScene, report);
            ValidateScene(SampleScenePath, ValidateSampleScene, report);

            if (report.ErrorCount == 0)
            {
                Debug.Log(
                    $"UI wiring validation passed for {report.CheckedAssetCount} assets.");
                return;
            }

            Debug.LogError(
                $"UI wiring validation found {report.ErrorCount} error(s) across " +
                $"{report.CheckedAssetCount} assets.");
        }

        static void ValidatePrefab(
            string path,
            Action<GameObject, ValidationReport> validate,
            ValidationReport report)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                report.Error(path, "Prefab is missing.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                report.Error(path, "Prefab contents could not be loaded.");
                return;
            }

            report.CheckedAssetCount++;
            try
            {
                validate(root, report);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void ValidateScene(
            string path,
            Action<Scene, ValidationReport> validate,
            ValidationReport report)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (sceneAsset == null)
            {
                report.Error(path, "Scene is missing.");
                return;
            }

            var scene = SceneManager.GetSceneByPath(path);
            var openedByValidator = false;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                openedByValidator = scene.IsValid() && scene.isLoaded;
            }

            if (!scene.IsValid() || !scene.isLoaded)
            {
                report.Error(path, "Scene could not be loaded.");
                return;
            }

            report.CheckedAssetCount++;
            try
            {
                validate(scene, report);
            }
            finally
            {
                if (openedByValidator)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        static void ValidateIdentityPrefab(GameObject root, ValidationReport report)
        {
            var scope = IdentityPrefabPath;
            ValidateTextStyles(root, scope, report);
            var controller = root.GetComponentInChildren<CompanyIdentityFlowController>(true);
            if (controller == null)
            {
                report.Error(scope, "CompanyIdentityFlowController is missing.");
            }
            else
            {
                var serialized = new SerializedObject(controller);
                RequireReference(serialized, "_creationViewBindings", scope, report);
                RequireReference(serialized, "_editorViewBindings", scope, report);
                RequireReference(serialized, "_summaryViewBindings", scope, report);
                RequireReference(serialized, "_editorPreviewBindings", scope, report);
                RequireReference(serialized, "_summaryPreviewBindings", scope, report);
            }

            var editorBindings =
                root.GetComponentInChildren<CompanyIdentityEditorViewBindings>(true);
            ValidateEditorBindings(editorBindings, scope, report);

            var previews =
                root.GetComponentsInChildren<CompanyIdentityPreviewViewBindings>(true);
            if (previews.Length == 0)
            {
                report.Error(scope, "At least one identity preview binding is required.");
                return;
            }

            for (var i = 0; i < previews.Length; i++)
            {
                ValidateFullPreviewBindings(previews[i], $"{scope} preview {i}", report);
            }
        }

        static void ValidatePreviewPrefab(GameObject root, ValidationReport report)
        {
            var scope = PreviewPrefabPath;
            ValidateTextStyles(root, scope, report);
            var binding =
                root.GetComponentInChildren<CompanyIdentityPreviewViewBindings>(true);
            if (binding == null)
            {
                report.Error(scope, "CompanyIdentityPreviewViewBindings is missing.");
                return;
            }

            Require(binding.EmblemPanel, "EmblemPanel", scope, report);
            if (binding.BodyPairCount <= 0)
            {
                report.Error(scope, "At least one paired body fill/border sprite is required.");
            }
        }

        static void ValidateMainMenuScene(Scene scene, ValidationReport report)
        {
            const string scope = MainMenuScenePath;
            var roots = scene.GetRootGameObjects();
            var canvasFound = false;
            for (var i = 0; i < roots.Length; i++)
            {
                ValidateTextStyles(roots[i], scope, report);
                var canvas = roots[i].GetComponent<Canvas>();
                if (canvas == null || canvas.gameObject.name != "Canvas")
                {
                    continue;
                }

                canvasFound = true;
                ValidateCanvasScaler(canvas, scope, report);
            }

            if (!canvasFound)
            {
                report.Error(scope, "Root Canvas named 'Canvas' is missing.");
            }
        }

        static void ValidateCanvasScaler(
            Canvas canvas,
            string scope,
            ValidationReport report)
        {
            var canvasScaler = canvas.GetComponent<CanvasScaler>();
            if (canvasScaler == null)
            {
                report.Error(
                    scope,
                    $"Canvas '{canvas.gameObject.name}' needs a CanvasScaler " +
                    "for resolution-independent layout.");
                return;
            }

            if (canvasScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                report.Error(
                    scope,
                    $"Canvas '{canvas.gameObject.name}' CanvasScaler uses " +
                    $"{canvasScaler.uiScaleMode}; expected ScaleWithScreenSize.");
            }

            if (canvasScaler.referenceResolution.x <= 0f ||
                canvasScaler.referenceResolution.y <= 0f)
            {
                report.Error(
                    scope,
                    $"Canvas '{canvas.gameObject.name}' reference resolution is invalid: " +
                    $"{canvasScaler.referenceResolution}.");
            }
        }

        static void ValidateSampleScene(Scene scene, ValidationReport report)
        {
            const string scope = SampleScenePath;
            var roots = scene.GetRootGameObjects();
            var canvasFound = false;
            for (var i = 0; i < roots.Length; i++)
            {
                ValidateTextStyles(roots[i], scope, report);
                var canvas = roots[i].GetComponent<Canvas>();
                if (canvas == null || canvas.gameObject.name != "Dashboard")
                {
                    continue;
                }

                canvasFound = true;
                ValidateCanvasScaler(canvas, scope, report);
            }

            if (!canvasFound)
            {
                report.Error(scope, "Root Canvas named 'Dashboard' is missing.");
            }

            var controllers = ComponentsInScene<CompanySessionController>(scene);
            if (controllers.Count == 0)
            {
                report.Error(scope, "CompanySessionController is missing.");
                return;
            }

            if (controllers.Count > 1)
            {
                report.Error(
                    scope,
                    $"Expected one CompanySessionController, found {controllers.Count}.");
            }

            for (var i = 0; i < controllers.Count; i++)
            {
                ValidateSessionController(controllers[i], scope, report);
            }
        }

        static void ValidateTextStyles(
            GameObject root,
            string scope,
            ValidationReport report)
        {
            var styles = root.GetComponentsInChildren<UITextStyle>(true);
            for (var i = 0; i < styles.Length; i++)
            {
                var serialized = new SerializedObject(styles[i]);
                var themeProperty = RequireReference(serialized, "theme", scope, report);
                RequireReference(serialized, "label", scope, report);
                var styleIdProperty = serialized.FindProperty("styleId");
                if (styleIdProperty == null || string.IsNullOrWhiteSpace(styleIdProperty.stringValue))
                {
                    report.Error(scope, "UITextStyle has no styleId.");
                    continue;
                }

                var theme = themeProperty == null
                    ? null
                    : themeProperty.objectReferenceValue as UITextTheme;
                if (theme == null || !theme.TryGet(styleIdProperty.stringValue, out var style))
                {
                    report.Error(
                        scope,
                        $"UITextStyle references missing style '{styleIdProperty.stringValue}'.");
                    continue;
                }

                if (style.font == null)
                {
                    report.Error(
                        scope,
                        $"UITextStyle style '{styleIdProperty.stringValue}' has no font.");
                }
            }
        }

        static void ValidateSessionController(
            CompanySessionController controller,
            string scope,
            ValidationReport report)
        {
            var serialized = new SerializedObject(controller);
            var deploymentProperty =
                RequireReference(serialized, "deployment", scope, report);
            var offersProperty = serialized.FindProperty("matchOffers");
            if (offersProperty == null || !offersProperty.isArray)
            {
                report.Error(scope, "matchOffers must be a serialized array.");
            }
            else
            {
                if (offersProperty.arraySize < 4)
                {
                    report.Error(
                        scope,
                        $"matchOffers has {offersProperty.arraySize} entries; at least 4 are required.");
                }

                for (var i = 0; i < offersProperty.arraySize; i++)
                {
                    var offerProperty = offersProperty.GetArrayElementAtIndex(i);
                    var offer = offerProperty.objectReferenceValue as MatchOfferView;
                    if (offer == null)
                    {
                        report.Error(scope, $"matchOffers[{i}] is missing.");
                        continue;
                    }

                    RequireReference(
                        new SerializedObject(offer),
                        "company",
                        $"{scope} matchOffers[{i}]",
                        report);
                }
            }

            var deployment =
                deploymentProperty == null
                    ? null
                    : deploymentProperty.objectReferenceValue as DeploymentPanel;
            if (deployment != null)
            {
                RequireReference(
                    new SerializedObject(deployment),
                    "company",
                    $"{scope} deployment",
                    report);
            }
        }

        static void ValidateEditorBindings(
            CompanyIdentityEditorViewBindings bindings,
            string scope,
            ValidationReport report)
        {
            if (bindings == null)
            {
                report.Error(scope, "CompanyIdentityEditorViewBindings is missing.");
                return;
            }

            Require(bindings.PreviewParent, "PreviewParent", scope, report);
            Require(bindings.DoneButton, "DoneButton", scope, report);
            Require(bindings.PreviousBodyButton, "PreviousBodyButton", scope, report);
            Require(bindings.NextBodyButton, "NextBodyButton", scope, report);
            Require(bindings.BodyIndexLabel, "BodyIndexLabel", scope, report);
            Require(bindings.BodySpriteImage, "BodySpriteImage", scope, report);
            Require(bindings.ShapeTabButton, "ShapeTabButton", scope, report);
            Require(bindings.SymbolTabButton, "SymbolTabButton", scope, report);
            Require(bindings.ShapeTabImage, "ShapeTabImage", scope, report);
            Require(bindings.SymbolTabImage, "SymbolTabImage", scope, report);
            Require(bindings.ShapeOptions, "ShapeOptions", scope, report);
            Require(bindings.SymbolOptions, "SymbolOptions", scope, report);
            Require(bindings.BodyPaintButton, "BodyPaintButton", scope, report);
            Require(bindings.BorderPaintButton, "BorderPaintButton", scope, report);
            Require(bindings.BodyPaintImage, "BodyPaintImage", scope, report);
            Require(bindings.BorderPaintImage, "BorderPaintImage", scope, report);
            Require(bindings.PreviousSymbolButton, "PreviousSymbolButton", scope, report);
            Require(bindings.NextSymbolButton, "NextSymbolButton", scope, report);
            Require(bindings.SymbolIndexLabel, "SymbolIndexLabel", scope, report);
            Require(bindings.PositionXSlider, "PositionXSlider", scope, report);
            Require(bindings.PositionYSlider, "PositionYSlider", scope, report);
            Require(bindings.RotationSlider, "RotationSlider", scope, report);
            Require(bindings.ScaleSlider, "ScaleSlider", scope, report);
            Require(bindings.ResetSymbolButton, "ResetSymbolButton", scope, report);

            if (bindings.EmblemSwatches == null ||
                bindings.EmblemSwatches.Length != CompanyIdentityRules.EmblemPalette.Length)
            {
                report.Error(
                    scope,
                    $"EmblemSwatches must contain {CompanyIdentityRules.EmblemPalette.Length} entries.");
            }
            else
            {
                for (var i = 0; i < bindings.EmblemSwatches.Length; i++)
                {
                    var swatch = bindings.EmblemSwatches[i];
                    if (swatch == null)
                    {
                        report.Error(scope, $"EmblemSwatches[{i}] is missing.");
                        continue;
                    }

                    Require(swatch.Button, $"EmblemSwatches[{i}].Button", scope, report);
                    Require(swatch.Image, $"EmblemSwatches[{i}].Image", scope, report);
                    Require(swatch.Outline, $"EmblemSwatches[{i}].Outline", scope, report);
                    Require(swatch.Marker, $"EmblemSwatches[{i}].Marker", scope, report);
                }
            }

            if (bindings.Tabs == null || bindings.Tabs.Length != 3)
            {
                report.Error(scope, "Tabs must contain exactly 3 entries.");
                return;
            }

            for (var tabIndex = 0; tabIndex < bindings.Tabs.Length; tabIndex++)
            {
                var tab = bindings.Tabs[tabIndex];
                var tabScope = $"{scope} tab {tabIndex}";
                if (tab == null)
                {
                    report.Error(tabScope, "Tab binding is missing.");
                    continue;
                }

                Require(tab.TabButton, "TabButton", tabScope, report);
                Require(tab.TabImage, "TabImage", tabScope, report);
                Require(tab.Content, "Content", tabScope, report);
                ValidateOptions(tab.Options, tabScope, report);
                ValidateColorSlots(tab.ColorSlots, tabScope, report);
                ValidatePalette(tab.Palette, tabScope, report);
            }
        }

        static void ValidateFullPreviewBindings(
            CompanyIdentityPreviewViewBindings bindings,
            string scope,
            ValidationReport report)
        {
            Require(bindings.CompanyNameText, "CompanyNameText", scope, report);
            Require(bindings.EmblemPanel, "EmblemPanel", scope, report);
            Require(bindings.ShapeLabel, "ShapeLabel", scope, report);
            Require(bindings.IconLabel, "IconLabel", scope, report);
            Require(bindings.FlagLabel, "FlagLabel", scope, report);
            Require(bindings.PrimaryColorBar, "PrimaryColorBar", scope, report);
            Require(bindings.SecondaryColorBar, "SecondaryColorBar", scope, report);
            Require(bindings.AccentColorBar, "AccentColorBar", scope, report);
            if (bindings.BodyPairCount <= 0)
            {
                report.Error(scope, "At least one paired body fill/border sprite is required.");
            }
        }

        static void ValidateOptions(
            CompanyIdentityEditorOptionBinding[] options,
            string scope,
            ValidationReport report)
        {
            if (options == null || options.Length != 4)
            {
                report.Error(scope, "Options must contain exactly 4 entries.");
                return;
            }

            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                if (option == null)
                {
                    report.Error(scope, $"Options[{i}] is missing.");
                    continue;
                }

                Require(option.Button, $"Options[{i}].Button", scope, report);
                Require(option.Label, $"Options[{i}].Label", scope, report);
                Require(option.Image, $"Options[{i}].Image", scope, report);
            }
        }

        static void ValidateColorSlots(
            CompanyIdentityEditorSwatchBinding[] slots,
            string scope,
            ValidationReport report)
        {
            if (slots == null || slots.Length != 3)
            {
                report.Error(scope, "ColorSlots must contain exactly 3 entries.");
                return;
            }

            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null)
                {
                    report.Error(scope, $"ColorSlots[{i}] is missing.");
                    continue;
                }

                Require(slot.Button, $"ColorSlots[{i}].Button", scope, report);
                Require(slot.Image, $"ColorSlots[{i}].Image", scope, report);
                Require(slot.Outline, $"ColorSlots[{i}].Outline", scope, report);
            }
        }

        static void ValidatePalette(
            CompanyIdentityEditorSwatchBinding[] palette,
            string scope,
            ValidationReport report)
        {
            if (palette == null || palette.Length != CompanyIdentityRules.Palette.Length)
            {
                report.Error(
                    scope,
                    $"Palette must contain {CompanyIdentityRules.Palette.Length} entries.");
                return;
            }

            for (var i = 0; i < palette.Length; i++)
            {
                var swatch = palette[i];
                if (swatch == null)
                {
                    report.Error(scope, $"Palette[{i}] is missing.");
                    continue;
                }

                Require(swatch.Button, $"Palette[{i}].Button", scope, report);
                Require(swatch.Image, $"Palette[{i}].Image", scope, report);
                Require(swatch.Outline, $"Palette[{i}].Outline", scope, report);
            }
        }

        static SerializedProperty RequireReference(
            SerializedObject serialized,
            string propertyName,
            string scope,
            ValidationReport report)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                report.Error(scope, $"Serialized field '{propertyName}' is missing.");
                return null;
            }

            if (property.propertyType != SerializedPropertyType.ObjectReference ||
                property.objectReferenceValue == null)
            {
                report.Error(scope, $"Serialized reference '{propertyName}' is missing.");
            }

            return property;
        }

        static void Require(
            UnityEngine.Object value,
            string name,
            string scope,
            ValidationReport report)
        {
            if (value == null)
            {
                report.Error(scope, $"Reference '{name}' is missing.");
            }
        }

        static List<T> ComponentsInScene<T>(Scene scene) where T : Component
        {
            var components = new List<T>();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                components.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return components;
        }

        sealed class ValidationReport
        {
            public int ErrorCount { get; private set; }
            public int CheckedAssetCount { get; set; }

            public void Error(string scope, string message)
            {
                ErrorCount++;
                Debug.LogError($"UI wiring validation [{scope}]: {message}");
            }
        }
    }
}
