using System;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.CameraSystems
{
    [DisallowMultipleComponent]
    public sealed class WatchFade : MonoBehaviour
    {
        [Tooltip("Screen-space group that covers the cut between watched maps. Resting alpha is 0.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Tooltip("Full-screen cover under the canvas group.")]
        [SerializeField] private Image cover;

        [Min(0.01f)]
        [SerializeField] private float fadeDuration = 0.35f;

        [SerializeField] private Ease fadeEase = Ease.OutCubic;

        private MotionHandle fade;
        private Action onCovered;

        public float FadeDuration
        {
            get => fadeDuration;
            set => fadeDuration = Mathf.Max(0.01f, value);
        }

        private void OnDisable()
        {
            var wasFading = fade.IsActive();
            Cancel();
            if (wasFading && canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        // Fades the cover to opaque, then runs onCovered. Returns false when the fade cannot start.
        public bool Cover(Action onCovered)
        {
            if (!HasReferences())
            {
                return false;
            }

            Cancel();
            this.onCovered = onCovered;
            fade = LMotion.Create(canvasGroup.alpha, 1f, fadeDuration)
                .WithEase(fadeEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(CompleteCover)
                .BindToAlpha(canvasGroup);
            return true;
        }

        // Fades the cover back to clear.
        public void Reveal()
        {
            if (!HasReferences())
            {
                return;
            }

            Cancel();
            fade = LMotion.Create(canvasGroup.alpha, 0f, fadeDuration)
                .WithEase(fadeEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToAlpha(canvasGroup);
        }

        // Stops the fade and returns the cover to clear.
        public void Clear()
        {
            if (!HasReferences())
            {
                return;
            }

            Cancel();
            canvasGroup.alpha = 0f;
        }

        private void CompleteCover()
        {
            fade = default;
            var covered = onCovered;
            onCovered = null;
            covered?.Invoke();
        }

        private void Cancel()
        {
            onCovered = null;
            if (fade.IsActive())
            {
                fade.Cancel();
            }

            fade = default;
        }

        private bool HasReferences()
        {
            if (canvasGroup != null && cover != null)
            {
                return true;
            }

            Debug.LogError($"{nameof(WatchFade)} needs its canvas group and cover.", this);
            return false;
        }

        private void OnValidate()
        {
            fadeDuration = Mathf.Max(0.01f, fadeDuration);
        }
    }
}
