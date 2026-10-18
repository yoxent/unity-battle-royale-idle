using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace BattleRoyale.Company
{
    public sealed class CompanyDashboardMotion : MonoBehaviour
    {
        [SerializeField] RectTransform emblem;
        [SerializeField] CanvasGroup[] chips;
        [SerializeField] CanvasGroup[] cards;
        [SerializeField] float emblemDuration = 0.35f;
        [SerializeField] float itemDuration = 0.28f;
        [SerializeField] float stagger = 0.04f;

        readonly CompositeMotionHandle motions = new CompositeMotionHandle();

        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (emblem == null)
            {
                Debug.LogError("Company dashboard motion needs its emblem.", this);
                return;
            }

            Play();
        }

        void OnDisable()
        {
            motions.Cancel();
            if (emblem != null)
            {
                emblem.localScale = Vector3.one;
            }

            SetAlpha(chips, 1f);
            SetAlpha(cards, 1f);
            SetScale(cards, 1f);
        }

        void Play()
        {
            motions.Cancel();
            emblem.localScale = new Vector3(0.92f, 0.92f, 1f);
            motions.Add(LMotion.Create(0.92f, 1f, emblemDuration)
                .WithEase(Ease.OutBack)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(emblem, static (value, rect) =>
                {
                    if (rect != null)
                    {
                        rect.localScale = new Vector3(value, value, 1f);
                    }
                }));

            FadeGroup(chips, 0f);
            FadeGroup(cards, stagger * (chips == null ? 0 : chips.Length));
        }

        void FadeGroup(CanvasGroup[] groups, float startDelay)
        {
            if (groups == null)
            {
                return;
            }

            for (var i = 0; i < groups.Length; i++)
            {
                var group = groups[i];
                if (group == null)
                {
                    Debug.LogError("Company dashboard motion is missing a group.", this);
                    continue;
                }

                var delay = startDelay + stagger * i;
                group.alpha = 0f;
                motions.Add(LMotion.Create(0f, 1f, itemDuration)
                    .WithDelay(delay)
                    .WithEase(Ease.OutCubic)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .BindToAlpha(group));

                var rect = group.transform as RectTransform;
                if (rect == null)
                {
                    continue;
                }

                rect.localScale = new Vector3(0.96f, 0.96f, 1f);
                motions.Add(LMotion.Create(0.96f, 1f, itemDuration)
                    .WithDelay(delay)
                    .WithEase(Ease.OutCubic)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(rect, static (value, target) =>
                    {
                        if (target != null)
                        {
                            target.localScale = new Vector3(value, value, 1f);
                        }
                    }));
            }
        }

        static void SetAlpha(CanvasGroup[] groups, float alpha)
        {
            if (groups == null)
            {
                return;
            }

            for (var i = 0; i < groups.Length; i++)
            {
                if (groups[i] != null)
                {
                    groups[i].alpha = alpha;
                }
            }
        }

        static void SetScale(CanvasGroup[] groups, float scale)
        {
            if (groups == null)
            {
                return;
            }

            for (var i = 0; i < groups.Length; i++)
            {
                if (groups[i] != null)
                {
                    groups[i].transform.localScale = new Vector3(scale, scale, 1f);
                }
            }
        }
    }
}
