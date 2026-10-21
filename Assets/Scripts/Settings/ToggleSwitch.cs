using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Settings
{
    public sealed class ToggleSwitch : MonoBehaviour
    {
        const float SpriteSwapStart = 0.85f;

        [SerializeField] Toggle toggle;
        [SerializeField] RectTransform knob;
        [SerializeField] Image background;
        [SerializeField] float knobSize = 40f;
        [SerializeField] float padding = 8f;
        [SerializeField] float duration = 0.15f;
        [SerializeField] Color onColor = new Color(0.29f, 0.78f, 0.55f, 1f);
        [SerializeField] Color offColor = Color.white;
        [SerializeField] Sprite onSprite;
        [SerializeField] Sprite offSprite;
        [SerializeField] CanvasGroup onLabelCanvasGroup;
        [SerializeField] CanvasGroup offLabelCanvasGroup;

        readonly CompositeMotionHandle motion = new CompositeMotionHandle();
        bool subscribed;

        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!HasReferences())
            {
                return;
            }

            toggle.onValueChanged.AddListener(HandleChanged);
            subscribed = true;
            Apply(toggle.isOn, false);
        }

        void Start()
        {
            if (!subscribed)
            {
                return;
            }

            Apply(toggle.isOn, false);
        }

        void OnDisable()
        {
            if (subscribed)
            {
                toggle.onValueChanged.RemoveListener(HandleChanged);
                subscribed = false;
            }

            motion.Cancel();
        }

        public void SetOn(bool isOn, bool animate)
        {
            if (toggle == null)
            {
                Debug.LogError("ToggleSwitch: assign a toggle.", this);
                return;
            }

            toggle.SetIsOnWithoutNotify(isOn);
            Apply(isOn, animate);
        }

        void HandleChanged(bool isOn)
        {
            Apply(isOn, true);
        }

        void Apply(bool isOn, bool animate)
        {
            if (!HasReferences())
            {
                return;
            }

            motion.Cancel();
            ApplyKnobSize();
            if (!animate || duration <= 0f)
            {
                RebuildLayout();
            }

            float knobX = ToggleSwitchLayout.KnobX(Width(), knobSize, padding, isOn);
            float onAlpha = isOn ? 1f : 0f;
            float offAlpha = isOn ? 0f : 1f;
            Sprite sprite = isOn ? onSprite : offSprite;
            Color color = isOn ? onColor : offColor;
            if (!animate || duration <= 0f)
            {
                Place(knobX, onAlpha, offAlpha, sprite, color);
                return;
            }

            motion.Add(LMotion.Create(knob.anchoredPosition.x, knobX, duration)
                .WithEase(Ease.OutBack)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToAnchoredPositionX(knob));
            motion.Add(LMotion.Create(onLabelCanvasGroup.alpha, onAlpha, duration)
                .WithEase(Ease.InOutSine)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToAlpha(onLabelCanvasGroup));
            motion.Add(LMotion.Create(offLabelCanvasGroup.alpha, offAlpha, duration)
                .WithEase(Ease.InOutSine)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToAlpha(offLabelCanvasGroup));
            motion.Add(LMotion.Create(background.color, color, duration)
                .WithEase(Ease.InOutSine)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToColor(background));

            float swapDelay = duration * SpriteSwapStart;
            motion.Add(LMotion.Create(0f, 1f, duration - swapDelay)
                .WithDelay(swapDelay)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(new SpriteSwap(background, sprite), static (value, swap) => swap.image.sprite = swap.sprite));
        }

        void Place(float knobX, float onAlpha, float offAlpha, Sprite sprite, Color color)
        {
            Vector2 position = knob.anchoredPosition;
            position.x = knobX;
            knob.anchoredPosition = position;
            onLabelCanvasGroup.alpha = onAlpha;
            offLabelCanvasGroup.alpha = offAlpha;
            background.sprite = sprite;
            background.color = color;
        }

        void ApplyKnobSize()
        {
            knob.sizeDelta = new Vector2(knobSize, knobSize);
        }

        void RebuildLayout()
        {
            var rect = (RectTransform)transform;
            if (rect.parent is RectTransform parent)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            }
        }

        float Width()
        {
            var rect = (RectTransform)transform;
            return rect.rect.width > 1f ? rect.rect.width : rect.sizeDelta.x;
        }

        bool HasReferences()
        {
            if (toggle != null &&
                knob != null &&
                background != null &&
                onSprite != null &&
                offSprite != null &&
                onLabelCanvasGroup != null &&
                offLabelCanvasGroup != null)
            {
                return true;
            }

            Debug.LogError("ToggleSwitch: assign the toggle, knob, background, sprites, and label canvas groups.", this);
            return false;
        }

        readonly struct SpriteSwap
        {
            public readonly Image image;
            public readonly Sprite sprite;

            public SpriteSwap(Image image, Sprite sprite)
            {
                this.image = image;
                this.sprite = sprite;
            }
        }
    }
}
