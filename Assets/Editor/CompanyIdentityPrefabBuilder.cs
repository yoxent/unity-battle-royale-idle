using System;
using TMPro;
using BattleRoyale.CompanyIdentity;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BattleRoyale.CompanyIdentity.Editor
{
    internal static class CompanyIdentityPrefabBuilder
    {
        private const string RootFolder = "Assets/Resources/CompanyIdentity/Ui";
        private const string TypeRoleFolder = RootFolder + "/TypeRoles";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string TextBasePrefabPath =
            TypeRoleFolder + "/CompanyIdentityTextBase.prefab";
        private const string ButtonTextPrefabPath =
            TypeRoleFolder + "/CompanyIdentityTextButton.prefab";
        private const string TitleTextPrefabPath =
            TypeRoleFolder + "/CompanyIdentityTextTitle.prefab";
        private const string BodyTextPrefabPath =
            TypeRoleFolder + "/CompanyIdentityTextBody.prefab";
        private const string CaptionTextPrefabPath =
            TypeRoleFolder + "/CompanyIdentityTextCaption.prefab";
        private const string PlaceholderTextPrefabPath =
            TypeRoleFolder + "/CompanyIdentityTextPlaceholder.prefab";
        private const string ButtonPrefabPath =
            TypeRoleFolder + "/CompanyIdentityButton.prefab";

        [MenuItem("Tools/Company Identity/Create UI Prefabs")]
        public static void Build()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                Debug.LogError($"Company Identity prefab build could not find {FontPath}.");
                return;
            }

            EnsureFolder("Assets/Resources", "Assets", "Resources");
            EnsureFolder("Assets/Resources/CompanyIdentity", "Assets/Resources", "CompanyIdentity");
            EnsureFolder(RootFolder, "Assets/Resources/CompanyIdentity", "Ui");
            EnsureFolder(TypeRoleFolder, RootFolder, "TypeRoles");

            BuildTextRolePrefabs(font);
            BuildButtonPrefab();
            BuildCanvasPrefab();
            BuildEventSystemPrefab();
            BuildPreviewPrefab();
            BuildCreationPrefab();
            BuildSummaryPrefab();
            BuildEditorPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Company Identity UI prefabs created.");
        }

        private static void EnsureFolder(string path, string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
                if (!AssetDatabase.IsValidFolder(path))
                {
                    throw new InvalidOperationException(
                        $"Could not create Company Identity prefab folder '{path}'.");
                }
            }
        }

        private static GameObject New(string name, Transform parent = null)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            SetRect(gameObject, 0f, 0f, 1f, 1f);
            return gameObject;
        }

        private static void SetRect(
            GameObject gameObject,
            float minX,
            float minY,
            float maxX,
            float maxY,
            float left = 0f,
            float bottom = 0f,
            float right = 0f,
            float top = 0f)
        {
            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(minX, minY);
            rectTransform.anchorMax = new Vector2(maxX, maxY);
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private static Image AddImage(GameObject gameObject, Color color)
        {
            var image = gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text AddText(
            GameObject parent,
            string rolePrefabPath,
            string value,
            TextAlignmentOptions alignment,
            Color color,
            string childName = "Text",
            float left = 0f,
            float bottom = 0f,
            float right = 0f,
            float top = 0f)
        {
            var rolePrefab = LoadPrefab(rolePrefabPath);
            var textObject = PrefabUtility.InstantiatePrefab(rolePrefab, parent.transform) as GameObject;
            if (textObject == null)
            {
                throw new InvalidOperationException(
                    $"Could not instantiate text role prefab at {rolePrefabPath}.");
            }

            textObject.name = childName;
            SetRect(textObject, 0f, 0f, 1f, 1f, left, bottom, right, top);

            var text = textObject.GetComponent<TMP_Text>();
            if (text == null)
            {
                throw new InvalidOperationException(
                    $"Text role prefab '{rolePrefabPath}' is missing a TMP_Text component.");
            }

            text.text = value;
            text.alignment = alignment;
            text.color = color;
            return text;
        }

        private static Button AddButton(
            Transform parent,
            string name,
            string label,
            bool showLabel)
        {
            var buttonPrefab = LoadPrefab(ButtonPrefabPath);
            var buttonObject = PrefabUtility.InstantiatePrefab(buttonPrefab, parent) as GameObject;
            if (buttonObject == null)
            {
                throw new InvalidOperationException(
                    $"Could not instantiate the shared button prefab at {ButtonPrefabPath}.");
            }

            buttonObject.name = name;
            var button = buttonObject.GetComponent<Button>();
            var labelText = buttonObject.GetComponentInChildren<TMP_Text>();
            if (button == null || labelText == null)
            {
                throw new InvalidOperationException(
                    "The shared Company Identity button prefab is missing its Button or label.");
            }

            labelText.text = showLabel ? label : string.Empty;
            return button;
        }

        private static void ConfigureButton(Button button)
        {
            var colors = button.colors;
            colors.normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.selectedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;
        }

        private static void Save(GameObject root, string fileName)
        {
            SaveAtPath(root, $"{RootFolder}/{fileName}.prefab");
        }

        private static void SaveAtPath(GameObject root, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            bool success;
            PrefabUtility.SaveAsPrefabAsset(root, path, out success);
            UnityEngine.Object.DestroyImmediate(root);
            if (!success)
            {
                Debug.LogError($"Failed to save Company Identity prefab at {path}.");
            }
        }

        private static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Company Identity prefab asset '{path}' was not found.");
            }

            return prefab;
        }

        private static void BuildTextRolePrefabs(TMP_FontAsset font)
        {
            var baseRoot = New("CompanyIdentityTextBase");
            var baseText = baseRoot.AddComponent<TextMeshProUGUI>();
            baseText.font = font;
            baseText.fontSize = 14f;
            baseText.fontStyle = FontStyles.Normal;
            baseText.alignment = TextAlignmentOptions.Center;
            baseText.color = Color.black;
            baseText.textWrappingMode = TextWrappingModes.NoWrap;
            baseText.raycastTarget = false;
            SaveAtPath(baseRoot, TextBasePrefabPath);

            var basePrefab = LoadPrefab(TextBasePrefabPath);
            BuildTextRolePrefab(
                basePrefab,
                "CompanyIdentityTextButton",
                ButtonTextPrefabPath,
                14f,
                FontStyles.Normal);
            BuildTextRolePrefab(
                basePrefab,
                "CompanyIdentityTextTitle",
                TitleTextPrefabPath,
                24f,
                FontStyles.Normal);
            BuildTextRolePrefab(
                basePrefab,
                "CompanyIdentityTextBody",
                BodyTextPrefabPath,
                14f,
                FontStyles.Normal);
            BuildTextRolePrefab(
                basePrefab,
                "CompanyIdentityTextCaption",
                CaptionTextPrefabPath,
                12f,
                FontStyles.Normal);
            BuildTextRolePrefab(
                basePrefab,
                "CompanyIdentityTextPlaceholder",
                PlaceholderTextPrefabPath,
                14f,
                FontStyles.Italic);
        }

        private static void BuildTextRolePrefab(
            GameObject basePrefab,
            string name,
            string path,
            float fontSize,
            FontStyles fontStyle)
        {
            var root = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"Could not instantiate the base text prefab at {TextBasePrefabPath}.");
            }

            root.name = name;
            var text = root.GetComponent<TMP_Text>();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            SaveAtPath(root, path);
        }

        private static void BuildButtonPrefab()
        {
            var root = New("CompanyIdentityButton");
            var image = AddImage(root, new Color(0.9f, 0.9f, 0.9f, 1f));
            var button = root.AddComponent<Button>();
            button.targetGraphic = image;
            ConfigureButton(button);

            var outline = root.AddComponent<Outline>();
            outline.effectColor = Color.yellow;
            outline.effectDistance = new Vector2(3f, 3f);
            outline.enabled = false;

            var label = PrefabUtility.InstantiatePrefab(
                LoadPrefab(ButtonTextPrefabPath),
                root.transform) as GameObject;
            if (label == null)
            {
                throw new InvalidOperationException(
                    $"Could not instantiate the button text role at {ButtonTextPrefabPath}.");
            }

            label.name = "Label";
            SetRect(label, 0f, 0f, 1f, 1f, 5f, 2f, 5f, 2f);
            SaveAtPath(root, ButtonPrefabPath);
        }

        private static void BuildCanvasPrefab()
        {
            var gameObject = new GameObject("CompanyIdentityCanvas");
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440f, 900f);
            gameObject.AddComponent<GraphicRaycaster>();

            Save(gameObject, "CompanyIdentityCanvas");
        }

        private static void BuildEventSystemPrefab()
        {
            var gameObject = new GameObject("EventSystem");
            gameObject.AddComponent<EventSystem>();
            gameObject.AddComponent<InputSystemUIInputModule>();
            Save(gameObject, "CompanyIdentityEventSystem");
        }

        private static void BuildPreviewPrefab()
        {
            var root = New("CompanyIdentityPreview");

            var companyName = New("CompanyName", root.transform);
            SetRect(companyName, 0f, 0.85f, 1f, 1f, 10f, 0f, 10f, 10f);
            var companyNameText = AddText(
                companyName,
                TitleTextPrefabPath,
                string.Empty,
                TextAlignmentOptions.Center,
                Color.black);

            var emblem = New("EmblemPanel", root.transform);
            SetRect(emblem, 0.25f, 0.45f, 0.75f, 0.8f);
            var emblemImage = AddImage(emblem, Color.white);

            var shapeLabel = AddPreviewLabel(
                root.transform,
                "ShapeLabel",
                0f,
                0.35f,
                0.33f,
                0.4f);
            var iconLabel = AddPreviewLabel(
                root.transform,
                "IconLabel",
                0.33f,
                0.35f,
                0.67f,
                0.4f);
            var flagLabel = AddPreviewLabel(
                root.transform,
                "FlagLabel",
                0.67f,
                0.35f,
                1f,
                0.4f);

            var primaryColorBar = AddPreviewBar(root.transform, "PrimaryColorBar", 0.25f);
            var secondaryColorBar = AddPreviewBar(root.transform, "SecondaryColorBar", 0.15f);
            var accentColorBar = AddPreviewBar(root.transform, "AccentColorBar", 0.05f);

            var bindings = root.AddComponent<CompanyIdentityPreviewViewBindings>();
            bindings.SetReferences(
                companyNameText,
                emblemImage,
                shapeLabel,
                iconLabel,
                flagLabel,
                primaryColorBar,
                secondaryColorBar,
                accentColorBar);

            Save(root, "CompanyIdentityPreview");
        }

        private static TMP_Text AddPreviewLabel(
            Transform parent,
            string name,
            float minX,
            float minY,
            float maxX,
            float maxY)
        {
            var label = New(name, parent);
            SetRect(label, minX, minY, maxX, maxY, 5f, 0f, 5f, 0f);
            return AddText(
                label,
                CaptionTextPrefabPath,
                string.Empty,
                TextAlignmentOptions.Center,
                Color.black);
        }

        private static Image AddPreviewBar(Transform parent, string name, float minY)
        {
            var bar = New(name, parent);
            SetRect(bar, 0.1f, minY, 0.9f, minY + 0.05f);
            return AddImage(bar, Color.white);
        }

        private static void BuildCreationPrefab()
        {
            var root = New("CompanyIdentityCreationView");
            var panel = New("Panel", root.transform);
            SetRect(panel, 0.3f, 0.3f, 0.7f, 0.7f);
            AddImage(panel, new Color(0.95f, 0.95f, 0.95f, 1f));

            var title = New("Title", panel.transform);
            SetRect(title, 0.1f, 0.75f, 0.9f, 0.9f);
            AddText(
                title,
                TitleTextPrefabPath,
                "Create Your Company",
                TextAlignmentOptions.Center,
                Color.black);

            var inputWrapper = New("CompanyNameInput", panel.transform);
            SetRect(inputWrapper, 0.1f, 0.55f, 0.9f, 0.65f);
            var companyNameInput = BuildInputField(inputWrapper.transform);

            var validation = New("ValidationText", panel.transform);
            SetRect(validation, 0.1f, 0.45f, 0.9f, 0.52f);
            var validationText = AddText(
                validation,
                CaptionTextPrefabPath,
                string.Empty,
                TextAlignmentOptions.Center,
                new Color(0.8f, 0.2f, 0.2f, 1f));

            var createWrapper = New("CreateButton", panel.transform);
            SetRect(createWrapper, 0.3f, 0.2f, 0.7f, 0.35f);
            var createCompanyButton = AddButton(
                createWrapper.transform,
                "Button",
                "Create Company",
                true);

            var bindings = root.AddComponent<CompanyIdentityCreationViewBindings>();
            bindings.SetReferences(companyNameInput, validationText, createCompanyButton);

            Save(root, "CompanyIdentityCreationView");
        }

        private static TMP_InputField BuildInputField(Transform parent)
        {
            var inputObject = New("InputField", parent);
            AddImage(inputObject, Color.white);

            var input = inputObject.AddComponent<TMP_InputField>();
            input.characterLimit = 32;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.targetGraphic = inputObject.GetComponent<Image>();

            var inputText = AddText(
                inputObject,
                BodyTextPrefabPath,
                string.Empty,
                TextAlignmentOptions.MidlineLeft,
                Color.black,
                "Text",
                5f,
                2f,
                5f,
                2f);

            var placeholder = AddText(
                inputObject,
                PlaceholderTextPrefabPath,
                "Enter company name...",
                TextAlignmentOptions.MidlineLeft,
                new Color(0.5f, 0.5f, 0.5f, 1f),
                "Placeholder",
                5f,
                2f,
                5f,
                2f);

            input.textComponent = inputText;
            input.placeholder = placeholder;
            return input;
        }

        private static void BuildSummaryPrefab()
        {
            var root = New("CompanyIdentitySummaryView");
            var panel = New("Panel", root.transform);
            SetRect(panel, 0.25f, 0.2f, 0.75f, 0.8f);
            AddImage(panel, new Color(0.95f, 0.95f, 0.95f, 1f));

            var title = New("Title", panel.transform);
            SetRect(title, 0.1f, 0.85f, 0.9f, 0.95f);
            AddText(
                title,
                TitleTextPrefabPath,
                "Company Profile Created",
                TextAlignmentOptions.Center,
                Color.black);

            var previewParent = New("PreviewParent", panel.transform);
            SetRect(previewParent, 0.1f, 0.45f, 0.9f, 0.8f);

            var companyName = New("CompanyName", panel.transform);
            SetRect(companyName, 0.1f, 0.35f, 0.9f, 0.42f);
            var companyNameText = AddText(
                companyName,
                TitleTextPrefabPath,
                string.Empty,
                TextAlignmentOptions.Center,
                Color.black);

            var editButton = AddSummaryButton(
                panel.transform,
                "EditButton",
                "Edit Profile",
                0.2f,
                0.3f);
            var resetButton = AddSummaryButton(
                panel.transform,
                "ResetButton",
                "Create Another Company",
                0.08f,
                0.18f);

            var bindings = root.AddComponent<CompanyIdentitySummaryViewBindings>();
            bindings.SetReferences(previewParent.transform, companyNameText, editButton, resetButton);

            Save(root, "CompanyIdentitySummaryView");
        }

        private static Button AddSummaryButton(
            Transform parent,
            string name,
            string label,
            float minY,
            float maxY)
        {
            var wrapper = New(name, parent);
            SetRect(wrapper, 0.15f, minY, 0.85f, maxY);
            return AddButton(wrapper.transform, "Button", label, true);
        }

        private static void BuildEditorPrefab()
        {
            var root = New("CompanyIdentityEditorView");

            var previewColumn = New("PreviewColumn", root.transform);
            SetRect(previewColumn, 0f, 0f, 0.5f, 1f, 10f, 10f, 10f, 10f);
            AddImage(previewColumn, new Color(0.9f, 0.9f, 0.9f, 1f));

            var previewParent = New("PreviewParent", previewColumn.transform);
            SetRect(previewParent, 0.1f, 0.1f, 0.9f, 0.9f);

            var controls = New("ControlsColumn", root.transform);
            SetRect(controls, 0.5f, 0f, 1f, 1f, 10f, 10f, 10f, 10f);
            AddImage(controls, new Color(0.97f, 0.97f, 0.97f, 1f));

            var tabRow = New("TabRow", controls.transform);
            SetRect(tabRow, 0f, 0.88f, 1f, 0.95f);
            var tabLabels = new[] { "Insignia", "Icon", "Flag" };
            var tabButtons = new Button[tabLabels.Length];
            for (var i = 0; i < tabLabels.Length; i++)
            {
                var tabWrapper = New($"TabButton_{tabLabels[i]}", tabRow.transform);
                SetRect(tabWrapper, i / 3f, 0f, (i + 1) / 3f, 1f, 2f, 0f, 2f, 0f);
                tabButtons[i] = AddButton(
                    tabWrapper.transform,
                    "Button",
                    tabLabels[i],
                    true);
            }

            var contentRoot = New("TabContent", controls.transform);
            SetRect(contentRoot, 0f, 0.16f, 1f, 0.85f);
            var tabBindings = new CompanyIdentityEditorTabBinding[tabLabels.Length];
            for (var tab = 0; tab < tabLabels.Length; tab++)
            {
                tabBindings[tab] = BuildTabContent(
                    contentRoot.transform,
                    tabLabels[tab],
                    tab == 0,
                    tabButtons[tab]);
            }

            var doneWrapper = New("DoneButton", controls.transform);
            SetRect(doneWrapper, 0.25f, 0.05f, 0.75f, 0.15f);
            var done = AddButton(doneWrapper.transform, "Button", "Done", true);

            var bindings = root.AddComponent<CompanyIdentityEditorViewBindings>();
            bindings.SetReferences(previewParent.transform, done, tabBindings);

            Save(root, "CompanyIdentityEditorView");
        }

        private static CompanyIdentityEditorTabBinding BuildTabContent(
            Transform parent,
            string tabName,
            bool active,
            Button tabButton)
        {
            var content = New($"{tabName}Content", parent);
            SetRect(content, 0f, 0f, 1f, 1f);
            content.SetActive(active);

            var grid = New("OptionGridContainer", content.transform);
            SetRect(grid, 0f, 0.6f, 1f, 0.96f);
            var optionBindings = new CompanyIdentityEditorOptionBinding[4];
            for (var index = 0; index < 4; index++)
            {
                var row = index / 2;
                var column = index % 2;
                var optionWrapper = New($"OptionButton_{index}", grid.transform);
                SetRect(
                    optionWrapper,
                    column * 0.5f,
                    1f - (row + 1) * 0.5f,
                    (column + 1) * 0.5f,
                    1f - row * 0.5f,
                    5f,
                    5f,
                    5f,
                    5f);
                var option = AddButton(
                    optionWrapper.transform,
                    "Button",
                    string.Empty,
                    true);
                optionBindings[index] = new CompanyIdentityEditorOptionBinding();
                optionBindings[index].SetReferences(
                    option,
                    option.GetComponentInChildren<TMP_Text>(),
                    option.GetComponent<Image>());
            }

            var divider = New("Divider", content.transform);
            SetRect(divider, 0.1f, 0.56f, 0.9f, 0.58f);
            AddImage(divider, new Color(0.5f, 0.5f, 0.5f, 1f));

            var slotRow = New("ColorSlotRow", content.transform);
            SetRect(slotRow, 0f, 0.39f, 1f, 0.54f);
            var slots = new[] { "Primary", "Secondary", "Accent" };
            var colorSlotBindings = new CompanyIdentityEditorSwatchBinding[slots.Length];
            for (var slot = 0; slot < slots.Length; slot++)
            {
                var colorSlot = AddButton(
                    slotRow.transform,
                    $"ColorSlotButton_{slots[slot]}",
                    slots[slot],
                    true);
                SetRect(
                    colorSlot.gameObject,
                    slot / 3f,
                    0f,
                    (slot + 1) / 3f,
                    1f,
                    5f,
                    0f,
                    5f,
                    0f);
                colorSlotBindings[slot] = CreateSwatchBinding(colorSlot);
            }

            var palette = New("PaletteRow", content.transform);
            SetRect(palette, 0f, 0.2f, 1f, 0.35f);
            var paletteBindings = new CompanyIdentityEditorSwatchBinding[6];
            for (var color = 0; color < 6; color++)
            {
                var paletteOption = AddButton(
                    palette.transform,
                    $"PaletteButton_{color}",
                    string.Empty,
                    false);
                SetRect(
                    paletteOption.gameObject,
                    color / 6f,
                    0f,
                    (color + 1) / 6f,
                    1f,
                    3f,
                    0f,
                    3f,
                    0f);
                paletteBindings[color] = CreateSwatchBinding(paletteOption);
            }

            var tabBinding = new CompanyIdentityEditorTabBinding();
            tabBinding.SetReferences(
                tabButton,
                tabButton.GetComponent<Image>(),
                content,
                optionBindings,
                colorSlotBindings,
                paletteBindings);
            return tabBinding;
        }

        private static CompanyIdentityEditorSwatchBinding CreateSwatchBinding(Button button)
        {
            var binding = new CompanyIdentityEditorSwatchBinding();
            binding.SetReferences(
                button,
                button.GetComponent<Image>(),
                button.GetComponent<Outline>());
            return binding;
        }
    }
}
