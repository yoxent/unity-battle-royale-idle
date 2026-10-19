using System;
using System.Collections.Generic;
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
        private Button _previousBodyButton;
        private Button _nextBodyButton;
        private TMP_Text _bodyIndexLabel;
        private Image _bodySpriteImage;
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

        private bool _emblemPaint;
        private bool _symbolPage;
        private EmblemPaintTarget _shapePaint = EmblemPaintTarget.Body;
        private GameObject _shapeOptions;
        private GameObject _symbolOptions;
        private Image _shapeTabImage;
        private Image _symbolTabImage;
        private Image _bodyPaintImage;
        private Image _borderPaintImage;
        private TMP_Text _symbolIndexLabel;
        private Slider _positionXSlider;
        private Slider _positionYSlider;
        private Slider _rotationSlider;
        private Slider _scaleSlider;
        private bool _suppressSliders;
        private Image[] _emblemSwatchImages;
        private GameObject[] _emblemSwatchMarkers;

        private Action<CompanyIdentityTab, int> _onOptionSelected;
        private Action<CompanyIdentityColorSlot, int> _onColorSelected;
        private Action<int> _onBodyStep;
        private Action _onEmblemEdited;
        private Action _onDoneRequested;

        public Transform PreviewParent => _previewParent;

        public void Initialize(
            CompanyIdentityEditorViewBindings bindings,
            Action<CompanyIdentityTab, int> onOptionSelected,
            Action<CompanyIdentityColorSlot, int> onColorSelected,
            Action<int> onBodyStep,
            Action onEmblemEdited,
            Action onDoneRequested)
        {
            _onOptionSelected = onOptionSelected;
            _onColorSelected = onColorSelected;
            _onBodyStep = onBodyStep;
            _onEmblemEdited = onEmblemEdited;
            _onDoneRequested = onDoneRequested;
            _activeTab = CompanyIdentityTab.Insignia;
            _activeColorSlot = CompanyIdentityColorSlot.Primary;

            if (bindings == null)
            {
                Debug.LogError(
                    "CompanyIdentityEditorView: An editor view binding reference must be assigned.");
                return;
            }

            var tabBindings = bindings.Tabs;
            if (bindings.PreviewParent == null ||
                bindings.DoneButton == null ||
                bindings.PreviousBodyButton == null ||
                bindings.NextBodyButton == null ||
                bindings.BodyIndexLabel == null ||
                tabBindings == null ||
                tabBindings.Length != TabLabels.Length)
            {
                Debug.LogError(
                    "CompanyIdentityEditorView: The scene binding has missing or invalid references.");
                return;
            }

            _rootObject = bindings.gameObject;
            _previewParent = bindings.PreviewParent;
            _previousBodyButton = bindings.PreviousBodyButton;
            _nextBodyButton = bindings.NextBodyButton;
            _bodyIndexLabel = bindings.BodyIndexLabel;
            _bodySpriteImage = bindings.BodySpriteImage;
            if (_bodySpriteImage != null)
            {
                _bodySpriteImage.preserveAspect = true;
                _bodyIndexLabel.gameObject.SetActive(false);
            }

            _previousBodyButton.onClick.AddListener(() => _onBodyStep?.Invoke(-1));
            _nextBodyButton.onClick.AddListener(() => _onBodyStep?.Invoke(1));
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
                        _rootObject = null;
                        return;
                    }

                    var paletteButton = paletteBinding.Button;
                    _paletteButtons[tabIndex, paletteIndex] = paletteButton;
                    _paletteImages[tabIndex, paletteIndex] = paletteBinding.Image;
                    _paletteOutlines[tabIndex, paletteIndex] = paletteBinding.Outline;
                    paletteBinding.Image.color = CompanyIdentityRules.Palette[paletteIndex];
                    SetWhiteButtonColors(paletteButton);
                    paletteButton.onClick.AddListener(
                        () => OnPaletteClicked(capturedPaletteIndex));
                }
            }

            if (!TryWireEmblem(bindings))
            {
                _optionButtons = null;
                _rootObject = null;
                return;
            }

            var doneButton = bindings.DoneButton;
            doneButton.onClick.AddListener(() => _onDoneRequested?.Invoke());
            RefreshOptionGrid();
        }

        public void Refresh(CompanyIdentityData identity, IReadOnlyList<Sprite> bodySprites = null)
        {
            _currentIdentity = identity;
            if (_bodyIndexLabel != null)
            {
                _bodyIndexLabel.text = identity == null ? "0" : identity.bodyIndex.ToString();
            }

            if (_bodySpriteImage != null)
            {
                _bodySpriteImage.sprite = BodySprite(identity, bodySprites);
            }

            RefreshOptionGrid();
            RefreshEmblemControls();
        }

        private static Sprite BodySprite(CompanyIdentityData identity, IReadOnlyList<Sprite> bodySprites)
        {
            if (identity == null || bodySprites == null || bodySprites.Count <= 0)
            {
                return null;
            }

            var index = CompanyIdentityRules.StepBodyIndex(identity.bodyIndex, 0, bodySprites.Count);
            return bodySprites[index];
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
            if (_emblemPaint)
            {
                if (_currentIdentity == null ||
                    paletteIndex < 0 ||
                    paletteIndex >= CompanyIdentityRules.Palette.Length)
                {
                    return;
                }

                CompanyIdentityRules.Paint(
                    _currentIdentity,
                    ActivePaint,
                    CompanyIdentityRules.Palette[paletteIndex]);
                _onEmblemEdited?.Invoke();
                return;
            }

            _onColorSelected?.Invoke(_activeColorSlot, paletteIndex);
        }

        private void RefreshOptionGrid()
        {
            if (_optionButtons == null)
            {
                return;
            }

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

            var activeColor = _emblemPaint
                ? CompanyIdentityRules.PaintedColor(_currentIdentity, ActivePaint)
                : _currentIdentity.GetColor(_activeColorSlot);
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

        private EmblemPaintTarget ActivePaint =>
            _symbolPage ? EmblemPaintTarget.Symbol : _shapePaint;

        private bool TryWireEmblem(CompanyIdentityEditorViewBindings bindings)
        {
            if (bindings.ShapeTabButton == null)
            {
                return true;
            }

            if (bindings.SymbolTabButton == null ||
                bindings.ShapeTabImage == null ||
                bindings.SymbolTabImage == null ||
                bindings.ShapeOptions == null ||
                bindings.SymbolOptions == null ||
                bindings.BodyPaintButton == null ||
                bindings.BorderPaintButton == null ||
                bindings.BodyPaintImage == null ||
                bindings.BorderPaintImage == null ||
                bindings.PreviousSymbolButton == null ||
                bindings.NextSymbolButton == null ||
                bindings.SymbolIndexLabel == null ||
                bindings.PositionXSlider == null ||
                bindings.PositionYSlider == null ||
                bindings.RotationSlider == null ||
                bindings.ScaleSlider == null ||
                bindings.ResetSymbolButton == null)
            {
                Debug.LogError(
                    "CompanyIdentityEditorView: The shape and symbol bindings are incomplete.");
                return false;
            }

            _emblemPaint = true;
            _shapeOptions = bindings.ShapeOptions;
            _symbolOptions = bindings.SymbolOptions;
            _shapeTabImage = bindings.ShapeTabImage;
            _symbolTabImage = bindings.SymbolTabImage;
            _bodyPaintImage = bindings.BodyPaintImage;
            _borderPaintImage = bindings.BorderPaintImage;
            _symbolIndexLabel = bindings.SymbolIndexLabel;
            _positionXSlider = bindings.PositionXSlider;
            _positionYSlider = bindings.PositionYSlider;
            _rotationSlider = bindings.RotationSlider;
            _scaleSlider = bindings.ScaleSlider;

            bindings.ShapeTabButton.onClick.AddListener(() => ShowEmblemPage(false));
            bindings.SymbolTabButton.onClick.AddListener(() => ShowEmblemPage(true));
            bindings.BodyPaintButton.onClick.AddListener(() =>
            {
                _shapePaint = EmblemPaintTarget.Body;
                RefreshEmblemControls();
            });
            bindings.BorderPaintButton.onClick.AddListener(() =>
            {
                _shapePaint = EmblemPaintTarget.Border;
                RefreshEmblemControls();
            });
            bindings.PreviousSymbolButton.onClick.AddListener(() => StepSymbol(-1));
            bindings.NextSymbolButton.onClick.AddListener(() => StepSymbol(1));
            bindings.ResetSymbolButton.onClick.AddListener(ResetSymbol);
            BindAxis(_positionXSlider, CompanyIdentityRules.SymbolPositionMin, CompanyIdentityRules.SymbolPositionMax, value =>
            {
                var position = _currentIdentity.symbolPosition;
                position.x = CompanyIdentityRules.ClampSymbolPosition(value);
                _currentIdentity.symbolPosition = position;
            });
            BindAxis(_positionYSlider, CompanyIdentityRules.SymbolPositionMin, CompanyIdentityRules.SymbolPositionMax, value =>
            {
                var position = _currentIdentity.symbolPosition;
                position.y = CompanyIdentityRules.ClampSymbolPosition(value);
                _currentIdentity.symbolPosition = position;
            });
            BindAxis(_rotationSlider, CompanyIdentityRules.SymbolRotationMin, CompanyIdentityRules.SymbolRotationMax, value =>
            {
                _currentIdentity.symbolRotation = CompanyIdentityRules.ClampSymbolRotation(value);
            });
            BindAxis(_scaleSlider, CompanyIdentityRules.SymbolScaleMin, CompanyIdentityRules.SymbolScaleMax, value =>
            {
                _currentIdentity.symbolScale = CompanyIdentityRules.ClampSymbolScale(value);
            });
            return TryWireSwatches(bindings);
        }

        private bool TryWireSwatches(CompanyIdentityEditorViewBindings bindings)
        {
            var swatches = bindings.EmblemSwatches;
            if (swatches == null || swatches.Length == 0)
            {
                return true;
            }

            if (swatches.Length != CompanyIdentityRules.EmblemPalette.Length)
            {
                Debug.LogError(
                    "CompanyIdentityEditorView: The emblem palette does not match its colors.");
                return false;
            }

            _emblemSwatchImages = new Image[swatches.Length];
            _emblemSwatchMarkers = new GameObject[swatches.Length];
            for (var index = 0; index < swatches.Length; index++)
            {
                var capturedIndex = index;
                var swatch = swatches[index];
                if (swatch == null ||
                    swatch.Button == null ||
                    swatch.Image == null ||
                    swatch.Marker == null)
                {
                    Debug.LogError(
                        "CompanyIdentityEditorView: An emblem palette swatch is incomplete.");
                    return false;
                }

                _emblemSwatchImages[index] = swatch.Image;
                _emblemSwatchMarkers[index] = swatch.Marker;
                swatch.Image.color = CompanyIdentityRules.EmblemPalette[index];
                SetWhiteButtonColors(swatch.Button);
                swatch.Button.onClick.AddListener(() => PaintEmblem(capturedIndex));
            }

            return true;
        }

        private void PaintEmblem(int paletteIndex)
        {
            if (_currentIdentity == null ||
                paletteIndex < 0 ||
                paletteIndex >= CompanyIdentityRules.EmblemPalette.Length)
            {
                return;
            }

            CompanyIdentityRules.Paint(
                _currentIdentity,
                ActivePaint,
                CompanyIdentityRules.EmblemPalette[paletteIndex]);
            _onEmblemEdited?.Invoke();
        }

        private void RefreshEmblemSwatches()
        {
            if (_emblemSwatchImages == null)
            {
                return;
            }

            var active = _currentIdentity == null
                ? default(Color)
                : CompanyIdentityRules.PaintedColor(_currentIdentity, ActivePaint);
            for (var index = 0; index < _emblemSwatchImages.Length; index++)
            {
                _emblemSwatchImages[index].color = CompanyIdentityRules.EmblemPalette[index];
                _emblemSwatchMarkers[index].SetActive(
                    _currentIdentity != null &&
                    CompanyIdentityRules.EmblemPalette[index] == active);
            }
        }

        private void BindAxis(Slider slider, float min, float max, Action<float> apply)
        {
            slider.minValue = min;
            slider.maxValue = max;
            slider.onValueChanged.AddListener(value =>
            {
                if (_suppressSliders || _currentIdentity == null)
                {
                    return;
                }

                apply(value);
                _onEmblemEdited?.Invoke();
            });
        }

        private void ShowEmblemPage(bool symbol)
        {
            _symbolPage = symbol;
            RefreshEmblemControls();
        }

        private void StepSymbol(int direction)
        {
            if (_currentIdentity == null)
            {
                return;
            }

            _currentIdentity.symbolIndex = CompanyIdentityRules.StepBodyIndex(
                _currentIdentity.symbolIndex,
                direction,
                0);
            _onEmblemEdited?.Invoke();
        }

        private void ResetSymbol()
        {
            if (_currentIdentity == null)
            {
                return;
            }

            CompanyIdentityRules.ResetSymbolTransform(_currentIdentity);
            _onEmblemEdited?.Invoke();
        }

        private void RefreshEmblemControls()
        {
            if (!_emblemPaint)
            {
                return;
            }

            _shapeOptions.SetActive(!_symbolPage);
            _symbolOptions.SetActive(_symbolPage);
            var selected = new Color(0.6f, 0.8f, 1f, 1f);
            var idle = new Color(0.9f, 0.9f, 0.9f, 1f);
            _shapeTabImage.color = _symbolPage ? idle : selected;
            _symbolTabImage.color = _symbolPage ? selected : idle;
            _bodyPaintImage.color = _shapePaint == EmblemPaintTarget.Body ? selected : Color.white;
            _borderPaintImage.color = _shapePaint == EmblemPaintTarget.Border ? selected : Color.white;

            if (_currentIdentity == null)
            {
                _symbolIndexLabel.text = "0";
                return;
            }

            _symbolIndexLabel.text = _currentIdentity.symbolIndex.ToString();
            _suppressSliders = true;
            _positionXSlider.SetValueWithoutNotify(_currentIdentity.symbolPosition.x);
            _positionYSlider.SetValueWithoutNotify(_currentIdentity.symbolPosition.y);
            _rotationSlider.SetValueWithoutNotify(_currentIdentity.symbolRotation);
            _scaleSlider.SetValueWithoutNotify(_currentIdentity.symbolScale);
            _suppressSliders = false;

            for (var tabIndex = 0; tabIndex < TabLabels.Length; tabIndex++)
            {
                UpdatePaletteHighlight(tabIndex);
            }

            RefreshEmblemSwatches();
        }
    }
}
