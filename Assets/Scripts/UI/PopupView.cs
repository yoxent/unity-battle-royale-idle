using System;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.UI
{
    public sealed class PopupView : MonoBehaviour
    {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] RectTransform card;
        [SerializeField] TMP_Text headerLabel;
        [SerializeField] TMP_Text bodyLabel;
        [SerializeField] GameObject buttonRow;
        [SerializeField] Button confirmButton;
        [SerializeField] TMP_Text confirmLabel;
        [SerializeField] Button dismissButton;
        [SerializeField] TMP_Text dismissLabel;
        [SerializeField] Button closeButton;
        [SerializeField] GameObject checkboxRow;
        [SerializeField] Toggle doNotShowAgainToggle;
        [SerializeField] TMP_Text doNotShowAgainLabel;
        [SerializeField] float fadeDuration = 0.2f;
        [SerializeField] Ease fadeEase = Ease.OutCubic;

        public event Action<PopupResult> Finished;

        MotionHandle timer;
        MotionHandle fade;
        int fadeVersion;
        bool subscribed;
        bool finishing;

        public bool DoNotShowAgain =>
            checkboxRow != null &&
            checkboxRow.activeSelf &&
            doNotShowAgainToggle != null &&
            doNotShowAgainToggle.isOn;

        void OnEnable()
        {
            if (!HasReferences())
            {
                return;
            }

            confirmButton.onClick.AddListener(Confirm);
            dismissButton.onClick.AddListener(Dismiss);
            closeButton.onClick.AddListener(CloseButton);
            subscribed = true;
        }

        void OnDisable()
        {
            CancelTimer();
            CancelFade();

            if (!subscribed)
            {
                return;
            }

            confirmButton.onClick.RemoveListener(Confirm);
            dismissButton.onClick.RemoveListener(Dismiss);
            closeButton.onClick.RemoveListener(CloseButton);
            subscribed = false;
        }

        public bool Apply(PopupContent content)
        {
            if (!HasReferences())
            {
                return false;
            }

            if (content == null)
            {
                Debug.LogError("PopupView: the popup content is missing.", this);
                return false;
            }

            if (!content.CanClose)
            {
                Debug.LogError("PopupView: a popup needs a button, the close control, or a duration.", this);
                return false;
            }

            headerLabel.text = content.Header;
            headerLabel.gameObject.SetActive(content.ShowHeader);
            bodyLabel.text = content.Body;
            bodyLabel.gameObject.SetActive(content.ShowBody);

            buttonRow.SetActive(content.ShowsButtonRow);
            confirmButton.gameObject.SetActive(content.ShowButtons && content.ShowConfirm);
            dismissButton.gameObject.SetActive(content.ShowButtons && content.ShowDismiss);
            SetButtonCaption(confirmButton, confirmLabel, content.ConfirmLabel);
            SetButtonCaption(dismissButton, dismissLabel, content.DismissLabel);

            closeButton.gameObject.SetActive(content.ShowClose);

            checkboxRow.SetActive(content.ShowDoNotShowAgain);
            doNotShowAgainLabel.text = content.DoNotShowAgainLabel;
            doNotShowAgainToggle.SetIsOnWithoutNotify(content.DoNotShowAgain);

            StartTimer(content.AutoCloseSeconds);
            return true;
        }

        public void Open()
        {
            if (!HasReferences())
            {
                return;
            }

            finishing = false;
            bool alreadyShown = gameObject.activeSelf && canvasGroup.alpha >= 1f && !fade.IsActive();
            if (!alreadyShown)
            {
                canvasGroup.alpha = 0f;
            }

            gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(card);

            if (alreadyShown)
            {
                EnableInput();
                return;
            }

            Fade(0f, 1f, EnableInput);
        }

        public void Confirm()
        {
            Finish(PopupResult.Confirmed);
        }

        public void Dismiss()
        {
            Finish(PopupResult.Dismissed);
        }

        public void CloseButton()
        {
            Finish(PopupResult.Closed);
        }

        void Finish(PopupResult result)
        {
            if (finishing)
            {
                return;
            }

            finishing = true;
            CancelTimer();
            canvasGroup.interactable = false;
            Finished?.Invoke(result);
            Fade(canvasGroup.alpha, 0f, Deactivate);
        }

        void StartTimer(float seconds)
        {
            CancelTimer();

            if (seconds <= 0f)
            {
                return;
            }

            timer = LMotion.Create(0f, 1f, seconds)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(HandleTimedOut)
                .RunWithoutBinding();
        }

        void HandleTimedOut()
        {
            timer = default;
            Finish(PopupResult.TimedOut);
        }

        void Fade(float from, float to, Action completed)
        {
            CancelFade();
            int version = ++fadeVersion;

            if (fadeDuration <= 0f)
            {
                canvasGroup.alpha = to;
                completed?.Invoke();
                return;
            }

            fade = LMotion.Create(from, to, fadeDuration)
                .WithEase(fadeEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(() =>
                {
                    if (version != fadeVersion)
                    {
                        return;
                    }

                    completed?.Invoke();
                })
                .BindToAlpha(canvasGroup);
        }

        void EnableInput()
        {
            fade = default;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        void Deactivate()
        {
            fade = default;
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
            finishing = false;
        }

        void CancelTimer()
        {
            if (timer.IsActive())
            {
                timer.Cancel();
            }
        }

        void CancelFade()
        {
            fadeVersion++;

            if (fade.IsActive())
            {
                fade.Cancel();
            }
        }

        static void SetButtonCaption(Button button, TMP_Text label, string caption)
        {
            var uiButton = button as UiButton;
            if (uiButton != null)
            {
                uiButton.SetLabel(caption);
                return;
            }

            label.text = caption;
        }

        bool HasReferences()
        {
            if (canvasGroup == null || card == null
                || headerLabel == null || bodyLabel == null || buttonRow == null
                || confirmButton == null || confirmLabel == null
                || dismissButton == null || dismissLabel == null
                || closeButton == null || checkboxRow == null
                || doNotShowAgainToggle == null || doNotShowAgainLabel == null)
            {
                Debug.LogError("PopupView: assign the canvas group, card, header, body, buttons, close control, and checkbox.", this);
                return false;
            }

            return true;
        }
    }
}
