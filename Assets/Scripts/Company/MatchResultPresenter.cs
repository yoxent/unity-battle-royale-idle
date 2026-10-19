using System;
using BattleRoyale.GameModes;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Company
{
    public sealed class MatchResultPresenter : MonoBehaviour
    {
        [SerializeField] MatchPreviewStatView stats;
        [SerializeField] Button closeButton;
        [SerializeField] CanvasGroup[] lines;
        [SerializeField] TMP_Text[] lineLabels;
        [SerializeField] float lineDuration = 0.35f;
        [SerializeField] float lineGap = 0.12f;

        MotionHandle lineMotion;
        Action onClosed;
        bool listening;

        void Awake()
        {
            if (!HasReferences())
            {
                Debug.LogError(
                    "Match result presenter needs the stat view, close button, and three lines.",
                    this);
                return;
            }

            closeButton.gameObject.SetActive(false);
            HideLines();
        }

        void OnEnable()
        {
            if (!HasReferences() || listening)
            {
                return;
            }

            closeButton.onClick.AddListener(Close);
            listening = true;
        }

        void OnDisable()
        {
            if (onClosed != null)
            {
                CancelLine();
                onClosed = null;
            }

            if (!listening)
            {
                return;
            }

            closeButton.onClick.RemoveListener(Close);
            listening = false;
        }

        public void Begin(string placement, string prize, string rank, Action closed)
        {
            if (!HasReferences())
            {
                Debug.LogError(
                    "Match result presenter needs the stat view, close button, and three lines.",
                    this);
                return;
            }

            CancelLine();
            onClosed = closed;
            lineLabels[0].text = placement;
            lineLabels[1].text = prize;
            lineLabels[2].text = rank;
            HideLines();
            stats.HoldFullTable();
            closeButton.gameObject.SetActive(true);
        }

        public void Release()
        {
            onClosed = null;
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(false);
            }

            HideLines();
            if (stats != null)
            {
                stats.ReleaseFullTable();
            }
        }

        void Close()
        {
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(false);
            }

            PlayLine(0);
        }

        void PlayLine(int index)
        {
            if (lines == null || index >= lines.Length)
            {
                var closed = onClosed;
                onClosed = null;
                closed?.Invoke();
                return;
            }

            var group = lines[index];
            if (group == null)
            {
                Debug.LogError("Match result presenter is missing a line.", this);
                return;
            }

            group.gameObject.SetActive(true);
            group.alpha = 0f;
            lineMotion = LMotion.Create(0f, 1f, lineDuration)
                .WithDelay(index == 0 ? 0f : lineGap)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(() => PlayLine(index + 1))
                .BindToAlpha(group);
        }

        void HideLines()
        {
            if (lines == null)
            {
                return;
            }

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i] == null)
                {
                    continue;
                }

                lines[i].alpha = 0f;
                lines[i].gameObject.SetActive(false);
            }
        }

        bool HasReferences()
        {
            return stats != null &&
                closeButton != null &&
                lines != null &&
                lineLabels != null &&
                lines.Length == 3 &&
                lineLabels.Length == 3 &&
                lines[0] != null &&
                lines[1] != null &&
                lines[2] != null &&
                lineLabels[0] != null &&
                lineLabels[1] != null &&
                lineLabels[2] != null;
        }

        void CancelLine()
        {
            if (lineMotion.IsActive())
            {
                lineMotion.Cancel();
            }

            lineMotion = default;
        }
    }
}
