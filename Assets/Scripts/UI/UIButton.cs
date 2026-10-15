using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.UI
{
    public enum UIButtonTreatment
    {
        Filled = 0,
        Outlined = 1,
    }

    /// <summary>
    /// Blue or red button. The treatment dropdown swaps filled and outlined sprites.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Battle Royale/UI/Button")]
    public sealed class UIButton : Button
    {
        [SerializeField] UIButtonTreatment treatment;
        [Tooltip("Shown on the label. A screen can replace the text after the button enables.")]
        [SerializeField] string labelText = "Button";
        [SerializeField] bool showLabel = true;
        [SerializeField] Sprite iconSprite;
        [SerializeField] bool showIcon;

        [SerializeField] Image background;
        [SerializeField] Image icon;
        [SerializeField] LayoutElement iconLayout;
        [SerializeField] TMP_Text label;
        [SerializeField] LayoutElement labelLayout;

        [SerializeField] Sprite filledNormal;
        [SerializeField] Sprite filledHighlighted;
        [SerializeField] Sprite filledPressed;
        [SerializeField] Sprite filledDisabled;
        [SerializeField] Color filledContentColor = Color.white;
        [SerializeField] Color filledDisabledContentColor = Color.white;

        [SerializeField] Sprite outlinedNormal;
        [SerializeField] Sprite outlinedHighlighted;
        [SerializeField] Sprite outlinedPressed;
        [SerializeField] Sprite outlinedDisabled;
        [SerializeField] Color outlinedContentColor = Color.white;
        [SerializeField] Color outlinedDisabledContentColor = Color.white;

        protected override void OnEnable()
        {
            base.OnEnable();
            Apply(true);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            Apply(false);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            ApplyContentColor(state == SelectionState.Disabled);
        }

        void Apply(bool logMissing)
        {
            if (background == null || icon == null || iconLayout == null || label == null || labelLayout == null)
            {
                if (logMissing)
                {
                    Debug.LogError("UIButton: assign the background, icon, label, and layout elements.", this);
                }

                return;
            }

            if (!showLabel && !showIcon && logMissing)
            {
                Debug.LogError("UIButton: show the label, the icon, or both.", this);
            }

            if (!HasSprites())
            {
                if (logMissing)
                {
                    Debug.LogError("UIButton: assign the filled and outlined sprites.", this);
                }

                return;
            }

            var filled = treatment == UIButtonTreatment.Filled;
            background.sprite = filled ? filledNormal : outlinedNormal;
            background.type = Image.Type.Sliced;
            background.color = Color.white;
            background.raycastTarget = true;

            transition = Transition.SpriteSwap;
            targetGraphic = background;

            var sprites = spriteState;
            sprites.highlightedSprite = filled ? filledHighlighted : outlinedHighlighted;
            sprites.pressedSprite = filled ? filledPressed : outlinedPressed;
            sprites.selectedSprite = filled ? filledHighlighted : outlinedHighlighted;
            sprites.disabledSprite = filled ? filledDisabled : outlinedDisabled;
            spriteState = sprites;

            icon.sprite = iconSprite;
            icon.raycastTarget = false;
            icon.enabled = showIcon;
            iconLayout.ignoreLayout = !showIcon;

            label.raycastTarget = false;
            label.enabled = showLabel;
            labelLayout.ignoreLayout = !showLabel;
            var caption = labelText ?? string.Empty;
            if (label.text != caption)
            {
                label.text = caption;
            }

            if (isActiveAndEnabled)
            {
                DoStateTransition(
                    IsInteractable() ? SelectionState.Normal : SelectionState.Disabled,
                    true);
            }
            else
            {
                ApplyContentColor(!IsInteractable());
            }
        }

        public void SetLabel(string value)
        {
            labelText = value ?? string.Empty;
            if (label != null)
            {
                label.text = labelText;
            }
        }

        void ApplyContentColor(bool disabled)
        {
            if (label == null || icon == null)
            {
                return;
            }

            var filled = treatment == UIButtonTreatment.Filled;
            var color = filled
                ? (disabled ? filledDisabledContentColor : filledContentColor)
                : (disabled ? outlinedDisabledContentColor : outlinedContentColor);

            if (label.color != color)
            {
                label.color = color;
            }

            if (icon.color != color)
            {
                icon.color = color;
            }
        }

        bool HasSprites()
        {
            return filledNormal != null
                && filledHighlighted != null
                && filledPressed != null
                && filledDisabled != null
                && outlinedNormal != null
                && outlinedHighlighted != null
                && outlinedPressed != null
                && outlinedDisabled != null;
        }
    }
}
