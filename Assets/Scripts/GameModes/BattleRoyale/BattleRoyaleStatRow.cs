using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.GameModes.BattleRoyale
{
    public sealed class BattleRoyaleStatRow : MonoBehaviour
    {
        [SerializeField] Image aliveDot;
        [SerializeField] Image deadDot;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] CanvasGroup rowGroup;

        public void Show(string agentName, Color teamColor, bool alive)
        {
            if (nameLabel != null)
            {
                nameLabel.text = agentName;
            }

            if (aliveDot != null)
            {
                aliveDot.gameObject.SetActive(alive);
                aliveDot.color = teamColor;
            }

            if (deadDot != null)
            {
                deadDot.gameObject.SetActive(!alive);
            }

            if (rowGroup != null)
            {
                rowGroup.alpha = alive ? 1f : 0.55f;
            }
        }
    }
}
