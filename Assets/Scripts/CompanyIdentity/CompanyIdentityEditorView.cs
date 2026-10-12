using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.CompanyIdentity
{
    public enum CompanyIdentityTab
    {
        Insignia,
        Icon,
        Flag
    }

    public sealed class CompanyIdentityEditorView
    {
        private static readonly string[] TabLabels = { "Insignia", "Icon", "Flag" };
        private static readonly string[] ColorSlotLabels = { "Primary", "Secondary", "Accent" };

        private GameObject _rootObject;
        private Transform _previewParent;
        private Button[] _tabButtons;
        private Image[] _tabImages;
        private GameObject[] _tabContentObjects;
        private Button[,] _optionButtons;
        private TMP_Text[,] _optionLabels;
        private Image[,] _optionImages;
        private Button[,] _colorSlotButtons;
        private Image[,] _colorSlotImages;
        private Outline[,] _colorSlotOutlines;
        private Button[,] _paletteButtons;
        private Image[,] _paletteImages;
        private Outline[,] _paletteOutlines;

        private CompanyIdentityTab _activeTab;
        private CompanyIdentityColorSlot _activeColorSlot;
        private CompanyIdentityData _currentIdentity;

        private Action<CompanyIdentityTab, int> _onOptionSelected;
        private Action<CompanyIdentityColorSlot, int> _onColorSelected;
        private Action _onDoneRequested;

        public Transform PreviewParent => _previewParent;

        public void Build(
            Transform parent,
            Action<CompanyIdentityTab, int> onOptionSelected,
            Action<CompanyIdentityColorSlot, int> onColorSelected,
            Action onDoneRequested)
        {
            _onOptionSelected = onOptionSelected;
            _onColorSelected = onColorSelected;
            _onDoneRequested = onDoneRequested;
            _activeTab = CompanyIdentityTab.Insignia;
            _activeColorSlot = CompanyIdentityColorSlot.Primary;

            var rootObject = CompanyIdentityUiFactory.InstantiatePrefab(
                "CompanyIdentityEditorView",
                parent);
            var bindings = rootObject.GetComponent<CompanyIdentityEditorViewBindings>();
            if (bindings == null)
            {
                Debug.LogError(
                    "CompanyIdentityEditorView: The prefab is missing " +
                    "CompanyIdentityEditorViewBindings.");
                UnityEngine.Object.Destroy(rootObject);
                return;
            }

            var tabBindings = bindings.Tabs;
            if (bindings.PreviewParent == null ||
                bindings.DoneButton == null ||
                tabBindings == null ||
                tabBindings.Length != TabLabels.Length)
            {
                Debug.LogError(
                    "CompanyIdentityEditorView: The prefab bindings have missing or invalid references.");
                UnityEngine.Object.Destroy(rootObject);
                return;
            }

            _rootObject = rootObject;
            _previewParent = bindings.PreviewParent;
            _tabButtons = new Button[TabLabels.Length];
            _tabImages = new Image[TabLabels.Length];
            _tabContentObjects = new GameObject[TabLabels.Length];
            _optionButtons = new Button[TabLabels.Length, 4];
            _optionLabels = new TMP_Text[TabLabels.Length, 4];
            _optionImages = new Image[TabLabels.Length, 4];
            _colorSlotButtons = new Button[TabLabels.Length, ColorSlotLabels.Length];
            _colorSlotImages = new Image[TabLabels.Length, ColorSlotLabels.Length];
            _colorSlotOutlines = new Outline[TabLabels.Length, ColorSlotLabels.Length];
            _paletteButtons = new Button[TabLabels.Length, CompanyIdentityRules.Palette.Length];
            _paletteImages = new Image[TabLabels.Length, CompanyIdentityRules.Palette.Length];
            _paletteOutlines = new Outline[TabLabels.Length, CompanyIdentityRules.Palette.Length];

            for (var tabIndex = 0; tabIndex < TabLabels.Length; tabIndex++)
            {
                var capturedTabIndex = tabIndex;
                var tabBinding = tabBindings[tabIndex];
                if (tabBinding == null ||
                    tabBinding.TabButton == null ||
                    tabBinding.TabImage == null ||
                    tabBinding.Content == null ||
                    tabBinding.Options == null ||
                    tabBinding.Options.Length != 4 ||
                    tabBinding.ColorSlots == null ||
                    tabBinding.ColorSlots.Length != ColorSlotLabels.Length ||
                    tabBinding.Palette == null ||
                    tabBinding.Palette.Length != CompanyIdentityRules.Palette.Length)
                {
                    Debug.LogError(
                        $"CompanyIdentityEditorView: The {TabLabels[tabIndex]} tab bindings are incomplete.");
                    UnityEngine.Object.Destroy(rootObject);
                    _rootObject = null;
                    return;
                }

                _tabButtons[tabIndex] = tabBinding.TabButton;
                _tabImages[tabIndex] = tabBinding.TabImage;
                _tabContentObjects[tabIndex] = tabBinding.Content;
                _tabButtons[tabIndex].onClick.AddListener(
                    () => OnTabClicked((CompanyIdentityTab)capturedTabIndex));

                for (var optionIndex = 0; optionIndex < 4; optionIndex++)
                {
                    var capturedOptionIndex = optionIndex;
                    var optionBinding = tabBinding.Options[optionIndex];
                    if (optionBinding == null ||
                        optionBinding.Button == null ||
                        optionBinding.Label == null ||
                        optionBinding.Image == null)
                    {
                        Debug.LogError(
                            $"CompanyIdentityEditorView: The {TabLabels[tabIndex]} option " +
                            $"{optionIndex} bindings are incomplete.");
                        UnityEngine.Object.Destroy(rootObject);
                        _rootObject = null;
                        return;
                    }

                    var optionButton = optionBinding.Button;
                    _optionButtons[tabIndex, optionIndex] = optionButton;
                    _optionLabels[tabIndex, optionIndex] = optionBinding.Label;
                    _optionImages[tabIndex, optionIndex] = optionBinding.Image;
                    optionButton.onClick.AddListener(
                        () => OnOptionClicked(capturedOptionIndex));
                }

                for (var slotIndex = 0; slotIndex < ColorSlotLabels.Length; slotIndex++)
                {
                    var capturedSlotIndex = slotIndex;
                    var slotBinding = tabBinding.ColorSlots[slotIndex];
                    if (slotBinding == null ||
                        slotBinding.Button == null ||
                        slotBinding.Image == null ||
                        slotBinding.Outline == null)
                    {
                        Debug.LogError(
                            $"CompanyIdentityEditorView: The {TabLabels[tabIndex]} color slot " +
                            $"{ColorSlotLabels[slotIndex]} bindings are incomplete.");
                        UnityEngine.Object.Destroy(rootObject);
                        _rootObject = null;
                        return;
                    }

                    var slotButton = slotBinding.Button;
                    _colorSlotButtons[tabIndex, slotIndex] = slotButton;
                    _colorSlotImages[tabIndex, slotIndex] = slotBinding.Image;
                    _colorSlotOutlines[tabIndex, slotIndex] = slotBinding.Outline;
                    SetWhiteButtonColors(slotButton);
                    slotButton.onClick.AddListener(
                        () => OnColorSlotClicked((CompanyIdentityColorSlot)capturedSlotIndex));
                }

                for (var paletteIndex = 0;
                     paletteIndex < CompanyIdentityRules.Palette.Length;
                     paletteIndex++)
                {
                    var capturedPaletteIndex = paletteIndex;
                    var paletteBinding = tabBinding.Palette[paletteIndex];
                    if (paletteBinding == null ||
                        paletteBinding.Button == null ||
                        paletteBinding.Image == null ||
                        paletteBinding.Outline == null)
                    {
                        Debug.LogError(
                            $"CompanyIdentityEditorView: The {TabLabels[tabIndex]} palette " +
                            $"{paletteIndex} bindings are incomplete.");
                        UnityEngine.Object.Destroy(rootObject);
                        _rootObject = null;
                        return;
                    }

                    var paletteButton = paletteBinding.Button;
                    _paletteButtons[tabIndex, paletteIndex] = paletteButton;
                    _paletteImages[tabIndex, paletteIndex] = paletteBinding.Image;
                    _paletteOutlines[tabIndex, paletteIndex] = paletteBinding.Outline;
                    SetWhiteButtonColors(paletteButton);
                    paletteButton.onClick.AddListener(
                        () => OnPaletteClicked(capturedPaletteIndex));
                }
            }

            var doneButton = bindings.DoneButton;
            doneButton.onClick.AddListener(() => _onDoneRequested?.Invoke());
            RefreshOptionGrid();
        }

        public void Refresh(CompanyIdentityData identity)
        {
            _currentIdentity = identity;
            RefreshOptionGrid();
        }

        public void SetVisible(bool visible)
        {
            if (_rootObject != null)
            {
                _rootObject.SetActive(visible);
            }
        }

        private void OnTabClicked(CompanyIdentityTab tab)
        {
            _activeTab = tab;
            for (var index = 0; index < _tabContentObjects.Length; index++)
            {
                _tabContentObjects[index].SetActive(index == (int)_activeTab);
            }

            RefreshOptionGrid();
        }

        private void OnOptionClicked(int optionIndex)
        {
            _onOptionSelected?.Invoke(_activeTab, optionIndex);
        }

        private void OnColorSlotClicked(CompanyIdentityColorSlot slot)
        {
            _activeColorSlot = slot;
            UpdateColorSlotHighlight((int)_activeTab);
            UpdatePaletteHighlight((int)_activeTab);
        }

        private void OnPaletteClicked(int paletteIndex)
        {
            _onColorSelected?.Invoke(_activeColorSlot, paletteIndex);
        }

        private void RefreshOptionGrid()
        {
            for (var tabIndex = 0; tabIndex < TabLabels.Length; tabIndex++)
            {
                RefreshTabContent(tabIndex);
            }

            UpdateTabHighlight();
        }

        private void RefreshTabContent(int tabIndex)
        {
            var tab = (CompanyIdentityTab)tabIndex;
            var optionCount = GetOptionCount(tab);
            var selectedIndex = GetSelectedOptionIndex(tab);

            for (var optionIndex = 0; optionIndex < _optionButtons.GetLength(1); optionIndex++)
            {
                var isAvailable = optionIndex < optionCount;
                _optionButtons[tabIndex, optionIndex].gameObject.SetActive(isAvailable);
                if (!isAvailable)
                {
                    continue;
                }

                _optionLabels[tabIndex, optionIndex].text = GetOptionLabel(tab, optionIndex);
                _optionImages[tabIndex, optionIndex].color =
                    optionIndex == selectedIndex
                        ? new Color(0.6f, 0.8f, 1f, 1f)
                        : new Color(0.9f, 0.9f, 0.9f, 1f);
            }

            if (_currentIdentity == null)
            {
                for (var paletteIndex = 0;
                     paletteIndex < CompanyIdentityRules.Palette.Length;
                     paletteIndex++)
                {
                    _paletteOutlines[tabIndex, paletteIndex].enabled = false;
                }

                return;
            }

            for (var slotIndex = 0; slotIndex < ColorSlotLabels.Length; slotIndex++)
            {
                var slot = (CompanyIdentityColorSlot)slotIndex;
                _colorSlotImages[tabIndex, slotIndex].color = _currentIdentity.GetColor(slot);
            }

            UpdateColorSlotHighlight(tabIndex);
            UpdatePaletteHighlight(tabIndex);
        }

        private int GetOptionCount(CompanyIdentityTab tab)
        {
            switch (tab)
            {
                case CompanyIdentityTab.Insignia:
                    return CompanyIdentityRules.InsigniaShapes.Length;
                case CompanyIdentityTab.Icon:
                    return CompanyIdentityRules.Icons.Length;
                case CompanyIdentityTab.Flag:
                    return CompanyIdentityRules.FlagPatterns.Length;
                default:
                    return 0;
            }
        }

        private string GetOptionLabel(CompanyIdentityTab tab, int optionIndex)
        {
            switch (tab)
            {
                case CompanyIdentityTab.Insignia:
                    return CompanyIdentityRules.GetDisplayName(
                        CompanyIdentityRules.InsigniaShapes[optionIndex]);
                case CompanyIdentityTab.Icon:
                    return CompanyIdentityRules.GetDisplayName(
                        CompanyIdentityRules.Icons[optionIndex]);
                case CompanyIdentityTab.Flag:
                    return CompanyIdentityRules.GetDisplayName(
                        CompanyIdentityRules.FlagPatterns[optionIndex]);
                default:
                    return string.Empty;
            }
        }

        private int GetSelectedOptionIndex(CompanyIdentityTab tab)
        {
            if (_currentIdentity == null)
            {
                return -1;
            }

            switch (tab)
            {
                case CompanyIdentityTab.Insignia:
                    return Array.IndexOf(
                        CompanyIdentityRules.InsigniaShapes,
                        _currentIdentity.insigniaShape);
                case CompanyIdentityTab.Icon:
                    return Array.IndexOf(CompanyIdentityRules.Icons, _currentIdentity.icon);
                case CompanyIdentityTab.Flag:
                    return Array.IndexOf(
                        CompanyIdentityRules.FlagPatterns,
                        _currentIdentity.flagPattern);
                default:
                    return -1;
            }
        }

        private void UpdateTabHighlight()
        {
            for (var index = 0; index < _tabButtons.Length; index++)
            {
                _tabImages[index].color = index == (int)_activeTab
                    ? new Color(0.6f, 0.8f, 1f, 1f)
                    : new Color(0.9f, 0.9f, 0.9f, 1f);
            }
        }

        private void UpdateColorSlotHighlight(int tabIndex)
        {
            for (var slotIndex = 0; slotIndex < ColorSlotLabels.Length; slotIndex++)
            {
                _colorSlotOutlines[tabIndex, slotIndex].enabled =
                    slotIndex == (int)_activeColorSlot;
            }
        }

        private void UpdatePaletteHighlight(int tabIndex)
        {
            if (_currentIdentity == null)
            {
                return;
            }

            var activeColor = _currentIdentity.GetColor(_activeColorSlot);
            for (var paletteIndex = 0;
                 paletteIndex < CompanyIdentityRules.Palette.Length;
                 paletteIndex++)
            {
                _paletteOutlines[tabIndex, paletteIndex].enabled =
                    CompanyIdentityRules.Palette[paletteIndex] == activeColor;
            }
        }

        private static void SetWhiteButtonColors(Button button)
        {
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            button.colors = colors;
        }
    }
}
