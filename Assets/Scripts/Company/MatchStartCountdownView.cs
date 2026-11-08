using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Company
{
    /// <summary>
    /// The match-start countdown drawn over the map preview. The session owns the
    /// entry window and passes the time left, so this view paints the band, the
    /// number, and the draining bar without calculating an entry rule.
    /// </summary>
    public sealed class MatchStartCountdownView : MonoBehaviour
    {
        [Tooltip("The band that hides outside the entry window.")]
        [SerializeField] GameObject band;
        [SerializeField] TMP_Text secondsLabel;
        [Tooltip("Filled image that drains from full to empty across the window.")]
        [SerializeField] Image fill;

        [Header("Feedback")]
        [Min(0f)]
        [SerializeField] float pulseDuration = 0.16f;
        [Min(1f)]
        [SerializeField] float pulseScale = 1.1f;

        MotionHandle pulse;
        Vector3 labelScale = Vector3.one;
        bool configured;
        bool shown;
        int shownSecond = -1;
        float shownFill = -1f;

        public bool IsShown => shown;

        void Awake()
        {
            if (secondsLabel != null)
            {
                labelScale = secondsLabel.rectTransform.localScale;
            }
        }

        void OnEnable()
        {
            CancelPulse();
            configured = band != null && secondsLabel != null && fill != null;
            shown = false;
            shownSecond = -1;
            shownFill = -1f;
            SetBandVisible(false);

            if (!configured)
            {
                Debug.LogError(
                    "Match start countdown needs its band, seconds label, and fill.",
                    this);
            }
        }

        void OnDisable()
        {
            CancelPulse();
            if (secondsLabel != null)
            {
                secondsLabel.rectTransform.localScale = labelScale;
            }
        }

        /// <summary>
        /// Paints the countdown. Call it each frame the entry window runs; it
        /// writes only the values that changed.
        /// </summary>
        public void Show(float remaining, float window)
        {
            if (!configured)
            {
                return;
            }

            SetShown(true);

            var second = Mathf.Max(0, Mathf.CeilToInt(remaining));
            if (second != shownSecond)
            {
                var popping = shownSecond >= 0;
                shownSecond = second;
                secondsLabel.text = second.ToString();
                if (popping)
                {
                    Pulse();
                }
            }

            var amount = window <= 0f ? 0f : Mathf.Clamp01(remaining / window);
            if (!Mathf.Approximately(shownFill, amount))
            {
                shownFill = amount;
                fill.fillAmount = amount;
            }
        }

        public void Hide()
        {
            if (!configured || !shown)
            {
                return;
            }

            SetShown(false);
            CancelPulse();
            shownSecond = -1;
            shownFill = -1f;
            fill.fillAmount = 1f;
        }

        void SetShown(bool visible)
        {
            shown = visible;
            if (band != null && band.activeSelf != visible)
            {
                band.SetActive(visible);
            }
        }

        /// <summary>
        /// A short scale pop when the number changes, so the last seconds read at
        /// a glance. Scale does not move the layout, so it is safe over the map.
        /// </summary>
        void Pulse()
        {
            if (pulseDuration <= 0f || pulseScale <= 1f)
            {
                return;
            }

            CancelPulse();
            pulse = LMotion.Create(pulseScale, 1f, pulseDuration)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(value =>
                {
                    if (secondsLabel != null)
                    {
                        secondsLabel.rectTransform.localScale = labelScale * value;
                    }
                });
        }

        void SetBandVisible(bool visible)
        {
            SetShown(visible);
        }

        void CancelPulse()
        {
            if (pulse.IsActive())
            {
                pulse.Cancel();
            }

            pulse = default;
        }
    }
}
