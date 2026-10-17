using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Company
{
    public sealed class DeploymentPanel : MonoBehaviour
    {
        [SerializeField] DeploymentChoice[] slots;
        [SerializeField] DeploymentChoice[] agents;
        [SerializeField] Button autoFillButton;
        [SerializeField] CompanySessionController company;

        void OnEnable()
        {
            if (!HasReferences())
            {
                Debug.LogError(
                    "Deployment panel needs its slots, agents, and auto fill.",
                    this);
                return;
            }

            for (var i = 0; i < slots.Length; i++)
            {
                var slot = i;
                slots[i].Button.onClick.AddListener(() => Clear(slot));
            }

            for (var i = 0; i < agents.Length; i++)
            {
                var agent = i;
                agents[i].Button.onClick.AddListener(() => Toggle(agent));
            }

            autoFillButton.onClick.AddListener(Fill);
        }

        void OnDisable()
        {
            ClearListeners(slots);
            ClearListeners(agents);
            if (autoFillButton != null)
            {
                autoFillButton.onClick.RemoveListener(Fill);
            }
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

        static void ClearListeners(DeploymentChoice[] rows)
        {
            if (rows == null)
            {
                return;
            }

            for (var i = 0; i < rows.Length; i++)
            {
                if (rows[i] != null && rows[i].Button != null)
                {
                    rows[i].Button.onClick.RemoveAllListeners();
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
