using System;
using BattleRoyale.Agents;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.GameModes.Deathmatch
{
    public sealed class DeathmatchStatRow : MonoBehaviour
    {
        [SerializeField] Image aliveDot;
        [SerializeField] Image deadDot;
        [SerializeField] TMP_Text repawnTimerLabel;
        [SerializeField] GameObject portrait;
        [SerializeField] AgentAvatarView avatar;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text summaryLabel;
        [SerializeField] TMP_Text killsLabel;
        [SerializeField] TMP_Text deathsLabel;
        [SerializeField] TMP_Text assistsLabel;
        [SerializeField] TMP_Text damageLabel;
        [SerializeField] TMP_Text accuracyLabel;
        [SerializeField] CanvasGroup rowGroup;

        bool avatarRolled;

        public void Show(
            string agentName,
            Color teamColor,
            bool alive,
            int kills,
            int deaths,
            int assists,
            int damage,
            int accuracy,
            bool breakdown,
            int respawnSeconds)
        {
            if (portrait != null)
            {
                portrait.SetActive(breakdown);
            }

            if (breakdown && avatar != null && !avatarRolled)
            {
                avatarRolled = true;
                avatar.TryShowRandom(new System.Random(Guid.NewGuid().GetHashCode()));
            }

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

            if (repawnTimerLabel != null)
            {
                var showTimer = !alive && respawnSeconds > 0;
                repawnTimerLabel.gameObject.SetActive(showTimer);
                if (showTimer)
                {
                    repawnTimerLabel.text = respawnSeconds.ToString();
                }
            }

            if (summaryLabel != null)
            {
                summaryLabel.gameObject.SetActive(!breakdown);
                if (!breakdown)
                {
                    summaryLabel.text = kills + "/" + deaths + "/" + assists;
                }
            }

            SetNumber(killsLabel, kills, breakdown);
            SetNumber(deathsLabel, deaths, breakdown);
            SetNumber(assistsLabel, assists, breakdown);
            SetNumber(damageLabel, damage, breakdown);
            SetNumber(accuracyLabel, accuracy, breakdown);
            if (accuracyLabel != null && breakdown)
            {
                accuracyLabel.text = accuracy + "%";
            }

            if (rowGroup != null)
            {
                rowGroup.alpha = alive ? 1f : 0.55f;
            }
        }

        static void SetNumber(TMP_Text label, int value, bool visible)
        {
            if (label == null)
            {
                return;
            }

            label.gameObject.SetActive(visible);
            if (visible)
            {
                label.text = value.ToString();
            }
        }
    }
}
