using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Company
{
    public sealed class RecruitCardView : MonoBehaviour
    {
        [SerializeField] TMP_Text body;
        [SerializeField] TMP_Text blockReason;
        [SerializeField] Button hireButton;

        public Button HireButton => hireButton;

        public void ShowEmpty()
        {
            if (body != null)
            {
                body.text = "Empty";
            }

            if (blockReason != null)
            {
                blockReason.text = "";
            }

            if (hireButton != null)
            {
                hireButton.interactable = false;
            }
        }

        public void Show(string text, string block, bool canHire)
        {
            if (body != null)
            {
                body.text = text;
            }

            if (blockReason != null)
            {
                blockReason.text = block ?? "";
            }

            if (hireButton != null)
            {
                hireButton.interactable = canHire;
            }
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
