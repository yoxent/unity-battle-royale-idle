using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Company
{
    public sealed class TutorialPanelView : MonoBehaviour
    {
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text bodyLabel;
        [SerializeField] GameObject scrim;
        [SerializeField] RectTransform dimRoot;
        [SerializeField] RectTransform top;
        [SerializeField] RectTransform bottom;
        [SerializeField] RectTransform left;
        [SerializeField] RectTransform right;
        [SerializeField] RectTransform card;
        [SerializeField] float holePadding = 16f;
        [SerializeField] float cardGap = 16f;

        public void Show(string title, string body, RectTransform focus = null)
        {
            if (!HasReferences())
            {
                Debug.LogError("Tutorial panel needs its labels, dim, and card.", this);
                return;
            }

            titleLabel.text = title ?? "";
            bodyLabel.text = body ?? "";
            gameObject.SetActive(true);
            ApplyFocus(focus);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void ApplyFocus(RectTransform focus)
        {
            if (focus == null || !TryHole(focus, out var hole))
            {
                scrim.SetActive(true);
                SetActive(top, false);
                SetActive(bottom, false);
                SetActive(left, false);
                SetActive(right, false);
                card.anchorMin = new Vector2(0.5f, 0.5f);
                card.anchorMax = new Vector2(0.5f, 0.5f);
                card.anchoredPosition = Vector2.zero;
                return;
            }

            var area = dimRoot.rect;
            var nx0 = Mathf.Clamp01((hole.xMin - area.xMin) / area.width);
            var ny0 = Mathf.Clamp01((hole.yMin - area.yMin) / area.height);
            var nx1 = Mathf.Clamp01((hole.xMax - area.xMin) / area.width);
            var ny1 = Mathf.Clamp01((hole.yMax - area.yMin) / area.height);
            scrim.SetActive(false);
            Place(top, 0f, ny1, 1f, 1f);
            Place(bottom, 0f, 0f, 1f, ny0);
            Place(left, 0f, ny0, nx0, ny1);
            Place(right, nx1, ny0, 1f, ny1);
            PlaceCard(hole, area);
        }

        bool TryHole(RectTransform focus, out Rect hole)
        {
            hole = default;
            var area = dimRoot.rect;
            if (area.width <= 0f || area.height <= 0f)
            {
                return false;
            }

            var canvas = dimRoot.GetComponentInParent<Canvas>();
            Camera camera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                camera = canvas.worldCamera;
            }

            var corners = new Vector3[4];
            focus.GetWorldCorners(corners);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dimRoot,
                    RectTransformUtility.WorldToScreenPoint(camera, corners[0]),
                    camera,
                    out var min) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dimRoot,
                    RectTransformUtility.WorldToScreenPoint(camera, corners[2]),
                    camera,
                    out var max))
            {
                return false;
            }

            var pad = new Vector2(holePadding, holePadding);
            min -= pad;
            max += pad;
            hole = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return hole.width > 0f && hole.height > 0f;
        }

        void PlaceCard(Rect hole, Rect area)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(card);
            var size = card.rect.size;
            var half = size * 0.5f;
            var x = Mathf.Clamp(hole.center.x, area.xMin + half.x, area.xMax - half.x);
            var above = hole.yMax + cardGap + half.y;
            var below = hole.yMin - cardGap - half.y;
            var y = above + half.y <= area.yMax ? above : below;
            y = Mathf.Clamp(y, area.yMin + half.y, area.yMax - half.y);
            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = new Vector2(x, y);
        }

        static void Place(RectTransform strip, float xMin, float yMin, float xMax, float yMax)
        {
            strip.anchorMin = new Vector2(xMin, yMin);
            strip.anchorMax = new Vector2(xMax, yMax);
            strip.offsetMin = Vector2.zero;
            strip.offsetMax = Vector2.zero;
            strip.gameObject.SetActive(true);
        }

        static void SetActive(RectTransform strip, bool visible)
        {
            strip.gameObject.SetActive(visible);
        }

        bool HasReferences()
        {
            return titleLabel != null &&
                bodyLabel != null &&
                scrim != null &&
                dimRoot != null &&
                top != null &&
                bottom != null &&
                left != null &&
                right != null &&
                card != null;
        }
    }
}
