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
            SetValue(nameValue, "Empty", "name");
            SetValue(specialtyValue, "", "position");
            SetValue(rarityValue, "", "rarity");
            SetValue(weaponValue, "", "weapon");
            SetValue(upkeepValue, "", "upkeep");
            SetValue(waivedValue, "", "waived");
            SetHireLabel(null);
            ShowAvatar(null, false);
            SetBorder(Color.white);
            SetBackground(Color.white);
            SetAvatarBorder(Color.white);
            if (avatar != null)
            {
                avatar.SetBorder(Color.white);
            }

            if (blockReason != null)
            {
                blockReason.text = "";
            }

            if (hireButton != null)
            {
                hireButton.interactable = false;
            }

            if (chart == null)
            {
                Debug.LogError("Recruit slot is missing its spider chart.", this);
                return;
            }

            chart.Clear();
            chart.gameObject.SetActive(false);
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
        [SerializeField] CompanyDayController company;
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
        [SerializeField] CompanyDayController company;

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
}
