using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BattleRoyale.Company
{
    public sealed class DeploymentPanel : MonoBehaviour
    {
        [SerializeField] DeploymentChoice[] slots;
        [SerializeField] DeploymentChoice[] agents;
        [SerializeField] Button autoFillButton;
        [SerializeField] CompanySessionController company;

        UnityAction[] clearActions;
        UnityAction[] toggleActions;
        bool subscribed;

        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!HasReferences())
            {
                Debug.LogError(
                    "Deployment panel needs its slots, agents, and auto fill.",
                    this);
                return;
            }

            if (clearActions == null || clearActions.Length != slots.Length)
            {
                clearActions = new UnityAction[slots.Length];
            }

            if (toggleActions == null || toggleActions.Length != agents.Length)
            {
                toggleActions = new UnityAction[agents.Length];
            }

            for (var i = 0; i < slots.Length; i++)
            {
                var slot = i;
                clearActions[i] = () => Clear(slot);
                slots[i].Button.onClick.AddListener(clearActions[i]);
            }

            for (var i = 0; i < agents.Length; i++)
            {
                var agent = i;
                toggleActions[i] = () => Toggle(agent);
                agents[i].Button.onClick.AddListener(toggleActions[i]);
            }

            autoFillButton.onClick.AddListener(Fill);
            subscribed = true;
        }

        void OnDisable()
        {
            if (!subscribed)
            {
                return;
            }

            RemoveListeners(slots, clearActions);
            RemoveListeners(agents, toggleActions);
            if (autoFillButton != null)
            {
                autoFillButton.onClick.RemoveListener(Fill);
            }

            subscribed = false;
        }

        public void ShowLineup(string[] slotText, string[] agentText, bool[] chosen)
        {
            if (!HasReferences())
            {
                Debug.LogError(
                    "Deployment panel needs its slots, agents, and auto fill.",
                    this);
                return;
            }

            var slotCount = slotText == null ? 0 : slotText.Length;
            if (slotCount > slots.Length)
            {
                Debug.LogError(
                    "Deployment panel does not have a slot for every agent the player must field.",
                    this);
            }

            for (var i = 0; i < slots.Length; i++)
            {
                if (i < slotCount)
                {
                    slots[i].Show(slotText[i]);
                }
                else
                {
                    slots[i].Hide();
                }
            }

            var agentCount = agentText == null ? 0 : agentText.Length;
            if (agentCount > agents.Length)
            {
                Debug.LogError(
                    "Deployment panel does not have a row for every agent.",
                    this);
            }

            for (var i = 0; i < agents.Length; i++)
            {
                if (i >= agentCount)
                {
                    agents[i].Hide();
                    continue;
                }

                var mark = chosen != null && i < chosen.Length && chosen[i]
                    ? "  · chosen"
                    : "";
                agents[i].Show(agentText[i] + mark);
            }
        }

        void Clear(int slot)
        {
            if (company == null)
            {
                Debug.LogError("Deployment panel needs the company session.", this);
                return;
            }

            company.ClearDeploymentSlot(slot);
        }

        void Toggle(int agent)
        {
            if (company == null)
            {
                Debug.LogError("Deployment panel needs the company session.", this);
                return;
            }

            company.ToggleDeploymentAgent(agent);
        }

        void Fill()
        {
            if (company == null)
            {
                Debug.LogError("Deployment panel needs the company session.", this);
                return;
            }

            company.AutoFillDeployment();
        }

        static void RemoveListeners(DeploymentChoice[] rows, UnityAction[] actions)
        {
            if (rows == null || actions == null)
            {
                return;
            }

            for (var i = 0; i < rows.Length; i++)
            {
                if (rows[i] != null &&
                    rows[i].Button != null &&
                    i < actions.Length &&
                    actions[i] != null)
                {
                    rows[i].Button.onClick.RemoveListener(actions[i]);
                    actions[i] = null;
                }
            }
        }

        bool HasReferences()
        {
            return RowsReady(slots) && RowsReady(agents) && autoFillButton != null;
        }

        static bool RowsReady(DeploymentChoice[] rows)
        {
            if (rows == null || rows.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null || rows[i].Button == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
