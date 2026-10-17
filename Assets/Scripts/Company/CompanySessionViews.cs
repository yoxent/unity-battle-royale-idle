using BattleRoyale.Agents;
using BattleRoyale.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Company
{
    public sealed class RecruitSlotView : MonoBehaviour
    {
        [SerializeField] TMP_Text nameValue;
        [SerializeField] TMP_Text specialtyValue;
        [SerializeField] TMP_Text rarityValue;
        [SerializeField] AgentRarityColors rarityColors;
        [SerializeField] Image border;
        [SerializeField] Image background;
        [SerializeField] TMP_Text weaponValue;
        [SerializeField] TMP_Text upkeepValue;
        [SerializeField] TMP_Text waivedValue;
        [SerializeField] TMP_Text blockReason;
        [SerializeField] UIButton hireButton;
        [SerializeField] SpiderChartGraphic chart;
        [SerializeField] AgentAvatarView avatar;
        [SerializeField] Image avatarBorder;

        public Button HireButton => hireButton;

        public void ShowEmpty()
        {
            gameObject.SetActive(false);
        }

        public void Show(
            string name,
            string specialty,
            AgentRarity rarity,
            string weapon,
            string upkeep,
            string waived,
            int contract,
            int[] rolls,
            AgentAvatarAppearance appearance,
            string block,
            bool canHire)
        {
            gameObject.SetActive(true);
            SetValue(nameValue, name, "name");
            SetValue(specialtyValue, specialty, "position");
            SetValue(rarityValue, rarity.ToString(), "rarity");
            SetValue(weaponValue, weapon, "weapon");
            SetValue(upkeepValue, upkeep, "upkeep");
            SetValue(waivedValue, waived, "waived");
            SetHireLabel(contract);
            ShowAvatar(appearance, true);
            SetRarityColor(rarity);

            if (blockReason != null)
            {
                blockReason.text = block ?? "";
            }

            if (hireButton != null)
            {
                hireButton.interactable = canHire;
            }

            if (chart == null)
            {
                Debug.LogError("Recruit slot is missing its spider chart.", this);
                return;
            }

            chart.gameObject.SetActive(true);
            chart.Show(rolls);
        }

        void SetValue(TMP_Text label, string value, string slot)
        {
            if (label == null)
            {
                Debug.LogError("Recruit slot is missing its " + slot + " value.", this);
                return;
            }

            label.text = value ?? "";
        }

        void ShowAvatar(AgentAvatarAppearance appearance, bool required)
        {
            if (avatar == null)
            {
                Debug.LogError("Recruit slot is missing its avatar.", this);
                return;
            }

            if (appearance == null)
            {
                avatar.gameObject.SetActive(false);
                if (required)
                {
                    Debug.LogError("Recruit slot is missing an avatar appearance.", this);
                }

                return;
            }

            avatar.gameObject.SetActive(true);
            avatar.Show(appearance);
        }

        void SetRarityColor(AgentRarity rarity)
        {
            if (rarityColors == null || !rarityColors.TryGet(rarity, out var color))
            {
                Debug.LogError("Recruit slot is missing a color for " + rarity + ".", this);
                return;
            }

            if (rarityValue != null)
            {
                rarityValue.color = color;
            }

            SetBorder(color);
            SetBackground(color);
            SetAvatarBorder(color);
            if (avatar != null)
            {
                avatar.SetBorder(color);
            }
        }

        void SetAvatarBorder(Color color)
        {
            if (avatarBorder == null)
            {
                Debug.LogError("Recruit slot is missing its avatar border.", this);
                return;
            }

            avatarBorder.color = color;
        }

        void SetBorder(Color color)
        {
            if (border == null)
            {
                Debug.LogError("Recruit slot is missing its border.", this);
                return;
            }

            border.color = color;
        }

        void SetBackground(Color color)
        {
            if (background == null)
            {
                Debug.LogError("Recruit slot is missing its background.", this);
                return;
            }

            var wash = Color.Lerp(color, Color.white, 0.35f);
            wash.a = 0.2f;
            background.color = wash;
        }

        void SetHireLabel(int? contract)
        {
            if (hireButton == null)
            {
                Debug.LogError("Recruit slot is missing its hire button.", this);
                return;
            }

            hireButton.SetLabel(contract.HasValue ? "Hire · " + contract.Value + "g" : "Hire");
        }
    }

    public sealed class RosterRowView : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] Button releaseButton;

        public Button ReleaseButton => releaseButton;

        public void Show(string text, bool canRelease)
        {
            gameObject.SetActive(true);
            if (label != null)
            {
                label.text = text;
            }

            if (releaseButton != null)
            {
                releaseButton.interactable = canRelease;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }

    public sealed class CompanyRosterView : MonoBehaviour
    {
        [SerializeField] CompanySessionController company;
        [SerializeField] RosterRowView[] rows;

        bool subscribed;

        void OnEnable()
        {
            if (company == null || rows == null || rows.Length != RecruitBoardRules.RosterCap)
            {
                Debug.LogError("The roster needs the company and eight rows.", this);
                return;
            }

            for (var i = 0; i < rows.Length; i++)
            {
                var slot = i;
                if (rows[i] == null || rows[i].ReleaseButton == null)
                {
                    Debug.LogError("The roster is missing a row.", this);
                    return;
                }

                rows[i].ReleaseButton.onClick.AddListener(() => company.ReleaseAgent(slot));
            }

            subscribed = true;
        }

        void OnDisable()
        {
            if (!subscribed)
            {
                return;
            }

            for (var i = 0; i < rows.Length; i++)
            {
                rows[i].ReleaseButton.onClick.RemoveAllListeners();
            }

            subscribed = false;
        }

        public void Show(string[] lines, bool[] canRelease)
        {
            if (rows == null)
            {
                return;
            }

            for (var i = 0; i < rows.Length; i++)
            {
                if (lines == null || i >= lines.Length)
                {
                    rows[i].Hide();
                    continue;
                }

                var allowed = canRelease != null && i < canRelease.Length && canRelease[i];
                rows[i].Show(lines[i], allowed);
            }
        }
    }

    public sealed class OfflineReportView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text body;
        [SerializeField] Button dismissButton;
        [SerializeField] CompanySessionController company;

        bool subscribed;

        void OnEnable()
        {
            if (dismissButton == null || company == null)
            {
                Debug.LogError("The offline report needs a dismiss button.", this);
                return;
            }

            dismissButton.onClick.AddListener(company.DismissOfflineReport);
            subscribed = true;
        }

        void OnDisable()
        {
            if (!subscribed)
            {
                return;
            }

            dismissButton.onClick.RemoveListener(company.DismissOfflineReport);
            subscribed = false;
        }

        public void Show(string text)
        {
            if (root != null)
            {
                root.SetActive(true);
            }

            if (body != null)
            {
                body.text = text;
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }
    }

    public sealed class TutorialHintView : MonoBehaviour
    {
        [SerializeField] GameObject recruitHint;
        [SerializeField] GameObject hireHint;
        [SerializeField] GameObject deployHint;

        public void Show(TutorialStep step)
        {
            Set(recruitHint, step == TutorialStep.Recruit);
            Set(hireHint, step == TutorialStep.Hire);
            Set(deployHint, step == TutorialStep.Deploy);
        }

        static void Set(GameObject hint, bool visible)
        {
            if (hint != null)
            {
                hint.SetActive(visible);
            }
        }
    }

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
