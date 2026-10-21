using TMPro;
using LitMotion;
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
        [Tooltip("Optional persistent selected state. Falls back to the serialized Button state.")]
        [SerializeField] Sprite filledSelected;
        [SerializeField] Color filledContentColor = Color.white;
        [SerializeField] Color filledDisabledContentColor = Color.white;

        [SerializeField] Sprite outlinedNormal;
        [SerializeField] Sprite outlinedHighlighted;
        [SerializeField] Sprite outlinedPressed;
        [SerializeField] Sprite outlinedDisabled;
        [Tooltip("Optional persistent selected state. Falls back to the serialized Button state.")]
        [SerializeField] Sprite outlinedSelected;
        [SerializeField] Color outlinedContentColor = Color.white;
        [SerializeField] Color outlinedDisabledContentColor = Color.white;

        [Header("Feedback")]
        [Min(0.01f)]
        [SerializeField] float feedbackDuration = 0.12f;
        [Min(1f)]
        [SerializeField] float highlightedScale = 1.02f;
        [Range(0.9f, 1f)]
        [SerializeField] float pressedScale = 0.98f;
        [Min(1f)]
        [SerializeField] float selectedScale = 1.01f;

        MotionHandle feedbackMotion;
        RectTransform rectTransform;
        Vector3 baseScale = Vector3.one;

        protected override void Awake()
        {
            base.Awake();
            rectTransform = transform as RectTransform;
            if (rectTransform != null)
            {
                baseScale = rectTransform.localScale;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Apply(true);
        }

        protected override void OnDisable()
        {
            CancelFeedback();
            if (rectTransform != null)
            {
                rectTransform.localScale = baseScale;
            }

            base.OnDisable();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            feedbackDuration = Mathf.Max(0.01f, feedbackDuration);
            highlightedScale = Mathf.Max(1f, highlightedScale);
            pressedScale = Mathf.Clamp(pressedScale, 0.9f, 1f);
            selectedScale = Mathf.Max(1f, selectedScale);
            Apply(false);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            ApplyContentColor(state == SelectionState.Disabled);
            ApplyScale(state, instant);
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
            var selected = filled ? filledSelected : outlinedSelected;
            sprites.selectedSprite = selected != null
                ? selected
                : (filled ? filledHighlighted : outlinedHighlighted);
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

        public void SetTreatment(UIButtonTreatment value)
        {
            treatment = value;
            Apply(false);
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

        void ApplyScale(SelectionState state, bool instant)
        {
            if (rectTransform == null)
            {
                return;
            }

            var multiplier = 1f;
            switch (state)
            {
                case SelectionState.Highlighted:
                    multiplier = highlightedScale;
                    break;
                case SelectionState.Pressed:
                    multiplier = pressedScale;
                    break;
                case SelectionState.Selected:
                    multiplier = selectedScale;
                    break;
            }

            var target = baseScale * multiplier;
            if (instant || !Application.isPlaying || feedbackDuration <= 0f)
            {
                CancelFeedback();
                rectTransform.localScale = target;
                return;
            }

            if ((rectTransform.localScale - target).sqrMagnitude <= 0.000001f)
            {
                return;
            }

            CancelFeedback();
            feedbackMotion = LMotion.Create(
                    rectTransform.localScale.x / Mathf.Max(0.0001f, baseScale.x),
                    multiplier,
                    feedbackDuration)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(value =>
                {
                    if (rectTransform != null)
                    {
                        rectTransform.localScale = baseScale * value;
                    }
                });
        }

        void CancelFeedback()
        {
            if (feedbackMotion.IsActive())
            {
                feedbackMotion.Cancel();
            }

            feedbackMotion = default;
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
