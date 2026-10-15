using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace BattleRoyale.UI
{
    /// <summary>
    /// Fades one page out and the next page in. Input stays off until the fade finishes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIPageTransition : MonoBehaviour
    {
        [SerializeField] UIPage[] pages;

        [Min(0.01f)]
        [SerializeField] float duration = 0.2f;

        [SerializeField] Ease ease = Ease.OutCubic;

        readonly CompositeMotionHandle motion = new CompositeMotionHandle();

        UIPage current;
        int version;

        void OnDisable()
        {
            version++;
            motion.Cancel();
        }

        public void Show(UIPage next)
        {
            if (!Ready(next))
            {
                return;
            }

            if (next == current && !Fading() && next.IsFullyShown)
            {
                HideStrays(next, null);
                return;
            }

            var token = ++version;
            motion.Cancel();

            var outgoing = OtherVisible(next);
            HideStrays(next, outgoing);
            current = next;
            next.SetInteractive(false);

            if (!next.gameObject.activeSelf)
            {
                next.PrepareFadeIn();
            }

            if (outgoing == null && next.Alpha >= 1f)
            {
                next.ShowImmediate();
                return;
            }

            if (outgoing != null)
            {
                outgoing.SetInteractive(false);
                Fade(outgoing, 0f, token, outgoing.HideImmediate);
            }

            Fade(next, 1f, token, next.ShowImmediate);
        }

        public void Hide()
        {
            if (pages == null)
            {
                Debug.LogError("UIPageTransition needs its pages.", this);
                return;
            }

            var token = ++version;
            motion.Cancel();

            var outgoing = current != null && current.gameObject.activeSelf
                ? current
                : OtherVisible(null);
            current = null;
            HideStrays(null, outgoing);

            if (outgoing == null)
            {
                return;
            }

            outgoing.SetInteractive(false);
            if (outgoing.Alpha <= 0f)
            {
                outgoing.HideImmediate();
                return;
            }

            Fade(outgoing, 0f, token, outgoing.HideImmediate);
        }

        bool Fading()
        {
            foreach (var handle in motion)
            {
                if (handle.IsActive())
                {
                    return true;
                }
            }

            return false;
        }

        void Fade(UIPage page, float target, int token, System.Action completed)
        {
            motion.Add(LMotion.Create(page.Alpha, target, duration)
                .WithEase(ease)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(() =>
                {
                    if (token != version)
                    {
                        return;
                    }

                    completed?.Invoke();
                })
                .BindToAlpha(page.Group));
        }

        UIPage OtherVisible(UIPage except)
        {
            UIPage visible = null;
            for (var i = 0; i < pages.Length; i++)
            {
                var page = pages[i];
                if (page == null || page == except || !page.gameObject.activeSelf || page.Alpha <= 0f)
                {
                    continue;
                }

                visible = page;
            }

            return visible;
        }

        void HideStrays(UIPage keep, UIPage alsoKeep)
        {
            for (var i = 0; i < pages.Length; i++)
            {
                var page = pages[i];
                if (page == null || page == keep || page == alsoKeep)
                {
                    continue;
                }

                page.HideImmediate();
            }
        }

        bool Ready(UIPage next)
        {
            if (pages == null || pages.Length == 0)
            {
                Debug.LogError("UIPageTransition needs its pages.", this);
                return false;
            }

            if (next == null)
            {
                Debug.LogError("UIPageTransition needs a page to show.", this);
                return false;
            }

            if (!next.HasGroup())
            {
                return false;
            }

            var found = false;
            for (var i = 0; i < pages.Length; i++)
            {
                var page = pages[i];
                if (page == null)
                {
                    Debug.LogError("UIPageTransition is missing a page.", this);
                    return false;
                }

                if (!page.HasGroup())
                {
                    return false;
                }

                if (page == next)
                {
                    found = true;
                }
            }

            if (found)
            {
                return true;
            }

            Debug.LogError("UIPageTransition can only show one of its pages.", this);
            return false;
        }

        void OnValidate()
        {
            duration = Mathf.Max(0.01f, duration);
        }
    }
}
